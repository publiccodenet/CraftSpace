import { IACollectionsRegistry } from './ia-collections-registry.js';
import { downloadIACollection } from './ia-collection-downloader.js';
import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import dotenv from 'dotenv';
import { exportItem } from './metadata-exporter.js';
import { pipeline } from 'stream';
import { DownloadManager } from './download-manager.js';

// Load environment variables from .env file
dotenv.config();

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

/**
 * Process Internet Archive collections with smart concurrency
 * @param {Object} options - Processing options
 */
export async function processIACollections(options = {}) {
  const config = {
    full: options.mode === 'full',
    // Source directory - where we read collections from
    collectionsDir: path.resolve(process.cwd(), '../../Collections'),
    // SvelteKit output directory - where we write processed collections for web
    outputDir: path.resolve(process.cwd(), './static/data/collections'),
    unityExport: options.unityExport || false,
    // Unity output directory - where we export for Unity
    unityDir: path.resolve(process.cwd(), '../Unity/CraftSpace/Assets/Resources/Collections'),
    collection: options.collection || null,
    batchSize: options.batchSize || 1000,
    limit: options.limit || 0,
    sort: options.sort || 'downloads desc',
    downloadContent: options.downloadContent !== false,
    concurrency: options.concurrency || 1,
    // Concurrency options
    maxConcurrentItems: options.maxConcurrentItems || 5,  // Increased from 3 to 5
    maxConcurrentDownloads: options.maxConcurrentDownloads || 5,  // Already set to 5
  };
  
  console.log(`Processing collections with concurrency: ${config.maxConcurrentItems} items, ${config.maxConcurrentDownloads} downloads`);
  
  // Then ensure all output directories exist
  fs.ensureDirSync(config.outputDir);
  if (config.unityExport) {
    fs.ensureDirSync(config.unityDir);
  }
  
  // Load collections from directory structure
  const registry = new IACollectionsRegistry(config.collectionsDir);
  let collections = registry.getCollections();
  
  // Filter for specific collection if specified
  if (config.collection) {
    collections = collections.filter(c => c.prefix === config.collection);
    if (collections.length === 0) {
      console.error(`Collection "${config.collection}" not found.`);
      return { success: false, error: 'Collection not found' };
    }
  }
  
  if (collections.length === 0) {
    console.log('No collections to process. Exiting.');
    return { success: true, processed: 0, collections: [] };
  }
  
  console.log(`Found ${collections.length} collections to process.`);
  
  // Create global download manager
  const downloadManager = new DownloadManager(config.maxConcurrentDownloads);
  
  // Process collections in sequence (one at a time)
  let processedCount = 0;
  const processedCollections = [];
  
  for (const collection of collections) {
    console.log(`\nProcessing collection: ${collection.name} (${collection.prefix})`);
    
    try {
      // Parse command line arguments
      const args = process.argv.slice(2);
      const forceRefresh = args.includes('--force-refresh');
      
      // Configure download options with the download manager
      const downloadOptions = {
        prefix: collection.prefix,
        name: collection.name,
        query: collection.query,
        sort: collection.sort || config.sort,
        limit: collection.limit || config.limit,
        batchSize: config.batchSize,
        outputDir: config.collectionsDir, // Save to raw collections directory
        includeInUnity: collection.includeInUnity,
        downloadContent: config.downloadContent,
        downloadManager, // Pass the download manager
        forceRefresh: forceRefresh,
        retryForbidden: options.retryForbidden,
      };
      
      console.log(`Downloading collection data from Internet Archive...`);
      console.log(`Query: ${downloadOptions.query}`);
      console.log(`Limit: ${downloadOptions.limit || 'No limit'}`);
      
      // Call the download function with concurrency support
      const result = await downloadIACollection(downloadOptions);
      
      console.log(`Collection ${collection.prefix} processed successfully.`);
      console.log(`Downloaded ${result.itemCount} items.`);
      
      // Update collection information
      collection.totalItems = result.itemCount;
      collection.lastUpdated = new Date().toISOString();
      
      // Update collection metadata file
      const collectionPath = path.join(config.collectionsDir, collection.prefix);
      fs.writeJSONSync(
        path.join(collectionPath, 'collection.json'),
        collection,
        { spaces: 2 }
      );
      
      processedCount++;
      processedCollections.push(collection);
    } catch (error) {
      console.error(`Error processing collection ${collection.prefix}:`, error);
    }
  }
  
  // Wait for all downloads to complete
  await downloadManager.waitForAll();
  console.log('All downloads completed!');
  
  // Export to Unity if requested
  if (config.unityExport) {
    console.log('\nExporting collections to Unity...');
    try {
      await registry.exportCollectionsToUnity(config.unityDir);
    } catch (error) {
      console.error('Error exporting to Unity:', error);
    }
  }
  
  // Process export targets if specified
  if (config.exportTargets) {
    console.log('\nProcessing export targets...');
    
    for (const collection of processedCollections) {
      if (!collection.exportTargets || collection.exportTargets.length === 0) {
        console.log(`No export targets defined for collection ${collection.prefix}, skipping.`);
        continue;
      }
      
      console.log(`\nExporting collection: ${collection.name} (${collection.prefix})`);
      
      // Get items for this collection
      const items = registry.getCollectionItems(collection.prefix);
      
      // Process each target
      for (const target of collection.exportTargets) {
        if (target.enabled === false) {
          console.log(`Target ${target.name} is disabled, skipping.`);
          continue;
        }
        
        console.log(`Processing export target: ${target.name}`);
        
        try {
          const targetDir = path.join(config.outputDir, collection.prefix, 'exports', target.name);
          await fs.ensureDir(targetDir);
          
          // Get export options based on configuration
          const exportOptions = {
            sourceDir: path.join(config.collectionsDir, collection.prefix, 'items'),
            outputDir: targetDir,
            exportAssets: true
          };
          
          // Get items to export (may apply filtering)
          let itemsToExport = items;
          if (target.limit > 0 && target.limit < items.length) {
            console.log(`Limiting export to ${target.limit} items.`);
            itemsToExport = items.slice(0, target.limit);
          }
          
          // Process items in sequence (or could use Promise.all for parallel)
          for (const item of itemsToExport) {
            try {
              await exportItem(item, [target], exportOptions);
            } catch (error) {
              console.error(`Error exporting item ${item.id} for target ${target.name}:`, error);
            }
          }
          
          console.log(`Exported ${itemsToExport.length} items for target ${target.name}.`);
        } catch (error) {
          console.error(`Error processing export target ${target.name}:`, error);
        }
      }
    }
  }
  
  return {
    success: true,
    processed: processedCount,
    collections: processedCollections
  };
}

