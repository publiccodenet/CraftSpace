#!/usr/bin/env node
/**
 * Fetch items from Internet Archive based on collection queries ⬇️🧩
 */
import fs from 'fs-extra';
import path from 'path';
import chalk from 'chalk';
import retry from 'async-retry';
import PQueue from 'p-queue';
import { BaseCommand } from './base-command.js';
import { 
  COLLECTIONS_PATH, 
  FILE_NAMES, 
  EMOJI,
  CLI_DEFAULTS,
  IA_API 
} from '../src/lib/constants/index.js';
import { NotFoundError } from '../src/lib/errors/errors.js';

interface FetchItemsOptions {
  collection?: string;
  force?: boolean;
  limit?: number;
  concurrency?: number;
  json?: boolean;
  verbose?: boolean;
}

class FetchItemsCommand extends BaseCommand {
  constructor() {
    super('fetch-items', 'Fetch items from Internet Archive based on collection queries');
    
    this.program
      .option('-c, --collection <id>', 'Specific collection ID')
      .option('-f, --force', 'Force refresh of all items', CLI_DEFAULTS.FORCE)
      .option('-l, --limit <number>', 'Maximum number of items to fetch per collection', '100')
      .option('--concurrency <number>', 'Number of concurrent requests', '3')
      .option('-j, --json', 'Output as JSON', CLI_DEFAULTS.JSON)
      .option('-v, --verbose', 'Show verbose output', CLI_DEFAULTS.VERBOSE)
      .action(options => this.fetchItems(options));
  }
  
  async fetchItems(options: FetchItemsOptions) {
    try {
      this.banner(`${EMOJI.START} FETCHING ITEMS FROM INTERNET ARCHIVE ${EMOJI.DOWNLOAD}`);
      
      const collectionsPath = COLLECTIONS_PATH;
      
      // Check if collections directory exists
      if (!fs.existsSync(collectionsPath)) {
        throw new NotFoundError(
          `${EMOJI.ERROR} Collections directory does not exist: ${collectionsPath}`,
          'directory',
          collectionsPath
        );
      }
      
      // Get all collections or filter by specified collection
      let collectionDirs = fs.readdirSync(collectionsPath)
        .filter(dir => fs.statSync(path.join(collectionsPath, dir)).isDirectory());
      
      if (options.collection) {
        if (!collectionDirs.includes(options.collection)) {
          throw new NotFoundError(
            `${EMOJI.ERROR} Collection not found: ${options.collection}`,
            'collection',
            options.collection
          );
        }
        collectionDirs = [options.collection];
      }
      
      if (collectionDirs.length === 0) {
        this.warn(`${EMOJI.WARNING} No collections found.`);
        return;
      }
      
      const concurrency = parseInt(options.concurrency || '3', 10);
      const queue = new PQueue({ concurrency });
      
      let totalFetched = 0;
      let totalErrors = 0;
      
      const results = [];
      
      // Process each collection
      for (const dir of collectionDirs) {
        const collectionPath = path.join(collectionsPath, dir);
        const configFile = path.join(collectionPath, FILE_NAMES.COLLECTION);
        
        if (!fs.existsSync(configFile)) {
          this.warn(`${EMOJI.WARNING} No collection configuration found for ${dir}, skipping...`);
          continue;
        }
        
        const collection = await fs.readJSON(configFile);
        
        if (!collection.query) {
          this.warn(`${EMOJI.WARNING} No query defined for collection ${collection.name || dir}, skipping...`);
          continue;
        }
        
        this.info(`${EMOJI.COLLECTION} Processing collection: ${chalk.blue(collection.name || dir)}`);
        this.info(`${EMOJI.API} Query: ${chalk.yellow(collection.query)}`);
        
        // Prepare for items
        const itemsDir = path.join(collectionPath, 'items');
        await fs.ensureDir(itemsDir);
        
        // Fetch items from Internet Archive
        try {
          const limit = parseInt(options.limit || '100', 10);
          this.info(`${EMOJI.DOWNLOAD} Fetching up to ${limit} items...`);
          
          // Simulated API call to get items from Internet Archive
          // In a real implementation, this would make HTTP requests to the IA API
          const fetchedItems = await this.fetchItemsFromIA(collection.query, limit);
          
          if (options.verbose) {
            this.info(`${EMOJI.INFO} Found ${fetchedItems.length} items from Internet Archive`);
          }
          
          // Process each item
          const collectionResult = {
            id: collection.id || dir,
            name: collection.name || dir,
            totalItems: fetchedItems.length,
            processedItems: 0,
            errors: 0
          };
          
          for (const item of fetchedItems) {
            queue.add(async () => {
              try {
                const itemResult = await this.processItem(collection, item, itemsDir, options);
                
                if (itemResult.success) {
                  collectionResult.processedItems++;
                  totalFetched++;
                } else {
                  collectionResult.errors++;
                  totalErrors++;
                }
                
                // Update progress
                if (!options.json) {
                  this.progress(
                    collectionResult.processedItems + collectionResult.errors,
                    fetchedItems.length,
                    `Processing ${collection.name || dir} (${collection.id || dir})...`
                  );
                }
              } catch (error) {
                collectionResult.errors++;
                totalErrors++;
                this.error(`${EMOJI.ERROR} Error processing item: ${error.message}`);
              }
            });
          }
          
          // Add result
          results.push(collectionResult);
        } catch (error) {
          this.error(`${EMOJI.ERROR} Error fetching items for collection ${collection.name || dir}: ${error.message}`);
          results.push({
            id: collection.id || dir,
            name: collection.name || dir,
            error: error.message
          });
        }
      }
      
      // Wait for all items to be processed
      await queue.onIdle();
      
      // Update collection metadata
      for (const result of results) {
        if (result.processedItems !== undefined) {
          const collectionPath = path.join(collectionsPath, result.id);
          const configFile = path.join(collectionPath, FILE_NAMES.COLLECTION);
          
          try {
            const collection = await fs.readJSON(configFile);
            collection.totalItems = result.processedItems;
            collection.lastUpdated = new Date().toISOString();
            await fs.writeJSON(configFile, collection, { spaces: 2 });
          } catch (error) {
            this.error(`${EMOJI.ERROR} Error updating collection metadata for ${result.name}: ${error.message}`);
          }
        }
      }
      
      if (options.json) {
        console.log(JSON.stringify({
          results,
          stats: {
            totalFetched,
            totalErrors,
            collections: results.length
          }
        }, null, 2));
      } else {
        this.banner(`${EMOJI.FINISH} FETCH COMPLETE ${EMOJI.SUCCESS}`);
        console.log(`${EMOJI.COLLECTION} Collections processed: ${chalk.green(results.length.toString())}`);
        console.log(`${EMOJI.ITEM} Items fetched: ${chalk.green(totalFetched.toString())}`);
        
        if (totalErrors > 0) {
          console.log(`${EMOJI.ERROR} Errors: ${chalk.red(totalErrors.toString())}`);
        }
      }
    } catch (error) {
      this.error(`${EMOJI.ERROR} Error fetching items: ${error.message}`);
      process.exit(1);
    }
  }
  
