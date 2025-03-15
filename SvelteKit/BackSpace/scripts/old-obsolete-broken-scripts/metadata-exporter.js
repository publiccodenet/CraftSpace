import micromatch from 'micromatch';
import fs from 'fs-extra';
import path from 'path';
import sharp from 'sharp';

/**
 * Export item metadata and assets according to collection export targets
 * @param {Object} item - Item metadata to export
 * @param {Array} exportTargets - Array of export target configurations
 * @param {Object} options - Additional export options
 * @returns {Object} - Results of export operations
 */
export async function exportItem(item, exportTargets, options = {}) {
  const results = {};
  
  // Process each export target
  for (const target of exportTargets) {
    // Skip disabled targets
    if (target.enabled === false) continue;
    
    const targetName = target.name;
    if (!targetName) {
      console.warn('Skipping export target with no name');
      continue;
    }
    
    // Initialize result for this target
    results[targetName] = { 
      metadata: null,
      assets: []
    };
    
    try {
      // 1. Filter metadata according to target specifications
      const filteredMetadata = filterMetadataForTarget(item, target);
      results[targetName].metadata = filteredMetadata;
      
      // 2. Process and export assets
      if (options.exportAssets !== false) {
        results[targetName].assets = await exportAssetsForTarget(
          item, 
          target, 
          {
            sourceDir: options.sourceDir,
            outputDir: options.outputDir ? 
              path.join(options.outputDir, targetName) : 
              null
          }
        );
      }
      
      // 3. Write metadata to output location if specified
      if (options.outputDir) {
        const targetDir = path.join(options.outputDir, targetName);
        await fs.ensureDir(targetDir);
        
        // Write filtered metadata
        const metadataPath = path.join(targetDir, `${item.id}.json`);
        await fs.writeJSON(metadataPath, filteredMetadata, { spaces: 2 });
      }
    } catch (error) {
      console.error(`Error exporting item ${item.id} for target ${targetName}:`, error);
      results[targetName].error = error.message;
    }
  }
  
  return results;
}

/**
 * Filter metadata according to target specifications
 * @param {Object} metadata - Source metadata to export
 * @param {Object} target - Export target configuration
 * @returns {Object} - Filtered metadata for target
 */
export function filterMetadataForTarget(metadata, target) {
  // Default patterns if not specified
  const include = target.includePatterns || ['*'];
  const exclude = target.excludePatterns || [];
  
  // Add target-specific exclusions
  if (target.name === 'unity') {
    exclude.push('*_cached', 'cache_metadata.*', 'epub_original_metadata');
  } else if (target.name === 'minimal') {
    return filterToEssentialFields(metadata);
  } else if (target.name === 'cdn') {
    exclude.push('internal_*', '_*');
  } else if (target.name === 'mobile') {
    exclude.push('*_full', '*_hires');
  }
  
  return filterObjectByPatterns(metadata, include, exclude);
}

/**
 * Export assets for a specific target
 * @param {Object} item - Item metadata
 * @param {Object} target - Export target configuration
 * @param {Object} options - Export options
 * @returns {Array} - List of exported assets
 */
async function exportAssetsForTarget(item, target, options = {}) {
  const { sourceDir, outputDir } = options;
  if (!sourceDir || !outputDir) {
    return [];
  }
  
  const assets = [];
  const itemId = item.id;
  
  // Determine which assets to export based on target configuration
  const assetTypes = target.assets || ['cover'];
  
  // Process each asset type
  for (const assetType of assetTypes) {
    try {
      switch (assetType) {
        case 'cover':
          // Export cover image with target-specific options
          const coverResult = await exportCoverImage(
            item, 
            sourceDir, 
            outputDir, 
            target.coverOptions || {}
          );
          if (coverResult) assets.push(coverResult);
          break;
          
        case 'tile':
          // Export tile image (standard IA thumbnail)
          const tileResult = await exportTileImage(
            item,
            sourceDir,
            outputDir
          );
          if (tileResult) assets.push(tileResult);
          break;
          
        case 'pixel':
          // Export single pixel color
          const pixelResult = await exportSinglePixelColor(
            item,
            sourceDir,
            outputDir
          );
          if (pixelResult) assets.push(pixelResult);
          break;
          
        case 'atlas':
          // Placeholder for texture atlas generation
          console.log(`Atlas generation not yet implemented for ${itemId}`);
          break;
          
        case 'pyramid':
          // Placeholder for tile pyramid generation
          console.log(`Tile pyramid not yet implemented for ${itemId}`);
          break;
          
        case 'content':
          // Export content file (e.g., PDF, EPUB)
          const contentResult = await exportContentFile(
            item,
            sourceDir,
            outputDir,
            target.contentOptions || {}
          );
          if (contentResult) assets.push(contentResult);
          break;
          
        default:
          console.warn(`Unknown asset type: ${assetType}`);
      }
    } catch (error) {
      console.error(`Error exporting ${assetType} for ${itemId}:`, error);
    }
  }
  
  return assets;
}

