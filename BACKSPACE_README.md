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
   - Encoded directly in JSON as base64 strings
   - Always available without additional requests
   - Typically used for 1x1 and 2x3 pixel representations
   - Example: `"1x1": "AAABBB=="` (base64 encoded pixel data)

2. **Client Level**:
   - `cacheLevel: "client"`
   - Stored in Unity client as pre-bundled assets
   - Also available on server/CDN as fallback
   - Typically used for medium-resolution atlases (16x24)
   - Prioritized for frequently accessed collections

3. **Server Level**:
   - `cacheLevel: "server"`
   - Only available from server/CDN, not pre-bundled
   - Used for higher-resolution assets (64x96, tile, full)
   - Loaded on-demand when user approaches or selects items

### Deployment Options

The static data can be deployed in multiple ways:

1. **Integrated Deployment**:
   - SvelteKit Node.js server serves both the application and static data
   - Simplest deployment option but can increase server load

2. **Split Deployment** (recommended for production):
   - Static data files are deployed to a dedicated storage service (S3, GCS, etc.)
   - Load balancer routes `/data/collections/*` requests directly to the storage service
   - Dynamic queries in `/data/dynamic/*` are handled by the SvelteKit server
   - SvelteKit server only handles dynamic API requests and application routes
   - Benefits: Reduced server load, better scalability, cheaper hosting

#### Hybrid Deployment Architecture

```
           ┌───────────────┐
 User ───►│ Load Balancer │
           └───────┬───────┘
                   │
          ┌────────┴────────┐
          │                 │
          ▼                 ▼
 ┌─────────────────┐      ┌─────────────────┐
 │  SvelteKit      │      │  CDN/Storage    │
 │  Node.js Server │      │  Service        │
 └────────┬────────┘      └────────┬────────┘
          │                        │
          ▼                        ▼
 ┌─────────────────┐      ┌─────────────────┐
 │ Dynamic Queries │      │ Static          │
 │ /data/dynamic/* │      │ Collections     │
 └─────────────────┘      └─────────────────┘
```

### Resolution Levels and Caching Strategy

Each collection can specify which resolution levels to generate and cache:

```
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│  Resolution Levels:                                            │
│                                                                │
│  1x1    - Single pixel color (embedded directly in metadata)   │
│  2x3    - Ultra-low resolution (embedded directly in metadata) │
│  16x24  - Low resolution atlas (server/CDN hosted)             │
│  64x96  - Medium resolution atlas (server/CDN hosted)          │
│  tile   - Individual book cover tiles (server/CDN hosted)      │
│  full   - Original cover image (server/CDN hosted)             │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

The system handles resolutions as follows:

1. **Cache Levels**:
   - **metadata**: Encoded directly in JSON as base64 strings
   - **server**: Stored as image files on the server/CDN only
   - **client**: Cached in the Unity client for offline use (also implies server caching)

2. **Embedded in Metadata**:
   - Resolutions with `cacheLevel: "metadata"` are stored as base64 encoded pixel data
   - These are always available to the Unity client without additional requests
   - Example: `"1x1": "AAABBB=="` (base64 encoded pixel data)
   - Example: `"2x3": "AAABBBCCCDDDEEEFFF=="` (base64 encoded pixel data)

3. **Caching Strategy**:
   - Collections marked with `includeInUnity: true` have their metadata included in the Unity build
   - Within each collection, resolution levels are cached according to their `cacheLevel`:
     - `metadata`: Always included in collection metadata as base64 encoded strings
     - `client`: Included in Unity build as atlas files (and also on server)
     - `server`: Only available from the SvelteKit server/CDN
   - Higher resolution assets are loaded on-demand as needed

### Collection Processing Workflow

1. **Download Script**:
   - The `download-collections.js` script reads the master configuration
   - For each collection, it fetches items from Internet Archive based on the query
   - Processes metadata and generates low-resolution color icons
   - Creates hierarchical index files and collection metadata

2. **Storage Strategy**:
   - All collections are stored in `static/data/{prefix}/` regardless of Unity inclusion
   - Collections marked with `includeInUnity: true` are also copied to Unity's Resources

3. **Atlas Generation**:
   - The system generates texture atlases for each resolution level specified
   - Atlas files are stored in `static/data/{prefix}/atlases/`
   - Only the atlases marked with `cacheInUnity: true` are copied to Unity

4. **Progressive Loading**:
   - Unity client starts with the lowest resolution data (1x1 and 2x3)
   - As the user approaches books, higher resolution assets are loaded
   - Requests are made to the SvelteKit server or CDN for non-cached resolutions

### Dynamic Content Workflow

When a user creates a dynamic query through search or filters:

1. **Request Processing**:
   - Client sends search parameters to SvelteKit server
   - Server generates a unique hash for the query (e.g., `dyn_a7f3b2`)
   - System checks if this query has been processed before

2. **Content Generation**:
   - If new, server executes query against Internet Archive API
   - Processes results into the same format as static collections
   - Stores in `static/data/dynamic/{hash}/` with appropriate TTL

3. **Client Caching**:
   - Dynamic content is never pre-bundled with Unity
   - Browser may cache results for subsequent visits
   - Unity client can store in browser's IndexedDB
   - Automatic cleanup based on access frequency and age

4. **Lifecycle Management**:
   - Popular dynamic queries may be promoted to static collections
   - Scheduled cleanup jobs remove unused dynamic content
   - Administrators can manually promote/demote collections

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