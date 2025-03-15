import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import { createExportTarget, listExportProfiles, getExportProfile } from './export-profiles.js';

/**
 * Manages collections registry with Internet Archive metadata structure
 * Uses directory structure as the single source of truth
 * Uses *_metadata.json suffix for item metadata files
 */
export class IACollectionsRegistry {
  constructor(collectionsDir = path.resolve(process.cwd(), '../../Collections'), cacheFile = path.resolve(process.cwd(), './collections-cache.json')) {
    this.collectionsDir = collectionsDir;
    this.cacheFile = cacheFile;
    
    // In-memory registry used only during runtime
    this.collections = [];
    // Load collections from directory structure
    this.refresh();
  }
  
  /**
   * Refresh the in-memory collection list from directory structure
   */
  refresh() {
    try {
      // Ensure collections directory exists
      if (!fs.existsSync(this.collectionsDir)) {
        fs.ensureDirSync(this.collectionsDir);
        return;
      }
      
      // Clear existing in-memory registry
      this.collections = [];
      
      // Get all subdirectories
      const dirs = fs.readdirSync(this.collectionsDir);
      
      // Process each directory
      for (const dir of dirs) {
        const collectionPath = path.join(this.collectionsDir, dir);
        if (fs.statSync(collectionPath).isDirectory()) {
          const metadataPath = path.join(collectionPath, 'collection.json');
          
          if (fs.existsSync(metadataPath)) {
            try {
              const collection = fs.readJSONSync(metadataPath);
              
              // Count items in the collection by looking for item directories
              const itemsDir = path.join(collectionPath, 'items');
              if (fs.existsSync(itemsDir)) {
                const itemDirs = fs.readdirSync(itemsDir).filter(file => 
                  fs.statSync(path.join(itemsDir, file)).isDirectory()
                );
                collection.totalItems = itemDirs.length;
              } else {
                collection.totalItems = 0;
              }
              
              this.collections.push(collection);
            } catch (error) {
              console.error(`Error reading collection metadata at ${metadataPath}:`, error);
            }
          }
        }
      }
      
      // Optionally update the cache file
      this.updateCache();
    } catch (error) {
      console.error('Error refreshing collections:', error);
    }
  }
  
  /**
   * Update the collections cache file
   */
  updateCache() {
    try {
      // Create a cache object with just the metadata, not the items
      const cache = {
        collections: this.collections.map(c => ({
          prefix: c.prefix,
          name: c.name,
          query: c.query,
          sort: c.sort,
          limit: c.limit,
          includeInUnity: c.includeInUnity,
          totalItems: c.totalItems || 0,
          lastUpdated: c.lastUpdated
        })),
        lastUpdated: new Date().toISOString()
      };
      
      // Write to cache file
      fs.writeJSONSync(this.cacheFile, cache, { spaces: 2 });
    } catch (error) {
      console.error('Error updating cache file:', error);
    }
  }
  
  /**
   * Get all collections
   */
  getCollections() {
    return this.collections;
  }
  
  /**
   * Get a specific collection by prefix
   */
  getCollection(prefix) {
    return this.collections.find(collection => collection.prefix === prefix);
  }
  
  /**
   * Get items for a specific collection by scanning the items directory
   */
  getCollectionItems(prefix) {
    try {
      const collectionDir = path.join(this.collectionsDir, prefix);
      const itemsDir = path.join(collectionDir, 'items');
      
      if (!fs.existsSync(itemsDir)) {
        return [];
      }
      
      const itemDirs = fs.readdirSync(itemsDir).filter(file => {
        const stat = fs.statSync(path.join(itemsDir, file));
        return stat.isDirectory();
      });
      
      const items = [];
      for (const itemDir of itemDirs) {
        try {
          const metadataPath = path.join(itemsDir, itemDir, 'item.json');
          if (fs.existsSync(metadataPath)) {
            const item = fs.readJSONSync(metadataPath);
            items.push(item);
          }
        } catch (error) {
          console.error(`Error reading item metadata for ${itemDir}:`, error);
        }
      }
      
      return items;
    } catch (error) {
      console.error(`Error getting items for collection ${prefix}:`, error);
      return [];
    }
  }
  
