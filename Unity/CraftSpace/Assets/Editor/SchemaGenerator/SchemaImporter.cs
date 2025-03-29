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

                // Append "Schema" to the class name
                string schemaClassName = className + "Schema";
                
                Debug.Log($"SchemaImporter: Read title property: {className}, using class name: {schemaClassName}");
                
                // Generate code using our schema type system
                SchemaType schemaType = JsonConvert.DeserializeObject<SchemaType>(schemaJson);
                
                // Set the title in the schema type to include "Schema" suffix
                schemaType.Title = schemaClassName;
                
                // Double-check the title was copied to the SchemaType
                Debug.Log($"SchemaImporter: SchemaType.Title = {schemaType.Title}");
                
                // Process type converters from the schema
                ProcessTypeConverters(schemaType);
                
                string generatedCode = ScriptableGenerator.GenerateClass(schemaType, schemaClassName);
                
                // Save the generated code
                SaveGeneratedCode(schemaClassName, generatedCode);
                
                Debug.Log($"SchemaImporter: Successfully imported schema for {schemaClassName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SchemaImporter: Error importing schema: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Process type converters referenced in the schema
        /// </summary>
        private static void ProcessTypeConverters(SchemaType schema)
        {
            // Process schema level type converters
            var schemaConverter = schema.GetTypeConverter();
            if (schemaConverter != null)
            {
                MapTypeConverter(schemaConverter);
            }
            
            // Process property level type converters
            if (schema.Properties != null)
            {
                foreach (var prop in schema.Properties.Values)
                {
                    var propConverter = prop.GetTypeConverter();
                    if (propConverter != null)
                    {
                        MapTypeConverter(propConverter);
                    }
                    
                    // Recursively process nested object properties
                    if (prop.Type == "object" && prop.Properties != null)
                    {
                        ProcessTypeConverters(prop);
                    }
                }
            }
        }
        
        /// <summary>
        /// Map type converter names to appropriate C# types
        /// </summary>
        private static void MapTypeConverter(TypeConverterInfo converter)
        {
            switch (converter.Name)
            {
                case "StringOrStringArrayToString":
                    converter.CSharpType = "string";
                    break;
                case "StringOrNullToString":
                    converter.CSharpType = "string";
                    break;
                case "NullOrStringToStringArray":
                    converter.CSharpType = "List<string>";
                    break;
                case "StringToDateTime":
                    converter.CSharpType = "DateTime";
                    break;
                case "UnixTimestampToDateTime":
                    converter.CSharpType = "DateTime";
                    break;
                case "Base64ToBinary":
                    converter.CSharpType = "byte[]";
                    break;
                default:
                    converter.CSharpType = "string"; // Default fallback
                    break;
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