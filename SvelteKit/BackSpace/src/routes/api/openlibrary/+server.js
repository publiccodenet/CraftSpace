import { json } from '@sveltejs/kit';
import { InternetArchiveSDK } from 'internetarchive-sdk-js';

const ia = new InternetArchiveSDK();

/**
 * GET handler for the /api/openlibrary endpoint
 * @returns {Response} JSON response with OpenLibrary data
 */
export async function GET({ url }) {
    try {
        const query = url.searchParams.get('q') || '';
        const subject = url.searchParams.get('subject') || 'Science fiction';
        const page = parseInt(url.searchParams.get('page') || '1');
        const limit = parseInt(url.searchParams.get('limit') || '20');
        
        // Build search query combining user query, subject and collection
        let searchQuery = `collection:openlibrary AND mediatype:texts`;
        
        if (subject) {
            searchQuery += ` AND subject:"${subject}"`;
        }
        
        if (query) {
            searchQuery += ` AND (title:"${query}" OR creator:"${query}")`;
        }
        
        // Search for books based on query
        const response = await ia.search({
            query: searchQuery,
            fields: [
                'identifier', 'title', 'creator', 'description', 
                'subject', 'publisher', 'date', 'language',
                'isbn', 'oclc', 'lccn', 'cover_image'
            ],
            rows: limit,
            page: page,
            sort: ['title asc']
        });
        
        return json({
            results: response.response.docs,
            total: response.response.numFound,
            page,
            limit
        });
    } catch (error) {
        console.error('Error fetching OpenLibrary data:', error);
        return json({ error: 'Failed to fetch OpenLibrary data' }, { status: 500 });
    }
} 