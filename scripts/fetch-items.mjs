#!/usr/bin/env node
/**
 * Fetch items for a collection 
 * Uses the correct top-level repository Content directory
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Get the top-level repo directory (two directories up from BackSpace)
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '../..');
const CONTENT_DIR = path.join(REPO_ROOT, 'Content');
const COLLECTIONS_DIR = path.join(CONTENT_DIR, 'collections');

console.log('Content directory:', CONTENT_DIR);
console.log('Collections directory:', COLLECTIONS_DIR);

// Parse command line arguments
const args = process.argv.slice(2);
if (args.length < 1) {
  console.error('Usage: node fetch-items.mjs <collection_id> [limit]');
  process.exit(1);
}

const [collectionId, limitArg] = args;
const limit = limitArg ? parseInt(limitArg, 10) : 10;

const collectionPath = path.join(COLLECTIONS_DIR, collectionId, 'collection.json');

// Check if collection exists
if (!fs.existsSync(collectionPath)) {
  console.error(`Collection with ID "${collectionId}" not found!`);
  console.error(`Path checked: ${collectionPath}`);
  process.exit(1);
}

// Read collection
const collection = JSON.parse(fs.readFileSync(collectionPath, 'utf8'));
console.log(`Collection: ${collection.name} (${collection.collection_id || collectionId})`);
console.log(`Query: ${collection.query || 'No query defined'}`);
console.log(`Items: ${collection.totalItems || 'Unknown'}`);

// Function to fetch items from Internet Archive
async function fetchItemsFromIA(query, maxResults) {
  if (!query) {
    console.error('No query defined in collection');
    return [];
  }
  
  console.log(`Fetching from Internet Archive: ${query}`);
  
  const url = `https://archive.org/advancedsearch.php?q=${encodeURIComponent(query)}&fl[]=identifier&fl[]=title&fl[]=description&fl[]=creator&fl[]=date&fl[]=mediatype&fl[]=collection&fl[]=downloads&fl[]=item_size&fl[]=format&output=json&rows=${maxResults}&page=1`;
  
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP error ${response.status}`);
    }
    
    const data = await response.json();
    return data.response.docs;
  } catch (error) {
    console.error(`Error fetching from IA: ${error.message}`);
    return [];
  }
}

// Main function
async function main() {
  try {
    // Special case for scifi collection
    if (collectionId === 'scifi') {
      console.log('⚠️ WARNING: The scifi collection should not be modified');
      console.log('This collection contains valuable data that should be preserved.');
      
      // Just show some stats instead
      const itemsPath = path.join(COLLECTIONS_DIR, collectionId, 'items');
      if (fs.existsSync(itemsPath)) {
        const itemDirs = fs.readdirSync(itemsPath);
        console.log(`Collection has ${itemDirs.length} item directories`);
        
        // Show some example items
        const sampleSize = Math.min(5, itemDirs.length);
        console.log(`\nSample of ${sampleSize} items:`);
        for (let i = 0; i < sampleSize; i++) {
          const itemDir = itemDirs[i];
          const itemPath = path.join(itemsPath, itemDir, 'item.json');
          if (fs.existsSync(itemPath)) {
            try {
              const item = JSON.parse(fs.readFileSync(itemPath, 'utf8'));
              console.log(`- ${item.title} (${item.id})`);
            } catch (err) {
              console.log(`- ${itemDir} (error reading data)`);
            }
          } else {
            console.log(`- ${itemDir} (no item.json found)`);
          }
        }
      } else {
        console.log('No items directory found for this collection');
      }
      
      return;
    }
    
    // For other collections, proceed with fetching
    const items = await fetchItemsFromIA(collection.query, limit);
    console.log(`Found ${items.length} items from Internet Archive`);
    
    if (items.length === 0) {
      console.log('No items found. Check your query.');
      return;
    }
    
    // Process items
    const processedItems = {};
    for (const item of items) {
      processedItems[item.identifier] = {
        id: item.identifier,
        title: item.title || 'Untitled',
        creator: Array.isArray(item.creator) ? item.creator[0] : item.creator,
        date: item.date || new Date().toISOString().split('T')[0] + 'T00:00:00Z',
        description: item.description || '',
        mediatype: item.mediatype || 'texts',
        collection: item.collection || [],
        subject: [],
        downloads: item.downloads || 0,
        item_size: item.item_size || 0,
        format: item.format || [],
        addedAt: new Date().toISOString()
      };
    }
    
    // Ask for confirmation before modifying
    console.log(`\nReady to add ${items.length} items to collection ${collection.name}`);
    console.log('Press Ctrl+C to cancel or Enter to continue...');
    await new Promise(resolve => process.stdin.once('data', resolve));
    
    // Create items directory if it doesn't exist
    const itemsDir = path.join(COLLECTIONS_DIR, collectionId, 'items');
    if (!fs.existsSync(itemsDir)) {
      fs.mkdirSync(itemsDir, { recursive: true });
    }
    
    // Save each item
    for (const [id, item] of Object.entries(processedItems)) {
      const itemDir = path.join(itemsDir, id);
      if (!fs.existsSync(itemDir)) {
        fs.mkdirSync(itemDir, { recursive: true });
      }
      
      const itemPath = path.join(itemDir, 'item.json');
      fs.writeFileSync(itemPath, JSON.stringify(item, null, 2));
    }
    
    // Update collection
    collection.items = collection.items || {};
    Object.assign(collection.items, processedItems);
    collection.totalItems = Object.keys(collection.items).length;
    collection.lastUpdated = new Date().toISOString();
    
    fs.writeFileSync(collectionPath, JSON.stringify(collection, null, 2));
    
    console.log(`\n✅ Added ${items.length} items to collection ${collection.name}`);
    console.log(`Total items in collection: ${collection.totalItems}`);
    
  } catch (error) {
    console.error(`\nError: ${error.message}`);
    process.exit(1);
  }
}

main(); 