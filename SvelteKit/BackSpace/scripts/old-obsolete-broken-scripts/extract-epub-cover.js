/**
 * Extract cover image from an EPUB file
 * @param {string} epubPath - Path to the EPUB file
 * @param {string} itemId - The item ID 
 * @param {string} outputDir - Output directory (items folder)
 * @param {Object} options - Options for processing (width, height, format)
 */
async function extractEpubCover(epubPath, itemId, outputDir, options = {}) {
  const { width, height, format = 'jpeg' } = options;
  
  try {
    // Ensure output directory exists
    fs.ensureDirSync(outputDir);
    
    // Open the EPUB file
    const book = ePub(epubPath);
    await book.ready;
    
    // Get the cover URL (path inside the EPUB)
    const coverUrl = await book.coverUrl();
    
    if (!coverUrl) {
      console.log(`No cover found for ${itemId}`);
      return null;
    }
    
    // Extract the cover as a buffer
    const coverData = await book.resources.get(coverUrl);
    const coverBuffer = coverData.data;
    
    // Process with sharp
    let imageProcessor = sharp(coverBuffer);
    
    if (width || height) {
      imageProcessor = imageProcessor.resize(width, height);
    }
    
    // Save to the output path using the new naming convention
    // itemId + "__" + content name + extension
    const outputPath = path.join(outputDir, `${itemId}__cover.${format}`);
    await imageProcessor.toFormat(format).toFile(outputPath);
    
    console.log(`Extracted cover for ${itemId}`);
    return outputPath;
  } catch (error) {
    console.error(`Error extracting cover from ${itemId}:`, error);
    return null;
  }
} 