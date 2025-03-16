using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Unity Editor tool to import JSON schemas and generate C# classes.
/// Uses NJsonSchema to generate C# classes from JSON schemas.
/// </summary>
public class SchemaImporter : EditorWindow
{
    private string schemaDirectory = "Assets/Schemas";
    private string outputDirectory = "Assets/Scripts/Models/Generated";
    private bool includeNamespace = true;
    private string namespaceName = "CraftSpace.Models";
    private bool overwriteExisting = true;
    private Vector2 scrollPosition;
    private List<string> availableSchemas = new List<string>();
    private List<bool> selectedSchemas = new List<bool>();
    private bool selectAll = true;
    private string statusMessage = "";

    [MenuItem("CraftSpace/Schema Importer")]
    public static void ShowWindow()
    {
        GetWindow<SchemaImporter>("Schema Importer");
    }

    private void OnEnable()
    {
        RefreshSchemaList();
    }

    private void RefreshSchemaList()
    {
        availableSchemas.Clear();
        selectedSchemas.Clear();
        
        if (!Directory.Exists(schemaDirectory))
        {
            statusMessage = $"Schema directory not found: {schemaDirectory}";
            return;
        }
        
        string[] schemaFiles = Directory.GetFiles(schemaDirectory, "*.json");
        foreach (string schemaFile in schemaFiles)
        {
            availableSchemas.Add(Path.GetFileNameWithoutExtension(schemaFile));
            selectedSchemas.Add(selectAll);
        }
        
        statusMessage = $"Found {schemaFiles.Length} schema files.";
    }

