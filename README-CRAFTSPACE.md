# CraftSpace Monorepo

This repository contains all components of the CraftSpace project, an immersive 3D visualization platform for Internet Archive collections.

## Repository Structure

```
CraftSpace/
├── .github/                  # GitHub Actions workflows and scripts
├── Unity/                    # Unity WebGL application
│   └── CraftSpace/           # Main Unity project
├── SvelteKit/                # Web application components
│   └── BackSpace/            # SvelteKit web application
├── collections.json          # Collection configuration
└── Notes/                    # Project documentation
```

## Components

### 1. Unity Application (Unity/CraftSpace)

The 3D visualization client built with Unity WebGL that provides:
- Immersive 3D browsing of digital collections
- Multi-resolution rendering of book covers
- Intelligent caching and progressive loading

### 2. SvelteKit Application (SvelteKit/BackSpace)

The web application that:
- Hosts the Unity WebGL build
- Serves collection data
- Provides API endpoints for dynamic queries
- Handles collection processing

### 3. Collections System

A data pipeline that:
- Fetches data from Internet Archive
- Processes book covers at multiple resolutions
- Generates optimized texture atlases
- Implements multi-tiered caching

## Texture Atlases in Unity

### Overview of Texture Atlases

Texture atlases are an optimization technique that combines multiple smaller textures into a single larger texture. In the context of CraftSpace, they are essential for efficiently rendering large collections of book covers at different resolutions and distances.

### Importance of Texture Gutters

Even though CraftSpace implements a custom multi-resolution approach for book covers, gutters (padding between images in an atlas) remain important:

1. **Prevents Texture Bleeding**: Without gutters, textures can "bleed" into neighboring textures during texture filtering.

2. **Bilinear/Trilinear Filtering**: Unity's texture filtering samples neighboring pixels, which can cross boundaries between atlas entries without adequate gutters.

3. **Perspective Distortion**: In 3D space, texture sampling can slightly overshoot intended UV coordinates due to perspective calculations.

4. **UV Precision Issues**: Even with precise UV coordinates, floating-point precision in GPU calculations can cause sampling errors at boundaries.

### Recommended Gutter Sizes for CraftSpace

For optimal visual quality in CraftSpace's multi-resolution system:

- **Low Resolutions (2x3, 4x6)**: 1-pixel gutters are typically sufficient
- **Medium Resolutions (8x12, 16x24)**: 2-pixel gutters recommended
- **High Resolutions (32x48, 64x96)**: 3-4 pixel gutters for best quality

### Implementation Details

CraftSpace uses custom atlas generation rather than Unity's built-in Sprite Atlas system:

```csharp
// Example of creating a texture atlas with appropriate gutters
private Texture2D CreateBookAtlas(List<BookCoverData> books, int resolution) {
    int booksPerRow = Mathf.CeilToInt(Mathf.Sqrt(books.Count));
    
    // Determine appropriate gutter size based on resolution
    int gutterSize = resolution <= 6 ? 1 : (resolution <= 24 ? 2 : 4);
    
    // Size of each cell in the atlas (book size + gutters)
    int cellSize = resolution + gutterSize;
    
    // Create texture with appropriate dimensions
    int atlasSize = cellSize * booksPerRow;
    Texture2D atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGB24, false);
    
    // Fill atlas with books, leaving gutters between them
    for (int i = 0; i < books.Count; i++) {
        int x = (i % booksPerRow) * cellSize + gutterSize/2;
        int y = (i / booksPerRow) * cellSize + gutterSize/2;
        
        // Copy book pixels to atlas, positioning to leave gutters
        atlas.SetPixels(x, y, resolution, resolution, books[i].pixels);
        
        // Store UV coordinates for this book
        books[i].uvRect = new Rect(
            (float)x / atlasSize,
            (float)y / atlasSize,
            (float)resolution / atlasSize,
            (float)resolution / atlasSize
        );
    }
    
    atlas.Apply();
    return atlas;
}
```

### UV Mapping & Texture Sampling

When sampling from the atlas in shaders:

1. **Inset UVs**: For maximum safety, inset UV coordinates slightly to avoid edge sampling:

```glsl
// Fragment shader excerpt for safer atlas sampling
float2 AdjustUV(float2 uv, float2 atlasSize) {
    // Pull sampling points inward slightly
    float2 pixelSize = 1.0 / atlasSize;
    float2 safeUV = uv + pixelSize * 0.5;
    return safeUV;
}
```

2. **LOD Selection**: The shader should select the appropriate resolution level based on distance:

```csharp
// C# code to select appropriate resolution
void UpdateBookCoverLOD(Transform bookTransform, MeshRenderer renderer) {
    float distanceToCamera = Vector3.Distance(Camera.main.transform.position, bookTransform.position);
    
    // Select appropriate resolution based on distance
    string resolutionKey = distanceToCamera > 20f ? "2x3" :
                          (distanceToCamera > 10f ? "8x12" :
                          (distanceToCamera > 5f ? "16x24" : "64x96"));
    
    // Set the appropriate texture and UV rect for this resolution
    MaterialPropertyBlock props = new MaterialPropertyBlock();
    renderer.GetPropertyBlock(props);
    props.SetTexture("_MainTex", atlases[resolutionKey]);
    props.SetVector("_UVRect", books[bookId].uvRects[resolutionKey]);
    renderer.SetPropertyBlock(props);
}
```

### Dynamic Atlas Generation

For dynamic collections that aren't pre-generated during build:

1. Generate atlases at runtime for newly requested collections
2. Consider adding low-resolution atlases first for immediate visualization
3. Generate higher-resolution atlases in background threads if possible
4. Update materials when higher resolution atlases become available

This approach allows for efficient rendering of book collections while maintaining visual quality at all viewing distances.

## Development Setup

1. **Clone Repository**:
   ```bash
   git clone https://github.com/SimHacker/CraftSpace.git
   cd CraftSpace
   ```

2. **Unity Setup**:
   - Open Unity Hub
   - Add project from `Unity/CraftSpace`
   - Use Unity version 2021.3.11f1 or later

3. **SvelteKit Setup**:
   ```bash
   cd SvelteKit/BackSpace
   npm install
   # Starts development server on http://localhost:5173
   npm run dev
   ```

4. **Collection Processing**:
   ```bash
   cd SvelteKit/BackSpace
   npm run build:scripts
   npm run pipeline-full
   ```

## Building and Deployment

The project uses GitHub Actions for CI/CD. See [GITHUB-README.md](GITHUB-README.md) for details.

Manual build process:

1. **Process Collections**:
   ```bash
   cd SvelteKit/BackSpace
   npm run build:scripts
   npm run pipeline-full
   ```

2. **Build Unity WebGL**:
   - Open Unity project
   - File > Build Settings > WebGL > Build
   - Output to `SvelteKit/BackSpace/static/unity`

3. **Build SvelteKit App**:
   ```bash
   cd SvelteKit/BackSpace
   npm run build
   ```

4. **Deploy**:
   - Deploy `SvelteKit/BackSpace/build` to web server
   - Deploy collection data to CDN (optional)
