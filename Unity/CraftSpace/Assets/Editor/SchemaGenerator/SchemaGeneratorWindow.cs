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
        private List<SchemaInfo> importedSchemas = new List<SchemaInfo>();

        public class SchemaInfo
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public long FileSize { get; set; }
        }

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
            
            int successCount = 0;
            int failureCount = 0;
            
            foreach (string file in schemaFiles)
            {
                Debug.Log($"Processing schema file: {file}");
                try 
                {
                    string fileContents = File.ReadAllText(file);
                    Debug.Log($"File size: {fileContents.Length} bytes");
                    
                    // Actually import the schema now
                    bool success = SchemaImporter.ImportSchema(fileContents);
                    if (success)
                    {
                        successCount++;
                        Debug.Log($"Successfully imported {Path.GetFileName(file)}");
                    }
                    else
                    {
                        failureCount++;
                        Debug.LogError($"Failed to import {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Debug.LogError($"Error processing {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
                }
            }
            
            string message = $"Schema processing complete.\nSuccessful: {successCount}\nFailed: {failureCount}";
            Debug.Log(message);
            EditorUtility.DisplayDialog("Schema Import", message, "OK");
        }
        
        /// <summary>
        /// Import schemas from command line
        /// </summary>
        public static void ImportSchemasFromCommandLine()
        {
            Debug.Log("Starting schema import from command line...");
            ImportSchemas();
            Debug.Log("Schema import from command line completed.");
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
            EditorGUILayout.LabelField("Imported Schemas", EditorStyles.boldLabel);

            // List imported schemas with detailed info
            foreach (var schema in importedSchemas)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(schema.FileName + " - " + schema.FileSize + " bytes");
                if (GUILayout.Button("Select"))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(schema.FilePath));
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
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