  /**
   * Register a new collection
   */
  registerCollection(collection) {
    try {
      // Ensure collections directory exists
      const collectionPath = path.join(this.collectionsDir, collection.prefix);
      
      fs.ensureDirSync(this.collectionsDir);
      fs.ensureDirSync(collectionPath);
      fs.ensureDirSync(path.join(collectionPath, 'items'));
      
      // Write collection metadata to collection.json
      fs.writeJSONSync(
        path.join(collectionPath, 'collection.json'),
        collection,
        { spaces: 2 }
      );
      
      // Update in-memory registry
      const existingIndex = this.collections.findIndex(c => c.prefix === collection.prefix);
      if (existingIndex === -1) {
        this.collections.push(collection);
      } else {
        this.collections[existingIndex] = collection;
      }
      
      // Update cache
      this.updateCache();
      
      // Process export profiles if specified
      if (collection.exportProfiles && Array.isArray(collection.exportProfiles)) {
        collection.exportTargets = collection.exportTargets || [];
        
        // Convert each profile name to an export target
        for (const profileConfig of collection.exportProfiles) {
          let profileName, overrides = {};
          
          // Handle both string profiles and profile objects with overrides
          if (typeof profileConfig === 'string') {
            profileName = profileConfig;
          } else if (profileConfig && profileConfig.name) {
            profileName = profileConfig.name;
            overrides = { ...profileConfig };
            delete overrides.name; // Remove name from overrides
          } else {
            console.warn(`Invalid export profile configuration: ${JSON.stringify(profileConfig)}`);
            continue;
          }
          
          try {
            // Create export target from profile with any overrides
            const exportTarget = createExportTarget(profileName, overrides);
            
            // Add to export targets
            collection.exportTargets.push(exportTarget);
          } catch (profileError) {
            console.error(`Error creating export target from profile ${profileName}:`, profileError);
          }
        }
      }
      
      return collection;
    } catch (error) {
      console.error('Error creating collection directories:', error);
      console.error('Error details:', error.stack);
      throw error;
    }
  }
  
  /**
   * Add or update an item in a collection
   */
  setItemMetadata(collectionPrefix, itemId, metadata) {
    try {
      const collectionDir = path.join(this.collectionsDir, collectionPrefix);
      const itemsDir = path.join(collectionDir, 'items');
      const itemDir = path.join(itemsDir, itemId);
      
      // Ensure directories exist
      fs.ensureDirSync(itemsDir);
      fs.ensureDirSync(itemDir);
      
      // Save item metadata to item.json
      const metadataPath = path.join(itemDir, 'item.json');
      fs.writeJSONSync(metadataPath, metadata, { spaces: 2 });
      
      // Update collection totalItems count in memory
      const collection = this.getCollection(collectionPrefix);
      if (collection) {
        const items = this.getCollectionItems(collectionPrefix);
        collection.totalItems = items.length;
        
        // Update collection metadata file
        const collectionPath = path.join(collectionDir, 'collection.json');
        fs.writeJSONSync(collectionPath, collection, { spaces: 2 });
        
        // Update cache
        this.updateCache();
      }
      
      return true;
    } catch (error) {
      console.error(`Error setting item metadata for ${itemId} in ${collectionPrefix}:`, error);
      return false;
    }
  }
  
