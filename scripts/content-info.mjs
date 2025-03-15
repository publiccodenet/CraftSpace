#!/usr/bin/env node
/**
 * Content Info - Safely display information about top-level Content directory
 * WITHOUT modifying anything
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Get the top-level repo directory
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '../..');
const CONTENT_DIR = path.join(REPO_ROOT, 'Content');
const COLLECTIONS_DIR = path.join(CONTENT_DIR, 'collections');

console.log('Repository root:', REPO_ROOT);
console.log('Content directory:', CONTENT_DIR);
console.log('Collections directory:', COLLECTIONS_DIR);

// Check if directories exist
console.log('\nDirectory existence:');
console.log(`- Content dir exists: ${fs.existsSync(CONTENT_DIR)}`);
console.log(`- Collections dir exists: ${fs.existsSync(COLLECTIONS_DIR)}`);

// List collections without modifying anything
if (fs.existsSync(COLLECTIONS_DIR)) {
  const items = fs.readdirSync(COLLECTIONS_DIR);
  const collectionDirs = items.filter(item => {
    if (item === '.gitignore' || item === '.gitattributes' || item === 'README.md') {
      return false;
    }
    
    const itemPath = path.join(COLLECTIONS_DIR, item);
    return fs.statSync(itemPath).isDirectory();
  });
  
  console.log(`\nFound ${collectionDirs.length} collections:`);
  
  for (const dir of collectionDirs) {
    const collectionPath = path.join(COLLECTIONS_DIR, dir, 'collection.json');
    if (fs.existsSync(collectionPath)) {
      try {
        const data = fs.readFileSync(collectionPath, 'utf8');
        const collection = JSON.parse(data);
        console.log(`- ${collection.name || dir} (${collection.collection_id || dir})`);
        
        // Check for items directory
        const itemsDir = path.join(COLLECTIONS_DIR, dir, 'items');
        if (fs.existsSync(itemsDir)) {
          try {
            const itemFiles = fs.readdirSync(itemsDir);
            console.log(`  Items directory: ${itemFiles.length} entries`);
          } catch (err) {
            console.log(`  Items directory: Error reading`);
          }
        } else {
          console.log(`  No items directory found`);
        }
        
        // Special handling for scifi collection
        if (dir === 'scifi') {
          console.log('  ⚠️ This is the protected scifi collection - DO NOT MODIFY');
        }
        
      } catch (err) {
        console.log(`- ${dir} (Error reading collection.json)`);
      }
    } else {
      console.log(`- ${dir} (No collection.json found)`);
    }
  }
} else {
  console.log('\nNo collections directory found');
}

// Add to package.json
console.log('\nTo add this script to package.json, run:');
console.log('npm pkg set scripts.content:info="node scripts/content-info.mjs"'); 