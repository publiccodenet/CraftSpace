import { json } from '@sveltejs/kit';
import { CollectionCreateSchema } from '$lib/schemas';

export async function POST({ request }) {
  const data = await request.json();
  
  // Parse and validate with Zod
  const result = CollectionCreateSchema.safeParse(data);
  
  if (!result.success) {
    return json({ 
      success: false, 
      errors: result.error.format() 
    }, { status: 400 });
  }
  
  const validatedCollection = result.data;
  
  // Now work with fully validated data...
  // Save to database, etc.
  
  return json({ 
    success: true, 
    collection: validatedCollection 
  });
} 