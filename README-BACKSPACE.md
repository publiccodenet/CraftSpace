# BackSpace SvelteKit Application

The BackSpace application is the web platform component of CraftSpace, built with SvelteKit. It serves as both the host for the Unity WebGL client and the data processing pipeline for Internet Archive collections.

## Overview

BackSpace handles several key responsibilities:

1. **Content Pipeline**: Processes Internet Archive collections, generates metadata, and creates texture atlases
2. **Web Interface**: Hosts the Unity WebGL build and provides UI elements
3. **API Server**: Provides endpoints for dynamic queries and data retrieval
4. **Deployment**: Manages static and dynamic content delivery

## Project Structure

```
SvelteKit/BackSpace/
├── scripts/                 # Collection processing scripts
│   ├── download-items.js    # Fetch items from Internet Archive 
│   ├── generate-atlases.js  # Create texture atlases
│   ├── pipeline-*.js        # Data pipeline workflows
│   └── ...
├── src/                     # SvelteKit application source
│   ├── routes/              # Application routes
│   ├── lib/                 # Shared components and utilities
│   └── ...
├── static/                  # Static assets
│   ├── data/                # Collection data
│   ├── unity/               # Unity WebGL build
│   └── ...
└── build/                   # Production build output
```

## Data Pipeline and Deployment Strategy

BackSpace uses a multi-tiered caching and deployment strategy for Internet Archive collections data.

### Collection Data Flow

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  Internet       │         │  Data           │         │  Deployment      │
│  Archive API    │ ──────► │  Processing     │ ──────► │  Destinations    │
└─────────────────┘         └─────────────────┘         └─────────────────┘
                                      │
                                      │
                                      ▼
                             Generate Metadata
                             with Color Icons
                                      │
                                      │
                            ┌─────────┴──────────┐
                            │                    │
                            ▼                    ▼
               ┌─────────────────┐    ┌─────────────────┐
               │  Unity          │    │  SvelteKit      │
               │  Resources Dir  │    │  Static Dir     │
               └─────────────────┘    └─────────────────┘
                 (Selected Sets)        (All Collections)
