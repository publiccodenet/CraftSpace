#!/usr/bin/env node

/**
 * process-item.js
 * 
 * Script to process a single Internet Archive item.
 * This can be used directly from the command line or imported by other scripts.
 * 
 * Usage: 
 * node process-item.js <identifier> <outputDir> [options]
 * 
 * Options:
 *   --prefix=<prefix>        Collection prefix
 *   --include-in-unity=true  Include this item in Unity
 */

const fs = require('fs-extra');
const path = require('path');
const { InternetArchiveSDK } = require('internetarchive-sdk-js');
const sharp = require('sharp');

// Parse command line arguments
const args = process.argv.slice(2);
const identifier = args[0];
const outputDir = args[1];

if (!identifier || !outputDir) {
  console.log('Usage: node process-item.js <identifier> <outputDir> [options]');
  console.log('Example: node process-item.js frankenstein00shel ./data --prefix=sf --include-in-unity=true');
  process.exit(1);
}

// Parse options
const options = {};
for (let i = 2; i < args.length; i++) {
  const arg = args[i];
  if (arg.startsWith('--')) {
    const [key, value] = arg.substring(2).split('=');
    options[key] = value === undefined ? true : value;
  }
}

// Default options
const prefix = options.prefix || 'default';
const includeInUnity = options['include-in-unity'] === 'true';

// Main processing function
async function processItem() {
  try {
    console.log(`Processing item: ${identifier}`);
    const ia = new InternetArchiveSDK();
    
    // Fetch metadata
    console.log(`Fetching metadata for ${identifier}...`);
    const metadata = await ia.getItemMetadata(identifier);
    
    // Download cover image
    console.log(`Downloading cover for ${identifier}...`);
    const coverPath = path.join(outputDir, `${identifier}_cover.jpg`);
    await downloadCover(identifier, coverPath);
    
    // Generate color representations
    console.log(`Generating color representations...`);
    const singlePixelColor = getSinglePixelColor(coverPath);
    const ultraLowColors = getUltraLowResColors(coverPath);
    
    // Create item metadata
    const itemMetadata = {
      id: identifier,
      title: metadata.title || "Unknown title",
      creator: metadata.creator || "Unknown creator",
      date: metadata.date || "",
      description: metadata.description || "",
      subject: metadata.subject || [],
      collection: metadata.collection || [],
      icons: {
        "1x1": singlePixelColor,
        "2x3": ultraLowColors
      }
    };
    
    // Save to appropriate locations
    await saveMetadata(itemMetadata, outputDir, prefix, includeInUnity);
    
    console.log(`✓ Processed ${identifier} successfully`);
    return itemMetadata;
  } catch (error) {
    console.error(`Error processing ${identifier}:`, error);
    throw error;
  }
}

// Helper functions
async function downloadCover(identifier, outputPath) {
  // Implementation placeholder for cover download
  // Real implementation would use HTTP client to fetch from IA
  console.log(`Would download cover for ${identifier} to ${outputPath}`);
  return true;
}

function getSinglePixelColor(imagePath) {
  // Implementation placeholder for single pixel color extraction
  return "4080FF"; // Default for testing
}

function getUltraLowResColors(imagePath) {
  // Implementation placeholder for ultra-low resolution colors
  return "FF2010,80A0C0,20FF40,D0D0D0,302080,FFC040"; // Default for testing
}

async function saveMetadata(metadata, outputDir, prefix, includeInUnity) {
  // Save to main output directory
  const itemDir = path.join(outputDir, prefix, metadata.id);
  await fs.ensureDir(itemDir);
  await fs.writeJson(path.join(itemDir, 'metadata.json'), metadata, { spaces: 2 });
  
  // Save to Unity if requested
  if (includeInUnity) {
    const unityDir = path.join(process.cwd(), '../../Unity/CraftSpace/Assets/Resources/Collections', prefix, metadata.id);
    await fs.ensureDir(unityDir);
    await fs.writeJson(path.join(unityDir, 'metadata.json'), metadata, { spaces: 2 });
  }
}

// Check if this script is being run directly
if (require.main === module) {
  processItem()
    .then(() => console.log('Processing complete'))
    .catch(error => {
      console.error('Processing failed:', error);
      process.exit(1);
    });
} else {
  // Export for use as a module
  module.exports = { processItem };
} 