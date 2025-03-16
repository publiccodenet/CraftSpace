# CraftSpace Unity Code Documentation

This document provides an overview of the Unity code architecture for the Internet Archive CraftSpace viewer. The application allows users to browse and interact with Internet Archive collections in an immersive 3D environment.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
3. [Data Model](#data-model)
4. [View System](#view-system)
5. [Renderer System](#renderer-system)
6. [Layout System](#layout-system)
7. [Control System](#control-system)
8. [Utilities](#utilities)
9. [Scene Setup](#scene-setup)
10. [Setup Instructions](#setup-instructions)

## Architecture Overview

CraftSpace follows a Model-View-Renderer architecture:

- **Models**: Data containers that hold collection and item metadata
- **Views**: Components that display models and manage user interaction
- **Renderers**: Specialized components that provide different visualizations of models
- **Layout**: Components that arrange views in 3D space
- **Controls**: Components that handle user input and camera movement

## Core Components

### Brewster

The central "god object" that manages content loading and access:

```csharp
public class Brewster : MonoBehaviour
{
    public static Brewster Instance { get; private set; }
    public List<CollectionData> collections = new List<CollectionData>();
    
    // Loads all collections from Resources
    public void LoadAllContent() { ... }
    
    // Gets collection by ID
    public CollectionData GetCollection(string collectionId) { ... }
    
    // Gets item by collection and item ID
    public ItemData GetItem(string collectionId, string itemId) { ... }
}
```

### CollectionBrowserManager

Manages the display of multiple collections in the scene:

```csharp
public class CollectionBrowserManager : MonoBehaviour
{
    // Creates layout objects for all collections
    private void CreateCollectionLayouts() { ... }
    
    // Focuses camera on a specific collection
    public void FocusOnCollection(int collectionIndex) { ... }
}
```

## Data Model

### CollectionData

ScriptableObject that stores collection metadata:

```csharp
public class CollectionData : ScriptableObject
{
    public string id;
    public string name;
    public string description;
    public string query;
    public string lastUpdated;
    public int totalItems;
    public List<ItemData> items = new List<ItemData>();
    public Texture2D thumbnail;
    
    // View management
    public void RegisterView(CollectionView view) { ... }
    public void UnregisterView(CollectionView view) { ... }
    public void NotifyViewsOfUpdate() { ... }
}
```

### ItemData

ScriptableObject that stores item metadata:

```csharp
public class ItemData : ScriptableObject
{
    public string id;
    public string title;
    public string creator;
    public string date;
    public string description;
    public string mediatype;
    public List<string> subject = new List<string>();
    public int downloads;
    public CollectionData parentCollection;
    public Texture2D cover;
    public bool isFavorite;
    
    // View management
    public void RegisterView(ItemView view) { ... }
    public void UnregisterView(ItemView view) { ... }
    public void NotifyViewsOfUpdate() { ... }
    public void OnCoverLoaded() { ... }
}
```

## View System

### CollectionView

Base component for displaying a collection:

```csharp
public class CollectionView : MonoBehaviour
{
    public CollectionData Model { get; set; }
    
    // Called when model data changes
    public virtual void OnModelUpdated() { ... }
    
    // Creates views for all items in the collection
    public void CreateItemViews() { ... }
    
    // Creates a view for a specific item
    public ItemView CreateItemView(ItemData itemModel) { ... }
}
```

### ItemView

Base component for displaying an item:

```csharp
public class ItemView : MonoBehaviour
{
    public ItemData Model { get; set; }
    
    // Called when model data changes
    public virtual void OnModelUpdated() { ... }
    
    // Gets or adds a renderer of specified type
    public T GetOrAddRenderer<T>() where T : BaseViewRenderer { ... }
    
    // Shows or hides a specific renderer
    public void ShowRenderer<T>(bool show) where T : BaseViewRenderer { ... }
}
```

## Renderer System

### BaseViewRenderer

Abstract base class for all renderers:

```csharp
public abstract class BaseViewRenderer : MonoBehaviour
{
    // Activates the renderer
    public virtual void Activate() { ... }
    
    // Deactivates the renderer
    public virtual void Deactivate() { ... }
    
    // Updates the renderer with model data
    public abstract void UpdateWithModel(object model);
}
```

### ArchiveTileRenderer

Renders an item as a tile with image and title:

```csharp
public class ArchiveTileRenderer : ItemViewRenderer
{
    // Updates the tile with item data
    public override void UpdateWithItemModel(ItemData model) { ... }
    
    // Gets the image URL for an item
    private string GetTileImageUrl(ItemData model) { ... }
    
    // Generates a random color for placeholder display
    private void GenerateRandomColorForItem() { ... }
}
```

The ArchiveTileRenderer displays items with the following priorities:
1. Use model.cover texture if already loaded
2. Show a random color while attempting to load from Internet Archive
3. Keep the random color if image loading fails

### TextMetadataRenderer

Renders detailed item metadata using TextMeshPro:

```csharp
public class TextMetadataRenderer : ItemViewRenderer
{
    // Updates the text display with item data
    public override void UpdateWithItemModel(ItemData model) { ... }
}
```

### Other Renderers

- **PixelIconRenderer**: Simple pixel-art representation for distant views
- **SingleImageRenderer**: Just shows the cover image
- **HighlightParticleRenderer**: Particle effects for selection/hover

## Layout System

### CollectionGridLayout

Arranges items in a grid layout:

```csharp
public class CollectionGridLayout : MonoBehaviour
{
    // Creates the grid of items
    private void CreateItemGrid() { ... }
    
    // Creates an item view at a position
    private ItemView CreateItemView(ItemData itemData, Vector3 position) { ... }
    
    // Gets the total size of the grid
    public Vector2 GetGridSize() { ... }
}
```

## Control System

### CameraController

Handles orthographic camera movement and zoom:

```csharp
using UnityEngine;

public class OrthographicCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fastMoveSpeed = 15f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 2f;
    [SerializeField] private float _minOrthographicSize = 2f;
    [SerializeField] private float _maxOrthographicSize = 20f;
    
    private Camera _camera;
    
    private void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
        
        // Ensure camera is set to orthographic
        if (_camera && !_camera.orthographic)
        {
            _camera.orthographic = true;
            _camera.orthographicSize = (_minOrthographicSize + _maxOrthographicSize) / 2;
        }
    }
    
    private void Update()
    {
        HandleMovementInput();
        HandleZoomInput();
    }
    
    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
            float speed = Input.GetKey(KeyCode.LeftShift) ? _fastMoveSpeed : _moveSpeed;
            transform.position += moveDirection * speed * Time.deltaTime;
        }
    }
    
    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (_camera && Mathf.Abs(scrollInput) > 0.01f)
        {
            // Zoom by adjusting orthographic size
            float newSize = _camera.orthographicSize - scrollInput * _zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(newSize, _minOrthographicSize, _maxOrthographicSize);
        }
    }
    
    public void FocusOnCollection(CollectionGridLayout collectionLayout)
    {
        if (collectionLayout == null)
            return;
        
        // Get the collection position and size
        Vector3 targetPosition = collectionLayout.transform.position;
        Vector2 gridSize = collectionLayout.GetGridSize();
        
        // Move to position (keeping y coordinate the same)
        transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        
        // Set appropriate zoom level based on grid size
        if (_camera)
        {
            float maxDimension = Mathf.Max(gridSize.x, gridSize.y);
            _camera.orthographicSize = Mathf.Clamp(maxDimension / 1.8f, _minOrthographicSize, _maxOrthographicSize);
        }
    }
}
```

### ItemSelectionManager

Manages selection and hovering on items:

```csharp
public class ItemSelectionManager : MonoBehaviour
{
    // Handles mouse hover detection
    private void HandleMouseHover() { ... }
    
    // Handles mouse click selection
    private void HandleMouseClick() { ... }
    
    // Selects an item
    public void SelectItem(ItemView itemView) { ... }
    
    // Deselects the current item
    public void DeselectItem() { ... }
}
```

## Utilities

### ImageLoader

Static utility for loading images from URLs:

```csharp
public static class ImageLoader
{
    // Loads an image from URL with callbacks
    public static IEnumerator LoadImageFromUrl(
        string url, 
        Action<Texture2D> onComplete, 
        Action<string> onError = null) { ... }
    
    // Clears the texture cache
    public static void ClearCache() { ... }
}
```

## Scene Setup

A properly configured scene requires:

1. A **Brewster** GameObject to manage content
2. A **CameraRig** with the OrthographicCameraController component
3. A **CollectionsContainer** for holding collection layouts
4. An **ItemSelectionManager** for handling selection
5. **Prefabs** for ItemView and CollectionGridLayout

## Setup Instructions

1. Create a new Unity scene
2. Add the hierarchy as described in the Scene Setup section
3. Configure components according to the provided specifications
4. Create the required prefabs
5. Ensure proper configuration of layers for item selection
6. Place collection JSON files in the Resources folder
7. Run the scene to test loading and navigation

For a detailed walkthrough of scene setup, see below.

## Detailed Scene Setup Guide

This section provides complete step-by-step instructions to set up the CraftSpace scene from scratch.

### Required Unity Packages

First, make sure you have these packages installed (Window > Package Manager):

1. **TextMeshPro** - For text rendering
2. **Input System** - For advanced input handling (optional but recommended)

### Required Scripts

The following scripts must be created in your project:

1. **Models/**
   - CollectionData.cs
   - ItemData.cs
2. **Views/**
   - CollectionView.cs
   - ItemView.cs
3. **Views/Renderers/Base/**
   - BaseViewRenderer.cs
4. **Views/Renderers/**
   - ArchiveTileRenderer.cs
   - TextMetadataRenderer.cs
   - PixelIconRenderer.cs
   - SingleImageRenderer.cs
   - HighlightParticleRenderer.cs
5. **Layout/**
   - CollectionGridLayout.cs
6. **Controls/**
   - OrthographicCameraController.cs
   - ItemSelectionManager.cs
7. **Core/**
   - Brewster.cs
   - CollectionBrowserManager.cs
8. **Utils/**
   - ImageLoader.cs

### Layer Setup

1. Go to Edit > Project Settings > Tags and Layers
2. Under "Layers" section, set User Layer 6 to "Items"
3. Click "Apply"

### Creating Prefabs

#### ItemViewPrefab

1. In Hierarchy window, right-click and select Create Empty
2. Rename to "ItemViewPrefab"
3. In Inspector panel:
   - Add Component > Scripts > Views > ItemView
   - Add Component > Physics > Box Collider
   - Set Box Collider Size: X=1.5, Y=1.5, Z=0.1
   - Check "Is Trigger" on the Box Collider
4. Select "Items" from the Layer dropdown at the top of the Inspector
5. Configure the ItemView component:
   - Auto Initialize Renderers: false
   - Close Distance: 5
   - Medium Distance: 20
   - Far Distance: 100
6. Create a Prefabs folder in your Assets if it doesn't exist
7. Drag the ItemViewPrefab from Hierarchy to the Prefabs folder

#### CollectionLayoutPrefab

1. In Hierarchy window, right-click and select Create Empty
2. Rename to "CollectionLayoutPrefab"
3. In Inspector panel:
   - Add Component > Scripts > Layout > CollectionGridLayout
4. Configure the CollectionGridLayout component:
   - Item Width: 2.0
   - Item Height: 2.5
   - Item Spacing: 0.5
   - Center Grid: true
   - Item View Prefab: Drag the ItemViewPrefab you created from the Project window
5. Drag the CollectionLayoutPrefab from Hierarchy to the Prefabs folder

### Scene Hierarchy Setup

Create this hierarchy structure:

1. **Main** (Create empty GameObject)
   - Add Component > Scripts > Controls > ItemSelectionManager
   - Configure ItemSelectionManager:
     - Selection Layer Mask: Click the dropdown and check only "Items"
     - Max Selection Distance: 100
     - Hover Highlight Color: Set to light yellow (1, 1, 0.5, 0.7)
     - Selected Highlight Color: Set to gold (1, 0.8, 0.2, 0.9)

2. **Brewster** (Create empty GameObject as child of Main)
   - Add Component > Scripts > Core > Brewster
   - Add Component > Scripts > Core > CollectionBrowserManager
   - Configure Brewster:
     - Base Resource Path: "Content"
     - Load On Start: checked
     - Create Scriptable Objects: checked
     - Verbose: checked (for debugging)

3. **CollectionsContainer** (Create empty GameObject as child of Main)
   - No components needed

4. **CameraRig** (Create empty GameObject as child of Main)
   - Position: (0, 15, 0)
   - Rotation: (90, 0, 0)
   - Add Component > Scripts > Controls > OrthographicCameraController
   - Configure CameraController:
     - Move Speed: 5
     - Fast Move Speed: 15
     - Zoom Speed: 10
     - Min Zoom: 2
     - Max Zoom: 20

5. **Main Camera** (Create Camera as child of CameraRig)
   - Position: (0, 0, 0) relative to CameraRig
   - Rotation: (0, 0, 0)
   - Clear Flags: Solid Color
   - Background: Dark blue (0.1, 0.1, 0.2)
   - Projection: Orthographic
   - Orthographic Size: 10

6. **UI** (Create Canvas)
   - Render Mode: Screen Space - Overlay
   - Create a child Panel named "ItemDetails"
   - Set ItemDetails Panel initially inactive
   - Add TextMeshPro Text objects for title, creator, date, description

### Configuration Connections

1. Select the **Brewster** GameObject
2. In the CollectionBrowserManager component:
   - Collections Container: Drag the CollectionsContainer GameObject here
   - Item View Prefab: Drag the ItemViewPrefab from the Project window
   - Collection Layout Prefab: Drag the CollectionLayoutPrefab from the Project window
   - Camera Rig: Drag the CameraRig GameObject here

### Content Setup

1. Create folders in your Assets/Resources directory:
   - Content
   - Content/collections
   - Content/collections/scifi
   - Content/collections/scifi/items
   - Content/placeholders

2. Create placeholder images:
   - Create or import a 180x180 image as "cover.jpg" in Content/placeholders
   - Create or import a 400x300 image as "collection-thumbnail.jpg" in Content/placeholders

3. Create your collections-index.json in Content folder:
   ```json
   ["scifi"]
   ```

4. Create your items-index.json in Content/collections/scifi folder:
   ```json
   ["item1", "item2", "item3", "item4"]
   ```

5. Create item.json files in Content/collections/scifi/items/item1, item2, etc.

### Testing

1. Make sure all scripts are compiled without errors
2. Press Play in the Unity editor
3. Check the Console for any errors or warnings
4. Verify that collections and items are loaded and displayed
5. Test camera navigation:
   - WASD keys for movement
   - Mouse wheel for zoom
   - Hold Shift for faster movement
6. Test item selection by clicking on items

### Troubleshooting

If collections don't load:
- Check the Console for error messages
- Verify that your JSON files are in the correct format and location
- Make sure the Brewster.baseResourcePath is set correctly

If renderers don't appear:
- Check that the ItemView prefab has the correct layer
- Verify that the CollectionGridLayout is correctly configured
- Check that the camera is positioned correctly to see the items 

## Future Development Roadmap

Before we get to the roadmap, here's the implementation of the OrthographicCameraController script:

```csharp
using UnityEngine;

public class OrthographicCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fastMoveSpeed = 15f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 2f;
    [SerializeField] private float _minOrthographicSize = 2f;
    [SerializeField] private float _maxOrthographicSize = 20f;
    
    private Camera _camera;
    
    private void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
        
        // Ensure camera is set to orthographic
        if (_camera && !_camera.orthographic)
        {
            _camera.orthographic = true;
            _camera.orthographicSize = (_minOrthographicSize + _maxOrthographicSize) / 2;
        }
    }
    
    private void Update()
    {
        HandleMovementInput();
        HandleZoomInput();
    }
    
    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
            float speed = Input.GetKey(KeyCode.LeftShift) ? _fastMoveSpeed : _moveSpeed;
            transform.position += moveDirection * speed * Time.deltaTime;
        }
    }
    
    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (_camera && Mathf.Abs(scrollInput) > 0.01f)
        {
            // Zoom by adjusting orthographic size
            float newSize = _camera.orthographicSize - scrollInput * _zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(newSize, _minOrthographicSize, _maxOrthographicSize);
        }
    }
    
    public void FocusOnCollection(CollectionGridLayout collectionLayout)
    {
        if (collectionLayout == null)
            return;
        
        // Get the collection position and size
        Vector3 targetPosition = collectionLayout.transform.position;
        Vector2 gridSize = collectionLayout.GetGridSize();
        
        // Move to position (keeping y coordinate the same)
        transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        
        // Set appropriate zoom level based on grid size
        if (_camera)
        {
            float maxDimension = Mathf.Max(gridSize.x, gridSize.y);
            _camera.orthographicSize = Mathf.Clamp(maxDimension / 1.8f, _minOrthographicSize, _maxOrthographicSize);
        }
    }
}
```

The following features and enhancements are planned for future development:

### Camera and Navigation Enhancements

- **Advanced Camera Controls**
  - Support for perspective view with orbital navigation
  - "Street view" mode for immersive exploration
  - Smooth transitions between camera modes
  - Camera path animations and guided tours

- **Level of Detail (LOD) System**
  - Automatic LOD based on distance/zoom level
  - Optimized rendering for different view distances
  - Snapping to optimal elevations to maximize detail fidelity
  - Performance optimizations for large collections

- **View Transitions**
  - Cross-fading between different view renderers
  - Smooth animations when switching view modes
  - Progressive loading of higher-detail assets

### Rendering and Visualization

- **Enhanced Renderers**
  - 3D book models with page turning animations
  - Dynamic text rendering for readable content
  - Audio preview integration for audio items
  - Video preview integration for video items

- **Advanced Visual Effects**
  - Ambient occlusion and realistic shadows
  - Dynamic lighting based on content themes
  - Particle effects for highlighting special items
  - Visual indicators for item relationships

### User Interaction

- **Advanced Selection and Manipulation**
  - Multi-item selection
  - Drag and drop organization
  - Pinning/favoriting items
  - Custom collection creation

- **Search and Filter Tools**
  - Advanced search interface
  - Real-time filtering by metadata
  - Tag-based navigation
  - Timeline visualization

### Data and Content

- **Extended Media Support**
  - PDF viewing with page navigation
  - Audio player with visualization
  - Video player with timeline
  - 3D object viewer for applicable content

- **Enhanced Metadata Integration**
  - Rich metadata display
  - Cross-linking between related items
  - External reference integration
  - Citation and source tools

### Integration and Synchronization

- **SvelteKit/Unity Integration**
  - Real-time synchronization between platforms
  - Shared user states and preferences
  - Unified authentication system
  - Bidirectional content updates

- **Cloud Features**
  - User collections stored in the cloud
  - Collaborative viewing and annotation
  - Sharing capabilities
  - Version history and change tracking

This roadmap represents our vision for the project's evolution. Features will be implemented incrementally, with priority given to core functionality and user experience improvements. 