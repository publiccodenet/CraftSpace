using UnityEngine;
using UnityEditor;
using System.IO;

public class SchemaDebugger : EditorWindow
{
    // Constants
    private const string SCHEMA_DIR = "Assets/Schemas";
    
    [MenuItem("CraftSpace/Schema Debugger")]
    public static void ShowWindow()
    {
        GetWindow<SchemaDebugger>("Schema Debugger");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Debug Schema Files"))
        {
            Debug.Log($"Checking schema directory: {SCHEMA_DIR}");
            
            if (Directory.Exists(SCHEMA_DIR))
            {
                string[] files = Directory.GetFiles(SCHEMA_DIR, "*.json");
                Debug.Log($"Found {files.Length} schema files");
                
                foreach (string file in files)
                {
                    Debug.Log($"Schema file: {file}");
                    string content = File.ReadAllText(file);
                    Debug.Log($"Content preview: {content.Substring(0, Mathf.Min(100, content.Length))}...");
                }
            }
            else
            {
                Debug.LogError($"Schema directory not found: {SCHEMA_DIR}");
            }
        }
    }
} 