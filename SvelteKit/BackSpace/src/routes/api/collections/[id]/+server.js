import { json } from '@sveltejs/kit';

// Sample collections data (would come from a database in production)
const sampleCollections = [
  {
    id: 'scifi-books',
    name: 'Science Fiction Books',
    query: 'collection:sciencefiction',
    lastUpdated: new Date().toISOString(),
    totalItems: 42,
    description: 'A collection of classic science fiction works',
    sort: 'downloads desc',
    limit: 100,
    items: {
      // Sample items would go here
      'asimov-foundation': {
        id: 'asimov-foundation',
        title: 'Foundation',
        creator: 'Isaac Asimov',
        date: '1951',
        description: 'The first novel in the Foundation Series'
      },
      'clarke-odyssey': {
        id: 'clarke-odyssey',
        title: '2001: A Space Odyssey',
        creator: 'Arthur C. Clarke',
        date: '1968',
        description: 'Science fiction novel about space exploration'
      }
    }
  },
  {
    id: 'golden-age-comics',
    name: 'Golden Age Comics',
    query: 'collection:comics AND date:[1938 TO 1956]',
    lastUpdated: new Date().toISOString(),
    totalItems: 24,
    description: 'Comic books from the Golden Age era (1938-1956)',
    sort: 'date asc',
    limit: 50,
    items: {}
  },
  {
    id: 'vintage-software',
    name: 'Vintage Software',
    query: 'collection:softwarelibrary',
    lastUpdated: new Date().toISOString(),
    totalItems: 18,
    description: 'Classic software and games from computing history',
    sort: 'downloads desc',
    limit: 100,
    items: {}
  }
];

export function GET({ params }) {
  const { id } = params;
  const collection = sampleCollections.find(col => col.id === id);
  
  if (!collection) {
    return json({ error: 'Collection not found' }, { status: 404 });
  }
  
  return json(collection);
}

export async function PATCH({ params, request }) {
  const { id } = params;
  const updates = await request.json();
  const collectionIndex = sampleCollections.findIndex(col => col.id === id);
  
  if (collectionIndex === -1) {
    return json({ error: 'Collection not found' }, { status: 404 });
  }
  
  // In a real implementation, we would update the collection in the database
  // For now, just return a mock response
  return json({
    success: true,
    collection: {
      ...sampleCollections[collectionIndex],
      ...updates,
      lastUpdated: new Date().toISOString()
    }
  });
}

export async function DELETE({ params }) {
  const { id } = params;
  const collectionIndex = sampleCollections.findIndex(col => col.id === id);
  
  if (collectionIndex === -1) {
    return json({ error: 'Collection not found' }, { status: 404 });
  }
  
  // In a real implementation, we would delete the collection from the database
  // For now, just return a mock response
  return json({
    success: true,
    message: `Collection ${id} deleted successfully`
  });
} 