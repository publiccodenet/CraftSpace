#!/usr/bin/env node
/**
 * Minimal version of the collections management script
 */
import fs from 'fs-extra';
import path from 'path';
import chalk from 'chalk';
import { Command } from 'commander';

// Create simple logger
const log = {
  info: (...args) => console.log(chalk.blue('INFO:'), ...args),
  error: (...args) => console.error(chalk.red('ERROR:'), ...args),
  debug: (...args) => console.log(chalk.gray('DEBUG:'), ...args)
};

// Root directory for collections
const CONTENT_DIR = path.resolve('Content');
const COLLECTIONS_DIR = path.join(CONTENT_DIR, 'collections');

// Basic command setup
const program = new Command();
program
  .name('manage-collections-minimal')
  .description('Manage BackSpace collections')
  .version('1.0.0');

// List command
program
  .command('list')
  .description('List all collections')
  .option('-j, --json', 'Output as JSON')
  .action(async (options) => {
    try {
      log.info('Listing collections from:', COLLECTIONS_DIR);
      
      // Ensure directories exist
      await fs.ensureDir(COLLECTIONS_DIR);
      
      // Read collection directories
      const items = await fs.readdir(COLLECTIONS_DIR);
      
      // Filter out non-directories and special files
      const collectionDirs = [];
      for (const item of items) {
        if (item === '.gitignore' || item === '.gitattributes' || item === 'README.md') {
          continue;
        }
        
        const itemPath = path.join(COLLECTIONS_DIR, item);
        const stat = await fs.stat(itemPath);
        
        if (stat.isDirectory()) {
          collectionDirs.push(item);
        }
      }
      
      log.info(`Found ${collectionDirs.length} collections`);
      
      // Read collection data
      const collections = [];
      for (const dir of collectionDirs) {
        const collectionPath = path.join(COLLECTIONS_DIR, dir, 'collection.json');
        
        if (await fs.pathExists(collectionPath)) {
          try {
            const collection = await fs.readJson(collectionPath);
            collections.push(collection);
          } catch (err) {
            log.error(`Error reading collection ${dir}:`, err.message);
          }
        }
      }
      
      // Display collections
      if (options.json) {
        console.log(JSON.stringify(collections, null, 2));
      } else {
        console.log(chalk.green('\nCollections:'));
        if (collections.length === 0) {
          console.log('  No collections found');
        } else {
          collections.forEach(collection => {
            console.log(`  - ${chalk.yellow(collection.name)} (${collection.collection_id})`);
            console.log(`    Query: ${collection.query}`);
            console.log(`    Items: ${collection.totalItems || 0}`);
            console.log(`    Last Updated: ${collection.lastUpdated || 'Never'}`);
            console.log();
          });
        }
      }
    } catch (error) {
      log.error('Error listing collections:', error.message);
      process.exit(1);
    }
  });

// Create command
program
  .command('create')
  .description('Create a new collection')
  .requiredOption('-i, --id <id>', 'Collection ID')
  .requiredOption('-n, --name <name>', 'Collection name')
  .requiredOption('-q, --query <query>', 'IA query string')
  .option('-d, --description <description>', 'Collection description')
  .option('-s, --sort <sort>', 'Sort order', 'downloads desc')
  .option('-l, --limit <limit>', 'Limit number of items', '0')
  .action(async (options) => {
    try {
      log.info(`Creating collection: ${options.name} (${options.id})`);
      
      // Ensure collections directory exists
      await fs.ensureDir(COLLECTIONS_DIR);
      
      const collectionDir = path.join(COLLECTIONS_DIR, options.id);
      const collectionPath = path.join(collectionDir, 'collection.json');
      
      // Check if collection already exists
      if (await fs.pathExists(collectionPath)) {
        log.error(`Collection with ID '${options.id}' already exists`);
        process.exit(1);
      }
      
      // Create collection object
      const collection = {
        collection_id: options.id,
        name: options.name,
        query: options.query,
        description: options.description || '',
        sort: options.sort,
        limit: parseInt(options.limit, 10),
        lastUpdated: new Date().toISOString(),
        totalItems: 0,
        items: {}
      };
      
      // Create collection directory
      await fs.ensureDir(collectionDir);
      
      // Write collection file
      await fs.writeJson(collectionPath, collection, { spaces: 2 });
      
      log.info(`Collection created: ${options.name} (${options.id})`);
    } catch (error) {
      log.error('Error creating collection:', error.message);
      process.exit(1);
    }
  });

// Parse command line arguments
program.parse(); 