using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Brewster))]
public class BrewsterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Brewster brewster = (Brewster)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Content Operations", EditorStyles.boldLabel);
        
        // Add controls for content management
        if (GUILayout.Button("Load Content"))
        {
            brewster.LoadAllContent();
        }
        
        if (GUILayout.Button("Clear Content"))
        {
            brewster.ClearAllData();
        }
        
        // Stats
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Content Stats", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Collections: {brewster.collections.Count}");
        
        int itemCount = 0;
        foreach (var collection in brewster.collections)
        {
            itemCount += collection.items.Count;
        }
        
        EditorGUILayout.LabelField($"Total Items: {itemCount}");
    }
} 