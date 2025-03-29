#!/usr/bin/env node
/**
 * Export Zod schemas DIRECTLY to Content/schema at top level of repo
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

// Output directly to Content/schema at the top level of the repo
const outputDir = PATHS.CONTENT_SCHEMAS_DIR;

// Add debug output
console.log(`${EMOJI.START} EXPORTING ZOD SCHEMAS DIRECTLY TO CONTENT/SCHEMA`);
console.log(`Output directory: ${outputDir}`);

// Ensure output directory exists
fs.ensureDirSync(outputDir);

// Process specific schemas
const schemas = [
  { name: 'CollectionSchema', schema: CollectionSchema, outputName: 'Collection' },
  { name: 'ItemSchema', schema: ItemSchema, outputName: 'Item' }
];

console.log(`Found ${schemas.length} schemas to export`);

// Export each schema
for (const { name, schema, outputName } of schemas) {
  try {
    console.log(`Processing ${name}...`);
    
    // Convert schema to JSON Schema format
    const jsonSchema = zodToJsonSchema(schema, {
      $refStrategy: 'replace',
      target: 'openApi3'
    });
    
    // Ensure required JSON Schema properties
    jsonSchema.$schema = "http://json-schema.org/draft-07/schema#";
    jsonSchema.description = `Schema for ${outputName}`;
    jsonSchema.additionalProperties = false;

    // Output filename
    const outputFile = path.join(outputDir, `${outputName}.json`);
    
    // Write schema to file
    fs.writeFileSync(
      outputFile, 
      JSON.stringify(jsonSchema, null, 2)
    );
    
    console.log(`Exported schema: ${name} → ${outputFile}`);
  } catch (error) {
    console.error(`Error exporting schema ${name}:`, error);
  }
}

console.log('SCHEMA EXPORT COMPLETE!'); 