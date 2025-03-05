import { json } from '@sveltejs/kit';

/**
 * GET handler for the /api/index endpoint
 * @returns {Response} JSON response with an empty array
 */
export async function GET() {
    // Return an empty array as JSON
    // This will be expanded later to return actual data
    return json([]);
} 