import { json } from '@sveltejs/kit';
import { InternetArchiveSDK } from 'internetarchive-sdk-js';
import fs from 'fs-extra';
import path from 'path';

const ia = new InternetArchiveSDK();

/**
 * GET handler for the /api/collections endpoint
 * @returns {Response} JSON response with collections data
 */
export async function GET({ url }) {
    try {
        const query = url.searchParams.get('q') || '';
        const page = parseInt(url.searchParams.get('page') || '1');
        const limit = parseInt(url.searchParams.get('limit') || '20');
        
        // Search for collections based on query
        const response = await ia.search({
            query: query ? `${query} AND mediatype:collection` : 'mediatype:collection',
            fields: ['identifier', 'title', 'description', 'date', 'creator', 'subject'],
            rows: limit,
            page: page
        });
        
        return json({
            results: response.response.docs,
            total: response.response.numFound,
            page,
            limit
        });
    } catch (error) {
        console.error('Error fetching collections:', error);
        return json({ error: 'Failed to fetch collections' }, { status: 500 });
    }
}

/**
 * GET handler for /api/collections
 * Returns the top-level index of all collections
 */
export async function GET_top_level_index({ url }) {
  try {
    const dataPath = path.join(process.cwd(), 'static/data/index.json');
    
    if (!fs.existsSync(dataPath)) {
      return json({
        collections: []
      }, { status: 200 });
    }
    
    const indexData = await fs.readJson(dataPath);
    
    // Enhance with full URLs
    const baseUrl = `${url.protocol}//${url.host}`;
    const collectionsWithUrls = indexData.collections.map(collection => ({
      ...collection,
      url: `${baseUrl}/api/collections/${collection.prefix}`,
      dataUrl: `${baseUrl}/data/${collection.indexFile}`
    }));
    
    return json({
      ...indexData,
      collections: collectionsWithUrls
    }, { status: 200 });
  } catch (error) {
    console.error('Error serving collections index:', error);
    return json({ error: 'Failed to retrieve collections' }, { status: 500 });
  }
} 