/**
 * Export cover image with options
 * @param {Object} item - Item metadata
 * @param {string} sourceDir - Source directory for item
 * @param {string} outputDir - Output directory for export
 * @param {Object} options - Image processing options
 */
async function exportCoverImage(item, sourceDir, outputDir, options = {}) {
  const itemId = item.id;
  const itemDir = path.join(sourceDir, itemId);
  
  // Try to find cover image with case-insensitive search
  const files = await fs.readdir(itemDir);
  const coverFile = files.find(f => f.toLowerCase() === 'cover.jpg');
  
  if (!coverFile) {
    return null;
  }
  
  const sourcePath = path.join(itemDir, coverFile);
  
  try {
    // Create output directory
    await fs.ensureDir(outputDir);
    
    // Determine output options
    const maxWidth = options.maxWidth || 0;
    const maxHeight = options.maxHeight || 0;
    const format = options.format || 'jpg';
    
    // Determine output filename
    const outputFilename = `${itemId}_cover.${format}`;
    const outputPath = path.join(outputDir, outputFilename);
    
    // Process image according to options
    let image = sharp(sourcePath);
    
    // Resize if needed
    if (maxWidth > 0 || maxHeight > 0) {
      image = image.resize({
        width: maxWidth > 0 ? maxWidth : undefined,
        height: maxHeight > 0 ? maxHeight : undefined,
        fit: 'inside',
        withoutEnlargement: true
      });
    }
    
    // Set format
    image = image.toFormat(format);
    
    // Save the processed image
    await image.toFile(outputPath);
    
    return {
      type: 'cover',
      path: outputPath,
      filename: outputFilename
    };
  } catch (error) {
    console.error(`Error exporting cover for ${itemId}:`, error);
    return null;
  }
}

/**
 * Filter to essential fields only
 * @param {Object} metadata - Source metadata
 * @returns {Object} - Minimal metadata
 */
function filterToEssentialFields(metadata) {
  // Return only the most essential fields for minimal representations
  const { id, title, creator } = metadata;
  return { id, title, creator };
}

/**
 * Filter object properties using glob patterns
 */
function filterObjectByPatterns(obj, includePatterns = ['*'], excludePatterns = []) {
  if (!obj) return {};
  
  const keys = Object.keys(obj);
  
  // First apply includes (whitelist)
  let filteredKeys = includePatterns.length ? 
    micromatch(keys, includePatterns) : keys;
  
  // Then apply excludes (blacklist)
  if (excludePatterns.length) {
    filteredKeys = filteredKeys.filter(key => 
      !micromatch.isMatch(key, excludePatterns)
    );
  }
  
  // Build new object with filtered keys
  const result = {};
  for (const key of filteredKeys) {
    // Handle nested objects recursively
    if (typeof obj[key] === 'object' && obj[key] !== null && !Array.isArray(obj[key])) {
      result[key] = filterObjectByPatterns(
        obj[key], 
        includePatterns.map(p => p.startsWith(`${key}.`) ? p.substring(key.length + 1) : '*'),
        excludePatterns.map(p => p.startsWith(`${key}.`) ? p.substring(key.length + 1) : null).filter(Boolean)
      );
    } else {
      result[key] = obj[key];
    }
  }
  
  return result;
}

// Placeholder implementations for other asset exporters
async function exportTileImage(item, sourceDir, outputDir) {
  // Implementation will go here
  return null;
}

async function exportSinglePixelColor(item, sourceDir, outputDir) {
  // Implementation will go here
  return null;
}

async function exportContentFile(item, sourceDir, outputDir, options) {
  // Implementation will go here
  return null;
} 