/**
 * Advanced content processing (placeholder for future feature)
 * This will be where we implement advanced processing for book covers, pages, etc.
 */
export async function processAdvancedContent(options = {}) {
  console.log('Advanced content processing - Coming soon...');
  
  // This will be expanded in the future to:
  // 1. Extract full resolution book covers
  // 2. Process book pages
  // 3. Generate multi-resolution atlases
  // 4. Create manifest icons
  
  return { success: true, message: 'Advanced processing not yet implemented' };
}

/**
 * CLI usage
 */
export async function main() {
  const args = process.argv.slice(2);
  const mode = args[0] || 'incremental';
  
  // Default options
  const options = {
    mode,
    collection: '',
    unityExport: false,
    batchSize: 1000,
    limit: 0,
    downloadContent: true
  };
  
  // Parse additional arguments
  for (let i = 1; i < args.length; i++) {
    const arg = args[i];
    
    if (arg.startsWith('--collection=')) {
      options.collection = arg.split('=')[1];
    }
    else if (arg === '--unity-export') {
      options.unityExport = true;
    } else if (arg.startsWith('--batch-size=')) {
      options.batchSize = parseInt(arg.split('=')[1], 10);
    } else if (arg.startsWith('--limit=')) {
      options.limit = parseInt(arg.split('=')[1], 10);
    } else if (arg.startsWith('--unity-dir=')) {
      options.unityDir = arg.split('=')[1];
    } else if (arg === '--no-content') {
      options.downloadContent = false;
    } else if (arg.startsWith('--concurrent-items=')) {
      options.maxConcurrentItems = parseInt(arg.split('=')[1], 10);
    } else if (arg.startsWith('--concurrent-downloads=')) {
      options.maxConcurrentDownloads = parseInt(arg.split('=')[1], 10);
    } else if (arg === '--force-refresh') {
      options.forceRefresh = true;
    } else if (arg === '--retry-forbidden') {
      options.retryForbidden = true;
    }
  }
  
  try {
    const result = await processIACollections(options);
    
    if (result.success) {
      console.log(`\nProcessing summary:`);
      console.log(`- Collections processed: ${result.processed}`);
      // Fix the reduce error by checking if collections exists and has items
      const totalItems = result.collections && result.collections.length > 0 
        ? result.collections.reduce((sum, c) => sum + (c.totalItems || 0), 0)
        : 0;
      console.log(`- Total items: ${totalItems}`);
      console.log(`- Unity export: ${options.unityExport ? 'Yes' : 'No'}`);
    } else {
      console.error(`\nProcessing failed: ${result.error}`);
      process.exit(1);
    }
  } catch (error) {
    console.error('Error processing collections:', error);
    process.exit(1);
  }
}

// Run main function if this script is executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  main().catch(error => {
    console.error('Unhandled error:', error);
    process.exit(1);
  });
} 