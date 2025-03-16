#!/usr/bin/env node
/**
 * Debug JSON Schema issues
 * 
 * This script checks and validates the exported schemas to make sure
 * they have the correct structure for Unity's schema importer.
 */
import fs from 'fs-extra';
import path from 'path';
import { PATHS } from '../src/lib/constants/index.ts';

// Use path constant
const schemaDir = PATHS.SCHEMAS_DIR;

console.log('🔍 DEBUGGING JSON SCHEMAS');
console.log(`Schema directory: ${schemaDir}`);

// Find all schema files
const schemaFiles = fs.readdirSync(schemaDir)
  .filter(file => file.endsWith('.json'));

console.log(`Found ${schemaFiles.length} schema files to check`);

// Check each schema
for (const file of schemaFiles) {
  const schemaPath = path.join(schemaDir, file);
  console.log(`\nChecking schema: ${file}...`);
  
  try {
    const schema = fs.readJSONSync(schemaPath);
    
    // Check for required fields
    const requiredFields = ['$schema', 'type', 'properties'];
    const missingFields = requiredFields.filter(field => !schema[field]);
    
    if (missingFields.length > 0) {
      console.log(`⚠️ Missing required fields: ${missingFields.join(', ')}`);
    }
    
    // Check if properties exist and have the right format
    if (!schema.properties || Object.keys(schema.properties).length === 0) {
      console.log('❌ No properties defined in schema!');
    } else {
      console.log(`✅ Found ${Object.keys(schema.properties).length} properties`);
      
      // Check each property
      for (const [propName, propDef] of Object.entries(schema.properties)) {
        console.log(`  - ${propName}: ${propDef.type}`);
        
        // Check for missing type
        if (!propDef.type) {
          console.log(`    ❌ Missing type for property ${propName}`);
        }
        
        // Check for descriptions
        if (!propDef.description) {
          console.log(`    ⚠️ No description for property ${propName}`);
        }
      }
    }
    
    // Check for Unity-specific metadata
    if (!schema.description) {
      console.log('⚠️ Missing schema description');
    }
    
  } catch (error) {
    console.error(`❌ Error reading schema ${file}:`, error.message);
  }
}

console.log('\nSCHEMA DEBUG COMPLETE!'); 