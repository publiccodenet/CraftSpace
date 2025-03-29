using UnityEngine;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace CraftSpace.Editor.SchemaGenerator
{
    public static class SchemaImporter
    {
        private const string GENERATED_DIR = "Assets/Scripts/Schemas/Generated";
        
        /// <summary>
        /// Import a schema and generate a C# class from it
        /// </summary>
        /// <returns>True if the schema was successfully imported, false otherwise</returns>
        public static bool ImportSchema(string schemaJson)
        {
            if (string.IsNullOrEmpty(schemaJson))
            {
                Debug.LogError("SchemaImporter: Empty schema JSON");
                return false;
            }
            
            try
            {
                JObject schema = JObject.Parse(schemaJson);
                string className = schema["title"]?.ToString();
                
                if (string.IsNullOrEmpty(className))
                {
                    Debug.LogError("SchemaImporter: Schema is missing a title property");
                    return false;
                }
                
                // Generate code using our schema type system
                SchemaType schemaType = JsonConvert.DeserializeObject<SchemaType>(schemaJson);
                string generatedCode = ScriptableGenerator.GenerateClass(schemaType, className);
                
                // Save the generated code
                SaveGeneratedCode(className, generatedCode);
                
                Debug.Log($"SchemaImporter: Successfully imported schema for {className}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SchemaImporter: Error importing schema: {ex.Message}");
                return false;
            }
        }
        
        private static void SaveGeneratedCode(string className, string generatedCode)
        {
            // Ensure directory exists
            if (!Directory.Exists(GENERATED_DIR))
            {
                Directory.CreateDirectory(GENERATED_DIR);
            }
            
            // Write the file
            string filePath = Path.Combine(GENERATED_DIR, $"{className}.cs");
            File.WriteAllText(filePath, generatedCode);
            
            // Refresh the AssetDatabase
            AssetDatabase.Refresh();
        }
    }
} 