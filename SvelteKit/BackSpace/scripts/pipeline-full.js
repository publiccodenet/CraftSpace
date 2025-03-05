#!/usr/bin/env node

/**
 * pipeline-full.js
 * 
 * Main script to rebuild the entire data pipeline from scratch.
 * This script orchestrates the complete process of:
 * 1. Reading the collections configuration
 * 2. Downloading data from Internet Archive
 * 3. Processing metadata and images
 * 4. Generating texture atlases
 * 5. Syncing with Unity project
 */

const fs = require('fs-extra');
const path = require('path');
const { execSync } = require('child_process');
const chalk = require('chalk');

// Configuration
const PROJECT_ROOT = path.resolve(__dirname, '../../..');
const COLLECTIONS_CONFIG = path.join(PROJECT_ROOT, 'collections.json');
const DATA_DIR = path.join(__dirname, '../static/data');
const UNITY_COLLECTIONS_DIR = path.join(PROJECT_ROOT, 'Unity/CraftSpace/Assets/Resources/Collections');

// Helper function to run a command and log the output
function runCommand(command, message) {
  console.log(chalk.blue('→ ') + message);
  try {
    execSync(command, { stdio: 'inherit' });
    console.log(chalk.green('✓ ') + 'Completed: ' + message);
  } catch (error) {
    console.error(chalk.red('✗ ') + 'Failed: ' + message);
    console.error(error.message);
    process.exit(1);
  }
}

// Main function to execute the full pipeline
async function runFullPipeline() {
  console.log(chalk.bold.blue('===== STARTING FULL DATA PIPELINE ====='));
  console.log('This will rebuild all collections from scratch. It may take a while.');

  // 1. Clean any existing data
  console.log(chalk.blue('\n=== CLEANING EXISTING DATA ==='));
  await fs.emptyDir(DATA_DIR);
  console.log(chalk.green('✓ ') + 'Cleared static data directory');

  // 2. Ensure collections.json exists
  if (!await fs.pathExists(COLLECTIONS_CONFIG)) {
    console.error(chalk.red('✗ ') + 'collections.json not found at: ' + COLLECTIONS_CONFIG);
    console.log('Please create a collections configuration file. See documentation for format.');
    process.exit(1);
  }

  // 3. Download all collections
  console.log(chalk.blue('\n=== DOWNLOADING COLLECTIONS ==='));
  runCommand('node scripts/download-collections.js', 'Download collections from Internet Archive');

  // 4. Generate texture atlases for all collections
  console.log(chalk.blue('\n=== GENERATING TEXTURE ATLASES ==='));
  runCommand('node scripts/generate-atlases.js', 'Generate texture atlases');

  // 5. Sync with Unity project
  console.log(chalk.blue('\n=== SYNCING WITH UNITY ==='));
  runCommand('node scripts/sync-unity-collections.js', 'Sync collections to Unity');

  // 6. Final notification
  console.log(chalk.bold.green('\n===== PIPELINE COMPLETE ====='));
  console.log('Data pipeline has been fully rebuilt.');
  console.log(chalk.dim('Next steps:'));
  console.log(chalk.dim('- To build the Unity WebGL application: npm run build:unity'));
  console.log(chalk.dim('- To build the SvelteKit application: npm run build'));
  console.log(chalk.dim('- To run the full application: npm run build:all && npm run preview'));
}

// Run the pipeline
runFullPipeline().catch(error => {
  console.error(chalk.red('Pipeline failed:'), error);
  process.exit(1);
}); 