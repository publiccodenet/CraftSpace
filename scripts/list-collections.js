#!/usr/bin/env node
/**
 * Ultra-minimal collection lister with no dependencies
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Get current directory in ESM
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const COLLECTIONS_DIR = path.resolve('Content/collections');

console.log('Listing collections from:', COLLECTIONS_DIR);

try {
  // Ensure directory exists
  if (!fs.existsSync(COLLECTIONS_DIR)) {
    console.log('Collections directory does not exist');
    process.exit(0);
  }

  // List directories
  const items = fs.readdirSync(COLLECTIONS_DIR);
  const collectionDirs = items.filter(item => {
    if (item === '.gitignore' || item === '.gitattributes' || item === 'README.md') {
      return false;
    }
    
    const itemPath = path.join(COLLECTIONS_DIR, item);
    return fs.statSync(itemPath).isDirectory();
  });
  
  console.log(`Found ${collectionDirs.length} collections:`);
  
  // List collections
  for (const dir of collectionDirs) {
    const collectionPath = path.join(COLLECTIONS_DIR, dir, 'collection.json');
    if (fs.existsSync(collectionPath)) {
      try {
        const data = fs.readFileSync(collectionPath, 'utf8');
        const collection = JSON.parse(data);
        console.log(`- ${collection.name} (${collection.collection_id})`);
        console.log(`  Query: ${collection.query}`);
        console.log(`  Items: ${collection.totalItems || 0}`);
        console.log(`  Last Updated: ${collection.lastUpdated || 'Never'}`);
        console.log();
      } catch (err) {
        console.error(`Error reading collection ${dir}:`, err.message);
      }
    }
  }
} catch (error) {
  console.error('Error:', error.message);
  process.exit(1);
} 