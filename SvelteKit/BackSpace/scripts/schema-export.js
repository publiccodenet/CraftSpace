#!/usr/bin/env node
/**
 * Export Zod schemas to JSON Schema format
 * 
 * Usage:
 *   npm run schema:export
 */
import fs from 'fs-extra';
import path from 'path';
import { zodToJsonSchema } from 'zod-to-json-schema';
import { EMOJI, PATHS } from '../src/lib/constants/index.ts';

// Import schemas
import { CollectionSchema } from '../src/lib/schemas/collection.ts';
import { ItemSchema } from '../src/lib/schemas/item.ts';

// Use path constant
const outputDir = PATHS.SCHEMAS_DIR;

// Add debug output
console.log(`${EMOJI.START} EXPORTING ZOD SCHEMAS TO JSON SCHEMAS`);
console.log(`BackSpace dir: ${PATHS.BACKSPACE_DIR}`);
console.log(`Content dir: ${PATHS.CONTENT_DIR}`);
console.log(`Output directory: ${outputDir}`);

// Ensure output directory exists
fs.ensureDirSync(outputDir);

// Process specific schemas
const schemas = [
  { name: 'CollectionSchema', schema: CollectionSchema },
  { name: 'ItemSchema', schema: ItemSchema }
];

console.log(`Found ${schemas.length} schemas to export`);

// Export each schema
for (const { name, schema } of schemas) {
  try {
    console.log(`Processing ${name}...`);
    
    // First, get the schema with definitions to extract the structure
    const tempSchema = zodToJsonSchema(schema, {
      name,
      $refStrategy: 'none',
      definitions: true
    });
    
    // Extract the definition name from the $ref
    const refName = tempSchema.$ref?.replace('#/definitions/', '');
    
    // Now create a clean schema with the properties directly at the root
    let finalSchema;
    
    if (refName && tempSchema.definitions && tempSchema.definitions[refName]) {
      // Get the main definition
      const mainDef = tempSchema.definitions[refName];
      
      // Create a new schema with properties at the root level
      finalSchema = {
        $schema: "http://json-schema.org/draft-07/schema#",
        type: "object",
        properties: mainDef.properties || {},
        required: mainDef.required || [],
        additionalProperties: false,
        description: `Schema for ${name.replace(/Schema$/, '')}`
      };
      
      console.log(`Extracted properties from definition: ${refName}`);
    } else {
      console.warn(`Warning: Could not find proper structure for ${name}`);
      
      // Fallback: generate a simpler schema without $ref
      finalSchema = zodToJsonSchema(schema, {
        $refStrategy: 'replace',
        target: 'openApi3'
      });
      
      // Ensure root level properties
      finalSchema.$schema = "http://json-schema.org/draft-07/schema#";
      finalSchema.description = `Schema for ${name.replace(/Schema$/, '')}`;
      finalSchema.additionalProperties = false;
    }
    
    // Output filename based on schema name
    const outputFile = path.join(
      outputDir, 
      `${name.replace(/Schema$/, '')}.json`
    );
    
    // Write schema to file
    fs.writeFileSync(
      outputFile, 
      JSON.stringify(finalSchema, null, 2)
    );
    
    console.log(`Exported schema: ${name} → ${outputFile}`);
    
    // Debug output
    if (process.env.DEBUG) {
      console.log('Schema Structure:');
      console.log(JSON.stringify(finalSchema, null, 2));
    }
  } catch (error) {
    console.error(`Error exporting schema ${name}:`, error);
  }
}

console.log('SCHEMA EXPORT COMPLETE!'); 