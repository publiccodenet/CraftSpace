# CraftSpace Unity Client

Unity-based 3D client for browsing Internet Archive collections.

## Overview

CraftSpace is a WebGL-based 3D environment for exploring digital collections. It works in conjunction with the BackSpace SvelteKit application to provide:

1. Immersive 3D visualization of book collections
2. Intelligent low-resolution representations of book covers
3. Progressive loading of collection data
4. Multi-level caching for performance optimization

## Collection Management

CraftSpace implements a sophisticated caching and loading system for collection data:

```
┌────────────────┐     ┌─────────────────┐     ┌──────────────────┐
│                │     │                 │     │                  │
│ Unity          │     │ Browser         │     │ SvelteKit Server │
│ Resources      │ ──► │ Local           │ ──► │ Static Data      │
│ (Pre-bundled)  │     │ Storage         │     │ (All Collections)│
│                │     │ (User Cache)    │     │                  │
└────────────────┘     └─────────────────┘     └──────────────────┘
      Primary               Secondary               Tertiary
```

### Resolution Levels

The system handles book covers at multiple resolution levels:

| Resolution | Size   | Description                  | Cache Strategy         |
|------------|--------|------------------------------|------------------------|
| 1x1        | 1 px   | Single color representation  | Always in metadata     |
| 2x3        | 6 px   | Ultra-low resolution colors  | Always in metadata     |
| 16x24      | 384 px | Low resolution atlas         | Configurable caching   |
| 64x96      | 6144 px| Medium resolution atlas      | Typically server-only  |
| full       | Varies | Original cover image         | Server-only            |

Each resolution level serves a specific purpose in the visualization pipeline:

- **1x1 & 2x3**: Used for distant view of books, embedded directly in collection metadata
- **16x24**: Used for medium-distance viewing, may be cached in Unity or loaded on demand
- **64x96 & full**: Used for close inspection, loaded from server as needed

### 1. Resource-Based Collections

Collections marked as `includeInUnity: true` in the master configuration are bundled with the Unity WebGL build:

- Located in `Assets/Resources/Collections/{prefix}`
- Loaded instantly on application start
- No network requests required for initial use
- Limited to high-priority collections to manage build size
- Only includes resolution levels marked with `cacheInUnity: true`

### 2. Browser Persistent Storage

Collections not bundled with Unity but accessed by the user:

- Stored in browser's IndexedDB via Unity's caching system 
- Persists between sessions unless explicitly cleared
- Managed by `CollectionLoader.cs` with configurable storage limits
- Falls back to network requests when cache is invalid or missing
- Stores higher resolution assets as they're requested

### 3. Network Collections

All other collections are loaded from the SvelteKit server:

- Accessed via RESTful API endpoints
- Low-resolution metadata loaded first for immediate visualization
- Higher resolution assets loaded progressively as needed
- Cache headers optimize repeated requests

### Progressive Loading Strategy

CraftSpace uses a distance-based progressive loading approach:

```
┌───────────────┐     ┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│               │     │               │     │               │     │               │
│ Far Distance  │     │ Medium        │     │ Close         │     │ Book          │
│ 1x1 & 2x3     │ ──► │ 16x24         │ ──► │ 64x96         │ ──► │ Full Cover    │
│ (Embedded)    │     │ (Atlas)       │     │ (Atlas)       │     │ (Individual)  │
│               │     │               │     │               │     │               │
└───────────────┘     └───────────────┘     └───────────────┘     └───────────────┘
```

1. **Initial View**: Uses embedded 1x1 and 2x3 data from metadata
2. **Approaching**: Loads 16x24 atlas for the visible section as user gets closer
3. **Examination**: Loads 64x96 atlas when user is examining books closely
4. **Interaction**: Loads full cover when user selects or interacts with a book

This strategy ensures efficient memory usage while maintaining responsive performance.

## Cache Control

### Manual Cache Clearing

CraftSpace responds to the following URL parameters from the hosting SvelteKit application:

- `?clearcache=true` - Clears all persistent browser storage
- `?reload=collections` - Refreshes collection index from server
- `?version={hash}` - Force-reloads when version mismatch detected

### Programmatic Control

The collection loader exposes methods for programmatic cache management:

```csharp
// Clear cache for a specific collection
CollectionLoader.ClearCollectionCache("sf");

// Clear all cached collections
CollectionLoader.ClearAllCaches();

// Check if collection exists in cache
bool isCached = CollectionLoader.IsCollectionCached("poetry");
```

## Integration with BackSpace

CraftSpace communicates with the BackSpace SvelteKit application through:

1. **Direct API Calls**: For collection data and metadata
2. **WebSocket Messages**: For real-time updates and user interactions
3. **URL Parameters**: For control and configuration

See the [BackSpace README](../SvelteKit/BackSpace/README.md) for details on the server-side implementation and data pipeline.

## Building for Deployment

To build CraftSpace for deployment:

```bash
# From the SvelteKit directory
npm run build:unity
```

This builds the Unity WebGL application and places it in the SvelteKit static directory.

Alternatively, to build directly from Unity:

1. Open the project in Unity Editor
2. Select "Build/WebGL" from the menu
3. The build output will be in `Build/WebGL`

## Development Notes

When developing new features:

1. Collections bundled with Unity are defined in `collections.json` in the project root
2. Use the CollectionLoader component to access collections in your Unity scripts
3. For rapid iteration, use the `useBuiltInDataFirst = false` setting during development 