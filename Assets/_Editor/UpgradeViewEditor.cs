#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CraneMachine.EditorTools
{
    [CustomEditor(typeof(UpgradeView))]
    public class UpgradeViewEditor : Editor
    {
        private static string[] _errors = new string[0];
        private static bool _showHelp;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var view = (UpgradeView)target;

            EditorGUILayout.Space();

            _showHelp = EditorGUILayout.Foldout(_showHelp, "Script syntax");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "=== Page: Title                 starts a page (always unlocked)\n" +
                    "=== Page: Title > needs X       page unlocks when X is bought\n" +
                    "=== Page: Title > needs X:3      page unlocks when X reaches Lv.3\n" +
                    "=== Page: Title > needs 12 upgrades   unlocks after 12 purchases\n" +
                    "# Group Title                   starts a group inside the page\n" +
                    "UpgradeName                     adds a button\n" +
                    "UpgradeName > Gate              hidden until Gate is bought\n" +
                    "UpgradeName > Gate:3            hidden until Gate reaches level 3\n" +
                    "// comment                      ignored\n\n" +
                    "(No '=== Page' lines? Everything goes on one implicit page.)\n\n" +
                    "Known upgrades:\n  " +
                    string.Join("\n  ", UpgradeScriptParser.KnownUpgradeNames()),
                    MessageType.None);
            }

            if (GUILayout.Button("Apply Script to Pages", GUILayout.Height(24)))
                ApplyScript(view);

            foreach (var e in _errors)
                EditorGUILayout.HelpBox(e, MessageType.Error);

            EditorGUILayout.Space();

            if (view.Content == null || view.GroupPrefab == null || view.ButtonPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign Group Prefab, Button Prefab and Content before building.",
                    MessageType.Warning);
                return;
            }

            bool canPage = view.PagePrefab != null && view.TabPrefab != null && view.TabBar != null;
            if (!canPage)
            {
                EditorGUILayout.HelpBox(
                    "Assign Page Prefab, Tab Prefab and Tab Bar to build a paged window. " +
                    "Without them, a single-page (legacy) build is used.",
                    MessageType.Info);
            }

            if (GUILayout.Button("Build Upgrade UI", GUILayout.Height(30)))
                Build(view);

            if (GUILayout.Button("Apply Script + Build", GUILayout.Height(24)))
            {
                if (ApplyScript(view)) Build(view);
            }

            if (GUILayout.Button("Clear Generated UI"))
                Clear(view);
        }

        private static bool ApplyScript(UpgradeView view)
        {
            var result = UpgradeScriptParser.Parse(view.SetupScript);
            _errors = result.Errors.ToArray();

            if (!result.Ok) return false;

            Undo.RecordObject(view, "Apply Upgrade Script");

            view.Pages.Clear();
            view.Pages.AddRange(result.Pages);

            // Keep the legacy groups list mirrored to page 1 for compatibility.
            view.Groups.Clear();
            if (result.Pages.Count > 0)
                view.Groups.AddRange(result.Pages[0].groups);

            EditorUtility.SetDirty(view);
            return true;
        }

        private static void Clear(UpgradeView view)
        {
            var content = view.Content;
            for (int i = content.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(content.GetChild(i).gameObject);

            if (view.TabBar != null)
                for (int i = view.TabBar.childCount - 1; i >= 0; i--)
                    Undo.DestroyObjectImmediate(view.TabBar.GetChild(i).gameObject);

            MarkDirty(view);
        }

        private static void Build(UpgradeView view)
        {
            Clear(view);

            bool paged = view.PagePrefab != null && view.TabPrefab != null && view.TabBar != null
                         && view.Pages.Count > 0;

            if (!paged)
            {
                BuildSinglePage(view);
                MarkDirty(view);
                return;
            }

            var pageComponents = new System.Collections.Generic.List<UpgradePage>();
            var tabComponents = new System.Collections.Generic.List<UpgradePageTab>();

            for (int p = 0; p < view.Pages.Count; p++)
            {
                var pageDef = view.Pages[p];

                var page = (UpgradePage)PrefabUtility.InstantiatePrefab(view.PagePrefab, view.Content);
                Undo.RegisterCreatedObjectUndo(page.gameObject, "Build Upgrade UI");
                page.name = $"UpgradePage - {pageDef.title}";

                // Inject the unlock condition onto the runtime page.
                var so = new SerializedObject(page);
                var unlockProp = so.FindProperty("unlock");
                if (unlockProp != null)
                {
                    unlockProp.FindPropertyRelative("mode").enumValueIndex = (int)pageDef.unlockMode;
                    unlockProp.FindPropertyRelative("requiredLevel").intValue = Mathf.Max(1, pageDef.requiredLevel);
                    unlockProp.FindPropertyRelative("requiredUpgradeCount").intValue = Mathf.Max(1, pageDef.requiredUpgradeCount);
                    unlockProp.FindPropertyRelative("unlockedBy").managedReferenceValue =
                        pageDef.unlockedBy == null ? null : CloneOf(pageDef.unlockedBy);
                    so.ApplyModifiedProperties();
                }

                BuildGroupsInto(view, page.GroupParent, pageDef.groups);
                page.RefreshLockState();

                // Tab
                var tab = (UpgradePageTab)PrefabUtility.InstantiatePrefab(view.TabPrefab, view.TabBar);
                Undo.RegisterCreatedObjectUndo(tab.gameObject, "Build Upgrade UI");
                tab.name = $"PageTab - {pageDef.title}";

                pageComponents.Add(page);
                tabComponents.Add(tab);

                EditorUtility.SetDirty(page);
                EditorUtility.SetDirty(tab);
            }

            // Ensure a pager exists on the content and hand it the pages + tabs.
            var pager = view.Content.GetComponent<UpgradePager>();
            if (pager == null)
            {
                pager = Undo.AddComponent<UpgradePager>(view.Content.gameObject);
            }
            var pagerSo = new SerializedObject(pager);
            SetComponentList(pagerSo, "pages", pageComponents);
            SetComponentList(pagerSo, "tabs", tabComponents);
            pagerSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(pager);

            MarkDirty(view);
        }

        private static void BuildSinglePage(UpgradeView view)
        {
            // Legacy behaviour: build groups directly under content.
            var groups = view.Pages.Count > 0 ? view.Pages[0].groups : view.Groups;
            BuildGroupsInto(view, view.Content, groups);
        }

        private static void BuildGroupsInto(
            UpgradeView view, RectTransform parentContent,
            System.Collections.Generic.List<UpgradeGroupDefinition> groupDefs)
        {
            foreach (var groupDef in groupDefs)
            {
                var group = (UpgradeGroup)PrefabUtility.InstantiatePrefab(view.GroupPrefab, parentContent);
                Undo.RegisterCreatedObjectUndo(group.gameObject, "Build Upgrade UI");

                group.name = $"UpgradeGroup - {groupDef.title}";
                group.SetTitle(groupDef.title);

                var parent = group.ButtonParent != null ? group.ButtonParent : (RectTransform)group.transform;

                foreach (var entry in groupDef.upgrades)
                {
                    if (entry.upgrade == null) continue;

                    var button = (UpgradeButton)PrefabUtility.InstantiatePrefab(view.ButtonPrefab, parent);
                    Undo.RegisterCreatedObjectUndo(button.gameObject, "Build Upgrade UI");

                    button.name = $"UpgradeButton - {entry.upgrade.DisplayName}";

                    var so = new SerializedObject(button);
                    so.FindProperty("upgrade").managedReferenceValue = CloneOf(entry.upgrade);
                    so.FindProperty("unlockedBy").managedReferenceValue =
                        entry.unlockedBy == null ? null : CloneOf(entry.unlockedBy);
                    so.FindProperty("requiredLevel").intValue = Mathf.Max(1, entry.requiredLevel);
                    so.ApplyModifiedProperties();

                    button.ApplyStaticVisuals();
                    EditorUtility.SetDirty(button);
                }

                EditorUtility.SetDirty(group);
            }
        }

        private static void SetComponentList<T>(SerializedObject so, string prop,
            System.Collections.Generic.List<T> items) where T : Object
        {
            var list = so.FindProperty(prop);
            if (list == null) return;
            list.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static object CloneOf(IUpgrade source)
            => source == null ? null : System.Activator.CreateInstance(source.GetType());

        private static void MarkDirty(UpgradeView view)
        {
            EditorUtility.SetDirty(view);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
    }
}
#endif
