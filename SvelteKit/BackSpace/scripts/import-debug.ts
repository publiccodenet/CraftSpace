#!/usr/bin/env node
/**
 * Debug import issues
 */
import fs from 'fs-extra';
import path from 'path';
import chalk from 'chalk';

console.log(chalk.blue('Running import diagnostics...'));

// Check if Content directory exists
const contentDir = path.resolve('Content');
console.log(`Content directory exists: ${fs.existsSync(contentDir)}`);

// Check sample collection
const sampleCollectionPath = path.join(contentDir, 'collections', 'sample', 'collection.json');
console.log(`Sample collection exists: ${fs.existsSync(sampleCollectionPath)}`);

console.log('\nChecking script imports:');
const collectionsScriptPath = path.resolve('scripts', 'manage-collections.ts');
const content = fs.readFileSync(collectionsScriptPath, 'utf8');

// Identify all import statements
const importRegex = /import .* from ['"](.*)['"]/g;
const imports = [];
let match;
while ((match = importRegex.exec(content)) !== null) {
  imports.push(match[1]);
}

console.log('\nImports found:');
imports.forEach(imp => {
  console.log(`- ${imp}`);
});

// Check for double .js
const doubleJs = imports.filter(imp => imp.endsWith('.js.js'));
if (doubleJs.length > 0) {
  console.log(chalk.red('\nDouble .js extensions found:'));
  doubleJs.forEach(imp => console.log(`- ${imp}`));
}

// Let's also check for missing modules
try {
  console.log('\nTrying to import content manager directly:');
  // Attempt to directly import the content manager (but not execute anything)
  import('../src/lib/content/index.js')
    .then(module => {
      console.log(chalk.green('✅ Content manager module loaded successfully'));
      console.log('Exported keys:', Object.keys(module));
    })
    .catch(err => {
      console.log(chalk.red('❌ Failed to import content manager:'));
      console.error(err);
    });
} catch (err) {
  console.log(chalk.red('❌ Error importing content manager:'));
  console.error(err);
}

console.log(chalk.blue('\nDiagnostics complete. Check errors above.'));
