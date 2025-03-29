# Schema Management for CraftSpace

This document outlines our approach for managing schemas and generating C# model classes in the CraftSpace project.

## Core Concepts: Multi-Step Schema Pipeline

Our approach uses a multi-step pipeline that transforms schema definitions into usable C# code:

```
[TypeScript Zod Schema] → [JSON Schema] → [Generated C# Code]
```

Having JSON Schema as an intermediate format brings significant benefits:
- JSON Schema is a standard format consumable by many tools and platforms
- It separates data definition from code generation concerns
- Provides a clear inspection point for debugging and validation
- Can be used by various components beyond just C# code generation

## Directory Structure

```
Content/                     # Root content directory (outside Unity)
├── schemas/                # Schema definitions
│   ├── Collection.json
│   └── Item.json
└── collections/           # Content collections
    └── scifi/
        ├── collection.json
        └── items/
            └── item1/
                ├── cover.jpg
                └── item.json

Unity/CraftSpace/Assets/
├── StreamingAssets/
│   └── Content/          # Mirror of root Content directory
│       ├── schemas/      # JSON schema files (lowercase)
│       │   ├── Collection.json
│       │   └── Item.json
│       └── collections/  # Runtime content
│           └── scifi/    # Example collection
│               ├── collection.json
│               └── items/
│                   └── item1/
│                       ├── cover.jpg
│                       └── item.json
│
└── Scripts/
    └── Schemas/         # Schema-related code
        ├── Generated/   # Auto-generated C# base classes
        │   ├── Collection.cs
        │   └── Item.cs
        ├── Collection.cs
        ├── Item.cs
        ├── SchemaLoader.cs
        ├── ItemLoader.cs
        ├── SchemaHelper.cs
        └── SchemaImporter.cs
```

## Content Loading Strategy

All content is loaded from StreamingAssets, making it:
- Accessible to external tools (web viewers, PDF readers, etc.)
- Consistent with the root Content directory structure
- Easy to update without rebuilding the app
- Available for other runtime libraries

Example:
```csharp
// Base path for all content
string contentBase = Path.Combine(Application.streamingAssetsPath, "Content");

// Load schema
string schemaPath = Path.Combine(contentBase, "schemas/Item.json");
string schemaJson = File.ReadAllText(schemaPath);

// Load item content
string itemPath = Path.Combine(contentBase, $"collections/{collectionId}/items/{itemId}");
string coverPath = Path.Combine(itemPath, "cover.jpg");
string itemJsonPath = Path.Combine(itemPath, "item.json");

// Use UnityWebRequest for cross-platform compatibility
var coverRequest = UnityWebRequestTexture.GetTexture("file://" + coverPath);
yield return coverRequest.SendWebRequest();
Texture2D cover = DownloadHandlerTexture.GetContent(coverRequest);

var itemRequest = UnityWebRequest.Get("file://" + itemJsonPath);
yield return itemRequest.SendWebRequest();
string itemJson = itemRequest.downloadHandler.text;
```

## Schema Generation Process

1. **Define Schemas in TypeScript** (in BackSpace):
   ```typescript
   export const ItemSchema = z.object({
     id: z.string(),
     title: z.string(),
     // ...
   });
   ```

2. **Generate JSON Schemas**:
   ```bash
   # In BackSpace
   npm run schema:generate-all  # Exports and copies to Unity
   ```

3. **Generate C# Classes**:
   - In Unity Editor: `Tools > Import JSON Schema`
   - Generates base classes in `Scripts/Schemas/Generated/`

## Implementation Details

### Base Schema Loading

The `SchemaLoader` class provides core functionality:
```csharp
public abstract class SchemaLoader : MonoBehaviour
{
    // Caching for loaded objects
    protected static readonly Dictionary<string, Collection> CollectionCache;
    protected static readonly Dictionary<string, Item> ItemCache;
    
    // Common loading methods
    protected static T LoadFromJson<T>(string json, string id = null);
    protected static T LoadFromFile<T>(string filePath);
    protected static void SaveToFile<T>(T obj, string filePath);
}
```

### Schema Classes

Generated classes are extended with Unity functionality:

```csharp
// In Scripts/Schemas/Item.cs
public class Item : Generated.Item
{
    [NonSerialized] public Texture2D Cover;
    public void NotifyViewsOfUpdate() { /* ... */ }
}

// In Scripts/Schemas/Collection.cs
public class Collection : Generated.Collection
{
    [NonSerialized] public List<Item> LoadedItems;
    public void AddItem(Item item) { /* ... */ }
}
```

### Loading Implementation

The `ItemLoader` provides Unity-specific loading:
```csharp
public class ItemLoader : SchemaLoader
{
    public GameObject LoadItemModel(string collectionId, string itemId)
    {
        // Creates GameObject with mesh, material, etc.
        // Loads JSON data using base class methods
        // Sets up Unity components
    }
}
```

## Best Practices

1. **Schema Evolution**:
   - Make additive changes to schemas when possible
   - Version schemas for breaking changes
   - Provide migration utilities when needed

2. **Code Organization**:
   - Keep all schema-related code in `Scripts/Schemas`
   - Use inheritance over partial classes for clarity
   - Maintain separation between generated and custom code

3. **Loading and Caching**:
   - Use `SchemaLoader` methods for JSON operations
   - Leverage the built-in caching system
   - Handle errors gracefully with fallbacks

4. **Unity Integration**:
   - Use ScriptableObject for schema classes
   - Keep Unity-specific code in the extending classes
   - Use proper serialization attributes

## Workflow Tips

1. **Updating Schemas**:
   ```bash
   # In BackSpace
   npm run schema:generate-all  # Updates all schemas
   ```

2. **After Schema Changes**:
   - Run the Unity schema importer
   - Update extending classes if needed
   - Run tests to verify compatibility

3. **Debugging**:
   - Check generated JSON schemas in `Content/Schemas`
   - Verify generated C# in `Scripts/Schemas/Generated`
   - Use Unity console for runtime issues

## Future Improvements

1. **Performance Optimization**:
   - Async loading support
   - Improved caching strategies
   - Batch loading operations

2. **Developer Experience**:
   - Better error messages
   - Schema validation in editor
   - Visual schema designer

3. **Integration**:
   - Real-time schema updates
   - Better Unity editor tools
   - Automated testing support 