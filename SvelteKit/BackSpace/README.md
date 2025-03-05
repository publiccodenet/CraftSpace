# sv

Everything you need to build a Svelte project, powered by [`sv`](https://github.com/sveltejs/cli).

## Creating a project

If you're seeing this, you've probably already done this step. Congrats!

```bash
# create a new project in the current directory
npx sv create

# create a new project in my-app
npx sv create my-app
```

## Developing

Once you've created a project and installed dependencies with `npm install` (or `pnpm install` or `yarn`), start a development server:

```bash
npm run dev

# or start the server and open the app in a new browser tab
npm run dev -- --open
```

## Building

To create a production version of your app:

```bash
npm run build
```

You can preview the production build with `npm run preview`.

> To deploy your app, you may need to install an [adapter](https://svelte.dev/docs/kit/adapters) for your target environment.

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

### Implementation Steps

1. **Initial Setup** (Complete):
   - Download script places data in both Unity and SvelteKit directories
   - Basic caching strategy implemented in CollectionLoader.cs

2. **Enhanced Downloading** (Next):
   - Add flags to control Unity bundling: `--include-in-unity=true/false`
   - Implement color processing algorithms for 1x1 and 2x3 icons

3. **Local Testing**:
   - Test Unity WebGL build with embedded collections
   - Test progressive loading from SvelteKit static directory

4. **Production Deployment**:
   - Configure CDN/storage bucket for static data
   - Set up routing rules on load balancer
   - Deploy Unity build and SvelteKit application

5. **Performance Optimization**:
   - Add HTTP caching headers for static data
   - Implement compression for metadata files
   - Monitor and optimize data transfer between client and server

### Usage

To download a collection with the current implementation:

```bash
# Build the scripts first
npm run build:scripts

# Run the download process using the collections.json configuration
npm run download-collections
```

## Collection Management System

BackSpace uses a configuration-driven approach to manage Internet Archive collections.

### Master Collection Configuration

Collections are defined in `collections.json` in the project root:

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

5. **Fallback Mechanism**:
   - If a higher resolution asset fails to load, the system falls back to the next lower resolution
   - This ensures books are always visualized, even with connectivity issues

### Deployment Process

During the build process:

1. The `npm run build` command:
   - Builds the SvelteKit application
   - Processes all collections to ensure they're up to date
   - Prepares the static data directory for deployment

2. The `npm run build:unity` command:
   - Copies only the collections marked with `includeInUnity: true` to Unity
   - Updates Unity's `index.json` to include only these collections
   - Performs the Unity WebGL build

### Cache Control

The system supports cache invalidation through query parameters:

- `?clearcache=true` - Instructs Unity to clear its persistent browser storage
- `?reload=collections` - Forces a refresh of the collection index from the server
- `?version={hash}` - Used for cache-busting when new collections are deployed

These parameters are recognized by the SvelteKit app and passed to Unity.

1. **Initial View**: Uses embedded 1x1 and 2x3 data from metadata
2. **Approaching**: Loads 16x24 atlas for the visible section as user gets closer
3. **Examination**: Loads 64x96 atlas when user is examining books closely
4. **Interaction**: Loads full cover when user selects or interacts with a book
5. **Extended Interaction**: For dynamic or special collections, may load additional metadata

This ensures books are always visualized, even with connectivity issues

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

### Deployment Process

During the build process:

1. The `npm run build` command:
   - Builds the SvelteKit application
   - Processes all collections to ensure they're up to date
   - Prepares the static data directory for deployment

2. The `npm run build:unity` command:
   - Copies only the collections marked with `includeInUnity: true` to Unity
   - Updates Unity's `index.json` to include only these collections
   - Performs the Unity WebGL build
