#!/usr/bin/env node
/**
 * Debug path constants to find issues
 */
import path from 'path';
import { fileURLToPath } from 'url';
import { PATHS } from '../src/lib/constants/index.ts';
import fs from 'fs-extra';

// Debug script current path
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

console.log('--- PATH DEBUG ---');
console.log('Script location:', __dirname);
console.log('REPO_ROOT:', PATHS.REPO_ROOT);
console.log('BACKSPACE_DIR:', PATHS.BACKSPACE_DIR);
console.log('UNITY_DIR:', PATHS.UNITY_DIR);
console.log('CRAFTSPACE_DIR:', PATHS.CRAFTSPACE_DIR);
console.log('CRAFTSPACE_SCRIPTS_DIR:', PATHS.CRAFTSPACE_SCRIPTS_DIR);
console.log('SCHEMAS_DIR:', PATHS.SCHEMAS_DIR);
console.log('CRAFTSPACE_CONTENT_SCHEMAS_DIR:', PATHS.CRAFTSPACE_CONTENT_SCHEMAS_DIR);
console.log('CRAFTSPACE_GENERATED_SCHEMAS_DIR:', PATHS.CRAFTSPACE_GENERATED_SCHEMAS_DIR);

// Check critical paths
console.log('\n--- CRITICAL PATHS ---');
const criticalPaths = {
  'BACKSPACE_DIR': PATHS.BACKSPACE_DIR,
  'CRAFTSPACE_DIR': PATHS.CRAFTSPACE_DIR,
  'CONTENT_DIR': PATHS.CONTENT_DIR,
  'COLLECTIONS_DIR': PATHS.COLLECTIONS_DIR,
  'SCHEMAS_DIR': PATHS.SCHEMAS_DIR,
  'CRAFTSPACE_SCHEMAS_DIR': PATHS.CRAFTSPACE_SCHEMAS_DIR,
  'CRAFTSPACE_SCRIPTS_DIR': PATHS.CRAFTSPACE_SCRIPTS_DIR,
  'CRAFTSPACE_CONTENT_SCHEMAS_DIR': PATHS.CRAFTSPACE_CONTENT_SCHEMAS_DIR,
  'CRAFTSPACE_GENERATED_SCHEMAS_DIR': PATHS.CRAFTSPACE_GENERATED_SCHEMAS_DIR
};

// Verify each path
Object.entries(criticalPaths).forEach(([name, pathValue]) => {
  const exists = fs.existsSync(pathValue);
  console.log(`${name}: ${exists ? '✅' : '❌'} ${pathValue}`);
  
  // If missing, suggest how to fix
  if (!exists) {
    console.log(`  📂 Missing directory: ${pathValue}`);
    console.log(`  💡 Create with: mkdir -p "${pathValue}"`);
  }
});

// Test generated model paths
console.log('\n--- SCHEMA PIPELINE PATHS ---');
const modelPath = path.join(PATHS.CRAFTSPACE_SCRIPTS_DIR, 'Models', 'Generated');
console.log(`Models directory: ${fs.existsSync(modelPath) ? '✅' : '❌'} ${modelPath}`);

if (!fs.existsSync(modelPath)) {
  console.log(`  📂 Create with: mkdir -p "${modelPath}"`);
}

// Suggest commands to regenerate
console.log('\n--- REGENERATION COMMANDS ---');
console.log('1. Generate JSON schemas:');
console.log('   npm run schema:export');
console.log('2. Copy schemas to Unity Content mirror:');
console.log('   npm run schema:copy');
console.log('3. Generate C# classes:');
console.log('   npm run schema:csharp');
console.log('4. Or run everything:');
console.log('   npm run schema:generate-all'); 