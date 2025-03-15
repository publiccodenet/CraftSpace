#!/usr/bin/env node
/**
 * Export Zod schemas to JSON Schema format
 * 
 * Usage:
 *   npm run schema:export
 */
import fs from 'fs-extra';
import path from 'path';
import chalk from 'chalk';
import { zodToJsonSchema } from 'zod-to-json-schema';
import { EMOJI } from '../src/lib/constants/index.js';

// Import schemas directly from their source files to avoid index.ts issues
import { CollectionSchema } from '../src/lib/schemas/collection.js';
import { ItemSchema } from '../src/lib/schemas/item.js';

// Simple command without yargs to eliminate that as a potential issue
async function exportSchemas() {
  console.log(`${EMOJI?.START || '🚀'} EXPORTING SCHEMAS`);
  
  try {
    const outputDir = path.resolve('./exports/schemas');
    
    // Ensure output directory exists
    await fs.ensureDir(outputDir);
    
    console.log(`Output directory: ${outputDir}`);
    
    // Test with just Collection and Item schemas first
    const schemas = [
      { name: 'CollectionSchema', schema: CollectionSchema },
      { name: 'ItemSchema', schema: ItemSchema }
    ];
    
    console.log(`Found ${schemas.length} schemas to export`);
    
    // Process each schema
    for (const { name, schema } of schemas) {
      try {
        console.log(`Processing ${name}...`);
        
        // Debug log the schema to see what we're working with
        console.log(`Schema type: ${typeof schema}`);
        console.log(`Has parse method: ${typeof schema?.parse === 'function'}`);
        
        // Convert Zod schema to JSON Schema
        const jsonSchema = zodToJsonSchema(schema, {
          name, // Use schema name as the schema ID
          $refStrategy: 'none', // Don't use $ref for now for simplicity
        });
        
        // Output filename based on schema name
        const outputFile = path.join(
          outputDir, 
          `${name.replace(/Schema$/, '')}.json`
        );
        
        // Write schema to file
        await fs.writeFile(
          outputFile, 
          JSON.stringify(jsonSchema, null, 2)
        );
        
        console.log(`Exported schema: ${name} → ${outputFile}`);
      } catch (schemaError) {
        console.error(`Could not export schema ${name}:`, schemaError);
      }
    }
    
    console.log(`SCHEMA EXPORT COMPLETE!`);
  } catch (error) {
    console.error(`Error exporting schemas:`, error);
    process.exit(1);
  }
}

// Run the function directly without command parsing
exportSchemas().catch(err => {
  console.error('Unhandled error:', err);
  process.exit(1);
}); 