  /**
   * Remove an item from a collection
   */
  removeItem(collectionPrefix, itemId) {
    try {
      const collectionDir = path.join(this.collectionsDir, collectionPrefix);
      const itemsDir = path.join(collectionDir, 'items');
      const itemDir = path.join(itemsDir, itemId);
      
      // Remove the entire item directory if it exists
      if (fs.existsSync(itemDir)) {
        fs.removeSync(itemDir);
      }
      
      // Update collection totalItems count in memory
      const collection = this.getCollection(collectionPrefix);
      if (collection) {
        const items = this.getCollectionItems(collectionPrefix);
        collection.totalItems = items.length;
        
        // Update collection metadata file
        const collectionPath = path.join(collectionDir, 'collection.json');
        fs.writeJSONSync(collectionPath, collection, { spaces: 2 });
        
        // Update cache
        this.updateCache();
      }
      
      return true;
    } catch (error) {
      console.error(`Error removing item ${itemId} from ${collectionPrefix}:`, error);
      return false;
    }
  }
  
  /**
   * Unregister a collection by prefix
   */
  unregisterCollection(prefix) {
    const index = this.collections.findIndex(c => c.prefix === prefix);
    
    if (index >= 0) {
      const collection = this.collections[index];
      
      // Option 1: Just remove from in-memory registry (leaves files in place)
      this.collections.splice(index, 1);
      
      // Option 2 (more aggressive): Remove the collection directory
      // Uncomment the next line to enable directory removal
      // fs.removeSync(path.join(this.collectionsDir, prefix));
      
      // Update cache
      this.updateCache();
      
      return collection;
    }
    
    return null;
  }
  
  /**
   * Scan collections directory and update registry
   */
  async scanCollectionsDirectory() {
    try {
      // Ensure collections directory exists
      await fs.ensureDir(this.collectionsDir);
      
      // Clear existing in-memory registry
      this.collections = [];
      
      // Get all subdirectories
      const dirs = await fs.readdir(this.collectionsDir);
      let count = 0;
      
      // Process each directory
      for (const dir of dirs) {
        const collectionPath = path.join(this.collectionsDir, dir);
        const stat = await fs.stat(collectionPath);
        
        // Only process directories
        if (stat.isDirectory()) {
          const collectionJsonPath = path.join(collectionPath, 'collection.json');
          
          // Check if collection.json exists
          if (await fs.pathExists(collectionJsonPath)) {
            // Read collection data
            const collection = await fs.readJSON(collectionJsonPath);
            
            // Count items by scanning for item directories
            const itemsDir = path.join(collectionPath, 'items');
            if (await fs.pathExists(itemsDir)) {
              const itemDirs = await fs.readdir(itemsDir);
              const directories = [];
              
              for (const item of itemDirs) {
                const itemPath = path.join(itemsDir, item);
                const itemStat = await fs.stat(itemPath);
                if (itemStat.isDirectory()) {
                  directories.push(item);
                }
              }
              
              collection.totalItems = directories.length;
            } else {
              collection.totalItems = 0;
            }
            
            // Add to registry
            this.collections.push(collection);
            count++;
          }
        }
      }
      
      // Update cache
      this.updateCache();
      
      return count;
    } catch (error) {
      console.error('Error scanning collections directory:', error);
      throw error;
    }
  }
  
