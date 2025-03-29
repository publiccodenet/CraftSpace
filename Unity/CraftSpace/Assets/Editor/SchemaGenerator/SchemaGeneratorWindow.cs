using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace CraftSpace.Editor.SchemaGenerator
{
    public class SchemaGeneratorWindow : EditorWindow
    {
        // Path constants
        private static readonly string SCHEMA_SOURCE_DIR = Path.Combine(Application.streamingAssetsPath, "Content/schemas");
        private static readonly string DEFAULT_OUTPUT_PATH = "Assets/Scripts/Schemas/Generated";
        
        // UI state
        private string _schemaJson = "";
        private string _outputPath = DEFAULT_OUTPUT_PATH;
        private Vector2 _schemaScrollPos;
        private bool _showPreview = false;
        private string _generatedCode = "";
        private Vector2 _previewScrollPos;

        [MenuItem("CraftSpace/Schema Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SchemaGeneratorWindow>("Schema Generator");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        [MenuItem("Tools/JSON Schema/Open Generator")]
        public static void ShowWindowFromTools()
        {
            ShowWindow();
        }

        [MenuItem("Tools/JSON Schema/Import From StreamingAssets")]
        public static void ImportSchemasFromTools()
        {
            ImportSchemas();
        }

        [MenuItem("CraftSpace/Import JSON Schemas")]
        public static void ImportSchemas()
        {
            // Print debug information about paths
            Debug.Log("=== SCHEMA DEBUG INFO ===");
            Debug.Log($"StreamingAssetsPath: {Application.streamingAssetsPath}");
            Debug.Log($"SCHEMA_SOURCE_DIR: {SCHEMA_SOURCE_DIR}");
            Debug.Log($"DEFAULT_OUTPUT_PATH: {DEFAULT_OUTPUT_PATH}");
            
            bool directoryExists = Directory.Exists(SCHEMA_SOURCE_DIR);
            Debug.Log($"Schema directory exists: {directoryExists}");
            
            if (!directoryExists)
            {
                EditorUtility.DisplayDialog("Error", $"Schema source directory not found: {SCHEMA_SOURCE_DIR}", "OK");
                return;
            }
            
            string[] schemaFiles = Directory.GetFiles(SCHEMA_SOURCE_DIR, "*.json");
            Debug.Log($"Schema files found: {schemaFiles.Length}");
            
            foreach (string file in schemaFiles)
            {
                Debug.Log($"Schema file: {file}");
                try 
                {
                    string fileContents = File.ReadAllText(file);
                    Debug.Log($"File size: {fileContents.Length} bytes");
                    
                    // Try to parse as JObject 
                    JObject obj = JObject.Parse(fileContents);
                    Debug.Log($"Successfully parsed {Path.GetFileName(file)} as JObject");
                    
                    // Verify required field
                    if (obj["required"] != null)
                    {
                        Debug.Log($"Required field found, type: {obj["required"].Type}, value: {obj["required"]}");
                    }
                    else
                    {
                        Debug.Log("No required field found");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
                }
            }
            
            EditorUtility.DisplayDialog("Debug", "Schema processing debug info logged. Check the console for details.", "OK");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("JSON Schema Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Auto-import button at the top
            if (GUILayout.Button("Import Schemas from StreamingAssets"))
            {
                ImportSchemas();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manual Schema Entry", EditorStyles.boldLabel);

            // Output path field with browse button
            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Schema JSON input
            EditorGUILayout.LabelField("JSON Schema:", EditorStyles.boldLabel);
            _schemaScrollPos = EditorGUILayout.BeginScrollView(_schemaScrollPos, GUILayout.Height(200));
            _schemaJson = EditorGUILayout.TextArea(_schemaJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Generate and Preview buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate"))
            {
                GenerateCode();
            }
            _showPreview = EditorGUILayout.ToggleLeft("Show Preview", _showPreview, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Preview area
            if (_showPreview && !string.IsNullOrEmpty(_generatedCode))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Generated Code Preview:", EditorStyles.boldLabel);
                _previewScrollPos = EditorGUILayout.BeginScrollView(_previewScrollPos, GUILayout.Height(200));
                EditorGUILayout.TextArea(_generatedCode, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            
            // Help box at the bottom
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This tool generates C# classes from JSON schema files.\n\n" +
                "1. Automatic Import: Reads schema files from " + SCHEMA_SOURCE_DIR + "\n" +
                "2. Manual Entry: Paste schema JSON in the text area above\n\n" +
                "Generated classes will be placed in: " + DEFAULT_OUTPUT_PATH + "\n" +
                "Type converters will be detected and applied automatically based on schema annotations.", 
                MessageType.Info);
        }

        private void GenerateCode()
        {
            if (string.IsNullOrWhiteSpace(_schemaJson))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a JSON schema.", "OK");
                return;
            }

            try
            {
                Debug.Log("Attempting to parse schema in editor...");
                
                // Try to parse as JObject 
                JObject obj = JObject.Parse(_schemaJson);
                Debug.Log("Successfully parsed editor schema as JObject");
                
                // Verify required field
                if (obj["required"] != null)
                {
                    Debug.Log($"Required field found, type: {obj["required"].Type}, value: {obj["required"]}");
                }
                else
                {
                    Debug.Log("No required field found in editor schema");
                }
                
                EditorUtility.DisplayDialog("Debug", "Schema parsing successful. Check the console for details.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing editor schema: {ex.GetType().Name}: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to parse schema: {ex.Message}", "OK");
            }
        }
    }
} 