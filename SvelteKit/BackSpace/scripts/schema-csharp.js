#!/usr/bin/env node
/**
 * Generate C# classes directly from schemas
 * 
 * This script creates C# class files from our JSON schemas as a backup/alternative
 * to Unity's schema importer
 */
import fs from 'fs-extra';
import path from 'path';
import { EMOJI, PATHS } from '../src/lib/constants/index.ts';

// Input and output directories
const schemaDir = PATHS.SCHEMAS_DIR;
const outputDir = PATHS.CRAFTSPACE_GENERATED_SCHEMAS_DIR;

console.log(`${EMOJI.START} GENERATING C# MODEL CLASSES FROM SCHEMAS`);
console.log(`Schema directory: ${schemaDir}`);
console.log(`Output directory: ${outputDir}`);

// Ensure output directory exists
fs.ensureDirSync(outputDir);

// Find all schema files
const schemaFiles = fs.readdirSync(schemaDir)
  .filter(file => file.endsWith('.json'));

console.log(`Found ${schemaFiles.length} schema files to process`);

// Type mapping from JSON Schema to C#
const typeMap = {
  'string': 'string',
  'integer': 'int',
  'number': 'float',
  'boolean': 'bool',
  'array': 'List<object>',
  'object': 'Dictionary<string, object>'
};

// Process each schema
for (const file of schemaFiles) {
  const schemaPath = path.join(schemaDir, file);
  const className = file.replace('.json', '');
  console.log(`\nProcessing schema: ${file} -> ${className}.cs`);
  
  try {
    const schema = fs.readJSONSync(schemaPath);
    
    // Check if it's a valid schema
    if (!schema.type || !schema.properties) {
      console.warn(`⚠️ Schema ${file} doesn't have required type and properties`);
      // Try to look in definitions
      if (schema.definitions) {
        const refName = Object.keys(schema.definitions)[0];
        if (schema.definitions[refName].properties) {
          schema.properties = schema.definitions[refName].properties;
          schema.required = schema.definitions[refName].required || [];
          schema.type = schema.definitions[refName].type;
          console.log(`Found properties in definitions.${refName}`);
        }
      }
      
      // If still no properties, skip this schema
      if (!schema.properties) {
        console.error(`❌ Cannot generate C# class for ${file}: No properties found`);
        continue;
      }
    }
    
    // Start building C# class
    let csharpCode = `using System;\nusing System.Collections.Generic;\nusing Newtonsoft.Json;\n\nnamespace CraftSpace.Models\n{\n`;
    
    // Add class description
    csharpCode += `    /// <summary>\n`;
    csharpCode += `    /// ${schema.description || `Represents a ${className}`}\n`;
    csharpCode += `    /// </summary>\n`;
    
    // Start class definition
    csharpCode += `    public class ${className}\n    {\n`;
    
    // Process properties
    const properties = schema.properties;
    for (const [propName, propDef] of Object.entries(properties)) {
      // Determine property type
      let csharpType = typeMap[propDef.type] || 'object';
      
      // Handle arrays with specific item types
      if (propDef.type === 'array' && propDef.items) {
        if (propDef.items.type) {
          csharpType = `List<${typeMap[propDef.items.type] || 'object'}>`;
        } else if (propDef.items.$ref) {
          // Reference to another type
          const refType = propDef.items.$ref.split('/').pop();
          csharpType = `List<${refType}>`;
        }
      }
      
      // Add property description if available
      if (propDef.description) {
        csharpCode += `        /// <summary>\n`;
        csharpCode += `        /// ${propDef.description}\n`;
        csharpCode += `        /// </summary>\n`;
      }
      
      // Add JsonProperty attribute
      csharpCode += `        [JsonProperty("${propName}")]\n`;
      
      // Add property declaration
      const nullableMark = !schema.required?.includes(propName) ? '?' : '';
      csharpCode += `        public ${csharpType}${nullableMark} ${toPascalCase(propName)} { get; set; }\n\n`;
    }
    
    // Close class and namespace
    csharpCode += `    }\n}\n`;
    
    // Write C# file
    const outputFile = path.join(outputDir, `${className}.cs`);
    fs.writeFileSync(outputFile, csharpCode);
    console.log(`Generated C# class: ${outputFile}`);
    
  } catch (error) {
    console.error(`❌ Error processing schema ${file}:`, error.message);
  }
}

console.log('\nC# GENERATION COMPLETE!');

// Helper function to convert to PascalCase
function toPascalCase(str) {
  return str
    .split('_')
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');
} 