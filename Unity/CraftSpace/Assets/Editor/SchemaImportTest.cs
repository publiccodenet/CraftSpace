using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CraftSpace.Editor
{
    public class SchemaImportTest : EditorWindow
    {
        [MenuItem("CraftSpace/Schema Debug Tools/Test Schema Import")]
        public static void TestSchemaImport()
        {
            string schemasPath = Path.Combine(Application.dataPath, "Schemas");
            string[] schemaFiles = Directory.GetFiles(schemasPath, "*.json");
            
            Debug.Log($"Found {schemaFiles.Length} schema files to test");
            
            foreach (string schemaFile in schemaFiles)
            {
                string fileName = Path.GetFileName(schemaFile);
                Debug.Log($"Testing schema: {fileName}");
                
                try
                {
                    string jsonContent = File.ReadAllText(schemaFile);
                    JObject schema = JObject.Parse(jsonContent);
                    
                    // Check schema structure
                    Debug.Log($"Schema type: {schema["type"]}");
                    Debug.Log($"Schema description: {schema["description"]}");
                    
                    // Check properties
                    JObject properties = schema["properties"] as JObject;
                    if (properties != null)
                    {
                        Debug.Log($"Schema has {properties.Count} properties:");
                        foreach (var property in properties)
                        {
                            string propName = property.Key;
                            JObject propDef = property.Value as JObject;
                            string propType = propDef?["type"]?.ToString() ?? "unknown";
                            
                            Debug.Log($"  - {propName}: {propType}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Schema has no properties section!");
                    }
                    
                    // Check if schema has any required properties
                    JArray required = schema["required"] as JArray;
                    if (required != null && required.Count > 0)
                    {
                        Debug.Log($"Schema has {required.Count} required properties: {string.Join(", ", required)}");
                    }
                    else
                    {
                        Debug.LogWarning("Schema has no required properties!");
                    }
                    
                    // Add code here to test schema conversion to C# class
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error testing schema {fileName}: {ex.Message}");
                    Debug.LogException(ex);
                }
            }
            
            Debug.Log("Schema import test complete!");
        }
        
        [MenuItem("CraftSpace/Schema Debug Tools/View Generated C# Classes")]
        public static void ViewGeneratedClasses()
        {
            string modelsPath = Path.Combine(Application.dataPath, "Scripts/Models");
            string[] csharpFiles = Directory.GetFiles(modelsPath, "*.cs");
            
            Debug.Log($"Found {csharpFiles.Length} generated C# model files");
            
            foreach (string csharpFile in csharpFiles)
            {
                string fileName = Path.GetFileName(csharpFile);
                try
                {
                    string fileContent = File.ReadAllText(csharpFile);
                    Debug.Log($"Class {fileName}:\n{fileContent.Substring(0, Math.Min(500, fileContent.Length))}...");
                    
                    // Count properties
                    int propCount = CountProperties(fileContent);
                    Debug.Log($"Class {fileName} appears to have {propCount} properties");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error reading C# file {fileName}: {ex.Message}");
                }
            }
        }
        
        private static int CountProperties(string classContent)
        {
            // Very simple property counter - this is just a rough estimate
            int count = 0;
            string[] lines = classContent.Split('\n');
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("public ") && line.Contains("{ get; set; }"))
                {
                    count++;
                }
            }
            return count;
        }
    }
} 