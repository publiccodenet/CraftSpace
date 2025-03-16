import { json } from '@sveltejs/kit';
import fs from 'fs-extra';
import path from 'path';
import { PATHS } from '$lib/constants';

/**
 * GET handler for collections API
 * Returns list of all collections
 */
export async function GET() {
    try {
        // Path to collections directory
        const collectionsDir = PATHS.COLLECTIONS_DIR;
        
        // Check if collections directory exists
        if (!fs.existsSync(collectionsDir)) {
            return json({ error: 'Collections directory not found' }, { status: 404 });
        }
        
        // Get all subdirectories in the collections directory
        const collections = fs.readdirSync(collectionsDir)
            .filter(item => {
                const itemPath = path.join(collectionsDir, item);
                return fs.statSync(itemPath).isDirectory();
            })
            .map(collectionId => {
                // Path to collection.json
                const configPath = path.join(collectionsDir, collectionId, 'collection.json');
                
                if (fs.existsSync(configPath)) {
                    try {
                        // Read collection data
                        const collectionData = fs.readJSONSync(configPath);
                        return {
                            id: collectionId,
                            ...collectionData
                        };
                    } catch (error) {
                        console.error(`Error reading collection ${collectionId}:`, error);
                        return {
                            id: collectionId,
                            name: collectionId,
                            error: 'Error reading collection data'
                        };
                    }
                } else {
                    return {
                        id: collectionId,
                        name: collectionId
                    };
                }
            });
        
        return json(collections);
    } catch (error) {
        console.error('Error fetching collections:', error);
        return json({ error: 'Error fetching collections' }, { status: 500 });
    }
} 