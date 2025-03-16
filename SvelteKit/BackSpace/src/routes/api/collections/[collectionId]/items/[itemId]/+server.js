export async function GET({ params }) {
  const { collectionId, itemId } = params;

  const result = await getItem(collectionId, itemId);

  // Rest of the handler
}

export async function PUT({ request, params }) {
  const { collectionId, itemId } = params;
  const updates = await request.json();

  const result = await updateItem(collectionId, itemId, updates);

  // Rest of the handler
}

export async function DELETE({ params }) {
  const { collectionId, itemId } = params;

  const result = await deleteItem(collectionId, itemId);

  // Rest of the handler
} 