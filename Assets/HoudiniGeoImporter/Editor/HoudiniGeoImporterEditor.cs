using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace NmrgLibrary.HoudiniGeoImporter
{
    [CustomEditor(typeof(HoudiniGeoImporter))]
    public class HoudiniGeoImporterEditor : ScriptedImporterEditor
    {
        private bool showAttributeMapping = true;
        public override void OnInspectorGUI()
        {
            var reverseWinding = serializedObject.FindProperty("reverseWinding");
            EditorGUILayout.PropertyField(reverseWinding, new GUIContent("Reverse Winding"));

            var attribNames = List2PopupOptions(serializedObject.FindProperty("attribNames"));
            
            var posAttribNameProp = serializedObject.FindProperty("posAttribName");
            var normalAttribNameProp = serializedObject.FindProperty("normalAttribName");
            var colorAttribNameProp = serializedObject.FindProperty("colorAttribName");
            var uv1AttribNameProp = serializedObject.FindProperty("uv1AttribName");
            var uv2AttribNameProp = serializedObject.FindProperty("uv2AttribName");
            var uv3AttribNameProp = serializedObject.FindProperty("uv3AttribName");
            var uv4AttribNameProp = serializedObject.FindProperty("uv4AttribName");
            var uv5AttribNameProp = serializedObject.FindProperty("uv5AttribName");
            var uv6AttribNameProp = serializedObject.FindProperty("uv6AttribName");
            var uv7AttribNameProp = serializedObject.FindProperty("uv7AttribName");
            var uv8AttribNameProp = serializedObject.FindProperty("uv8AttribName");

            // showAttributeMapping = EditorGUILayout.Foldout(showAttributeMapping, "Attribute Mapping");

            EditorGUILayout.BeginVertical(GUI.skin.box);
            showAttributeMapping = EditorGUILayout.Foldout(showAttributeMapping, "Attribute Mapping");
            if (showAttributeMapping)
            {
                AttribLayout(posAttribNameProp, "Pos", attribNames);
                AttribLayout(normalAttribNameProp, "Normal", attribNames);
                AttribLayout(colorAttribNameProp, "Color", attribNames);
                AttribLayout(uv1AttribNameProp, "UV1", attribNames);
                AttribLayout(uv2AttribNameProp, "UV2", attribNames);
                AttribLayout(uv2AttribNameProp, "UV3", attribNames);
                AttribLayout(uv2AttribNameProp, "UV4", attribNames);
                AttribLayout(uv2AttribNameProp, "UV5", attribNames);
                AttribLayout(uv2AttribNameProp, "UV6", attribNames);
                AttribLayout(uv2AttribNameProp, "UV7", attribNames);
                AttribLayout(uv2AttribNameProp, "UV8", attribNames);
            }
            EditorGUILayout.EndVertical();

            ApplyRevertGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private void AttribLayout(SerializedProperty prop,string label, string[] options)
        {
            int currentIndex = System.Array.IndexOf(options, prop.stringValue);
            if (currentIndex == -1) currentIndex = 0; 
            int selectedIndex = EditorGUILayout.Popup(label, currentIndex, options);
            if (selectedIndex != currentIndex)
            {
                prop.stringValue = options[selectedIndex];
            }
        }

        private string[] List2PopupOptions(SerializedProperty serializedProperty)
        {
            var arraySize = serializedProperty.arraySize;
            var options = new string[arraySize + 1];
            options[0] = "_";
            for (int i = 0; i < arraySize; i++)
            {
                options[i+1] = serializedProperty.GetArrayElementAtIndex(i).stringValue;
            }
            return options;
        }
    }
}