#!/usr/bin/env node

/**
 * pipeline-clean.js
 * 
 * Script to clean all generated data.
 * This script:
 * 1. Removes all downloaded collections from SvelteKit static directory
 * 2. Removes all collections from Unity Resources directory
 * 3. Optionally preserves configuration files
 * 
 * Usage:
 * node pipeline-clean.js [--keep-config]
 */

const fs = require('fs-extra');
const path = require('path');
const chalk = require('chalk');

// Configuration
const PROJECT_ROOT = path.resolve(__dirname, '../../..');
const DATA_DIR = path.join(__dirname, '../static/data');
const UNITY_DIR = path.join(PROJECT_ROOT, 'Unity/CraftSpace/Assets/Resources/Collections');

// Parse command line arguments
const args = process.argv.slice(2);
const keepConfig = args.includes('--keep-config');

// Main function
async function cleanPipeline() {
  console.log(chalk.bold.blue('===== CLEANING DATA PIPELINE ====='));
  
  try {
    // 1. Clean SvelteKit static data directory
    console.log(chalk.blue('\n=== CLEANING SVELTEKIT DATA ==='));
    if (await fs.pathExists(DATA_DIR)) {
      await fs.emptyDir(DATA_DIR);
      console.log(chalk.green('✓ Cleared SvelteKit static data directory'));
    } else {
      console.log(chalk.dim('No SvelteKit data directory found'));
    }
    
    // 2. Clean Unity Resources directory
    console.log(chalk.blue('\n=== CLEANING UNITY RESOURCES ==='));
    if (await fs.pathExists(UNITY_DIR)) {
      if (keepConfig) {
        // Keep index.json if it exists
        const indexPath = path.join(UNITY_DIR, 'index.json');
        let indexContent = null;
        
        if (await fs.pathExists(indexPath)) {
          try {
            indexContent = await fs.readJson(indexPath);
            console.log(chalk.dim('Preserving Unity index.json'));
          } catch (error) {
            console.log(chalk.yellow('Could not read Unity index.json, it will be removed'));
          }
        }
        
        // Remove directory contents
        await fs.emptyDir(UNITY_DIR);
        
        // Restore index if it existed
        if (indexContent) {
          await fs.writeJson(indexPath, indexContent, { spaces: 2 });
        }
      } else {
        // Complete removal
        await fs.emptyDir(UNITY_DIR);
      }
      console.log(chalk.green('✓ Cleared Unity Resources directory'));
    } else {
      console.log(chalk.dim('No Unity Resources directory found'));
    }
    
    console.log(chalk.bold.green('\n===== CLEAN COMPLETE ====='));
    console.log('All collection data has been removed.');
    console.log(chalk.dim('Next steps:'));
    console.log(chalk.dim('- To rebuild everything: npm run pipeline:full'));
    console.log(chalk.dim('- To build only what changed: npm run pipeline:incremental'));
  } catch (error) {
    console.error(chalk.red('Clean operation failed:'), error);
    process.exit(1);
  }
}

// Execute the script
cleanPipeline().catch(error => {
  console.error(chalk.red('Pipeline cleanup failed:'), error);
  process.exit(1); 