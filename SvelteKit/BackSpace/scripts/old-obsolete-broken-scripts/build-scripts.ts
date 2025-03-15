#!/usr/bin/env node

import { execSync } from 'child_process';
import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';

// Get current directory
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Ensure dist directory exists
const distDir = path.resolve(__dirname, '../dist-scripts');
fs.ensureDirSync(distDir);

// Compile TypeScript scripts
console.log('Compiling TypeScript scripts...');
try {
  execSync('npx tsc --project scripts/tsconfig.json', { stdio: 'inherit' });
  console.log('Scripts compiled successfully!');
} catch (error) {
  console.error('Failed to compile scripts:', error);
  process.exit(1);
} 