  /**
   * Export collections to Unity
   */
  async exportCollectionsToUnity(unityDir = '../Unity/CraftSpace/Assets/Resources/Collections') {
    try {
      // Ensure Unity directory exists
      await fs.ensureDir(unityDir);
      
      const unityCollections = this.collections.filter(c => c.includeInUnity);
      
      for (const collection of unityCollections) {
        const sourceDir = path.join(this.collectionsDir, collection.prefix);
        const targetDir = path.join(unityDir, collection.prefix);
        
        // Ensure Unity collection directory exists
        await fs.ensureDir(targetDir);
        
        // First copy collection-level files
        if (await fs.pathExists(path.join(sourceDir, 'collection.json'))) {
          await fs.copy(
            path.join(sourceDir, 'collection.json'),
            path.join(targetDir, 'collection.json')
          );
        }
        
        // Create items directory in Unity
        const unityItemsDir = path.join(targetDir, 'items');
        await fs.ensureDir(unityItemsDir);
        
        // Copy items directory with metadata files and cover images
        const sourceItemsDir = path.join(sourceDir, 'items');
        
        if (await fs.pathExists(sourceItemsDir)) {
          // Get list of item directories
          const itemDirs = await fs.readdir(sourceItemsDir);
          
          // Track item IDs for index generation
          const itemIds = [];
          
          for (const itemDir of itemDirs) {
            const itemPath = path.join(sourceItemsDir, itemDir);
            const itemStat = await fs.stat(itemPath);
            
            // Only process directories
            if (itemStat.isDirectory()) {
              // Check for item.json
              const metadataPath = path.join(itemPath, 'item.json');
              if (await fs.pathExists(metadataPath)) {
                // Create item directory in Unity
                const unityItemDir = path.join(unityItemsDir, itemDir);
                await fs.ensureDir(unityItemDir);
                
                // Copy metadata JSON
                await fs.copy(metadataPath, path.join(unityItemDir, 'item.json'));
                
                // Copy cover image if it exists
                const coverFile = findFileCaseInsensitive(itemPath, 'cover.jpg');
                if (coverFile) {
                  await fs.copy(path.join(itemPath, coverFile), path.join(unityItemDir, 'cover.jpg'));
                }
                
                // Add item ID to index
                itemIds.push(itemDir);
              }
            }
          }
          
          // Generate and write index.json for Unity/runtime use
          if (itemIds.length > 0) {
            await fs.writeJSON(
              path.join(targetDir, 'index.json'),
              itemIds,
              { spaces: 2 }
            );
          }
        }
      }
      
      // Generate Unity registry
      const unityRegistryData = {
        collections: unityCollections.map(c => ({
          prefix: c.prefix,
          name: c.name,
          description: c.description || '',
          totalItems: c.totalItems || 0
        })),
        lastUpdated: new Date().toISOString()
      };
      
      // Save Unity registry
      await fs.writeJSON(
        path.join(unityDir, 'registry.json'), 
        unityRegistryData, 
        { spaces: 2 }
      );
      
      return unityCollections.length;
    } catch (error) {
      console.error('Error exporting to Unity:', error);
      throw error;
    }
  }
}

// During Unity export - case insensitive file checks
// Modify the file lookup by creating a helper function
function findFileCaseInsensitive(directory, targetFilename) {
  const targetLower = targetFilename.toLowerCase();
  const files = fs.readdirSync(directory);
  return files.find(f => f.toLowerCase() === targetLower);
}

/**
 * CLI usage
 */