  // Simulated method to fetch items from Internet Archive
  private async fetchItemsFromIA(query: string, limit: number): Promise<any[]> {
    // In a real implementation, this would make an API call to Internet Archive
    // For this example, we'll just return simulated data
    
    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Generate dummy items
    const items = [];
    const itemCount = Math.min(limit, 25); // Simulate fewer items for demo
    
    for (let i = 0; i < itemCount; i++) {
      items.push({
        identifier: `item-${i+1}`,
        title: `Sample Item ${i+1}`,
        creator: `Author ${i+1}`,
        description: `Description for item ${i+1}`,
        date: new Date().toISOString(),
        mediatype: 'texts',
        subject: ['sample', 'test', 'demo']
      });
    }
    
    return items;
  }
  
  private async processItem(collection: any, item: any, itemsDir: string, options: FetchItemsOptions): Promise<{ success: boolean; message?: string }> {
    const itemId = item.identifier;
    const itemPath = path.join(itemsDir, itemId);
    
    // Create item directory
    await fs.ensureDir(itemPath);
    
    // Check if item already exists and we're not forcing refresh
    const itemFile = path.join(itemPath, FILE_NAMES.ITEM);
    if (fs.existsSync(itemFile) && !options.force) {
      if (options.verbose) {
        this.debug(`${EMOJI.SKIPPED} Item ${itemId} already exists, skipping...`);
      }
      return { success: true, message: 'Item already exists' };
    }
    
    try {
      // Save item metadata
      const itemData = {
        id: itemId,
        collectionId: collection.id,
        title: item.title,
        creator: item.creator,
        description: item.description,
        date: item.date,
        mediaType: item.mediatype,
        subjects: item.subject,
        created: new Date().toISOString(),
        lastUpdated: new Date().toISOString(),
        files: []
      };
      
      await fs.writeJSON(itemFile, itemData, { spaces: 2 });
      
      // In a real implementation, you'd fetch additional metadata and files
      // This would be done with retry logic
      
      return { success: true };
    } catch (error) {
      return { success: false, message: error.message };
    }
  }
}

// Run the command if this script is executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  const command = new FetchItemsCommand();
  command.parse();
} 