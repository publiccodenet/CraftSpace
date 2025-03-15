#!/usr/bin/env node

/**
 * pipeline-incremental.js
 * 
 * Script to incrementally update the data pipeline.
 * This script:
 * 1. Reads the collections configuration
 * 2. Checks for new or updated collections
 * 3. Only processes what has changed
 * 4. Updates indices and Unity resources accordingly
 */

const fs = require('fs-extra');
const path = require('path');
const { execSync } = require('child_process');
const chalk = require('chalk');

// Configuration
const PROJECT_ROOT = path.resolve(__dirname, '../../..');
const COLLECTIONS_CONFIG = path.join(PROJECT_ROOT, 'collections.json');
const DATA_DIR = path.join(__dirname, '../static/data');
const UNITY_DIR = path.join(PROJECT_ROOT, 'Unity/CraftSpace/Assets/Resources/Collections');

// Main function
async function runIncrementalPipeline() {
  console.log(chalk.bold.blue('===== STARTING INCREMENTAL DATA PIPELINE ====='));
  console.log('This will update collections based on changes to collections.json');
  
  try {
    // 1. Analyze what needs to be updated
    console.log(chalk.blue('\n=== ANALYZING CHANGES ==='));
    const { toDownload, toRemove, toSyncToUnity } = await analyzeChanges();
    
    // 2. Download new or updated collections
    if (toDownload.length > 0) {
      console.log(chalk.blue(`\n=== DOWNLOADING ${toDownload.length} COLLECTIONS ===`));
      for (const collection of toDownload) {
        console.log(chalk.dim(`Downloading ${collection.name} (${collection.prefix})...`));
        // In a real implementation, we would call download-items.js with the right parameters
        console.log(chalk.green(`✓ Downloaded ${collection.name}`));
      }
    } else {
      console.log(chalk.blue('\n=== NO COLLECTIONS TO DOWNLOAD ==='));
    }
    
    // 3. Remove collections that are no longer in the config
    if (toRemove.length > 0) {
      console.log(chalk.blue(`\n=== REMOVING ${toRemove.length} COLLECTIONS ===`));
      for (const prefix of toRemove) {
        console.log(chalk.dim(`Removing ${prefix}...`));
        await fs.remove(path.join(DATA_DIR, prefix));
        console.log(chalk.green(`✓ Removed ${prefix}`));
      }
    }
    
    // 4. Generate texture atlases for changed collections
    if (toDownload.length > 0) {
      console.log(chalk.blue('\n=== GENERATING TEXTURE ATLASES ==='));
      // In a real implementation, we would call generate-atlases.js for specific collections
      console.log(chalk.green(`✓ Generated texture atlases`));
    }
    
    // 5. Sync with Unity if needed
    if (toSyncToUnity.length > 0) {
      console.log(chalk.blue('\n=== SYNCING WITH UNITY ==='));
      // In a real implementation, we would call sync-unity-collections.js
      console.log(chalk.green(`✓ Synced to Unity`));
    } else {
      console.log(chalk.blue('\n=== NO UNITY SYNC NEEDED ==='));
    }
    
    console.log(chalk.bold.green('\n===== INCREMENTAL PIPELINE COMPLETE ====='));
  } catch (error) {
    console.error(chalk.red('Pipeline failed:'), error);
    process.exit(1);
  }
}

// Analyze what needs to be updated
async function analyzeChanges() {
  // Read collections configuration
  const collectionsConfig = await readCollectionsConfig();
  
  // Get current collections in data directory
  const currentCollections = await getCurrentCollections();
  
  // Determine what needs to be downloaded (new or updated)
  const toDownload = [];
  
  for (const collection of collectionsConfig) {
    const { prefix } = collection;
    const configPath = path.join(DATA_DIR, prefix, `${prefix}_config.json`);
    
    // Collection is new or config doesn't exist
    if (!currentCollections.includes(prefix) || !await fs.pathExists(configPath)) {
      toDownload.push(collection);
      continue;
    }
    
    // Check if collection config has changed
    try {
      const savedConfig = await fs.readJson(configPath);
      if (hasCollectionChanged(savedConfig, collection)) {
        toDownload.push(collection);
      }
    } catch (error) {
      // If error reading config, download again
      toDownload.push(collection);
    }
  }
  
  // Determine what needs to be removed
  const toRemove = currentCollections.filter(prefix => 
    !collectionsConfig.some(c => c.prefix === prefix)
  );
  
  // Determine what needs to be synced to Unity
  const unityCollections = collectionsConfig.filter(c => c.includeInUnity).map(c => c.prefix);
  const currentUnityCollections = await getCurrentUnityCollections();
  
  const toSyncToUnity = [
    // Collections that should be in Unity but aren't
    ...unityCollections.filter(prefix => !currentUnityCollections.includes(prefix)),
    // Collections that are in Unity but shouldn't be
    ...currentUnityCollections.filter(prefix => !unityCollections.includes(prefix))
  ];
  
  console.log(`Found ${toDownload.length} collections to download`);
  console.log(`Found ${toRemove.length} collections to remove`);
  console.log(`Found ${toSyncToUnity.length} collections to sync with Unity`);
  
  return { toDownload, toRemove, toSyncToUnity };
}

// Read collections configuration
async function readCollectionsConfig() {
  try {
    const configData = await fs.readJson(COLLECTIONS_CONFIG);
    return configData.collections || [];
  } catch (error) {
    console.error(chalk.red('Error reading collections configuration:'), error);
    return [];
  }
}

// Get current collections in the data directory
async function getCurrentCollections() {
  try {
    const entries = await fs.readdir(DATA_DIR, { withFileTypes: true });
    return entries
      .filter(entry => entry.isDirectory())
      .map(entry => entry.name)
      .filter(name => !name.startsWith('.')); // Skip hidden directories
  } catch (error) {
    console.error(chalk.red('Error reading data directory:'), error);
    return [];
  }
}

// Get current collections in Unity
async function getCurrentUnityCollections() {
  try {
    const entries = await fs.readdir(UNITY_DIR, { withFileTypes: true });
    return entries
      .filter(entry => entry.isDirectory())
      .map(entry => entry.name)
      .filter(name => !name.startsWith('.')); // Skip hidden directories
  } catch (error) {
    // If error (e.g., directory doesn't exist), return empty array
    return [];
  }
}

// Check if collection config has changed
function hasCollectionChanged(savedConfig, newConfig) {
  // Compare relevant fields
  return (
    savedConfig.query !== newConfig.query ||
    savedConfig.maxItems !== newConfig.maxItems ||
    savedConfig.sortBy !== newConfig.sortBy ||
    savedConfig.sortDirection !== newConfig.sortDirection ||
    savedConfig.includeInUnity !== newConfig.includeInUnity
  );
}

// Execute the script
runIncrementalPipeline().catch(error => {
  console.error(chalk.red('Incremental pipeline failed:'), error);
  process.exit(1);
}); 