    private void OnGUI()
    {
        GUILayout.Label("Schema Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        schemaDirectory = EditorGUILayout.TextField("Schema Directory", schemaDirectory);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        includeNamespace = EditorGUILayout.Toggle("Include Namespace", includeNamespace);
        
        if (includeNamespace)
        {
            namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
        }
        
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Refresh Schema List"))
        {
            RefreshSchemaList();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Available Schemas", EditorStyles.boldLabel);
        
        bool newSelectAll = EditorGUILayout.Toggle("Select All", selectAll);
        if (newSelectAll != selectAll)
        {
            selectAll = newSelectAll;
            for (int i = 0; i < selectedSchemas.Count; i++)
            {
                selectedSchemas[i] = selectAll;
            }
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < availableSchemas.Count; i++)
        {
            selectedSchemas[i] = EditorGUILayout.Toggle(availableSchemas[i], selectedSchemas[i]);
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate C# Classes"))
        {
            GenerateClasses();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
    }

    private void GenerateClasses()
    {
        try
        {
            Debug.Log($"Starting class generation from schemas in {schemaDirectory}");
            
            if (!Directory.Exists(schemaDirectory))
            {
                Debug.LogError($"Schema directory does not exist: {schemaDirectory}");
                statusMessage = $"Error: Schema directory not found";
                return;
            }
            
            // List all files in the schema directory
            string[] allFiles = Directory.GetFiles(schemaDirectory);
            Debug.Log($"All files in schema directory: {string.Join(", ", allFiles)}");
            
            // List just JSON files
            string[] schemaFiles = Directory.GetFiles(schemaDirectory, "*.json");
            Debug.Log($"JSON files in schema directory: {string.Join(", ", schemaFiles)}");
            
            if (schemaFiles.Length == 0)
            {
                Debug.LogWarning("No JSON schema files found in directory");
                statusMessage = "No schema files found";
                return;
            }
            
            if (!Directory.Exists(outputDirectory))
            {
                Debug.Log($"Creating output directory: {outputDirectory}");
                Directory.CreateDirectory(outputDirectory);
            }
            
            int generatedCount = 0;
            
            for (int i = 0; i < availableSchemas.Count; i++)
            {
                if (selectedSchemas[i])
                {
                    string schemaName = availableSchemas[i];
                    string schemaPath = Path.Combine(schemaDirectory, $"{schemaName}.json");
                    string outputPath = Path.Combine(outputDirectory, $"{schemaName}.cs");
                    
                    Debug.Log($"Processing schema: {schemaName}");
                    Debug.Log($"Schema path: {schemaPath}");
                    Debug.Log($"Output path: {outputPath}");
                    
                    if (!File.Exists(schemaPath))
                    {
                        Debug.LogError($"Schema file not found: {schemaPath}");
                        continue;
                    }
                    
                    if (File.Exists(outputPath) && !overwriteExisting)
                    {
                        Debug.LogWarning($"Skipping {schemaName} - file already exists and overwrite is disabled.");
                        continue;
                    }
                    
                    string json = File.ReadAllText(schemaPath);
                    Debug.Log($"Schema file content (first 100 chars): {json.Substring(0, Math.Min(100, json.Length))}...");
                    
                    try
                    {
                        string csharpCode = ConvertJsonSchemaToClass(schemaName, json);
                        
                        if (string.IsNullOrEmpty(csharpCode))
                        {
                            Debug.LogError($"Generated C# code for {schemaName} was empty");
                            continue;
                        }
                        
                        Debug.Log($"Writing C# class to: {outputPath}");
                        File.WriteAllText(outputPath, csharpCode);
                        generatedCount++;
                        Debug.Log($"Successfully generated class for: {schemaName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing schema {schemaName}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            
            AssetDatabase.Refresh();
            statusMessage = $"Generated {generatedCount} C# classes.";
            Debug.Log(statusMessage);
        }
        catch (Exception ex)
        {
            statusMessage = $"Error generating classes: {ex.Message}";
            Debug.LogException(ex);
        }
    }

    private string ConvertJsonSchemaToClass(string className, string jsonSchema)
    {
        try
        {
            Debug.Log($"Parsing JSON schema for {className}");
            JObject schema = JObject.Parse(jsonSchema);
            
            // Navigate to the actual schema data - handle $ref structure
            JObject actualSchema = schema;
            
            // Check if we have a $ref at the root
            string refPath = schema["$ref"]?.ToString();
            if (!string.IsNullOrEmpty(refPath) && refPath.StartsWith("#/"))
            {
                // Parse the path - typical format is "#/definitions/SchemaName"
                string[] pathParts = refPath.Substring(2).Split('/');
                JToken current = schema;
                
                foreach (string part in pathParts)
                {
                    current = current[part];
                    if (current == null)
                    {
                        Debug.LogError($"Could not resolve reference path: {refPath}");
                        return $"// Error: Invalid reference path {refPath}";
                    }
                }
                
                actualSchema = current as JObject;
                Debug.Log($"Resolved $ref to actual schema with {actualSchema?.Properties().Count()} properties");
            }
            
            // Look for properties in the correct location
            JObject properties = actualSchema["properties"] as JObject;
            
            // Log what we found for debugging
            if (properties != null)
            {
                Debug.Log($"Found {properties.Count} properties in schema");
                foreach (var prop in properties)
                {
                    Debug.Log($"  Property: {prop.Key}");
                }
            }
            else
            {
                Debug.LogWarning($"No properties found in {className} schema");
                // Look in other places
                if (actualSchema["type"]?.ToString() == "object")
                {
                    Debug.Log("Schema is an object type but couldn't find properties");
                }
                
                // Dump full schema for debugging
                Debug.Log($"Full schema: {actualSchema.ToString(Formatting.Indented).Substring(0, Math.Min(500, actualSchema.ToString().Length))}...");
            }
            
            StringBuilder sb = new StringBuilder();
            
            // Add using directives
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine();
            
            // Add namespace if requested
            if (includeNamespace)
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            // Add class documentation
            string description = schema["description"]?.ToString() ?? $"Represents a {className}";
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description}");
            sb.AppendLine("/// </summary>");
            
            // Begin class definition
            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");
            
            // Process properties from schema
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    string propertyName = property.Key;
                    JObject propertyObj = property.Value as JObject;
                    
                    if (propertyObj != null)
                    {
                        // Get property description for documentation
                        string propertyDescription = propertyObj["description"]?.ToString() ?? $"Gets or sets the {propertyName}";
                        
                        sb.AppendLine("    /// <summary>");
                        sb.AppendLine($"    /// {propertyDescription}");
                        sb.AppendLine("    /// </summary>");
                        
                        // Determine property type
                        string propertyType = GetCSharpType(propertyObj);
                        
                        // Generate property
                        sb.AppendLine($"    [JsonProperty(\"{propertyName}\")]");
                        sb.AppendLine($"    public {propertyType} {FormatPropertyName(propertyName)} {{ get; set; }}");
                        sb.AppendLine();
                    }
                }
            }
            
            // End class definition
            sb.AppendLine("}");
            
            // End namespace if used
            if (includeNamespace)
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting schema {className}: {ex.Message}\n{ex.StackTrace}");
            return $"// Error generating class: {ex.Message}";
        }
    }

    private string GetCSharpType(JObject propertyObj)
    {
        string type = propertyObj["type"]?.ToString();
        
        if (type == null)
        {
            return "object";
        }
        
        switch (type)
        {
            case "string":
                string format = propertyObj["format"]?.ToString();
                if (format == "date-time")
                {
                    return "DateTime";
                }
                return "string";
                
            case "integer":
                return "int";
                
            case "number":
                return "double";
                
            case "boolean":
                return "bool";
                
            case "array":
                JObject items = propertyObj["items"] as JObject;
                if (items != null)
                {
                    string itemType = GetCSharpType(items);
                    return $"List<{itemType}>";
                }
                return "List<object>";
                
            case "object":
                return "Dictionary<string, object>";
                
            default:
                return "object";
        }
    }

    private string FormatPropertyName(string propertyName)
    {
        // Convert snake_case or kebab-case to PascalCase
        StringBuilder sb = new StringBuilder();
        bool capitalizeNext = true;
        
        foreach (char c in propertyName)
        {
            if (c == '_' || c == '-')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpper(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }
        
        return sb.ToString();
    }
} 