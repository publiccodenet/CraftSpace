#!/usr/bin/env node

// Simple wrapper script that doesn't require TypeScript compilation
const { InternetArchiveSDK } = require('internetarchive-sdk-js');
const fs = require('fs-extra');
const path = require('path');
const sharp = require('sharp');
const { IA } = require('internetarchive-sdk-js');
const PQueue = require('p-queue').default;

// Get command line arguments
const args = process.argv.slice(2);
if (args.length < 2) {
  console.log('Usage: node download-items.js <output_dir> <prefix> [subject] [mediatype]');
  console.log('Example: node download-items.js ./data sf "Science fiction" texts');
  console.log('Defaults: subject="Science fiction", mediatype="texts"');
  process.exit(1);
}

// Parse arguments with new order and defaults
const outputDir = args[0]; // Required
const prefix = args[1]; // Required - used for file naming
const subject = args.length > 2 ? args[2] : "Science fiction"; // Optional with default
const mediatype = args.length > 3 ? args[3] : "texts"; // Optional with default

// Directories to output data to
const UNITY_RESOURCES_DIR = path.join('Unity/CraftSpace/Assets/Resources/Collections', prefix);
const SVELTEKIT_STATIC_DIR = path.join('SvelteKit/BackSpace/static/data', prefix);

// Configure rate limiting
const queue = new PQueue({ concurrency: 5 });

// Add color processing functions
function getSinglePixelColor(imagePath) {
  // Extract dominant non-white/black color
  // Return as hex string
  return "4080FF"; // Default for testing
}

function getUltraLowResColors(imagePath) {
  // Extract 6 most representative colors using our 2x3 approach
  // Return as comma-separated hex values
  return "FF2010,80A0C0,20FF40,D0D0D0,302080,FFC040"; // Default for testing
}

async function downloadCover(identifier, outputPath) {
  try {
    // Download the cover image from Internet Archive
    const coverUrl = `https://archive.org/services/img/${identifier}`;
    
    // Use node-fetch or another HTTP client to download
    const response = await fetch(coverUrl);
    if (!response.ok) throw new Error(`Failed to download cover: ${response.statusText}`);
    
    const buffer = await response.arrayBuffer();
    await fs.writeFile(outputPath, Buffer.from(buffer));
    return true;
  } catch (error) {
    console.error(`Error downloading cover for ${identifier}:`, error);
    return false;
  }
}

