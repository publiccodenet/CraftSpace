import pkg from 'epub';
console.log('EPUB package contents:', typeof pkg);

// Get the EPub constructor
const EPub = pkg;

// For debugging
console.log('EPub constructor:', typeof EPub);

import sharp from 'sharp';
import fs from 'fs-extra';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

/**
 * Extract cover image from an EPUB file
 * @param {string} epubPath - Path to the EPUB file
 * @param {string} itemDir - Path to the item directory
 * @param {Object} options - Options for processing
 */
async function extractEpubCover(epubPath, itemDir, options = {}) {
  const { width, height, format = 'jpeg' } = options;
  
  try {
    // Ensure item directory exists
    fs.ensureDirSync(itemDir);
    
    // Use promise-based approach for the epub parsing
    return new Promise((resolve, reject) => {
      try {
        // Create the EPub object
        const book = new EPub(epubPath);
        
        // Handle end event (when parsing is complete)
        book.on('end', async function() {
          console.log(`EPUB parsed successfully for cover extraction: ${path.basename(epubPath)}`);
          
          try {
            // Get the cover image if available
            if (book.cover) {
              console.log(`Found cover image: ${book.cover.href}`);
              
              // Get the cover data
              const coverData = book.content[book.cover.id];
              if (!coverData) {
                console.log(`No cover data found for ${path.basename(itemDir)}`);
                return resolve(null);
              }
              
              // Process with sharp
              let imageProcessor = sharp(coverData);
              
              if (width || height) {
                imageProcessor = imageProcessor.resize(width, height);
              }
              
              // Save to the output path using a simple name
              const outputPath = path.join(itemDir, `cover.${format}`);
              await imageProcessor.toFormat(format).toFile(outputPath);
              
              console.log(`Extracted cover for ${path.basename(itemDir)}`);
              resolve(outputPath);
            } else {
              console.log(`No cover found for ${path.basename(itemDir)}`);
              resolve(null);
            }
          } catch (error) {
            console.error(`Error processing cover: ${error.message}`);
            resolve(null);
          }
        });
        
        // Handle errors
        book.on('error', function(err) {
          console.error(`Error parsing EPUB for cover: ${err.message}`);
          resolve(null);
        });
        
        // Start parsing the book
        book.parse();
      } catch (error) {
        console.error(`Error initializing EPub for cover extraction: ${error.message}`);
        resolve(null);
      }
    });
  } catch (error) {
    console.error(`Error extracting cover for ${path.basename(itemDir)}:`, error);
    return null;
  }
}

/**
 * Extract pages from an EPUB file
 * @param {string} epubPath - Path to the EPUB file
 * @param {string} itemDir - Path to the item directory
 * @param {Object} options - Options for processing
 */
async function extractEpubPages(epubPath, itemDir, options = {}) {
  const { width, height, format = 'jpeg', maxPages = 5 } = options;
  
  try {
    // Ensure pages directory exists
    const pagesDir = path.join(itemDir, 'pages');
    fs.ensureDirSync(pagesDir);
    
    return new Promise((resolve, reject) => {
      try {
        const book = new EPub(epubPath);
        
        book.on('end', async function() {
          console.log(`EPUB parsed successfully for page extraction: ${path.basename(epubPath)}`);
          
          try {
            // Get a list of chapters/content
            const extractedPages = [];
            const chapters = book.flow;
            
            // Extract up to maxPages
            const pagesToExtract = Math.min(maxPages, chapters.length);
            console.log(`Extracting ${pagesToExtract} pages from ${chapters.length} chapters`);
            
            for (let i = 0; i < pagesToExtract; i++) {
              const chapter = chapters[i];
              if (chapter && book.content[chapter.id]) {
                const pageContent = book.content[chapter.id];
                const pageFilename = `page_${i + 1}.${format}`;
                const pagePath = path.join(pagesDir, pageFilename);
                
                // TODO: Convert HTML to image using headless browser or other HTML-to-image solution
                // This would require additional libraries and processing
                
                // For now, just store the content as text for reference
                fs.writeFileSync(
                  path.join(pagesDir, `page_${i + 1}.html`),
                  pageContent
                );
                
                extractedPages.push(pagePath);
              }
            }
            
            console.log(`Extracted ${extractedPages.length} pages`);
            resolve(extractedPages);
          } catch (error) {
            console.error(`Error extracting pages: ${error.message}`);
            resolve([]);
          }
        });
        
        book.on('error', function(err) {
          console.error(`Error parsing EPUB for page extraction: ${err.message}`);
          resolve([]);
        });
        
        book.parse();
      } catch (error) {
        console.error(`Error initializing EPub for page extraction: ${error.message}`);
        resolve([]);
      }
    });
  } catch (error) {
    console.error(`Error extracting pages for ${path.basename(itemDir)}:`, error);
    return [];
  }
}

