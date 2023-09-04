/**********************************************
* Ancient Craft Games
* Copyright 2015-2017 James Clark
**********************************************/
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;


namespace Peg.ToolboxEditor
{
    /// <summary>
    /// Abstract base Editor class that can be derived from for custom editors.
    /// It has support for many custom attributes not present in Unity's default
    /// editor - such as: ShowInInspector, Inspectable, and FoldedEvent
    /// 
    /// The two biggest features are the ability to automatically group fields
    /// together within a single foldout using [FoldGroupField] and [FoldFlag]
    /// and the ability to display properties in the inspector using
    /// [ShowInInspector] or [Inspectable].
    /// 
    /// Public Service Anouncment: I never write good code when it comes to editors and tools.
    /// Please don't take this file as an example of how to do things well ;)
    /// 
    /// TODO / BUGS:
    /// 
    /// -needs to support property drawers
    /// 
    /// -all regular serialized properties need to be processed and stored into
    /// the MemberData list so that we can properly preserve the order in which elements are rendered
    /// in the inspector.
    /// 
    /// -some class properties still seem to revert to older values when recompiling code.
    /// 
    /// -need support for 'order' field in many attributes for properties. This way
    /// order of rendering for things like [PropertyHeader] and [PropertySpace] can specified.
    /// 
    /// -does not support non-public or static fold flags
    /// 
    /// -does not support grouping of properties?
    /// 
    /// -very limited support for arrays!
    /// 
    /// -no support for properties that expose arrays!
    /// 
    /// </summary>
    public static class AbstractSuperEditor
    {
        /// <summary>
        /// Displays a control that allows choosing a binding source string as well as a binding source destination.
        /// </summary>
        /// <param name="keys">The list of keys to display in a dropdown for binding sources.</param>
        /// <param name="binding">The binding datatype.</param>
        /// <param name="destType">The type that will be bound to in the destination object. A dropdown list of all fields and properties with this type will be displayed.</param>
        public static bool BindingField(string[] keys, TypeHelper.BindingMap binding, Type destType, object input, Func<string[], TypeHelper.BindingMap, object, bool> editorInjection)
        {
            //controls for selecting dest key
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(20))) return true;
            binding.KeyIndex = EditorGUILayout.Popup(binding.KeyIndex, keys, GUILayout.MaxWidth(150));
            if (binding.KeyIndex >= keys.Length)
            {
                binding.DestObj = null;
                binding.SourceKey = string.Empty;
            }
            else
            {
                BindingField(binding, destType);
                binding.SourceKey = keys[binding.KeyIndex];
            }
            EditorGUILayout.EndHorizontal();

            if (editorInjection != null)
                return editorInjection(keys, binding, input);
            else return false;
        }

        /// <summary>
        /// Displays a selection control for all properties and fields of a given type on an object.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destType">The type that will be bound to in the destination object. A dropdown list of all fields and properties with this type will be displayed.</param>
        public static void BindingField(TypeHelper.BindingMap source, Type destType)
        {
            Assert.IsNotNull(source);
            EditorGUILayout.BeginHorizontal();
            source.DestObj = EditorGUILayout.ObjectField(source.DestObj, typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(false));

            if (source.DestObj != null)
            {
                //compile list of all properties and fields on this dest
                List<string> names = new List<string>(10);
                FieldInfo[] fields = source.DestObj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var member in fields)
                {
                    if (member.FieldType == destType) names.Add(member.Name);
                }

                PropertyInfo[] props = source.DestObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var member in props)
                {
                    if (member.PropertyType == destType && member.CanWrite) names.Add(member.Name);
                }

                //display controls for member path
                GUILayout.Space(25);
                source.PathIndex = EditorGUILayout.Popup(source.PathIndex, names.ToArray());
                if (source.PathIndex >= names.Count) source.Path = "";
                else source.Path = names[source.PathIndex];
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        static LayerMask LayerMaskField(GUIContent content, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }
            maskWithoutEmpty = EditorGUILayout.MaskField(content, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }

        /*
        /// <summary>
        /// Dispplays a control for editing the HashedString object in an inspector.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="input"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string HashedStringField(GUIContent content, HashedString input, params GUILayoutOption[] options)
        {
            string output = EditorGUILayout.TextField(content, input.Value);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Hashed Value", input.Hash.ToString());
            EditorGUI.EndDisabledGroup();
            return output;
        }
        */
    }


    /// <summary>
    /// Base editor for common property drawers of custom classes.
    /// Object must be a reference type (class), must be serializable,
    /// and must have a public default constructor.
    /// </summary>
    public abstract class PropertyDrawerEx<T> : PropertyDrawer where T : class
    {
        protected T Edited;

        
        /// <summary>
        /// Ensures that the data being editor is never null.
        /// </summary>
        /// <param name="prop"></param>
        protected void Init(SerializedProperty prop)
        {
            var target = prop.serializedObject.targetObject;
            Edited = fieldInfo.GetValue(target) as T;
            if (Edited == null)
            {
                Edited = Activator.CreateInstance<T>();
                fieldInfo.SetValue(target, Edited);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);
            return 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);
            EditorGUI.BeginChangeCheck();
            OnInnerGUI(position, property, label);
            if (EditorGUI.EndChangeCheck() || GUI.changed)
            {
                if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        protected abstract void OnInnerGUI(Rect position, SerializedProperty property, GUIContent label);

    }
}
