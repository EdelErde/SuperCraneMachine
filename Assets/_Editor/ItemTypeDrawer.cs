#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraneMachine.EditorTools
{
    [CustomPropertyDrawer(typeof(ItemType), true)]
    public class ItemTypeDrawer : PropertyDrawer
    {
        private static Type[] _types;
        private static string[] _names;

        private static void EnsureTypes()
        {
            if (_types != null) return;
            _types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(ItemType).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();
            _names = _types.Select(t => t.Name).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureTypes();

            string current = string.IsNullOrEmpty(property.managedReferenceFullTypename)
                ? ""
                : property.managedReferenceFullTypename.Split(' ').Last().Split('.').Last();

            int index = Array.IndexOf(_names, current);

            var dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int newIndex = EditorGUI.Popup(dropdownRect, label.text, index, _names);

            if (newIndex != index && newIndex >= 0)
            {
                property.managedReferenceValue = Activator.CreateInstance(_types[newIndex]);
                property.serializedObject.ApplyModifiedProperties();
            }

            // draw any serialized fields the chosen type exposes
            EditorGUI.indentLevel++;
            var child = property.Copy();
            var end = child.GetEndProperty();
            bool enter = true;
            float y = position.y + EditorGUIUtility.singleLineHeight + 2f;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                float h = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                y += h + 2f;
            }
            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight + 2f;
            var child = property.Copy();
            var end = child.GetEndProperty();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                h += EditorGUI.GetPropertyHeight(child, true) + 2f;
            }
            return h;
        }
    }
}
#endif