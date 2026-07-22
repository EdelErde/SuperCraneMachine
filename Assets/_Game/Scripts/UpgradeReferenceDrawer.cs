#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraneMachine.EditorTools
{
    [CustomPropertyDrawer(typeof(UpgradeReferenceAttribute))]
    public class UpgradeReferenceDrawer : PropertyDrawer
    {
        private static Type[] _types;
        private static string[] _names;

        private static void EnsureTypes()
        {
            if (_types != null) return;
            var list = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(IUpgrade).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToList();

            _types = new Type[] { null }.Concat(list).ToArray();
            _names = new[] { "None" }.Concat(list.Select(t => t.Name)).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureTypes();

            string current = string.IsNullOrEmpty(property.managedReferenceFullTypename)
                ? ""
                : property.managedReferenceFullTypename.Split(' ').Last().Split('.').Last();

            int index = Mathf.Max(0, Array.IndexOf(_names, current));

            int newIndex = EditorGUI.Popup(position, label.text, index, _names);
            if (newIndex != index)
            {
                property.managedReferenceValue =
                    _types[newIndex] == null ? null : Activator.CreateInstance(_types[newIndex]);
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif