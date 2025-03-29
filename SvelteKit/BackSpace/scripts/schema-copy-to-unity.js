#!/usr/bin/env node
/**
 * Copy schemas from Content/schema to Unity's StreamingAssets/Content/schemas directory
 * 
 * Usage:
 *   npm run schema:copy-to-unity
 */
import fs from 'fs-extra';
import path from 'path';
import { EMOJI, PATHS } from '../src/lib/constants/index.ts';

// Source and target directories
const sourceDir = PATHS.CONTENT_SCHEMAS_DIR;  // Content/schema at repo root
const targetDir = PATHS.UNITY_SCHEMAS_DIR;    // Unity StreamingAssets location

// Add debug output
console.log(`${EMOJI.START} COPYING SCHEMAS TO UNITY`);
console.log(`Source: Content/schema (top level)`);
console.log(`Target: Unity StreamingAssets/Content/schemas`);
console.log('----------------------------------------');

console.log('Source directory:', path.relative(PATHS.ROOT_DIR, sourceDir));
console.log('Target directory:', path.relative(PATHS.ROOT_DIR, targetDir));

// Create target directory if it doesn't exist
if (!fs.existsSync(targetDir)) {
    console.log('Creating Unity schemas directory...');
    fs.mkdirSync(targetDir, { recursive: true });
}

// Get all JSON files in source directory
const schemaFiles = fs.readdirSync(sourceDir).filter(file => file.endsWith('.json'));
console.log(`\nFound ${schemaFiles.length} schema files to copy:`);

// Copy each file to Unity location
schemaFiles.forEach(file => {
    const sourcePath = path.join(sourceDir, file);
    const targetPath = path.join(targetDir, file);
    fs.copyFileSync(sourcePath, targetPath);
    console.log(`✓ ${file} → Unity StreamingAssets`);
});

console.log('\n✨ SCHEMA COPY TO UNITY COMPLETE!'); 