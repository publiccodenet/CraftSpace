#!/usr/bin/env node

/**
 * download-collections.js
 * 
 * Script to download collections from Internet Archive based on collections.json.
 * This script:
 * 1. Reads the collections configuration file
 * 2. Processes each collection entry
 * 3. Downloads items according to each query
 * 4. Creates the necessary directory structure
 */

const fs = require('fs-extra');
const path = require('path');
const crypto = require('crypto');
const chalk = require('chalk');
const { execSync } = require('child_process');

// Configuration
const PROJECT_ROOT = path.resolve(__dirname, '../../..');
const COLLECTIONS_CONFIG = path.join(PROJECT_ROOT, 'collections.json');
const OUTPUT_DIR = path.join(__dirname, '../static/data');

// Read collections configuration
async function readCollectionsConfig() {
  try {
    const configExists = await fs.pathExists(COLLECTIONS_CONFIG);
    if (!configExists) {
      console.error(chalk.red('Error: collections.json not found at:'), COLLECTIONS_CONFIG);
      process.exit(1);
    }
    
    const configData = await fs.readJson(COLLECTIONS_CONFIG);
    return configData.collections || [];
  } catch (error) {
    console.error(chalk.red('Error reading collections configuration:'), error);
    process.exit(1);
  }
}

// Generate a hash-based prefix for dynamic collections
function generateDynamicPrefix(query) {
  const hash = crypto.createHash('md5').update(query).digest('hex').substring(0, 6);
  return `dyn_${hash}`;
}

// Process a single collection
async function processCollection(collection) {
  try {
    console.log(chalk.blue(`\nProcessing collection: ${collection.name}`));
    
    // For dynamic collections, generate a prefix if not provided
    let prefix = collection.prefix;
    if (collection.dynamic && !prefix) {
      prefix = generateDynamicPrefix(collection.query);
      console.log(chalk.dim(`Generated dynamic prefix: ${prefix}`));
    }
    
    // Build command for download-items.js
    const maxItems = collection.maxItems ? `--max-items=${collection.maxItems}` : '';
    const includeInUnity = collection.includeInUnity ? '--include-in-unity=true' : '--include-in-unity=false';
    const sortBy = collection.sortBy ? `--sort-by=${collection.sortBy}` : '';
    const sortDirection = collection.sortDirection ? `--sort-direction=${collection.sortDirection}` : '';
    
    // Sanitize query for command line
    const query = collection.query.replace(/"/g, '\\"');
    
    // Build and execute command
    const command = `node dist-scripts/download-items.js "${OUTPUT_DIR}" "${prefix}" "${query}" ${maxItems} ${includeInUnity} ${sortBy} ${sortDirection}`;
    
    console.log(chalk.dim(`Executing: ${command}`));
    execSync(command, { stdio: 'inherit' });
    
    console.log(chalk.green(`✓ Completed download for ${collection.name} (${prefix})`));
    return { success: true, prefix };
  } catch (error) {
    console.error(chalk.red(`✗ Failed to process collection ${collection.name}:`), error);
    return { success: false, prefix: collection.prefix };
  }
}

// Main function
async function downloadAllCollections() {
  try {
    console.log(chalk.bold.blue('===== COLLECTION DOWNLOAD PROCESS ====='));
    
    // Ensure output directory exists
    await fs.ensureDir(OUTPUT_DIR);
    
    // Read collections configuration
    const collections = await readCollectionsConfig();
    console.log(chalk.dim(`Found ${collections.length} collections in configuration`));
    
    // Process each collection
    const results = [];
    for (const collection of collections) {
      const result = await processCollection(collection);
      results.push({ ...collection, ...result });
    }
    
    // Summary
    console.log(chalk.blue('\n=== DOWNLOAD SUMMARY ==='));
    const successful = results.filter(r => r.success).length;
    console.log(`Total collections: ${results.length}`);
    console.log(`Successfully downloaded: ${successful}`);
    console.log(`Failed: ${results.length - successful}`);
    
    if (successful > 0) {
      console.log(chalk.blue('\nSuccessfully downloaded collections:'));
      results.filter(r => r.success).forEach(c => {
        console.log(`- ${c.name} (${c.prefix})`);
      });
    }
    
    if (successful < results.length) {
      console.log(chalk.yellow('\nFailed collections:'));
      results.filter(r => !r.success).forEach(c => {
        console.log(`- ${c.name} (${c.prefix})`);
      });
    }
    
    console.log(chalk.bold.green('\n===== DOWNLOAD PROCESS COMPLETE ====='));
  } catch (error) {
    console.error(chalk.red('Error in download process:'), error);
    process.exit(1);
  }
}

// Execute the main function
downloadAllCollections().catch(error => {
  console.error(chalk.red('Download process failed:'), error);
  process.exit(1);
}); 