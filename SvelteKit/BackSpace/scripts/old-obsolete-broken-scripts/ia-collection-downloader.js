import fs from 'fs-extra';
import path from 'path';
import fetch from 'node-fetch';
import { pipeline } from 'stream/promises';
import dotenv from 'dotenv';
import { DownloadManager } from './download-manager.js';
dotenv.config();

// Default configuration
const DEFAULT_CONFIG = {
  batchSize: 1000,
  maxItems: 0, // 0 means no limit
  outputDir: './static/data/collections',
  includeInUnity: false,
  downloadContent: true,
  sort: 'downloads desc', // Sort by popularity (most downloaded first)
  limit: 0, // 0 means no limit (same as maxItems, but for query itself)
  forceDownload: false,
  maxConcurrentItems: 5,
  maxConcurrentDownloads: 5
};

// Available sorting options for reference
const SORT_OPTIONS = {
  DOWNLOADS: 'downloads desc',
  RECENT: 'date desc',
  OLDEST: 'date asc',
  TITLE: 'titleSorter asc',
  CREATOR: 'creatorSorter asc'
};

/**
 * Downloads a collection from Internet Archive based on a query
 */
export async function downloadIACollection(options) {
  // Merge with default config
  const config = { ...DEFAULT_CONFIG, ...options };
  
  // Create or use provided download manager
  const downloadManager = config.downloadManager || new DownloadManager(config.maxConcurrentDownloads || 5);
  
  // Validate required parameters
  if (!config.prefix || !config.query || !config.name) {
    throw new Error('Missing required parameters: prefix, query, and name are required');
  }
  
  console.log(`Starting download of collection: ${config.name} (${config.prefix})`);
  console.log(`Sorting by: ${config.sort}`);
  
  // Determine the effective limit (use limit if set, otherwise maxItems)
  const effectiveLimit = config.limit > 0 ? config.limit : config.maxItems;
  if (effectiveLimit > 0) {
    console.log(`Limiting to ${effectiveLimit} items`);
  }
  
  // Create collection directory structure
  const collectionDir = path.join(config.outputDir, config.prefix);
  await fs.ensureDir(collectionDir);
  
  // Create items directory for all item metadata files
  const itemsDir = path.join(collectionDir, 'items');
  await fs.ensureDir(itemsDir);
  
  // Create content directory for all downloaded files
  const contentDir = path.join(collectionDir, 'content');
  await fs.ensureDir(contentDir);
  
  // Initialize collection data
  const collection = {
    prefix: config.prefix,
    name: config.name,
    description: config.description || '',
    query: config.query,
    sort: config.sort,
    limit: effectiveLimit,
    includeInUnity: config.includeInUnity,
    lastUpdated: new Date().toISOString(),
    totalItems: 0,
    cache_dir: config.prefix,
    index_cached: `${config.prefix}/index.json` // Points to the index file in the cache
  };
  
  // Initialize index (list of item IDs)
  const index = {
    collection_id: config.prefix,
    item_ids: [],
    totalItems: 0,
    lastUpdated: new Date().toISOString()
  };
  
  // Fetch items in batches
  let page = 1;
  let totalFetched = 0;
  let hasMoreItems = true;
  
  while (hasMoreItems && (effectiveLimit === 0 || totalFetched < effectiveLimit)) {
    const limit = config.batchSize;
    const maxToFetch = effectiveLimit === 0 ? limit : Math.min(limit, effectiveLimit - totalFetched);
    
    console.log(`Fetching batch ${page} (${maxToFetch} items)...`);
    
    // Use Internet Archive's advanced search API with sorting
    const url = `https://archive.org/advancedsearch.php?q=${encodeURIComponent(config.query)}&fl=identifier,title,creator,date,description,mediatype,collection,subject,downloads,item_size,format&sort=${encodeURIComponent(config.sort)}&rows=${maxToFetch}&page=${page}&output=json`;
    
    try {
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error(`Failed to fetch items: ${response.statusText}`);
      }
      
      const data = await response.json();
      const batch = data.response.docs || [];
      
      if (batch.length === 0) {
        hasMoreItems = false;
        break;
      }
      
      console.log(`Retrieved ${batch.length} items`);
      
      // Process each item in the batch
      for (const item of batch) {
        const itemId = item.identifier;
        
        // Create item directory
        const itemDir = path.join(config.outputDir, config.prefix, 'items', itemId);
        await fs.ensureDir(itemDir);
        
        // Build item metadata
        const itemData = {
          id: itemId,
          title: item.title,
          creator: item.creator,
          date: item.date || null,
          description: item.description || '',
          mediatype: item.mediatype || 'texts',
          collection: item.collection || [],
          subject: item.subject || [],
          downloads: item.downloads || 0,
          item_size: item.item_size || 0,
          format: item.format || []
        };
        
        // Process the collection field to handle favorites
        if (Array.isArray(item.collection)) {
          // Extract favorite collections (those starting with 'fav-')
          const favoriteCollections = item.collection.filter(c => 
            typeof c === 'string' && c.startsWith('fav-')
          );
          
          // Count favorites
          itemData.favorite_count = favoriteCollections.length;
          
          // Keep only non-favorite collections
          itemData.collection = item.collection.filter(c => 
            typeof c === 'string' && !c.startsWith('fav-')
          );
          
          // Optionally add a sample of favorite usernames
          if (favoriteCollections.length > 0) {
            itemData.favorite_sample = favoriteCollections
              .slice(0, 5)  // Take first 5 favorites
              .map(fav => fav.substring(4));  // Remove 'fav-' prefix
          }
        } else {
          itemData.favorite_count = 0;
          itemData.collection = Array.isArray(item.collection) ? item.collection : [];
        }
        
        // Save metadata to item.json
        const metadataPath = path.join(itemDir, 'item.json');
        await fs.writeJSON(metadataPath, itemData, { spaces: 2 });
        
        // Download tile image
        if (itemData.tile_url) {
          try {
            const tilePath = path.join(itemDir, 'cover.jpg');
            await downloadFile(itemData.tile_url, tilePath);
          } catch (error) {
            console.error(`Error downloading tile for ${itemId}:`, error);
          }
        }
        
        // Download content files if enabled
        if (config.downloadContent) {
          try {
            // Fetch detailed metadata to get files information
            console.log(`Fetching metadata for ${itemId}...`);
            const metadataUrl = `https://archive.org/metadata/${itemId}`;
            const metadataResponse = await fetch(metadataUrl);
            
            if (metadataResponse.ok) {
              const iaMetadata = await metadataResponse.json();
              const files = iaMetadata.files || [];
              
              // Find the main content file - case insensitive
              const contentFile = files.find(file => {
                // Convert format to lowercase if it exists
                const format = file.format ? file.format.toLowerCase() : '';
                const name = file.name ? file.name.toLowerCase() : '';
                
                return format === 'text pdf' || 
                       format === 'pdf' || 
                       format === 'epub' ||
                       (name && (
                         name.endsWith('.pdf') ||
                         name.endsWith('.epub')
                       ));
              });
              
              if (contentFile) {
                const filename = contentFile.name;
                const fileSize = parseInt(contentFile.size, 10) || 0;
                const contentUrl = `https://archive.org/download/${itemId}/${encodeURIComponent(filename)}`;
                
                // Format: {itemId}_{format}.{ext}
                const contentFormat = contentFile.format ? contentFile.format.replace(/\s+/g, '_').toLowerCase() : 'content';
                const contentExt = path.extname(filename);
                const contentFilename = `${itemId}_${contentFormat}${contentExt}`;
                const contentPath = path.join(itemDir, contentFilename);
                
                // Standard IA download URL
                itemData.download_url = contentUrl;
                
                try {
                  // Check if we need to download the file
                  const needsDownload = await shouldDownloadFile(
                    contentUrl,         // Remote URL 
                    contentPath,        // Local path
                    fileSize,           // Remote size
                    itemData,           // Item metadata (may contain cache info)
                    config.forceDownload, // Force flag
                    config.retryForbidden // Flag to skip known forbidden items
                  );
                  
                  // Check if this is a known permanent error
                  if (needsDownload && 
                      itemData.download_errors && 
                      itemData.download_errors[contentUrl] && 
                      itemData.download_errors[contentUrl].permanent && 
                      !config.retryForbidden) {
                    console.log(`Skipping previously forbidden item: ${itemId}`);
                    return false; // Skip download attempt
                  }
                  
                  if (needsDownload) {
                    console.log(`Downloading content for ${itemId}: ${filename} (${formatFileSize(fileSize)})...`);
                    
                    try {
                      // Queue the download using the manager
                      const downloadResult = await downloadManager.download(contentUrl, contentPath);
                      
                      if (!downloadResult.success) {
                        console.error(`Error downloading content for ${itemId}: ${downloadResult.error}`);
                        
                        // Add detailed download error info to the metadata
                        itemData.download_error = {
                          message: downloadResult.error,
                          timestamp: new Date().toISOString(),
                          url: contentUrl,
                          status: downloadResult.status || 'unknown',
                          statusText: downloadResult.statusText || '',
                          permanent: downloadResult.permanent || false
                        };
                        
                        // Store the error by URL for forensics
                        if (!itemData.download_errors) {
                          itemData.download_errors = {};
                        }
                        itemData.download_errors[contentUrl] = {
                          error: downloadResult.error,
                          timestamp: new Date().toISOString(),
                          status: downloadResult.status,
                          permanent: downloadResult.permanent
                        };
                        
                        // Still save the metadata even if download failed
                        await fs.writeJSON(metadataPath, itemData, { spaces: 2 });
                        
                        // If error is permanent, log a clearer message
                        if (downloadResult.permanent) {
                          console.warn(`This item appears to be permanently restricted: ${itemId}`);
                        }
                        
                        // Skip the rest of the processing for this item
                        continue; // or return, depending on your flow control
                      }
                      
                      // Extract the ETag from headers if available
                      const etag = downloadResult.headers.etag || '';
                      
                      // Store download metrics with consistent suffixes
                      // Add the cached path (relative to collection)
                      itemData.download_url_cached = `${config.prefix}/content/${contentFilename}`;
                      
                      // Add timestamp of download
                      itemData.download_url_timestamp = new Date().toISOString();
                      
                      // Add ETag if available
                      if (etag) {
                        itemData.download_url_etag = etag;
                      }
                      
                      // Add download performance metrics
                      itemData.download_url_dl_duration = downloadResult.duration;
                      itemData.download_url_dl_mbps = downloadResult.speedMBps;
                      
                      // Update the metadata file
                      await fs.writeJSON(metadataPath, itemData, { spaces: 2 });
                      
                      console.log(`Downloaded ${formatFileSize(downloadResult.size)} at ${downloadResult.speedFormatted}`);
                      
                      // Process EPUB if needed
                      if (filename.toLowerCase().endsWith('.epub')) {
                        // We've downloaded an EPUB, process it to extract metadata
                        try {
                          console.log(`Processing EPUB metadata for ${itemId}...`);
                          
                          // Try to import the EPUB processor (safely)
                          try {
                            const { processEpub } = await import('./process-epub.js');
                            
                            // Process the EPUB to extract metadata
                            const epubResult = await processEpub(
                              contentPath,  // Path to the downloaded EPUB
                              itemId,       // Item ID
                              itemDir,      // Directory where the item is stored
                              {
                                extractMetadata: true,
                                extractCover: true,
                                extractPages: false
                              }
                            );
                            
                            if (epubResult.metadata) {
                              console.log(`Extracted enhanced metadata from EPUB for ${itemId}`);
                              
                              // Read back the enhanced metadata
                              const epubMetadata = fs.readJSONSync(epubResult.metadata);
                              
                              // Merge with Internet Archive metadata (with IA taking precedence)
                              itemData = {
                                ...epubMetadata,          // EPUB metadata as base
                                ...itemData,              // IA metadata overwrites duplicates
                                epub_metadata: epubMetadata  // Also store the full EPUB metadata separately
                              };
                              
                              // Update the metadata file with merged data
                              await fs.writeJSON(metadataPath, itemData, { spaces: 2 });
                            }
                          } catch (importError) {
                            console.warn(`EPUB processing modules not available: ${importError.message}`);
                            console.warn("To enable EPUB processing, install required packages: npm install epubjs sharp");
                          }
                        } catch (epubError) {
                          if (epubError.code === 'ERR_MODULE_NOT_FOUND') {
                            console.error(`Missing required package: ${epubError.message}`);
                            console.error(`Please run: npm install epub epubjs sharp`);
                          } else {
                            console.error(`Error processing EPUB metadata for ${itemId}:`, epubError);
                          }
                        }
                      }
                    } catch (error) {
                      console.error(`Error downloading content for ${itemId}:`, error);
                    }
                  } else {
                    console.log(`Using existing file for ${itemId}: ${filename}`);
                    
                    // Make sure we still set the cached path, even if we didn't download
                    itemData.download_url_cached = `${config.prefix}/content/${contentFilename}`;

                    // Add this new code to process existing EPUB files when forceRefresh is true
                    if (config.forceRefresh && filename.toLowerCase().endsWith('.epub')) {
                      console.log(`Force refresh requested, processing existing EPUB metadata for ${itemId}...`);
                      try {
                        // Import the EPUB processor
                        const { processEpub } = await import('./process-epub.js');
                        
                        // Process the EPUB with forceRefresh option
                        await processEpub(
                          contentPath,  // Path to the existing EPUB
                          itemId,       // Item ID
                          itemDir,      // Directory where the item is stored
                          {
                            extractMetadata: true,
                            extractCover: true,
                            extractPages: false,
                            forceRefresh: true
                          }
                        );
                        
                        // Read the updated metadata (it will be in the same location)
                        const updatedMetadata = await fs.readJSON(path.join(itemDir, 'item.json'));
                        
                        // Update our current item data with values from the refreshed metadata
                        Object.assign(itemData, updatedMetadata);
                        
                        // Save the updated metadata
                        await fs.writeJSON(metadataPath, itemData, { spaces: 2 });
                        
                        console.log(`Refreshed metadata for ${itemId}`);
                      } catch (epubError) {
                        if (epubError.code === 'ERR_MODULE_NOT_FOUND') {
                          console.error(`Missing required package: ${epubError.message}`);
                          console.error(`Please run: npm install epub epubjs sharp`);
                        } else {
                          console.error(`Error processing existing EPUB ${contentPath}:`, epubError);
                        }
                      }
                    }
                  }
                } catch (error) {
                  console.error(`Error downloading content for ${itemId}:`, error);
                }
              } else {
                console.warn(`No suitable content file found for ${itemId}`);
              }
            } else {
              console.error(`Failed to fetch metadata for ${itemId}: ${metadataResponse.statusText}`);
            }
          } catch (error) {
            console.error(`Error downloading content for ${itemId}:`, error);
          }
        }
        
        // Add the item ID to the index
        index.item_ids.push(itemId);
      }
      
      totalFetched += batch.length;
      page++;
      
      // Save progress after each batch
      collection.totalItems = totalFetched;
      index.totalItems = totalFetched;
      index.lastUpdated = new Date().toISOString();
      
      // Save the collection metadata
      await fs.writeJSON(path.join(collectionDir, 'collection.json'), collection, { spaces: 2 });
      
      // Save the index (list of item IDs)
      await fs.writeJSON(path.join(collectionDir, 'index.json'), index, { spaces: 2 });
      
      console.log(`Progress: ${totalFetched} items processed`);
      
    } catch (error) {
      console.error('Error fetching batch:', error);
      break;
    }
  }
  
  // Final collection update
  collection.totalItems = totalFetched;
  collection.lastUpdated = new Date().toISOString();
  
  index.totalItems = totalFetched;
  index.lastUpdated = new Date().toISOString();
  
  console.log(`Download complete. Total items: ${totalFetched}`);
  
  // Save final collection metadata
  await fs.writeJSON(path.join(collectionDir, 'collection.json'), collection, { spaces: 2 });
  
  // Save final index
  await fs.writeJSON(path.join(collectionDir, 'index.json'), index, { spaces: 2 });
  
  return {
    prefix: config.prefix,
    totalItems: totalFetched,
    path: collectionDir
  };
}