```

### Book Cover Visualization Techniques

The system uses advanced techniques to visualize book covers at multiple resolutions, particularly in 3D space. This enables efficient rendering while maintaining recognizable representations even at extreme distances.

#### Multi-Resolution Texture Atlas Hierarchy

For efficiently displaying large collections of books at different distances, the system utilizes a hierarchy of representations:

1. **Single Pixel (1x1)**: Single color representation
2. **Ultra Low (2x3)**: Six-pixel color pattern representation 
3. **Very Low (4x6)**: Minimal shape recognition
4. **Low (8x12)**: Basic color blocking becomes visible
5. **Medium (16x24)**: Simple cover design elements become visible
6. **High (32x48)**: Text becomes somewhat readable
7. **Original**: Full resolution for close-up viewing

Each level has approximately 2x the resolution of the previous level, maintaining the standard book aspect ratio of 2:3. This creates a mipmap-like structure ideal for LOD (Level of Detail) rendering.

#### Ultra-Low Resolution Techniques

The most critical representations for distant viewing are the 1x1 and 2x3 pixel representations, which are embedded directly in the metadata JSON:

**Single Color (1x1) Algorithm**:
The system extracts a dominant non-white/black color from the cover using a weighted region sampling that prioritizes colors in the center of the image.

**2x3 Pixel Color Icons**:
For the 2x3 representation (just 6 pixels total), the algorithm:
1. Divides the cover into six regions (2 columns × 3 rows)
2. Extracts the most representative color from each region
3. Uses color peaks instead of averages to preserve vibrant, distinctive hues
4. Maintains spatial relationships with the original cover

This approach creates a "color fingerprint" that remains surprisingly recognizable even at this extremely low resolution.

#### Metadata-Embedded Icon Representations

Low-resolution icons are embedded directly in metadata using compact encodings:

1. **Raw Pixel Encoding**: All embedded images use raw uncompressed RGB pixel data (24-bit per pixel)
   - Encoded as base64 strings without delimiters
   - No image headers or compression formats (not PNG/JPG) to save space
   - Fixed dimensions allow for predictable data size
   - The entire metadata JSON file is gzipped during transport for maximum efficiency

2. **1x1 Icons**: Single pixel encoded as base64 RGB data (4 characters)
   - Example: `"ABCD"` (decoded to RGB: 0,17,34)
   
3. **2x3 Icons**: Six pixels encoded as base64 (16 characters)
   - Example: `"ABCDEFGHIJKLMNop"`
   - Position is implicitly understood (left-to-right, top-to-bottom)
   
4. **4x6 Icons**: Twenty-four pixels encoded as base64 (32 characters)
   - Still small enough to embed directly in metadata for rapid visualization

5. **Larger Resolutions**: 8x12 and above typically stored as separate atlas files
   - PNG/JPG formats used for these larger images
   - Downloaded on demand based on visibility and distance

This approach maximizes space efficiency while ensuring immediate visualization with minimal download. The Unity client can parse these raw pixel values and dynamically generate textures without requiring separate image files for the smallest resolutions.

### Caching Strategy

BackSpace implements a multi-level caching strategy:

#### Static Collections vs Dynamic Queries

The system handles two types of collections:

1. **Static Collections**:
   - Defined in the master configuration with specific queries
   - Downloaded and processed during build time
   - Permanently stored in the static data directory or CDN
   - Example: `"prefix": "sf", "dynamic": false` (default)

2. **Dynamic Queries**:
   - Generated on-demand based on user searches or filters
   - Processed at runtime and cached temporarily
   - Stored in a separate directory structure from static collections
   - Example: `"prefix": "dyn_a7f3b2", "dynamic": true`

#### Storage Locations

1. **Unity Client-Side Cache**:
   - Selected high-priority collections are bundled directly with the Unity WebGL build
   - Stored in `Unity/CraftSpace/Assets/Resources/Collections/{prefix}`
   - These collections are available immediately without network requests
   - The Unity build's `index.json` only includes these pre-bundled collections
   - Flag during download: `--include-in-unity=true`

2. **SvelteKit Static Directory**:
   - Static collections are placed in `SvelteKit/BackSpace/static/data/collections/{prefix}`
   - Provides a server-side cache for collections not bundled with Unity
   - Can be offloaded to a CDN for improved performance

3. **Dynamic Content Directory**:
   - Dynamic queries are stored in `SvelteKit/BackSpace/static/data/dynamic/{hash}`
   - Managed by the SvelteKit app with automatic cleanup for old/unused queries
   - Not typically deployed to CDN due to their temporary nature
   - TTL (Time To Live) configuration controls how long dynamic content is kept

4. **Progressive Loading**:
   - Collections bundled with Unity load instantly
   - Additional collections load from the server as needed
   - Low-resolution icons (1x1, 2x3) load first for immediate visualization
   - Higher resolution assets load progressively

#### Cache Levels and Data Types

For each collection item, the system provides multiple resolution levels with specific caching strategies:

1. **Metadata Level**:
   - `cacheLevel: "metadata"`
   - Encoded as raw RGB pixel data (24-bit per pixel) in base64 without delimiters
   - Embedded directly in JSON metadata files
   - Always available without additional requests
   - Gzipped during transport for maximum efficiency
   - Typically used for 1x1, 2x3, and 4x6 pixel representations

2. **Unity Level**:
   - `cacheLevel: "unity"`
   - Included in the Unity app bundle
   - Stored in `Resources` directory for direct loading
   - Available instantly after initial app download
   - Typically used for 4x6 and 8x12 representation for high-priority collections

3. **Server Level**:
   - `cacheLevel: "server"`
   - Stored on the server or CDN
   - Loaded on demand via HTTP requests
   - Typically used for higher resolutions (16x24 and above)
   - Used for all resolutions of standard and low-priority collections

4. **Optional Level**:
   - `cacheLevel: "optional"`
   - Not generated by default
   - Created and stored only when specifically requested
   - Typically used for full-resolution covers
   - System may fetch from Internet Archive on demand

#### Example Collection Configuration

```json
{
  "collections": [
    {
      "prefix": "sf",
      "query": "subject:\"Science fiction\" AND mediatype:texts",
      "name": "Science Fiction",
      "description": "Classic science fiction literature",
      "includeInUnity": true,
      "maxItems": 100,
      "sortBy": "downloads",
      "sortDirection": "desc",
      "resolutions": {
        "1x1": { "generate": true, "cacheLevel": "metadata" },
        "2x3": { "generate": true, "cacheLevel": "metadata" },
        "16x24": { "generate": true, "cacheLevel": "server" },
        "64x96": { "generate": true, "cacheLevel": "server" },
        "tile": { "generate": true, "cacheLevel": "server" },
        "full": { "generate": false, "cacheLevel": "server" }
      }
    },
    {
      "prefix": "poetry",
      "query": "subject:Poetry AND mediatype:texts",
      "name": "Poetry Collection",
      "description": "Famous poetry works",
      "includeInUnity": false,
      "maxItems": 50
    },
    {
      "prefix": "dyn_a7f3b2",
      "query": "creator:\"Asimov, Isaac\" AND mediatype:texts",
      "name": "Dynamic Query - Asimov",
      "description": "Dynamically generated collection for Isaac Asimov",
      "includeInUnity": false,
      "maxItems": 30,
      "dynamic": true
    }
  ]
}
```

### Visualization Pipeline

The system implements a progressive visualization strategy:

1. **Initial View**: Uses embedded 1x1 and 2x3 data from metadata
2. **Approaching**: Loads 16x24 atlas for the visible section as user gets closer
3. **Examination**: Loads 64x96 atlas when user is examining books closely
4. **Interaction**: Loads full cover when user selects or interacts with a book
5. **Extended Interaction**: For dynamic or special collections, may load additional metadata

This ensures books are always visualized, even with connectivity issues.

### Cache Control and Versioning

The system supports cache invalidation through query parameters:

- `?clearcache=true` - Instructs Unity to clear its persistent browser storage
- `?reload=collections` - Forces a refresh of the collection index from the server
- `?version={hash}` - Used for cache-busting when new collections are deployed

These parameters are recognized by the SvelteKit app and passed to Unity.

## Development Setup

1. **Install Dependencies**:
   ```bash
   cd SvelteKit/BackSpace
   npm install
   ```

2. **Configure Collections**:
   Edit the `collections.json` file at the project root

3. **Run Development Server**:
   ```bash
   npm run dev
   ```

4. **Process Collections**:
   ```bash
   # Build TypeScript scripts
   npm run build:scripts
   
   # Run full pipeline
   npm run pipeline-full
   
   # Or run incremental updates
   npm run pipeline-incremental
   ```

## Building for Production

```bash
# Build scripts
npm run build:scripts

