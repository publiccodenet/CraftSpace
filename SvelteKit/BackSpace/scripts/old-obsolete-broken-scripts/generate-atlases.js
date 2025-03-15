#!/usr/bin/env node

/**
 * generate-atlases.js
 * 
 * Script to generate texture atlases for all collections.
 * This script:
 * 1. Reads the data directory to find all collections
 * 2. For each collection, creates texture atlases for different resolutions
 * 3. Outputs atlas images and metadata for Unity consumption
 */

const fs = require('fs-extra');
const path = require('path');
const chalk = require('chalk');
const sharp = require('sharp');
// For texture packing we would use free-tex-packer-core
// const texturePacker = require('free-tex-packer-core');

// Constants
const DATA_DIR = path.join(__dirname, '../static/data');
const UNITY_DIR = path.join(__dirname, '../../../Unity/CraftSpace/Assets/Resources/Collections');

// Atlas configurations for different resolutions
const ATLAS_CONFIGS = [
  { name: '1x1', width: 1024, height: 1024, itemSize: 1 },
  { name: '2x3', width: 2048, height: 2048, itemSize: { width: 2, height: 3 } }
];

// Main function to generate all atlases
async function generateAllAtlases() {
  console.log(chalk.bold.blue('===== GENERATING TEXTURE ATLASES ====='));
  
  try {
    // Get list of all collections
    const collections = await getCollections();
    console.log(chalk.dim(`Found ${collections.length} collections`));
    
    // Process each collection
    for (const collection of collections) {
      await processCollection(collection);
    }
    
    console.log(chalk.bold.green('\n===== ATLAS GENERATION COMPLETE ====='));
  } catch (error) {
    console.error(chalk.red('Error generating atlases:'), error);
    process.exit(1);
  }
}

// Get all collections by reading the data directory
async function getCollections() {
  const entries = await fs.readdir(DATA_DIR, { withFileTypes: true });
  return entries
    .filter(entry => entry.isDirectory())
    .map(entry => entry.name)
    .filter(name => !name.startsWith('.')); // Skip hidden directories
}

// Process a single collection
async function processCollection(collectionPrefix) {
  console.log(chalk.blue(`\nProcessing collection: ${collectionPrefix}`));
  
  try {
    // Get list of all items in the collection
    const collectionDir = path.join(DATA_DIR, collectionPrefix);
    const items = await getCollectionItems(collectionDir);
    console.log(chalk.dim(`Found ${items.length} items in collection`));
    
    // Generate each atlas type
    for (const config of ATLAS_CONFIGS) {
      await generateAtlas(collectionPrefix, items, config);
    }
    
    console.log(chalk.green(`✓ Completed atlas generation for ${collectionPrefix}`));
  } catch (error) {
    console.error(chalk.red(`✗ Failed to process collection ${collectionPrefix}:`), error);
  }
}

// Get all items in a collection
async function getCollectionItems(collectionDir) {
  const entries = await fs.readdir(collectionDir, { withFileTypes: true });
  return entries
    .filter(entry => entry.isDirectory())
    .map(entry => entry.name);
}

// Generate a specific atlas for a collection
async function generateAtlas(collectionPrefix, items, config) {
  console.log(chalk.dim(`Generating ${config.name} atlas for ${collectionPrefix}...`));
  
  // Atlas output paths
  const atlasDir = path.join(DATA_DIR, collectionPrefix, 'atlases');
  await fs.ensureDir(atlasDir);
  
  const atlasImagePath = path.join(atlasDir, `${config.name}.png`);
  const atlasMetadataPath = path.join(atlasDir, `${config.name}.json`);
  
  // Load all items metadata to get their icon data
  const itemsData = [];
  for (const itemId of items) {
    try {
      const metadataPath = path.join(DATA_DIR, collectionPrefix, itemId, 'metadata.json');
      const metadata = await fs.readJson(metadataPath);
      
      // Extract the appropriate icon data based on atlas type
      if (metadata.icons && metadata.icons[config.name]) {
        itemsData.push({
          id: itemId,
          iconData: metadata.icons[config.name]
        });
      }
    } catch (error) {
      console.warn(chalk.yellow(`  Warning: Could not process ${itemId}`), error.message);
    }
  }
  
  // In a real implementation, we would now:
  // 1. Convert icon data to actual images
  // 2. Pack them into an atlas using texture packer
  // 3. Save the atlas image and metadata
  
  // Placeholder implementation
  console.log(chalk.dim(`  Would create atlas with ${itemsData.length} items`));
  
  // Write placeholder metadata
  const atlasMetadata = {
    atlas: config.name,
    collection: collectionPrefix,
    itemCount: itemsData.length,
    items: itemsData.map(item => ({
      id: item.id,
      rect: { x: 0, y: 0, width: config.itemSize.width || config.itemSize, height: config.itemSize.height || config.itemSize }
    }))
  };
  
  await fs.writeJson(atlasMetadataPath, atlasMetadata, { spaces: 2 });
  console.log(chalk.green(`  ✓ Generated ${config.name} atlas metadata`));
  
  // If this collection should be included in Unity, copy atlas to Unity dir
  const unityIndexPath = path.join(UNITY_DIR, 'index.json');
  if (await fs.pathExists(unityIndexPath)) {
    const unityIndex = await fs.readJson(unityIndexPath);
    const includedInUnity = unityIndex.collections.some(c => c.prefix === collectionPrefix);
    
    if (includedInUnity) {
      const unityAtlasDir = path.join(UNITY_DIR, collectionPrefix, 'atlases');
      await fs.ensureDir(unityAtlasDir);
      
      // Copy atlas metadata
      await fs.copy(atlasMetadataPath, path.join(unityAtlasDir, `${config.name}.json`));
      
      // In a real implementation, we would also copy the atlas image
      console.log(chalk.green(`  ✓ Copied atlas to Unity Resources`));
    }
  }
}

// Execute the script
generateAllAtlases().catch(error => {
  console.error(chalk.red('Atlas generation failed:'), error);
  process.exit(1);
}); 