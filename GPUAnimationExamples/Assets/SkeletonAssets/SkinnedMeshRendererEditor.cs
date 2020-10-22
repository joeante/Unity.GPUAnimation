using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
/*
[CustomEditor(typeof(SkinnedMeshRenderer))]
public class SkinnedMeshRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Bones"), true);

        var arrayProp = serializedObject.FindProperty("m_Bones");
        arrayProp.Next(true);
        arrayProp.Next(true);
        var prop = serializedObject.FindProperty("m_Bones");
        EditorGUILayout.PropertyField(arrayProp, true);
        for (int i = 0;i != prop.arraySize;i++)
            EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(i), true);

        serializedObject.ApplyModifiedProperties();
    }
}
*/