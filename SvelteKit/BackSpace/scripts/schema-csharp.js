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
import chalk from 'chalk';

console.log(chalk.cyan('🏁 GENERATING C# MODEL CLASSES FROM SCHEMAS'));

// Directory configuration
const schemaDir = PATHS.SCHEMAS_DIR;
const outputDir = path.resolve(
  path.dirname(new URL(import.meta.url).pathname),
  '../../../Assets/Scripts/Models/Schema/Generated'
);

console.log(`Schema directory: ${schemaDir}`);
console.log(`Output directory: ${outputDir}`);

// Ensure output directory exists
fs.ensureDirSync(outputDir);

// Find all JSON schema files
const schemaFiles = fs.readdirSync(schemaDir)
  .filter(file => file.endsWith('.json'));

console.log(`Found ${schemaFiles.length} schema files to process\n`);

// Process each schema file
for (const schemaFile of schemaFiles) {
  const schemaPath = path.join(schemaDir, schemaFile);
  const schema = fs.readJSONSync(schemaPath);
  
  // Extract class name from file name
  const className = path.basename(schemaFile, '.json');
  
  // Generate C# class
  // CHANGED NAMESPACE FROM CraftSpace.Models.Schema to CraftSpace.Models
  const csharpCode = generateCSharpClass(className, schema, 'CraftSpace.Models.Schema.Generated');
  
  // Write to output file
  const outputPath = path.join(outputDir, `${className}.cs`);
  fs.writeFileSync(outputPath, csharpCode);
  
  console.log(`\nProcessing schema: ${schemaFile} -> ${className}.cs`);
  console.log(chalk.green(`Generated C# class: ${outputPath}`));
}

console.log('\nC# GENERATION COMPLETE!');

/**
 * Generate a C# class from a JSON schema
 */
function generateCSharpClass(className, schema, namespace = 'CraftSpace.Models.Schema.Generated') {
  // Generate properties with backing fields
  const propertiesCode = generateProperties(schema.properties || {}, schema.required || []);
  
  // Generate PopulateFromJson method with assignments
  const populateCode = generatePopulateMethod(schema.properties || {}, className);
  
  return `using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

#nullable enable

namespace ${namespace}
{
    /// <summary>
    /// ${schema.description || 'Schema for ' + className}
    /// </summary>
    public partial class ${className} : ScriptableObject
    {
${propertiesCode}

${populateCode}
    }
}
`;
}

/**
 * Generate C# properties from schema properties with backing fields
 */
function generateProperties(properties, required) {
  let code = '';
  
  for (const [propName, propSchema] of Object.entries(properties)) {
    // Add property description
    if (propSchema.description) {
      code += `        /// <summary>\n`;
      code += `        /// ${propSchema.description}\n`;
      code += `        /// </summary>\n`;
    }
    
    // Add JsonProperty attribute for serialization
    code += `        [JsonProperty("${propName}")]\n`;
    
    // Determine C# type
    const propType = getPropertyType(propSchema, propName);
    const isRequired = required.includes(propName);
    const nullableSuffix = !isRequired ? '?' : '';
    
    // Generate backing field with SerializeField attribute
    const backingFieldName = `_${propName}`;
    code += `        [SerializeField] private ${propType}${nullableSuffix} ${backingFieldName};\n`;
    
    // Generate property with PascalCase name
    const pascalPropName = toPascalCase(propName);
    code += `        public ${propType}${nullableSuffix} ${pascalPropName} { get => ${backingFieldName}; set => ${backingFieldName} = value; }\n\n`;
  }
  
  return code;
}

/**
 * Generate a PopulateFromJson method to copy data from deserialized JSON
 */
function generatePopulateMethod(properties, className) {
  let code = `        /// <summary>\n`;
  code += `        /// Populate this object from JSON deserialization\n`;
  code += `        /// </summary>\n`;
  code += `        public void PopulateFromJson(CraftSpace.Models.Schema.Generated.${className} jsonData)\n`;
  code += `        {\n`;
  
  // Generate assignments for all properties
  for (const propName of Object.keys(properties)) {
    const pascalPropName = toPascalCase(propName);
    code += `            _${propName} = jsonData.${pascalPropName};\n`;
  }
  
  // Add notification
  code += `\n            // Notify views of update\n`;
  code += `            NotifyViewsOfUpdate();\n`;
  code += `        }\n`;
  
  return code;
}

/**
 * Map JSON schema type to C# type
 */
function getPropertyType(property, propName) {
  if (property.type === 'array') {
    if (property.items && property.items.type === 'object') {
      return 'List<Dictionary<string, object>>';
    } else if (property.items && property.items.type === 'string') {
      return 'List<string>';
    } else {
      return 'List<object>';
    }
  } else if (property.type === 'object') {
    return 'Dictionary<string, object>';
  } else if (property.type === 'string') {
    return 'string';
  } else if (property.type === 'number') {
    return 'float';
  } else if (property.type === 'integer') {
    return 'int';
  } else if (property.type === 'boolean') {
    return 'bool';
  } else if (property.anyOf || property.oneOf) {
    return 'object';
  } else {
    return 'object';
  }
}

// Helper function to convert to PascalCase
function toPascalCase(str) {
  return str
    .split('_')
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');
} 