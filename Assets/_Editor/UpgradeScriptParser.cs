#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace CraneMachine.EditorTools
{
    // Parses the upgrade setup script into pages -> groups -> entries.
    //
    // Syntax:
    //   === Page: Title              starts a new page (always unlocked)
    //   === Page: Title > needs X    page unlocks when upgrade X is bought
    //   === Page: Title > needs X:3  page unlocks when upgrade X reaches level 3
    //   === Page: Title > needs 12 upgrades   page unlocks after 12 total purchases
    //   # Group Title                starts a group inside the current page
    //   UpgradeName                  adds a button
    //   UpgradeName > Gate           button hidden until Gate is bought
    //   UpgradeName > Gate:3         button hidden until Gate reaches level 3
    //   // comment                   ignored
    //
    // Backward compatible: a script with no "=== Page" lines produces a single implicit
    // page (Result.Groups still populated for the legacy single-page path).
    public static class UpgradeScriptParser
    {
        public class PageResult
        {
            public UpgradePageDefinition Page = new UpgradePageDefinition();
        }

        public class Result
        {
            // Legacy flat group list (single implicit page). Kept for compatibility.
            public List<UpgradeGroupDefinition> Groups = new List<UpgradeGroupDefinition>();
            // New paged structure.
            public List<UpgradePageDefinition> Pages = new List<UpgradePageDefinition>();
            public List<string> Errors = new List<string>();
            public bool Ok => Errors.Count == 0;
            public bool HasPages => Pages.Count > 0 &&
                                    !(Pages.Count == 1 && Pages[0].title == ImplicitPageTitle);
        }

        private const string ImplicitPageTitle = "Upgrades";

        private static Dictionary<string, Type> _types;

        private static void EnsureTypes()
        {
            if (_types != null) return;
            _types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(IUpgrade).IsAssignableFrom(t)
                            && !t.IsAbstract && !t.IsInterface
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .ToDictionary(t => t.Name, t => t);
        }

        public static Result Parse(string script)
        {
            EnsureTypes();
            var result = new Result();

            if (string.IsNullOrWhiteSpace(script))
            {
                result.Errors.Add("Setup script is empty.");
                return result;
            }

            UpgradePageDefinition currentPage = null;
            UpgradeGroupDefinition currentGroup = null;

            var lines = script.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string line = StripComment(lines[i]).Trim();
                if (line.Length == 0) continue;

                // ----- Page header -----
                if (line.StartsWith("==="))
                {
                    currentPage = ParsePage(line, lineNo, result);
                    if (currentPage != null) result.Pages.Add(currentPage);
                    currentGroup = null;
                    continue;
                }

                // ----- Group header -----
                if (line.StartsWith("#"))
                {
                    // Auto-create an implicit page if a group appears before any page header.
                    if (currentPage == null)
                    {
                        currentPage = new UpgradePageDefinition
                        {
                            title = ImplicitPageTitle,
                            unlockMode = PageUnlockMode.Always
                        };
                        result.Pages.Add(currentPage);
                    }

                    currentGroup = new UpgradeGroupDefinition
                    {
                        title = line.Substring(1).Trim(),
                        upgrades = new List<UpgradeEntry>()
                    };
                    currentPage.groups.Add(currentGroup);
                    continue;
                }

                // ----- Upgrade entry -----
                if (currentGroup == null)
                {
                    result.Errors.Add($"Line {lineNo}: upgrade '{line}' appears before any '# Group' header.");
                    continue;
                }

                var entry = ParseEntry(line, lineNo, result);
                if (entry != null) currentGroup.upgrades.Add(entry);
            }

            if (result.Pages.Count == 0)
                result.Errors.Add("No content. Start a group with '# Title' (or a page with '=== Page: Title').");

            // Populate the legacy flat group list from the first page for compatibility.
            if (result.Pages.Count > 0)
                result.Groups = result.Pages[0].groups;

            return result;
        }

        private static UpgradePageDefinition ParsePage(string line, int lineNo, Result result)
        {
            // Strip leading '=' run.
            string body = line.TrimStart('=').Trim();

            // Optional "Page:" prefix.
            if (body.StartsWith("Page:", StringComparison.OrdinalIgnoreCase))
                body = body.Substring("Page:".Length).Trim();

            string titlePart = body;
            string gatePart = null;

            int arrow = body.IndexOf('>');
            if (arrow >= 0)
            {
                titlePart = body.Substring(0, arrow).Trim();
                gatePart = body.Substring(arrow + 1).Trim();
            }

            var page = new UpgradePageDefinition
            {
                title = string.IsNullOrEmpty(titlePart) ? "Page" : titlePart,
                unlockMode = PageUnlockMode.Always,
                requiredLevel = 1,
                requiredUpgradeCount = 1
            };

            if (string.IsNullOrEmpty(gatePart)) return page;

            // Expect: "needs <rule>"
            if (gatePart.StartsWith("needs", StringComparison.OrdinalIgnoreCase))
                gatePart = gatePart.Substring("needs".Length).Trim();

            // Count rule: "<n> upgrades"
            var tokens = gatePart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2 &&
                int.TryParse(tokens[0], out int count) &&
                tokens[1].StartsWith("upgrade", StringComparison.OrdinalIgnoreCase))
            {
                if (count < 1)
                {
                    result.Errors.Add($"Line {lineNo}: upgrade count must be 1 or higher.");
                    return null;
                }
                page.unlockMode = PageUnlockMode.UpgradeCount;
                page.requiredUpgradeCount = count;
                return page;
            }

            // Specific-upgrade rule: "<UpgradeName>" or "<UpgradeName>:<level>"
            string gateName = gatePart;
            int level = 1;
            int colon = gatePart.IndexOf(':');
            if (colon >= 0)
            {
                gateName = gatePart.Substring(0, colon).Trim();
                string levelText = gatePart.Substring(colon + 1).Trim();
                if (!int.TryParse(levelText, out level) || level < 1)
                {
                    result.Errors.Add($"Line {lineNo}: '{levelText}' is not a valid level (must be 1 or higher).");
                    return null;
                }
            }

            var gate = Resolve(gateName, lineNo, result);
            if (gate == null) return null;

            page.unlockMode = PageUnlockMode.SpecificUpgrade;
            page.unlockedBy = gate;
            page.requiredLevel = level;
            return page;
        }

        private static UpgradeEntry ParseEntry(string line, int lineNo, Result result)
        {
            string upgradePart = line;
            string gatePart = null;

            int arrow = line.IndexOf('>');
            if (arrow >= 0)
            {
                upgradePart = line.Substring(0, arrow).Trim();
                gatePart = line.Substring(arrow + 1).Trim();
            }

            var upgrade = Resolve(upgradePart, lineNo, result);
            if (upgrade == null) return null;

            var entry = new UpgradeEntry { upgrade = upgrade, requiredLevel = 1 };

            if (!string.IsNullOrEmpty(gatePart))
            {
                string gateName = gatePart;
                int level = 1;

                int colon = gatePart.IndexOf(':');
                if (colon >= 0)
                {
                    gateName = gatePart.Substring(0, colon).Trim();
                    string levelText = gatePart.Substring(colon + 1).Trim();
                    if (!int.TryParse(levelText, out level) || level < 1)
                    {
                        result.Errors.Add($"Line {lineNo}: '{levelText}' is not a valid level (must be 1 or higher).");
                        return null;
                    }
                }

                var gate = Resolve(gateName, lineNo, result);
                if (gate == null) return null;

                entry.unlockedBy = gate;
                entry.requiredLevel = level;
            }

            return entry;
        }

        private static IUpgrade Resolve(string name, int lineNo, Result result)
        {
            if (string.IsNullOrEmpty(name))
            {
                result.Errors.Add($"Line {lineNo}: missing upgrade name.");
                return null;
            }

            if (!_types.TryGetValue(name, out var type))
            {
                var suggestion = _types.Keys
                    .FirstOrDefault(k => k.StartsWith(name, StringComparison.OrdinalIgnoreCase));
                string hint = suggestion != null ? $" Did you mean '{suggestion}'?" : "";
                result.Errors.Add($"Line {lineNo}: unknown upgrade '{name}'.{hint}");
                return null;
            }

            return (IUpgrade)Activator.CreateInstance(type);
        }

        private static string StripComment(string line)
        {
            int idx = line.IndexOf("//", StringComparison.Ordinal);
            return idx >= 0 ? line.Substring(0, idx) : line;
        }

        public static string[] KnownUpgradeNames()
        {
            EnsureTypes();
            return _types.Keys.OrderBy(k => k).ToArray();
        }
    }
}
#endif
