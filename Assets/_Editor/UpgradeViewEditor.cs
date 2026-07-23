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
                    "# Group Title        starts a new group\n" +
                    "UpgradeName          adds a button\n" +
                    "UpgradeName > Gate   hidden until Gate is bought\n" +
                    "UpgradeName > Gate:3 hidden until Gate reaches level 3\n" +
                    "// comment           ignored\n\n" +
                    "Known upgrades:\n  " +
                    string.Join("\n  ", UpgradeScriptParser.KnownUpgradeNames()),
                    MessageType.None);
            }

            if (GUILayout.Button("Apply Script to Groups", GUILayout.Height(24)))
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

            view.Groups.Clear();
            view.Groups.AddRange(result.Groups);

            EditorUtility.SetDirty(view);
            return true;
        }

        private static void Clear(UpgradeView view)
        {
            var content = view.Content;
            for (int i = content.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(content.GetChild(i).gameObject);

            MarkDirty(view);
        }

        private static void Build(UpgradeView view)
        {
            Clear(view);

            foreach (var groupDef in view.Groups)
            {
                var group = (UpgradeGroup)PrefabUtility.InstantiatePrefab(view.GroupPrefab, view.Content);
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

            MarkDirty(view);
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