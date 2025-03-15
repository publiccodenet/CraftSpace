# Collections Directory

This directory contains all Internet Archive collections downloaded and processed by CraftSpace. It serves as the primary cache and source of truth for all content across the system.

## Overview

The Collections directory follows a structured approach:

```
Collections/
  ├── scifi/                  # Collection prefix (unique identifier)
  │   ├── collection.json     # Collection metadata and configuration
  │   ├── index.json          # List of all item IDs in the collection
  │   └── items/              # Directory containing all items
  │       ├── frankenstein00  # Item directory (using IA identifier)
  │       │   ├── item.json   # Item metadata
  │       │   ├── cover.jpg   # Extracted cover image
  │       │   └── content.epub # Original content file
  │       └── timetravel42
  │           ├── item.json
  │           └── ...
  │
  ├── poetry/                 # Another collection
  │   ├── collection.json
  │   └── ...
  │
  └── README.md               # This file
```

## Collection Structure

### collection.json

Each collection has a `collection.json` file with configuration and metadata:

```json
{
  "prefix": "scifi",
  "name": "Science Fiction",
  "description": "Classic science fiction literature",
  "query": "subject:\"Science fiction\" AND mediatype:texts",
  "sort": "downloads desc",
  "limit": 100,
  "includeInUnity": true,
  "exportProfiles": ["unity", "web", "mobile"],
  "lastUpdated": "2023-05-15T12:34:56.789Z",
  "totalItems": 100
}
```

### Item Structure

Each item is stored in its own directory with:

1. **item.json** - Metadata including:
   - Basic information (title, creator, date)
   - Internet Archive metadata
   - Extracted EPUB/PDF metadata
   - Generated icons for visualization
   - Social statistics (downloads, favorites)

2. **Content Files**:
   - Original files from Internet Archive (.epub, .pdf, etc.)
   - Extracted cover images
   - Thumbnail images at various resolutions
   - Extracted text or page content

## Storage Strategy

### Large Files

This directory uses Git LFS (Large File Storage) for efficient handling of large binary files:

- **LFS Tracked**: Large binary files (.epub, .pdf, .jpg, etc.)
- **Git Tracked**: Metadata files (JSON)
- **Ignored**: Very large files (video, audio) based on .gitignore rules

Files tracked in Git LFS are defined in the `.gitattributes` file at the repository root.

### Ignored Content

The `.gitignore` file in the Collections directory excludes certain large files from Git entirely:

```
*.epub
*.pdf
*.mp4
*.mpg
*.mpeg
```

These files are downloaded during processing but not committed to Git. Instead, they are fetched from Internet Archive when needed.

## Metadata Schema

### Collection Metadata

Fields in `collection.json`:

| Field | Type | Description |
|-------|------|-------------|
| prefix | string | Unique identifier for the collection |
| name | string | Display name |
| description | string | Extended description |
| query | string | Internet Archive query that defines the collection |
| sort | string | Sort order for items (e.g., "downloads desc") |
| limit | number | Maximum items to include (0 = unlimited) |
| includeInUnity | boolean | Whether to include in Unity visualization |
| exportProfiles | string[] | Named export configurations to use |
| lastUpdated | string | ISO date of last update |
| totalItems | number | Number of items in the collection |

### Item Metadata

Fields in `item.json`:

| Field | Type | Description |
|-------|------|-------------|
| id | string | Internet Archive identifier |
| title | string | Item title |
| creator | object/string | Creator information (structured when available) |
| date | string | Publication date |
| description | string | Description/summary |
| mediatype | string | Media type (texts, audio, etc.) |
| subject | string[] | Subject tags |
| collection | string[] | Internet Archive collections containing the item |
| downloads | number | Download count from Internet Archive |
| favorite_count | number | Number of users who favorited the item |
| icons | object | Ultra-low-resolution representations for visualization |
| epub_metadata | object | Extracted metadata from EPUB if available |

#### Icons Object Example

```json
"icons": {
  "1x1": "4080FF",         // Single-pixel color as hex
  "2x3": "FF2010,80A0C0,20FF40,D0D0D0,302080,FFC040" // 6-pixel representation
}
```

## Managing Collections

### Adding a New Collection

New collections should be registered using the provided scripts:

```bash
cd SvelteKit/BackSpace
npm run ia:register scifi "Science Fiction" "subject:science fiction" --include-in-unity
npm run ia:process
```

### Updating Collections

To update existing collections:

```bash
npm run ia:process -- --collection=scifi
```

For a full refresh of all content:

```bash
npm run ia:process-full
```

### Verifying Collections

To see all registered collections:

```bash
npm run ia:list
```

To get details about a specific collection:

```bash
npm run ia:get scifi
```

## Performance Considerations

### Smart Downloading

The system implements several optimizations:

1. **Parallel Downloads**: Up to 5 concurrent downloads
2. **Resumable Downloads**: Partially downloaded files can be resumed
3. **ETag Checking**: Avoids re-downloading unchanged files
4. **Error Backoff**: Exponential backoff for failed downloads

### Storage Requirements

Plan for approximately:
- 2-5MB per EPUB file
- 10-50MB per PDF file
- 50-100KB per cover image
- Several MB per collection metadata

A collection of 1,000 items may require 2-5GB of storage.

## Best Practices

1. **Use Smaller Collections**: Start with smaller, focused collections during development
2. **Limit Concurrent Processing**: Avoid processing too many collections simultaneously
3. **Update Incrementally**: Use incremental updates when possible to save bandwidth
4. **Manage Large Files**: Be mindful of Git LFS quotas and download bandwidth
5. **Back Up Metadata**: Ensure collection and item metadata is backed up separately

## Troubleshooting

### Missing Content Files

If content files are missing but metadata exists:

1. Check if it was excluded by .gitignore
2. Try reprocessing with `--force-refresh` flag
3. Check for download errors in the logs

### Broken Cover Images

If cover images are missing or broken:

1. Try extracting again with `process-epub.js`
2. Download cover explicitly from Internet Archive services
3. Use fallback cover generation if needed

### Collection Not Found

If a collection is not discovered:

1. Verify the directory structure
2. Check that collection.json exists and is valid
3. Run `npm run ia:scan` to refresh the registry 