import { json } from '@sveltejs/kit';

export function GET() {
  return json({
    message: 'BackSpace API is running',
    version: '0.1.0',
    endpoints: [
      '/api/collections',
      '/api/collections/[id]'
    ]
  });
} 