#!/usr/bin/env node
/**
 * Copy exported schema files to Unity project
 * 
 * Usage:
 *   npm run schema:copy-to-unity
 */
import fs from 'fs-extra';
import path from 'path';
import glob from 'glob';
import { EMOJI } from '../src/lib/constants/index.js';

async function copySchemas() {
  console.log(`${EMOJI.START} COPYING SCHEMAS TO UNITY ${EMOJI.FILE}`);
  
  try {
    // Source directory (exported schemas)
    const sourceDir = path.resolve('./exports/schemas');
    
    // Target directory (Unity schemas folder)
    const targetDir = path.resolve('../Unity/CraftSpace/Assets/Schemas');
    
    // Ensure the target directory exists
    await fs.ensureDir(targetDir);
    
    console.log(`${EMOJI.FOLDER} Source directory: ${sourceDir}`);
    console.log(`${EMOJI.FOLDER} Target directory: ${targetDir}`);
    
    // Find all JSON schema files
    const schemaFiles = glob.sync('*.json', { cwd: sourceDir });
    
    console.log(`${EMOJI.INFO} Found ${schemaFiles.length} schema files to copy`);
    
    // Copy each schema file
    for (const file of schemaFiles) {
      const sourcePath = path.join(sourceDir, file);
      const targetPath = path.join(targetDir, file);
      
      await fs.copy(sourcePath, targetPath, { overwrite: true });
      console.log(`${EMOJI.SUCCESS} Copied schema: ${file}`);
    }
    
    console.log(`${EMOJI.FINISH} SCHEMA COPY COMPLETE! ${EMOJI.SUCCESS}`);
  } catch (error) {
    console.error(`${EMOJI.ERROR} Error copying schemas:`, error);
    process.exit(1);
  }
}

// Run the function directly without command parsing
copySchemas().catch(err => {
  console.error('Unhandled error:', err);
  process.exit(1);
}); 