// CLI usage
export async function main() {
  const args = process.argv.slice(2);
  
  if (args.length < 3) {
    console.error('Usage: node ia-collection-downloader.js <prefix> <name> <query> [options]');
    console.error('Options:');
    console.error('  --batch-size=<number>    Number of items to fetch per batch (default: 1000)');
    console.error('  --limit=<number>         Maximum number of items to download (default: 0 = unlimited)');
    console.error('  --output-dir=<path>      Output directory (default: ./static/data/collections)');
    console.error('  --include-in-unity       Flag to include this collection in Unity');
    console.error('  --no-content             Skip downloading content files');
    console.error('  --sort=<sort_option>     Sorting method (default: downloads desc)');
    console.error('                           Options: downloads desc, date desc, date asc, titleSorter asc, creatorSorter asc');
    process.exit(1);
  }
  
  const prefix = args[0];
  const name = args[1];
  const query = args[2];
  
  const options = { prefix, name, query };
  
  for (let i = 3; i < args.length; i++) {
    const arg = args[i];
    
    if (arg.startsWith('--batch-size=')) {
      options.batchSize = parseInt(arg.split('=')[1], 10);
    } else if (arg.startsWith('--limit=')) {
      options.limit = parseInt(arg.split('=')[1], 10);
    } else if (arg.startsWith('--output-dir=')) {
      options.outputDir = arg.split('=')[1];
    } else if (arg === '--include-in-unity') {
      options.includeInUnity = true;
    } else if (arg === '--no-content') {
      options.downloadContent = false;
    } else if (arg.startsWith('--sort=')) {
      options.sort = arg.split('=')[1];
    } else if (arg === '--force-download') {
      options.forceDownload = true;
    }
  }
  
  try {
    const result = await downloadIACollection(options);
    console.log('Download completed successfully!');
    console.log(result);
  } catch (error) {
    console.error('Error downloading collection:', error);
    process.exit(1);
  }
}

