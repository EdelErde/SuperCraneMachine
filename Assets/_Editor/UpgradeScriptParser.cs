#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace CraneMachine.EditorTools
{
    public static class UpgradeScriptParser
    {
        public class Result
        {
            public List<UpgradeGroupDefinition> Groups = new List<UpgradeGroupDefinition>();
            public List<string> Errors = new List<string>();
            public bool Ok => Errors.Count == 0;
        }

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

            UpgradeGroupDefinition current = null;
            var lines = script.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                int lineNo = i + 1;

                string line = StripComment(raw).Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("#"))
                {
                    current = new UpgradeGroupDefinition
                    {
                        title = line.Substring(1).Trim(),
                        upgrades = new List<UpgradeEntry>()
                    };
                    result.Groups.Add(current);
                    continue;
                }

                if (current == null)
                {
                    result.Errors.Add($"Line {lineNo}: upgrade '{line}' appears before any '# Group' header.");
                    continue;
                }

                var entry = ParseEntry(line, lineNo, result);
                if (entry != null) current.upgrades.Add(entry);
            }

            if (result.Groups.Count == 0)
                result.Errors.Add("No groups defined. Start a group with '# Title'.");

            return result;
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