export async function main() {
  const args = process.argv.slice(2);
  const command = args[0] || 'help';
  
  // Create registry with debug output
  try {
    const collectionsDir = path.resolve(process.cwd(), '../../Collections');
    const registry = new IACollectionsRegistry(collectionsDir);
    
    switch (command) {
      case 'list':
        console.log('Available collections:');
        const collections = registry.getCollections();
        
        if (collections.length === 0) {
          console.log('No collections found.');
        } else {
          for (const c of collections) {
            console.log(`[${c.prefix}] ${c.name} - ${c.totalItems || 0} items${c.includeInUnity ? ' (in Unity)' : ''}`);
          }
          console.log(`\nTotal: ${collections.length} collections`);
        }
        break;
        
      case 'get':
        if (args.length < 2) {
          console.error('Usage: node ia-collections-registry.js get <prefix>');
          process.exit(1);
        }
        
        const prefix = args[1];
        const collection = registry.getCollection(prefix);
        
        if (collection) {
          console.log(JSON.stringify(collection, null, 2));
        } else {
          console.error(`Collection "${prefix}" not found.`);
          process.exit(1);
        }
        break;
        
      case 'register':
        console.log('===== Registering Collection =====');
        
        if (args.length < 4) {
          console.error('Usage: node ia-collections-registry.js register <prefix> <name> <query> [--include-in-unity] [--sort=order] [--limit=number] [--profile=profile1,profile2]');
          process.exit(1);
        }
        
        const collectionPrefix = args[1];
        const collectionName = args[2];
        const collectionQuery = args[3];
        
        console.log(`Prefix: ${collectionPrefix}`);
        console.log(`Name: ${collectionName}`);
        console.log(`Query: ${collectionQuery}`);
        
        // Parse options
        const includeInUnity = args.includes('--include-in-unity');
        let sort = 'downloads desc';
        let limit = 0;
        let exportProfiles = [];
        
        for (let i = 4; i < args.length; i++) {
          const arg = args[i];
          if (arg.startsWith('--sort=')) {
            sort = arg.split('=')[1];
          } else if (arg.startsWith('--limit=')) {
            limit = parseInt(arg.split('=')[1], 10);
          } else if (arg.startsWith('--profile=') || arg.startsWith('--profiles=')) {
            const profilesStr = arg.split('=')[1];
            exportProfiles = profilesStr.split(',').map(p => p.trim()).filter(Boolean);
          }
        }
        
        try {
          const newCollection = registry.registerCollection({
            prefix: collectionPrefix,
            name: collectionName,
            query: collectionQuery,
            sort,
            limit,
            includeInUnity,
            exportProfiles,
            lastUpdated: new Date().toISOString()
          });
          
          console.log(`Collection "${newCollection.name}" registered with prefix "${newCollection.prefix}".`);
          
          if (exportProfiles.length > 0) {
            console.log(`Configured with export profiles: ${exportProfiles.join(', ')}`);
          }
        } catch (error) {
          console.error('ERROR REGISTERING COLLECTION:', error);
          process.exit(1);
        }
        break;
        
      case 'unregister':
        if (args.length < 2) {
          console.error('Usage: node ia-collections-registry.js unregister <prefix>');
          process.exit(1);
        }
        
        const removedCollection = registry.unregisterCollection(args[1]);
        
        if (removedCollection) {
          console.log(`Collection "${removedCollection.name}" unregistered.`);
        } else {
          console.error(`Collection "${args[1]}" not found.`);
          process.exit(1);
        }
        break;
        
      case 'scan':
        const count = await registry.scanCollectionsDirectory();
        console.log(`Scan complete. Found ${count} collections.`);
        break;
        
      case 'unity-export':
        const exportedCount = await registry.exportCollectionsToUnity();
        console.log(`Exported ${exportedCount} collections to Unity.`);
        break;
        
      case 'list-profiles':
        console.log('Available export profiles:');
        const profiles = listExportProfiles();
        
        if (profiles.length === 0) {
          console.log('No export profiles defined.');
        } else {
          for (const profileName of profiles) {
            const profile = getExportProfile(profileName);
            console.log(`- ${profileName}: ${profile.description || 'No description'}`);
          }
          console.log(`\nTotal: ${profiles.length} profiles`);
        }
        break;
        
      case 'help':
      default:
        console.log('Internet Archive Collections Registry Manager');
        console.log('');
        console.log('Usage:');
        console.log('  node ia-collections-registry.js <command> [options]');
        console.log('');
        console.log('Commands:');
        console.log('  list                                      List all registered collections');
        console.log('  get <prefix>                             Get details for a specific collection');
        console.log('  register <prefix> <name> <query> [opts]  Register a new collection');
        console.log('  unregister <prefix>                      Unregister a collection');
        console.log('  scan                                     Scan collections directory and update registry');
        console.log('  unity-export                             Export Unity collections to Resources directory');
        console.log('  list-profiles                            List available export profiles');
        console.log('  help                                     Show this help message');
    }
  } catch (error) {
    console.error('Error initializing registry:', error);
    process.exit(1);
  }
}

// Check if this file is being run directly
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// ES modules equivalent of the CommonJS require.main === module check
// We only need ONE way to detect if this is the main module
const isMainModule = process.argv[1] === __filename;

if (isMainModule) {
  // Running as main module
  try {
    main().catch(error => {
      console.error('Error:', error);
      process.exit(1);
    });
  } catch (error) {
    console.error("Error running main:", error);
    process.exit(1);
  }
} 