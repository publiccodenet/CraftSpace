#!/usr/bin/env node
/**
 * Minimal schema copy script using TypeScript
 */
import fs from 'fs-extra';
import path from 'path';
import glob from 'glob';

const copySchemas = async () => {
  console.log('🚀 COPYING SCHEMAS TO UNITY (MINIMAL)');
  
  try {
    // Source directory (exported schemas)
    const sourceDir = path.resolve('./exports/schemas');
    
    // Target directory (Unity schemas folder)
    const targetDir = path.resolve('../Unity/CraftSpace/Assets/Schemas');
    
    // Ensure the target directory exists
    await fs.ensureDir(targetDir);
    
    console.log(`Source directory: ${sourceDir}`);
    console.log(`Target directory: ${targetDir}`);
    
    // Find all JSON schema files
    const schemaFiles = glob.sync('*.json', { cwd: sourceDir });
    
    console.log(`Found ${schemaFiles.length} schema files to copy`);
    
    // Copy each schema file
    for (const file of schemaFiles) {
      const sourcePath = path.join(sourceDir, file);
      const targetPath = path.join(targetDir, file);
      
      await fs.copy(sourcePath, targetPath, { overwrite: true });
      console.log(`Copied schema: ${file}`);
    }
    
    console.log('SCHEMA COPY COMPLETE!');
  } catch (error) {
    console.error('Error copying schemas:', error);
    process.exit(1);
  }
};

copySchemas().catch(err => {
  console.error('Unhandled error:', err);
  process.exit(1);
}); 