# Process collections
npm run pipeline-full

# Build SvelteKit app
npm run build
```

The build output will be in `SvelteKit/BackSpace/build/`.

## Adding New Collections

To add a new collection:

1. Edit the `collections.json` file
2. Add a new entry with query parameters
3. Run the pipeline to process the collection
4. Update the Unity project if including in client

## API Reference

The BackSpace application provides several API endpoints:

- `/api/collections` - List all available collections
- `/api/collections/:prefix` - Get details for a specific collection
- `/api/search` - Perform dynamic queries against Internet Archive 

### Texture Atlas Generation

The BackSpace pipeline generates texture atlases for book covers at multiple resolutions. These atlases pack multiple book covers into single texture files for efficient rendering in Unity.

Key aspects of atlas generation include:

1. **Multiple Resolution Levels**: Atlases are generated for each resolution level (8x12, 16x24, 64x96)
2. **Efficient Packing**: Books are arranged in a grid pattern with appropriate gutters/spacing
3. **Metadata Output**: Each book's position in the atlas is recorded in the collection metadata
4. **Incremental Processing**: Only changed or new books are regenerated during incremental updates

```javascript
// Example atlas generation code snippet
function generateAtlas(collectionPrefix, resolution, books) {
  // Calculate atlas dimensions based on book count
  const booksPerRow = Math.ceil(Math.sqrt(books.length));
  
  // Determine appropriate gutter size based on resolution
  const gutterSize = resolution <= 6 ? 1 : (resolution <= 24 ? 2 : 4);
  
  // Size of each cell in the atlas (book + gutters)
  const cellSize = resolution + gutterSize;
  
  // Create canvas with appropriate dimensions
  const atlasWidth = cellSize * booksPerRow;
  const atlasHeight = cellSize * Math.ceil(books.length / booksPerRow);
  const canvas = createCanvas(atlasWidth, atlasHeight);
  const ctx = canvas.getContext('2d');
  
  // Fill atlas with books, tracking positions
  for (let i = 0; i < books.length; i++) {
    const x = (i % booksPerRow) * cellSize + gutterSize/2;
    const y = Math.floor(i / booksPerRow) * cellSize + gutterSize/2;
    
    // Draw book at position (x,y)
    ctx.drawImage(books[i].image, x, y, resolution, resolution);
    
    // Store atlas position in book metadata
    books[i].atlas = books[i].atlas || {};
    books[i].atlas[resolution] = {
      index: 0, // Atlas file index if multiple atlas files
      x, 
      y, 
      width: resolution,
      height: resolution
    };
  }
  
  // Save atlas to file
  const filename = `${collectionPrefix}_${resolution}.png`;
  const outputPath = path.join(outputDir, filename);
  fs.writeFileSync(outputPath, canvas.toBuffer('image/png'));
  
  // Return atlas metadata for collection
  return {
    filename,
    width: atlasWidth, 
    height: atlasHeight, 
    resolution,
    booksPerRow
  };
}
```

For Unity-specific details on texture atlases, gutters, and optimization techniques used in the CraftSpace renderer, see the [Texture Atlases in Unity section in README-CRAFTSPACE.md](./README-CRAFTSPACE.md#texture-atlases-in-unity). 