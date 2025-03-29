# CraftSpace Unity Code Documentation

This document provides an overview of the Unity code architecture for the Internet Archive CraftSpace viewer. The application allows users to browse and interact with Internet Archive collections in an immersive 3D environment.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
3. [Schema Architecture](#schema-architecture)
4. [Data Model](#data-model)
5. [View System](#view-system)
6. [Renderer System](#renderer-system)
7. [Layout System](#layout-system)
8. [Control System](#control-system)
9. [Utilities](#utilities)
10. [Scene Setup](#scene-setup)
11. [Setup Instructions](#setup-instructions)

## Architecture Overview

CraftSpace follows a Model-View-Renderer architecture:

- **Models**: Data containers that hold collection and item metadata
- **Views**: Components that display models and manage user interaction
- **Renderers**: Specialized components that provide specific visualizations of models
- **Layout**: Components that arrange views in 3D space
- **Controls**: Components that handle user input and camera movement

## Core Components

### Brewster

Named after Brewster Kahle, the visionary founder of the Internet Archive, the Brewster class embodies the same pragmatic philosophy as its namesake. Just as Brewster Kahle built the Internet Archive by initially taking on all necessary tasks himself before thoughtfully delegating as the organization grew, our Brewster class serves as the application's central orchestrator and singular "god object".

This architectural approach is intentional and philosophically aligned with the Internet Archive's own evolution. The Brewster class begins as a comprehensive "kitchen sink" object that directly manages all core responsibilities, gradually delegating specialized tasks to subordinate components only when clearly beneficial. This pragmatic approach avoids premature modularization while maintaining a clear chain of command.

The Brewster class's responsibilities include:

- Orchestrating content discovery and loading
- Managing the full lifecycle of collections and items
- Coordinating view creation and updates
- Handling asset management and caching
- Providing a centralized access point for application state

Like its real-world counterpart, the Brewster class exemplifies the principle that effective systems often start with a dedicated central authority that knows how to get things done, rather than an over-engineered committee of specialized services.

### CollectionBrowserManager

Manages the display of multiple collections in the scene:

- Creates layout objects for all collections
- Handles collection selection and focusing
- Coordinates collection-level operations

## Schema Architecture

CraftSpace uses a multi-stage schema pipeline that ensures type safety and consistency across platforms:

### Schema Generation Pipeline

1. **Zod Schemas (TypeScript)**
   - Single source of truth about JSON, TypeScript, and C# model types
   - Define the core schema types using Zod
   - Validate data structures at runtime
   - Generate TypeScript types from schema definitions
   - Flexible annotations for type conversion, field renaming, Unity editor integration, documentation, etc
   - Handles variations and quirks in Internet Archive metadata formats (e.g. fields that can be either single strings or arrays of strings)

2. **TypeScript Classes and JSON Schemas**
   - Auto-generate TypeScript interfaces from Zod schemas
   - Create JSON Schema definitions for cross-platform compatibility
   - Export schema specifications for C# generation and other tools

3. **C# Schema Classes**
   - Generate C# schema classes from JSON Schema definitions
   - Create data transfer objects (DTOs) for serialization/deserialization
   - Implement validation logic matching Zod schemas

4. **Unity Integration Classes**
   - Subclass schema classes to add Unity-specific functionality
   - Implement MonoBehaviour and ScriptableObject integration
   - Add view management, parent-child relationships, and asset handling

This pipeline ensures that all platforms share the same data structure definitions, maintaining consistency while allowing platform-specific extensions.

## Data Model

### Collection

The Collection class manages a set of related items:

- **Schema Integration**
  - Extends CollectionSchema generated from JSON schema
  - Adds Unity-specific functionality as a ScriptableObject

- **Properties**
  - Basic metadata: id, name, description, etc.
  - Item management: items list, lookup methods
  - Visual elements: thumbnail, preview images

- **Item Management**
  - Maintains a list of child Item objects
  - Provides lookup methods by ID and other properties
  - Handles child item creation and registration

- **View Integration**
  - Maintains references to associated CollectionView instances
  - Notifies views when data changes
  - Provides registration/unregistration methods for views

### Item

The Item class represents a single content item:

- **Schema Integration**
  - Extends ItemSchema generated from JSON schema
  - Adds Unity-specific functionality as a ScriptableObject

- **Properties**
  - Basic metadata: id, title, creator, description, etc.
  - Media references: cover image, content links
  - Relationship data: parent collection reference, related items

- **Parent-Child Relationships**
  - Maintains reference to parent Collection
  - Handles bidirectional updates
  - Provides navigation to related items

- **View Integration**
  - Maintains references to ItemView instances
  - Notifies views when data changes
  - Supports multiple simultaneous views of the same item

### Content Loader

The content loading system handles the retrieval and initialization of data:

- **Resource Loading**
  - Discovers collections and items from Resources folder
  - Loads JSON data from structured directory hierarchy
  - Parses and validates against schema definitions

- **Asset Management**
  - Loads associated assets (images, thumbnails, etc.)
  - Creates Unity-specific assets as needed
  - Handles placeholder content during loading

- **Caching and Performance**
  - Implements multi-level cache for data and assets
  - Performs background loading to maintain performance
  - Prioritizes visible content loading

- **Lifecycle Management**
  - Handles initialization of new data objects
  - Manages updates when data changes
  - Provides cleanup when objects are no longer needed

## View System

CraftSpace uses a modular prefab-based view system that provides several advantages over a traditional dynamic renderer approach:

### Multi-View Architecture

Instead of a single view that dynamically switches between renderers, CraftSpace employs multiple specialized views for each item, each with a fixed, purpose-built renderer:

- Different views can be active simultaneously, allowing cross-fading and combined visualization
- Each view specializes in one visualization context (detail level, mode, selection state)
- Views can be activated/deactivated based on distance, object state, selection state, or user preferences

### Collection View and Item Containers

The CollectionView acts as a parent container that manages ItemViewsContainers:

- Handles creation and arrangement of item views
- Manages the collection's overall visualization
- Responds to model updates by updating child views

### Item Views Container

A key innovation in the architecture is the ItemViewsContainer, which manages multiple specialized item views for a single item:

- Maintains references to specialized views (cover image, label, details)
- Handles view activation/deactivation based on context
- Supports cross-fading between view states
- Updates views when the underlying model changes

### Item View

Item views are specialized components that focus on one visualization approach:

- Each view handles a specific aspect of item visualization
- Views implement the IModelView interface for model updates
- Views maintain their own internal state for animations and transitions

### Specialized View Types

The system includes several specialized view types:

- **SingleImageItemView**: Displays the item's cover image with placeholder support
- **ItemLabelView**: Shows the item's title and basic metadata
- **DetailedItemView**: Shows comprehensive metadata for selected items

## Renderer System

In the new architecture, renderers are tightly integrated with their specific view prefabs, rather than being dynamically attached at runtime:

### SingleImageRenderer

The SingleImageRenderer handles displaying cover images with placeholder support:

- Configurable aspect ratio for placeholders
- Supports both placeholder and actual image states
- Handles transitions between states

### Prefab-Based Integration

Renderers are pre-configured in prefabs with standard Unity components:

- Mesh components are configured directly in the prefab hierarchy
- Materials and shaders are assigned in the inspector
- Text components (TMP) are set up with proper styling
- Additional effects like particles are added where needed

This prefab-based approach offers several advantages:
- Full use of Unity's prefab system for inheritance and overrides
- Better organization of complex component trees
- Easier configuration through the inspector
- Clearer responsibility boundaries at the prefab level

## Layout System

### CollectionGridLayout

Arranges items in a grid layout:
- Creates a grid arrangement of items
- Handles positioning and spacing of item views
- Adjusts layout based on collection properties

## Control System

### OrthographicCameraController

Handles camera movement and zoom:
- Implements keyboard and mouse navigation
- Manages orthographic camera settings
- Provides methods to focus on specific collections or items

### ItemSelectionManager

Manages selection and hovering on items:
- Handles mouse hover detection
- Manages item selection state
- Communicates with views for visual feedback

## Utilities

### ImageLoader

Handles loading and caching of images:
- Loads images from local resources
- Implements caching for performance
- Provides error handling and placeholders

## Scene Setup

A properly configured scene requires:

1. A **Brewster** GameObject to manage content
2. A **CameraRig** with the OrthographicCameraController component
3. A **CollectionsContainer** for holding collection layouts
4. An **ItemSelectionManager** for handling selection
5. **Prefabs** for various ItemView types and CollectionGridLayout

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

1. **Models/Schema/**
   - CollectionSchema.cs 
   - ItemSchema.cs
   - SchemaType.cs
2. **Models/**
   - Collection.cs
   - Item.cs
3. **Views/**
   - CollectionView.cs
   - ItemView.cs
   - ItemViewsContainer.cs
   - SingleImageItemView.cs
   - ItemLabelView.cs
4. **Views/Renderers/Base/**
   - BaseViewRenderer.cs
5. **Views/Renderers/**
   - SingleImageRenderer.cs
6. **Layout/**
   - CollectionGridLayout.cs
7. **Controls/**
   - OrthographicCameraController.cs
   - ItemSelectionManager.cs
8. **Core/**
   - Brewster.cs
   - CollectionBrowserManager.cs
9. **Utils/**
   - ImageLoader.cs

### Layer Setup

1. Go to Edit > Project Settings > Tags and Layers
2. Under "Layers" section, set User Layer 6 to "Items"
3. Click "Apply"

### Creating Prefabs

#### Item View Prefabs

1. Create specialized item view prefabs:
   - **SingleImageItemView Prefab**
     - Base GameObject with SingleImageItemView script
     - Child GameObject with SingleImageRenderer and mesh components
   - **ItemLabelView Prefab**
     - Base GameObject with ItemLabelView script
     - Child GameObject with TextMeshPro component
   - **DetailedItemView Prefab**
     - Base GameObject with DetailedItemView script
     - Child GameObjects for metadata display

#### ItemViewsContainer Prefab

1. In Hierarchy window, right-click and select Create Empty
2. Rename to "ItemViewsContainer"
3. In Inspector panel:
   - Add Component > Scripts > Views > ItemViewsContainer
   - Add Component > Physics > Box Collider
   - Set Box Collider Size: X=1.5, Y=1.5, Z=0.1
   - Check "Is Trigger" on the Box Collider
4. Select "Items" from the Layer dropdown at the top of the Inspector
5. Add specialized view prefabs as children:
   - Drag SingleImageItemView Prefab as a child
   - Drag ItemLabelView Prefab as a child
   - Configure references in the ItemViewsContainer component
6. Create a Prefabs folder in your Assets if it doesn't exist
7. Drag the ItemViewsContainer from Hierarchy to the Prefabs folder

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
   - Item View Container Prefab: Drag the ItemViewsContainer you created from the Project window
5. Drag the CollectionLayoutPrefab from Hierarchy to the Prefabs folder

### Scene Setup

1. Create a new scene or open an existing one
2. Add required GameObjects:
   - Create empty GameObject named "Brewster" and add BrewsterManager component
   - Create CameraRig with OrthographicCameraController
   - Create CollectionsContainer for holding collection layouts
   - Add ItemSelectionManager GameObject
3. Configure components:
   - Set references between components
   - Configure camera settings
   - Set up input mappings

### Control System

The control system handles user input and camera movement:

- **OrthographicCameraController**
  - WASD/Arrow keys for panning
  - Mouse wheel for zoom
  - Right-click drag for rotation
  
- **ItemSelectionManager** 
  - Left-click to select items
  - Hover detection
  - Selection highlighting

### Future Development

Planned features and improvements:

- Advanced filtering and search
- Collection organization tools
- Enhanced visualization modes
- Performance optimizations
- Mobile/touch support
- VR/AR compatibility