async function downloadItems() {
  const ia = new InternetArchiveSDK();
  
  // Ensure output directories exist
  fs.ensureDirSync(outputDir);
  fs.ensureDirSync(UNITY_RESOURCES_DIR);
  fs.ensureDirSync(SVELTEKIT_STATIC_DIR);
  
  // Build the query
  const query = `subject:"${subject}" AND mediatype:${mediatype}`;
  console.log(`Starting download for query: ${query}`);
  console.log(`Output directory: ${outputDir}`);
  console.log(`Prefix: ${prefix}`);
  
  // Set up pagination
  const batchSize = 100; // Smaller batch for more frequent indexing
  let page = 1;
  let hasMoreResults = true;
  let totalItems = 0;
  
  // Keep track of all chunk files and items
  const chunkFiles = [];
  const allItems = [];
  
  // Fields to fetch
  const fields = [
    'identifier', 'title', 'creator', 'date', 'description', 
    'subject', 'collection', 'publisher', 'language', 'downloads'
  ];
  
  // Download in batches until no more results
  while (hasMoreResults) {
    console.log(`Downloading page ${page}...`);
    
    try {
      const results = await ia.search({
        query,
        rows: batchSize,
        page,
        fields
      });
      
      const items = results.response.docs;
      const numResults = items.length;
      totalItems += numResults;
      
      console.log(`Retrieved ${numResults} results (total so far: ${totalItems})`);
      
      if (numResults === 0) {
        hasMoreResults = false;
        console.log('No more results, stopping');
      } else {
        // Create padded page number for sorting
        const paddedPage = page.toString().padStart(3, '0');
        const chunkName = `${prefix}_items_${paddedPage}.json`;
        const outputFile = path.join(outputDir, chunkName);
        
        // Save to file
        await fs.writeJson(outputFile, results, { spaces: 2 });
        console.log(`Saved to ${outputFile}`);
        
        // Add to tracking
        chunkFiles.push(chunkName);
        allItems.push(...items);
        
        // Process covers and generate metadata for each item
        await processItemBatch(items);
        
        // Move to next page
        page++;
        
        // Add a small delay to avoid rate limiting
        await new Promise(resolve => setTimeout(resolve, 500));
      }
    } catch (error) {
      console.error('Error downloading data:', error);
      hasMoreResults = false;
    }
  }
  
  // Create collection index file
  const collectionIndex = {
    prefix,
    subject,
    mediatype,
    totalItems,
    lastUpdated: new Date().toISOString(),
    chunks: chunkFiles
  };
  
  const collectionIndexFile = `${prefix}_index.json`;
  await fs.writeJson(path.join(outputDir, collectionIndexFile), collectionIndex, { spaces: 2 });
  console.log(`Created collection index: ${collectionIndexFile}`);
  
  // Copy to Unity and SvelteKit static dirs
  await fs.copy(path.join(outputDir, collectionIndexFile), path.join(UNITY_RESOURCES_DIR, collectionIndexFile));
  await fs.copy(path.join(outputDir, collectionIndexFile), path.join(SVELTEKIT_STATIC_DIR, collectionIndexFile));
  
  // Update the top-level index if it exists, or create it
  const topLevelIndexPath = path.join(outputDir, 'index.json');
  let topLevelIndex = { collections: [] };
  
  if (await fs.pathExists(topLevelIndexPath)) {
    topLevelIndex = await fs.readJson(topLevelIndexPath);
  }
  
  // Add or update this collection in the index
  const existingColIndex = topLevelIndex.collections.findIndex(col => col.prefix === prefix);
  if (existingColIndex >= 0) {
    topLevelIndex.collections[existingColIndex] = {
      prefix,
      subject,
      mediatype,
      totalItems,
      lastUpdated: new Date().toISOString(),
      indexFile: collectionIndexFile
    };
  } else {
    topLevelIndex.collections.push({
      prefix,
      subject,
      mediatype,
      totalItems,
      lastUpdated: new Date().toISOString(),
      indexFile: collectionIndexFile
    });
  }
  
  // Write the top-level index
  await fs.writeJson(topLevelIndexPath, topLevelIndex, { spaces: 2 });
  console.log(`Updated top-level index: index.json`);
  
  // Copy to Unity and SvelteKit static dirs
  await fs.copy(topLevelIndexPath, path.join(path.dirname(UNITY_RESOURCES_DIR), 'index.json'));
  await fs.copy(topLevelIndexPath, path.join(path.dirname(SVELTEKIT_STATIC_DIR), 'index.json'));
  
  console.log(`Done! Downloaded metadata for ${totalItems} items to ${outputDir}`);
  console.log(`Data also copied to Unity and SvelteKit directories.`);
}

async function processItemBatch(items) {
  return Promise.all(items.map(item => 
    queue.add(() => processBookCover(item))
  ));
}

async function processBookCover(item) {
  try {
    const identifier = item.identifier;
    const coverPath = path.join(outputDir, `${identifier}_cover.jpg`);
    
    // Download cover if needed
    if (!fs.existsSync(coverPath)) {
      const success = await downloadCover(identifier, coverPath);
      if (!success) return;
    }
    
    // Generate low-res representations
    const singlePixelColor = getSinglePixelColor(coverPath);
    const ultraLowColors = getUltraLowResColors(coverPath);
    
    // Create item metadata
    const metadata = {
      id: identifier,
      title: item.title || "Unknown title",
      creator: item.creator || "Unknown creator",
      date: item.date || "",
      subject: item.subject || [],
      collection: item.collection || [],
      description: item.description || "",
      icons: {
        "1x1": singlePixelColor,
        "2x3": ultraLowColors
      }
    };
    
    // Create item directory in all locations
    const itemDirOutput = path.join(outputDir, identifier);
    const itemDirUnity = path.join(UNITY_RESOURCES_DIR, identifier);
    const itemDirSvelteKit = path.join(SVELTEKIT_STATIC_DIR, identifier);
    
    fs.ensureDirSync(itemDirOutput);
    fs.ensureDirSync(itemDirUnity);
    fs.ensureDirSync(itemDirSvelteKit);
    
    // Write metadata to all locations
    await fs.writeJson(path.join(itemDirOutput, 'metadata.json'), metadata, { spaces: 2 });
    await fs.writeJson(path.join(itemDirUnity, 'metadata.json'), metadata, { spaces: 2 });
    await fs.writeJson(path.join(itemDirSvelteKit, 'metadata.json'), metadata, { spaces: 2 });
    
    console.log(`Processed ${identifier}`);
  } catch (error) {
    console.error(`Error processing ${item.identifier}:`, error);
  }
}

downloadItems().catch(error => {
  console.error('Download failed:', error);
  process.exit(1);
}); 