/**
 * Process an EPUB file, extracting cover and metadata
 * @param {string} epubPath - Path to the EPUB file
 * @param {string} itemId - The item ID
 * @param {string} itemsDir - Directory containing all item directories
 * @param {Object} options - Processing options
 */
async function processEpub(epubPath, itemId, itemsDir, options = {}) {
  const {
    extractCover = true,
    extractPages = false,
    extractMetadata = true,
    coverWidth = 512,
    coverHeight = 768,
    pageWidth = 1024,
    pageHeight = 1536,
    maxPages = 5,
    format = 'jpeg',
    forceRefresh = false
  } = options;
  
  try {
    console.log(`Processing EPUB: ${epubPath} for item: ${itemId}`);
    // Create the item directory
    const itemDir = path.join(itemsDir, itemId);
    fs.ensureDirSync(itemDir);
    
    const results = {
      itemDir,
      metadata: null,
      cover: null,
      pages: []
    };
    
    // Check if metadata file already exists
    const metadataPath = path.join(itemDir, 'item.json');
    const metadataExists = fs.existsSync(metadataPath);
    
    // Extract enhanced metadata from EPUB if it doesn't exist yet or if forceRefresh is true
    if (extractMetadata && (!metadataExists || forceRefresh)) {
      console.log(`${metadataExists ? 'Re-extracting' : 'Extracting'} metadata from EPUB: ${epubPath}`);
      
      try {
        // Use promise-based approach
        const metadata = await new Promise((resolve, reject) => {
          try {
            const book = new EPub(epubPath);
            
            // When parsing is complete
            book.on('end', function() {
              console.log("EPUB parsed successfully for metadata extraction");
              resolve({
                title: book.metadata.title,
                creator: book.metadata.creator,
                description: book.metadata.description,
                publisher: book.metadata.publisher,
                date: book.metadata.date,
                language: book.metadata.language,
                rights: book.metadata.rights,
                identifier: book.metadata.identifier,
                // Add any other metadata fields you need
              });
            });
            
            // Handle errors
            book.on('error', function(err) {
              console.error(`Error parsing EPUB for metadata: ${err.message}`);
              reject(err);
            });
            
            // Start parsing
            book.parse();
          } catch (error) {
            console.error(`Error initializing EPub for metadata: ${error.message}`);
            reject(error);
          }
        });
        
        console.log("Extracted EPUB metadata:", JSON.stringify(metadata, null, 2));
        
        // Extract structured data
        const epubMetadata = {
          // Core metadata
          id: itemId,
          title: metadata.title,
          
          // Creator metadata with structured parsing
          creator: parseCreator(metadata.creator),
          
          // Other metadata fields
          description: metadata.description,
          publisher: metadata.publisher,
          pubdate: metadata.date,
          language: metadata.language,
          rights: metadata.rights,
          
          // Identifiers (extract ISBN if available)
          identifiers: parseIdentifiers(metadata.identifier),
          
          // Source path reference
          source: epubPath,
          
          // Add timestamp when the metadata was processed
          processed_date: new Date().toISOString()
        };
        
        // Store original EPUB metadata for reference
        epubMetadata.epub_original_metadata = metadata;
        
        // Write the enhanced metadata
        fs.writeJSONSync(metadataPath, epubMetadata, { spaces: 2 });
        console.log(`Metadata ${metadataExists ? 'updated' : 'written'} to ${metadataPath}`);
        results.metadata = metadataPath;
      } catch (metadataError) {
        console.error("Error extracting metadata from EPUB:", metadataError);
        // Continue processing even if metadata extraction fails
      }
    } else if (metadataExists) {
      console.log(`Using existing metadata for ${itemId}`);
      results.metadata = metadataPath;
    }
    
    // Extract cover
    if (extractCover) {
      results.cover = await extractEpubCover(epubPath, itemDir, {
        width: coverWidth,
        height: coverHeight,
        format
      });
    }
    
    // Extract pages
    if (extractPages) {
      results.pages = await extractEpubPages(epubPath, itemDir, {
        width: pageWidth,
        height: pageHeight,
        format,
        maxPages
      });
    }
    
    // Copy the original EPUB file to the item directory
    const epubFilename = path.basename(epubPath);
    const destEpubPath = path.join(itemDir, epubFilename);
    await fs.copy(epubPath, destEpubPath);
    
    return results;
  } catch (error) {
    console.error(`Error processing EPUB ${epubPath}:`, error);
    return { itemDir: null, metadata: null, cover: null, pages: [] };
  }
}

