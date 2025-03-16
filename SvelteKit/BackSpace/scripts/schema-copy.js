#!/usr/bin/env node
/**
 * Schema copy script - Copies schemas to the correct Unity location
 */
import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import glob from 'glob';
import { PATHS } from '../src/lib/constants/index.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Source directory (schemas in Content)
const sourceDir = PATHS.SCHEMAS_DIR;

// Target directory (Unity schemas folder)
const targetDir = PATHS.CRAFTSPACE_CONTENT_SCHEMAS_DIR;

console.log('🚀 COPYING SCHEMAS TO UNITY CONTENT MIRROR');
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