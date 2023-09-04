using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace Peg.Editor
{
    /// <summary>
    /// Collection of static methods for helping with editor and inspector tools in Unity.
    /// </summary>
    public static class EditorGUIExtensions
    {
        private static readonly Dictionary<Type, Func<string, GUILayoutOption[], object, object>> _LayoutFields =
            new()
            {
                { typeof(bool), (label, option, value) => EditorGUILayout.Toggle(new GUIContent(label), (bool)value, option) },
                { typeof(int), (label, option, value) => EditorGUILayout.IntField(new GUIContent(label), (int)value, option)},
                { typeof(float), (label, option, value) => EditorGUILayout.FloatField(new GUIContent(label), (float)value, option) },
                { typeof(string), (label, option, value) => EditorGUILayout.TextField(new GUIContent(label), (string)value, option) },
                { typeof(Color), (label, option, value) => EditorGUILayout.ColorField(new GUIContent(label), (Color)value, true, true, true, option) },
                { typeof(Vector2), (label, option, value) => EditorGUILayout.Vector2Field(new GUIContent(label), (Vector2)value, option) },
                { typeof(Vector3), (label, option, value) => EditorGUILayout.Vector3Field(new GUIContent(label), (Vector3)value, option) },
                { typeof(Vector4), (label, option, value) => EditorGUILayout.Vector4Field(new GUIContent(label), (Vector4)value, option) },
                { typeof(Bounds), (label, option, value) => EditorGUILayout.BoundsField(new GUIContent(label), (Bounds)value, option) },
                { typeof(Rect), (label, option, value) => EditorGUILayout.RectField(new GUIContent(label), (Rect)value, option) },
                { typeof(Vector2Int), (label, option, value) => EditorGUILayout.Vector2IntField(new GUIContent(label), (Vector2Int)value, option) },
                { typeof(Vector3Int), (label, option, value) => EditorGUILayout.Vector3IntField(new GUIContent(label), (Vector3Int)value, option) },
                { typeof(RectInt), (label, option, value) => EditorGUILayout.RectIntField(new GUIContent(label), (RectInt)value, option) },
                { typeof(BoundsInt), (label, option, value) => EditorGUILayout.BoundsIntField(new GUIContent(label), (BoundsInt)value, option) },
                
            };

        /// <summary>
        /// A general-purpose control that will attempt to render for any valid datatype passed to it.
        /// Supports Color Vector2, Vector3, Bounds, Rects, UnityEngine.Object, and common primitives such as bools, ints, floats, strings, and enums.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static object DoFieldLayout(string label, Type type, object value, params GUILayoutOption[] options)
        {
            if (_LayoutFields.TryGetValue(type, out Func<string, GUILayoutOption[], object, object> field))
                return field(label, options, value);

            if (type.IsEnum)
                return EditorGUILayout.EnumPopup(new GUIContent(label), (Enum)value, options);

            if (type == typeof(Quaternion))
                return Quaternion.Euler(EditorGUILayout.Vector3Field(new GUIContent(label), ((Quaternion)value).eulerAngles, options));

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return EditorGUILayout.ObjectField(new GUIContent(label), value as UnityEngine.Object, type, true, options);

            return value;
        }
    }
}
