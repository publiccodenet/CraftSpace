import { json } from '@sveltejs/kit';

// Sample collections data 
const sampleCollections = [
  {
    id: 'scifi-books',
    name: 'Science Fiction Books',
    query: 'collection:sciencefiction',
    lastUpdated: new Date().toISOString(),
    totalItems: 42,
    description: 'A collection of classic science fiction works'
  },
  {
    id: 'golden-age-comics',
    name: 'Golden Age Comics',
    query: 'collection:comics AND date:[1938 TO 1956]',
    lastUpdated: new Date().toISOString(), 
    totalItems: 24,
    description: 'Comic books from the Golden Age era (1938-1956)'
  },
  {
    id: 'vintage-software',
    name: 'Vintage Software',
    query: 'collection:softwarelibrary',
    lastUpdated: new Date().toISOString(),
    totalItems: 18,
    description: 'Classic software and games from computing history'
  }
];

/**
 * GET handler for the /api/collections endpoint
 * @returns {Response} JSON response with collections data
 */
export function GET() {
  return json({
    collections: sampleCollections
  });
}

export async function POST({ request }) {
  try {
    const newCollection = await request.json();
    
    return json({
      success: true,
      collection: {
        ...newCollection,
        id: newCollection.id || `collection-${Date.now()}`,
        lastUpdated: new Date().toISOString(),
        totalItems: 0
      }
    }, { status: 201 });
  } catch (error) {
    return json({
      success: false,
      message: error.message || 'Failed to create collection'
    }, { status: 400 });
  }
} 