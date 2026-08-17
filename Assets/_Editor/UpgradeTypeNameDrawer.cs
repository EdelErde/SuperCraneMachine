#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CraneMachine.EditorTools
{
    // Searchable dropdown (same widget family as Unity's "Add Component" menu) for
    // picking an IUpgrade class name into a plain string field. Reflects every
    // concrete, parameterless-constructible IUpgrade type in the project — same
    // discovery logic as UpgradeReferenceDrawer, just targeting a string instead of a
    // [SerializeReference] field, since IUpgrade instances aren't stored as managed
    // references here (see UpgradePurchasedCondition's class comment).
    [CustomPropertyDrawer(typeof(UpgradeTypeNameAttribute))]
    public class UpgradeTypeNameDrawer : PropertyDrawer
    {
        private static string[] _names;

        private static void EnsureNames()
        {
            if (_names != null) return;

            _names = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(IUpgrade).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureNames();

            var fieldRect = EditorGUI.PrefixLabel(position, label);

            string current = string.IsNullOrEmpty(property.stringValue) ? "None" : property.stringValue;
            if (GUI.Button(fieldRect, current, EditorStyles.popup))
            {
                var dropdown = new UpgradeTypeNameSearchDropdown(
                    new AdvancedDropdownState(), _names, chosen =>
                    {
                        property.stringValue = chosen == "None" ? "" : chosen;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                dropdown.Show(fieldRect);
            }
        }
    }

    // The searchable list itself. AdvancedDropdown gives the search box for free —
    // typing filters the item list live, same as Unity's own component picker.
    internal class UpgradeTypeNameSearchDropdown : AdvancedDropdown
    {
        private readonly string[] _names;
        private readonly Action<string> _onChosen;

        public UpgradeTypeNameSearchDropdown(AdvancedDropdownState state, string[] names, Action<string> onChosen)
            : base(state)
        {
            _names = names;
            _onChosen = onChosen;
            minimumSize = new Vector2(minimumSize.x, 250f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Upgrade");
            root.AddChild(new AdvancedDropdownItem("None"));

            foreach (var name in _names)
                root.AddChild(new AdvancedDropdownItem(name));

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            _onChosen?.Invoke(item.name);
        }
    }
}
#endif