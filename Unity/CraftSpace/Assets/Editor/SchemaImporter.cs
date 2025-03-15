using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Unity Editor tool to import JSON schemas and generate C# classes.
/// Uses NJsonSchema to generate C# classes from JSON schemas.
/// </summary>
public class SchemaImporter : EditorWindow
{
    private string schemaDirectory = "";
    private string outputDirectory = "";
    private string namespaceName = "BackSpace.Models";
    
    [MenuItem("Tools/Schema Importer")]
    public static void ShowWindow()
    {
        GetWindow<SchemaImporter>("Schema Importer");
    }
    
    private void OnEnable()
    {
        // Default paths relative to project
        schemaDirectory = Path.Combine(Application.dataPath, "../Schemas");
        outputDirectory = Path.Combine(Application.dataPath, "Scripts/Generated/Models");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("JSON Schema to C# Class Generator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        schemaDirectory = EditorGUILayout.TextField("Schema Directory", schemaDirectory);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Browse Schema Directory"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Schema Directory", schemaDirectory, "");
            if (!string.IsNullOrEmpty(path))
                schemaDirectory = path;
        }
        
        if (GUILayout.Button("Browse Output Directory"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Directory", outputDirectory, "");
            if (!string.IsNullOrEmpty(path))
                outputDirectory = path;
        }
        
        EditorGUILayout.Space();
        
        GUI.enabled = Directory.Exists(schemaDirectory);
        if (GUILayout.Button("Generate C# Classes"))
        {
            GenerateClasses();
        }
        GUI.enabled = true;
        
        if (!Directory.Exists(schemaDirectory))
        {
            EditorGUILayout.HelpBox("Schema directory doesn't exist!", MessageType.Warning);
        }
    }
    
    private void GenerateClasses()
    {
        if (!Directory.Exists(schemaDirectory))
        {
            Debug.LogError("Schema directory doesn't exist: " + schemaDirectory);
            return;
        }
        
        // Ensure output directory exists
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        
        // Find all JSON Schema files
        string[] schemaFiles = Directory.GetFiles(schemaDirectory, "*.schema.json", SearchOption.TopDirectoryOnly);
        
        if (schemaFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("No Schemas Found", 
                "No JSON Schema files (*.schema.json) found in the selected directory.", "OK");
            return;
        }
        
        // Generate a class for each schema
        int successCount = 0;
        foreach (string schemaFile in schemaFiles)
        {
            try
            {
                string className = Path.GetFileNameWithoutExtension(schemaFile).Replace(".schema", "");
                
                // Call NJsonSchema code generation (see below)
                if (GenerateClassFromSchema(schemaFile, className))
                    successCount++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing {Path.GetFileName(schemaFile)}: {ex.Message}");
            }
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Code Generation Complete", 
            $"Successfully generated {successCount} of {schemaFiles.Length} C# classes.", "OK");
    }
    
    private bool GenerateClassFromSchema(string schemaFilePath, string className)
    {
        try
        {
            // Read the schema file
            string schemaJson = File.ReadAllText(schemaFilePath);
            
            // Generate C# code using NJsonSchema
            // We'll use a simple template-based approach for now
            string code = GenerateSimpleClass(schemaJson, className);
            
            // Write the output file
            string outputPath = Path.Combine(outputDirectory, $"{className}.cs");
            File.WriteAllText(outputPath, code);
            
            Debug.Log($"Generated: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating class from {schemaFilePath}: {ex.Message}");
            return false;
        }
    }
    
    private string GenerateSimpleClass(string schemaJson, string className)
    {
        // This is a simplified generator - in a real implementation,
        // you would use NJsonSchema.CodeGeneration.CSharp for proper JSON Schema parsing
        
        // For now, we'll generate a basic class with Newtonsoft.Json attributes
        var codeBuilder = new System.Text.StringBuilder();
        
        codeBuilder.AppendLine("// <auto-generated>");
        codeBuilder.AppendLine("// Generated from JSON Schema using SchemaImporter");
        codeBuilder.AppendLine("// </auto-generated>");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("using System;");
        codeBuilder.AppendLine("using System.Collections.Generic;");
        codeBuilder.AppendLine("using Newtonsoft.Json;");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"namespace {namespaceName}");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendLine($"    public class {className.Replace("Schema", "")}");
        codeBuilder.AppendLine("    {");
        
        // Dummy properties - in a real implementation, these would be parsed from the schema
        codeBuilder.AppendLine("        // TODO: Add actual properties from schema");
        codeBuilder.AppendLine("        [JsonProperty(\"id\")]");
        codeBuilder.AppendLine("        public string Id { get; set; }");
        codeBuilder.AppendLine();
        
        codeBuilder.AppendLine("    }");
        codeBuilder.AppendLine("}");
        
        return codeBuilder.ToString();
    }
} 