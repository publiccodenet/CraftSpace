#!/usr/bin/env node
/**
 * Simple schema copy script using ES modules
 */
import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import glob from 'glob';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Source directory (exported schemas)
const sourceDir = path.resolve('./exports/schemas');

// Target directory (Unity schemas folder)
const targetDir = path.resolve('../Unity/CraftSpace/Assets/Schemas');

console.log('🚀 COPYING SCHEMAS TO UNITY');
console.log(`Source directory: ${sourceDir}`);
console.log(`Target directory: ${targetDir}`);

// Ensure the target directory exists
fs.ensureDirSync(targetDir);

// Find all JSON schema files
const schemaFiles = glob.sync('*.json', { cwd: sourceDir });

console.log(`Found ${schemaFiles.length} schema files to copy`);

// Copy each schema file
for (const file of schemaFiles) {
  const sourcePath = path.join(sourceDir, file);
  const targetPath = path.join(targetDir, file);
  
  fs.copySync(sourcePath, targetPath, { overwrite: true });
  console.log(`Copied schema: ${file}`);
}

console.log('SCHEMA COPY COMPLETE!'); 