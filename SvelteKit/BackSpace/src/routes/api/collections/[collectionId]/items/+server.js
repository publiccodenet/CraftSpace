import { json } from '@sveltejs/kit';
import { contentManager } from '$lib/content';
import { error } from '@sveltejs/kit';

export async function GET({ params, url }) {
  const collectionId = params.collectionId;
  const limit = Number(url.searchParams.get('limit') || '50');
  const skip = Number(url.searchParams.get('skip') || '0');

  try {
    // Initialize content manager if needed
    if (!contentManager.isInitialized()) {
      await contentManager.initialize();
    }

    // For a simplified example, we can mock the response
    // In a real implementation, use: await contentManager.getCollectionItems(collectionId, { limit, skip });
    const result = await listItemsFromCollection(collectionId, { limit, skip });
    
    return json(result);
  } catch (err) {
    console.error(`Error listing items for collection ${collectionId}:`, err);
    
    if (err.code === 'NOT_FOUND') {
      throw error(404, { message: `Collection ${collectionId} not found` });
    }
    
    throw error(500, { message: 'Failed to list items', details: err.message });
  }
}

// Temporary mock function - this should be imported from contentManager in production
async function listItemsFromCollection(collectionId, { limit, skip }) {
  // This is a mock implementation
  // In production, you'd retrieve this from your content manager
  
  // Sample items for demo purposes
  const mockItems = Array.from({ length: 100 }, (_, i) => ({
    id: `item-${i + 1}`,
    title: `Item ${i + 1}`,
    creator: `Author ${i % 10 + 1}`,
    date: new Date(2000 + (i % 23), (i % 12), (i % 28) + 1).toISOString().split('T')[0],
    description: `This is a sample item description for item ${i + 1}`
  }));
  
  // Filter by collection
  const filteredItems = mockItems;
  
  // Apply pagination
  const paginatedItems = filteredItems.slice(skip, skip + limit);
  
  return {
    items: paginatedItems,
    total: filteredItems.length,
    limit,
    skip
  };
} 