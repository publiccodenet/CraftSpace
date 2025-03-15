#!/usr/bin/env node
/**
 * CommonJS version of collection management to avoid ESM issues
 */
const fs = require('fs-extra');
const path = require('path');
const chalk = require('chalk');
const { Command } = require('commander');

// Root directory for collections
const CONTENT_DIR = path.resolve('Content');
const COLLECTIONS_DIR = path.join(CONTENT_DIR, 'collections');

// Basic command setup
const program = new Command();
program
  .name('manage-collections')
  .description('Manage BackSpace collections')
  .version('1.0.0');

// List command
program
  .command('list')
  .description('List all collections')
  .option('-j, --json', 'Output as JSON')
  .action(async (options) => {
    try {
      console.log(chalk.blue('Listing collections from:'), COLLECTIONS_DIR);
      
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
      
      console.log(chalk.blue(`Found ${collectionDirs.length} collections`));
      
      // Read collection data
      const collections = [];
      for (const dir of collectionDirs) {
        const collectionPath = path.join(COLLECTIONS_DIR, dir, 'collection.json');
        
        if (await fs.pathExists(collectionPath)) {
          try {
            const collection = await fs.readJson(collectionPath);
            collections.push(collection);
          } catch (err) {
            console.error(chalk.red(`Error reading collection ${dir}:`), err.message);
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
      console.error(chalk.red('Error listing collections:'), error.message);
      process.exit(1);
    }
  });

// Add a new command for collections:list
program.parse(); 