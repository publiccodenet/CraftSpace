// Test script for EPUB parsing
import { testEpubParser } from './process-epub.js';
import path from 'path';

async function main() {
  const testFile = process.argv[2];
  
  if (!testFile) {
    console.error('Please provide a path to an EPUB file');
    process.exit(1);
  }
  
  const success = await testEpubParser(testFile);
  
  if (success) {
    console.log('EPUB parser test completed successfully');
  } else {
    console.error('EPUB parser test failed');
    process.exit(1);
  }
}

main().catch(console.error); 