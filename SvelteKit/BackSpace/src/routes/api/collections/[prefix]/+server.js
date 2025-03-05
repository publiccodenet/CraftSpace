import { json } from '@sveltejs/kit';
import fs from 'fs-extra';
import path from 'path';

/**
 * GET handler for /api/collections/:prefix
 * Returns the index for a specific collection
 */
export async function GET({ params, url }) {
  try {
    const { prefix } = params;
    const dataPath = path.join(process.cwd(), `static/data/${prefix}/${prefix}_index.json`);
    
    if (!fs.existsSync(dataPath)) {
      return json({ error: 'Collection not found' }, { status: 404 });
    }
    
    const indexData = await fs.readJson(dataPath);
    
    // Enhance with full URLs
    const baseUrl = `${url.protocol}//${url.host}`;
    const chunksWithUrls = indexData.chunks.map(chunk => ({
      file: chunk,
      url: `${baseUrl}/data/${prefix}/${chunk}`
    }));
    
    return json({
      ...indexData,
      chunks: chunksWithUrls
    }, { status: 200 });
  } catch (error) {
    console.error(`Error serving collection "${params.prefix}":`, error);
    return json({ error: 'Failed to retrieve collection' }, { status: 500 });
  }
} 