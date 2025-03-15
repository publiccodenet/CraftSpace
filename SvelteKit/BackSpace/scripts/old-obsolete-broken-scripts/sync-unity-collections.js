#!/usr/bin/env node

/**
 * sync-unity-collections.js
 * 
 * Script to synchronize collections to Unity's Resources directory.
 * This script:
 * 1. Reads collections.json to determine which collections should be in Unity
 * 2. Copies appropriate collections and their assets to Unity Resources
 * 3. Updates Unity's index.json to reflect the available collections
 */

const fs = require('fs-extra');
const path = require('path');
const chalk = require('chalk');

// Configuration
const PROJECT_ROOT = path.resolve(__dirname, '../../..');
const COLLECTIONS_CONFIG = path.join(PROJECT_ROOT, 'collections.json');
const DATA_DIR = path.join(__dirname, '../static/data');
const UNITY_DIR = path.join(PROJECT_ROOT, 'Unity/CraftSpace/Assets/Resources/Collections');

// Main function
async function syncUnityCollections() {
  console.log(chalk.bold.blue('===== SYNCING COLLECTIONS TO UNITY ====='));
  
  try {
    // Ensure Unity directory exists
    await fs.ensureDir(UNITY_DIR);
    
    // Read collections configuration
    const collectionsConfig = await readCollectionsConfig();
    
    // Filter to collections that should be included in Unity
    const unityCollections = collectionsConfig.filter(collection => collection.includeInUnity);
    console.log(chalk.dim(`Found ${unityCollections.length} collections to include in Unity`));
    
    // Clean Unity directory (but keep index.json if it exists)
    await cleanUnityDirectory(unityCollections.map(c => c.prefix));
    
    // Copy each collection to Unity
    for (const collection of unityCollections) {
      await copyCollectionToUnity(collection);
    }
    
    // Update Unity index.json
    await updateUnityIndex(unityCollections);
    
    console.log(chalk.bold.green('\n===== UNITY SYNC COMPLETE ====='));
    console.log(`${unityCollections.length} collections synced to Unity Resources directory`);
  } catch (error) {
    console.error(chalk.red('Error syncing to Unity:'), error);
    process.exit(1);
  }
}

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

// Clean Unity directory, keeping only specified collections
async function cleanUnityDirectory(prefixesToKeep) {
  console.log(chalk.blue('\n=== CLEANING UNITY DIRECTORY ==='));
  
  // Read current Unity collections
  const unityEntries = await fs.readdir(UNITY_DIR, { withFileTypes: true });
  const unityCollections = unityEntries
    .filter(entry => entry.isDirectory())
    .map(entry => entry.name)
    .filter(name => !name.startsWith('.')); // Skip hidden directories
  
  // Remove collections that shouldn't be there
  for (const collection of unityCollections) {
    if (!prefixesToKeep.includes(collection)) {
      console.log(chalk.dim(`Removing collection ${collection} from Unity`));
      await fs.remove(path.join(UNITY_DIR, collection));
    }
  }
  
  console.log(chalk.green(`✓ Cleaned Unity directory`));
}

// Copy a collection to Unity
async function copyCollectionToUnity(collection) {
  const { prefix, name } = collection;
  console.log(chalk.blue(`\nCopying collection: ${name} (${prefix})`));
  
  const sourceDir = path.join(DATA_DIR, prefix);
  const destDir = path.join(UNITY_DIR, prefix);
  
  // Check if source exists
  if (!await fs.pathExists(sourceDir)) {
    console.error(chalk.red(`✗ Source directory not found: ${sourceDir}`));
    return;
  }
  
  // Ensure destination directory exists
  await fs.ensureDir(destDir);
  
  // Copy collection index
  const indexPath = path.join(sourceDir, `${prefix}_index.json`);
  if (await fs.pathExists(indexPath)) {
    await fs.copy(indexPath, path.join(destDir, `${prefix}_index.json`));
  }
  
  // Copy atlases directory if it exists
  const atlasesDir = path.join(sourceDir, 'atlases');
  if (await fs.pathExists(atlasesDir)) {
    await fs.copy(atlasesDir, path.join(destDir, 'atlases'));
  }
  
  // Copy item metadata
  const items = await getCollectionItems(sourceDir);
  for (const itemId of items) {
    const sourceItemDir = path.join(sourceDir, itemId);
    const destItemDir = path.join(destDir, itemId);
    
    // Only copy metadata.json, not the actual book content
    const metadataPath = path.join(sourceItemDir, 'metadata.json');
    if (await fs.pathExists(metadataPath)) {
      await fs.ensureDir(destItemDir);
      await fs.copy(metadataPath, path.join(destItemDir, 'metadata.json'));
    }
  }
  
  console.log(chalk.green(`✓ Copied collection ${name} (${prefix}) to Unity`));
}

// Get all items in a collection
async function getCollectionItems(collectionDir) {
  try {
    const entries = await fs.readdir(collectionDir, { withFileTypes: true });
    return entries
      .filter(entry => entry.isDirectory())
      .map(entry => entry.name)
      .filter(name => name !== 'atlases'); // Skip the atlases directory
  } catch (error) {
    console.error(`Error reading collection directory ${collectionDir}:`, error);
    return [];
  }
}

// Update Unity index.json with the collections that are included
async function updateUnityIndex(collections) {
  console.log(chalk.blue('\n=== UPDATING UNITY INDEX ==='));
  
  // Create the index content
  const indexContent = {
    lastUpdated: new Date().toISOString(),
    collections: collections.map(collection => ({
      prefix: collection.prefix,
      name: collection.name,
      description: collection.description || '',
      subject: collection.subject || '',
      mediatype: collection.mediatype || 'texts',
      indexFile: `${collection.prefix}/${collection.prefix}_index.json`
    }))
  };
  
  // Write the index.json file
  const indexPath = path.join(UNITY_DIR, 'index.json');
  await fs.writeJson(indexPath, indexContent, { spaces: 2 });
  
  console.log(chalk.green(`✓ Updated Unity index.json with ${collections.length} collections`));
}

// Execute the script
syncUnityCollections().catch(error => {
  console.error(chalk.red('Unity sync failed:'), error);
  process.exit(1);
}); 