// Simple test function to validate epub parsing works
async function testEpubParser(epubPath) {
  console.log(`Testing EPUB parser with file: ${epubPath}`);
  
  return new Promise((resolve, reject) => {
    try {
      const book = new EPub(epubPath);
      console.log('EPub instance created successfully');
      
      // Add event listener for the 'end' event
      book.on('end', function() {
        console.log('EPUB parsed successfully');
        console.log('Title:', book.metadata.title);
        console.log('Author:', book.metadata.creator);
        resolve(true);
      });
      
      // Handle errors
      book.on('error', function(err) {
        console.error('Error parsing EPUB:', err);
        resolve(false);
      });
      
      // Parse the EPUB
      book.parse();
    } catch (error) {
      console.error('Error creating EPub instance:', error);
      resolve(false);
    }
  });
}

// Helper functions for structured metadata parsing
function parseCreator(creator) {
  // Handle various creator formats
  if (!creator) return { name: "Unknown" };
  
  if (Array.isArray(creator)) {
    return creator.map(c => parseCreatorString(c));
  }
  
  return parseCreatorString(creator);
}

function parseCreatorString(creator) {
  if (typeof creator !== 'string') return { name: creator || "Unknown" };
  
  // Try to parse out roles and structured data
  // Format might be "Author Name (Role)" or "Name, First (Role)"
  const roleMatch = creator.match(/^(.*?)\s*\(([^)]+)\)$/);
  if (roleMatch) {
    return {
      name: roleMatch[1].trim(),
      role: roleMatch[2].trim()
    };
  }
  
  // Check for lastname, firstname format
  const nameMatch = creator.match(/^([^,]+),\s*(.+)$/);
  if (nameMatch) {
    return {
      name: creator,
      lastName: nameMatch[1].trim(),
      firstName: nameMatch[2].trim()
    };
  }
  
  return { name: creator };
}

function parseIdentifiers(identifier) {
  const result = {
    ids: Array.isArray(identifier) ? identifier : (identifier ? [identifier] : [])
  };
  
  // Extract ISBN if available
  if (result.ids.length > 0) {
    // Look for ISBN pattern
    const isbnPattern = /ISBN[-:]?\s*(978[0-9]{10}|[0-9]{13}|[0-9]{10})/i;
    
    for (const id of result.ids) {
      if (typeof id === 'string') {
        const match = id.match(isbnPattern);
        if (match) {
          result.isbn = match[1].replace(/[-\s]/g, '');
          break;
        }
      }
    }
  }
  
  return result;
}

// Export functions
export { processEpub, extractEpubCover, testEpubParser };

// Run test if executed directly
if (import.meta.url === `file://${process.argv[1]}`) {
  const testFile = process.argv[2];
  if (testFile) {
    testEpubParser(testFile)
      .then(success => {
        console.log(success ? 'Test successful' : 'Test failed');
        process.exit(success ? 0 : 1);
      })
      .catch(err => {
        console.error('Test error:', err);
        process.exit(1);
      });
  } else {
    console.error('Please provide a path to an EPUB file to test');
    process.exit(1);
  }
}