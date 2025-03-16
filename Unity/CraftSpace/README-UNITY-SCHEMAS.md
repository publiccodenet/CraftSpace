# Unity Schema Integration

CraftSpace employs a robust schema-to-code pipeline to ensure type safety and consistency between the JavaScript and Unity codebases.

## Schema Pipeline Overview

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│                 │      │                 │      │                 │
│  Zod Schemas    │─────►│  JSON Schema    │─────►│  C# Classes     │
│  (TypeScript)   │      │  (JSON)         │      │  (C#)           │
│                 │      │                 │      │                 │
└─────────────────┘      └─────────────────┘      └─────────────────┘
```

## Process Steps

1. **Schema Definition**: Core data models defined using Zod in TypeScript
2. **Schema Export**: Zod schemas converted to standard JSON Schema format
3. **Schema Copy**: JSON Schema files copied to Unity project
4. **C# Generation**: SchemaImporter tool generates C# classes from JSON Schema
5. **Unity Integration**: Generated classes used in Unity for type-safe serialization

## Key Schema Models

### Collection Schema

The Collection schema represents a group of related items:

- **id**: Unique identifier for the collection, referred to as `collectionId` in other code
- **name**: Display name of the collection
- **query**: Internet Archive query string that defines the collection
- **description**: Detailed collection description
- **totalItems**: Count of items in the collection
- **lastUpdated**: Timestamp of last update

### Item Schema

The Item schema represents an individual content item:

- **id**: Unique identifier for the item, referred to as `itemId` in other code
- **title**: Display title of the item
- **creator**: Original creator/author
- **description**: Detailed item description
- **mediaType**: Type of media (text, video, audio, etc.)
- **date**: Publication or creation date
- **files**: Associated files for this item

## Using Generated C# Classes

The generated C# classes include:

- JsonProperty attributes for proper serialization
- XML documentation comments
- Type-safe properties
- Proper handling of collections and complex types

Example usage:

```csharp
// Deserialize item from JSON
string json = await GetJsonFromServer();
Item item = JsonConvert.DeserializeObject<Item>(json);

// Use strongly-typed properties
Debug.Log($"Item title: {item.Title}");
foreach (var file in item.Files) {
    Debug.Log($"File: {file.Name}");
}

// Serialize back to JSON
string updatedJson = JsonConvert.SerializeObject(item);
```

## Schema Importer Tool

Unity includes a custom Schema Importer tool (Window > CraftSpace > Schema Importer) that converts JSON Schema files to C# classes. This tool offers:

- Selection of specific schemas to import
- Custom namespace configuration
- Output directory selection
- Automatic property name formatting

## Updating Schemas

When schemas change in the backend:

1. Run `npm run schema:export` to update JSON Schema files
2. Run `npm run schema:copy` to copy schemas to Unity
3. In Unity, open the Schema Importer and generate updated C# classes
4. Unity will automatically recompile with the new type definitions 

## Schema Pipeline

The schema pipeline works as follows:

1. TypeScript source code defines Zod schemas in SvelteKit/BackSpace/src/lib/schemas
2. `npm run schema:export` converts Zod schemas to JSON Schema format using tsx
3. `npm run schema:copy` copies schema files to Unity/CraftSpace/Assets/Schemas
4. Inside Unity, the SchemaImporter converts JSON Schemas to C# classes 

## When working with data from Internet Archive:

- JSON data uses standard `id` field for both collections and items
- In C# models, we also use `id` to align with the JSON data
- In function signatures and variable names, use explicit `collectionId` and `itemId` for clarity

### Example:

```csharp
// C# model aligns with JSON structure
public class ItemData 
{
    public string id; // matches JSON property
    // other properties...
}

// Function signatures use explicit naming
public ItemData GetItemById(string collectionId, string itemId)
{
    // Implementation...
    return collection.items.Find(item => item.id == itemId);
}
``` 