// Run main function if called directly
if (typeof require !== 'undefined' && require.main === module) {
  main();
}

// In the download function, add authentication credentials
async function downloadFromIA(url, outputPath) {
  try {
    const options = {};
    
    // Add authentication if available
    if (process.env.IA_ACCESS_KEY && process.env.IA_SECRET_KEY) {
      options.headers = {
        'Authorization': `LOW ${process.env.IA_ACCESS_KEY}:${process.env.IA_SECRET_KEY}`
      };
      console.log('Using Internet Archive authentication');
    } else {
      console.warn('No Internet Archive credentials found. Some operations may be limited.');
    }
    
    const response = await fetch(url, options);
    // Rest of the download function...
  } catch (error) {
    console.error(`Error downloading from IA: ${error}`);
    throw error;
  }
}

/**
 * Checks if a file needs to be downloaded based on cache metadata
 * @param {string} url - Remote URL
 * @param {string} localPath - Local file path
 * @param {number} remoteSize - Remote file size
 * @param {Object} metadata - Item metadata containing cache info
 * @param {boolean} forceDownload - Force download regardless of cache
 * @param {boolean} retryForbidden - Flag to skip known forbidden items
 * @returns {boolean} - True if file should be downloaded
 */
async function shouldDownloadFile(url, localPath, remoteSize, metadata, forceDownload = false, retryForbidden = false) {
  // If force download is enabled, always download
  if (forceDownload) {
    return true;
  }
  
  try {
    // Check if the file exists on disk
    if (!await fs.pathExists(localPath)) {
      console.log(`File doesn't exist locally, downloading`);
      return true;
    }
    
    // Determine the property name from the URL (for checking suffixes)
    // In this case, we know it's "download_url" but we could make this more generic
    const propName = "download_url";
    
    // Get file stats to verify actual size on disk
    const stats = await fs.stat(localPath);
    const localSize = stats.size;
    
    // Check if we have cached metadata in the suffix properties
    const cachedTimestamp = metadata[`${propName}_timestamp`];
    
    // If size matches remote size, we can skip downloading
    if (Math.abs(localSize - remoteSize) < 100) { // 100 bytes tolerance
      if (cachedTimestamp) {
        const cachedDate = new Date(cachedTimestamp);
        const ageMs = Date.now() - cachedDate.getTime();
        const ageDays = ageMs / (1000 * 60 * 60 * 24);
        
        console.log(`File exists with matching size (${formatFileSize(localSize)}), cached ${ageDays.toFixed(1)} days ago`);
      } else {
        console.log(`File exists with matching size (${formatFileSize(localSize)}), skipping download`);
      }
      return false;
    } else {
      console.log(`File exists but size differs (local: ${formatFileSize(localSize)}, remote: ${formatFileSize(remoteSize)}), re-downloading`);
      return true;
    }
  } catch (error) {
    console.warn(`Error checking local file: ${error.message}`);
  }
  
  // Default to downloading if we can't verify
  return true;
}

/**
 * Format file size for human-readable output
 * @param {number} bytes - File size in bytes
 * @returns {string} - Formatted file size
 */
function formatFileSize(bytes) {
  if (bytes < 1024) {
    return `${bytes} bytes`;
  } else if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(2)} KB`;
  } else if (bytes < 1024 * 1024 * 1024) {
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  } else {
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
  }
}

// Helper Semaphore class for controlling concurrency
class Semaphore {
  constructor(max) {
    this.max = max;
    this.current = 0;
    this.queue = [];
  }
  
  async acquire() {
    if (this.current < this.max) {
      this.current++;
      return () => this.release();
    }
    
    // Create a promise that resolves when a permit is available
    return new Promise(resolve => {
      this.queue.push(() => {
        this.current++;
        resolve(() => this.release());
      });
    });
  }
  
  release() {
    this.current--;
    if (this.queue.length > 0) {
      const next = this.queue.shift();
      next();
    }
  }
} 