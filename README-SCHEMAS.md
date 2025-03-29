# BackSpace Schema System

This document outlines the schema-driven development approach used in BackSpace, focusing on how we maintain type safety and data consistency across multiple platforms.

## Core Principles

- **Single Source of Truth**: JSON schemas define all data structures
- **Cross-Platform Compatibility**: Schema definitions flow to all platforms
- **Static Type Safety**: Auto-generated types in TypeScript and C#
- **Runtime Validation**: Consistent validation across environments
- **Developer Experience**: Streamlined workflow for schema evolution
- **Direct Integration**: No adapters or translation layers needed

## Schema Generation Workflow

BackSpace uses a clean, straightforward schema generation process:

1. **Define Schemas**: Create JSON schema definitions
2. **Generate C# Classes**: Run the schema importer to generate C# classes in the CraftSpace.Schema namespace
3. **Import in Unity**: Use Unity's "Tools > Import JSON Schema" menu to apply changes
4. **Extend in Unity**: Use partial classes to add Unity-specific functionality

### Workflow Steps in Detail

#### 1. Define Schemas

JSON schemas are defined and stored in:
```
SvelteKit/BackSpace/schemas/
```

Example schemas:
- `collection.json` - Collection schema
- `item.json` - Item schema

#### 2. Generate C# Classes

Run the schema importer from the BackSpace directory:

```bash
cd SvelteKit/BackSpace
npm run schema:generate-all
```

This command:
- Generates C# classes from JSON schemas
- Copies JSON schemas to central Content/schemas directory
- Copies JSON schemas to Unity's Content/Schemas directory

#### 3. Import in Unity

After generating the schemas and copying them to Unity:

1. Open Unity and refresh the project
2. Go to the menu: `Tools > Import JSON Schema`
3. C# classes are generated in `Assets/Generated/Schemas`
4. Refresh Unity again to see the new classes

#### 4. Extend with Unity Functionality

Create extension files in Unity that add Unity-specific functionality:

```csharp
// Assets/Scripts/Models/Extensions/ItemExtensions.cs
[Serializable]
public partial class Item : ScriptableObject
{
    // Unity-specific functionality
    [NonSerialized] public Texture2D cover;
    
    public void NotifyViewsOfUpdate()
    {
        // Implementation
    }
}
```

### Directory Structure

```
BackSpace/
└── src/
    └── lib/
        └── schemas/         # TypeScript schema definitions

Content/
└── schemas/                # Central JSON schema repository

Unity/CraftSpace/Assets/
├── Content/
│   └── Schemas/            # Unity copy of JSON schemas
├── Generated/
│   └── Schemas/            # Generated C# classes
└── Scripts/
    └── Models/
        └── Extensions/     # Unity extensions
```

### Schema File Locations

- **JSON Schemas (BackSpace)**: `SvelteKit/BackSpace/schemas/*.json`
- **JSON Schemas (Central)**: `Content/schemas/*.json`
- **JSON Schemas (Unity)**: `Unity/CraftSpace/Assets/Content/Schemas/*.json`
- **C# Classes**: `Unity/CraftSpace/Assets/Generated/Schemas/*.cs`
- **Unity Extensions**: `Unity/CraftSpace/Assets/Scripts/Models/Extensions/*.cs`

### Schema Naming Convention

Schemas follow this naming convention:

1. JSON schema: `[Name].json` (e.g., `Collection.json`)
2. Generated C# class: `[Name].cs` (e.g., `Collection.cs`)
3. Unity extension: `[Name]Extensions.cs` (e.g., `CollectionExtensions.cs`)

### Troubleshooting

If you encounter issues:

1. Verify the schemas directory exists:
   ```bash
   mkdir -p SvelteKit/BackSpace/schemas Content/schemas
   ```

2. Run the debug script to check paths:
   ```bash
   npm run path:debug
   ```

3. Check for schema errors:
   ```bash
   npm run schema:debug
   ```

4. If Unity doesn't recognize new schema changes:
   - Delete the generated C# files
   - Restart Unity
   - Re-run the import process

## Simple Schema Module

The Simple Schema Module provides a streamlined approach for using schema objects directly in Unity without complex adapters or compatibility layers.

### Overview

Located in `Assets/Scripts/Schema`, this module:
- Directly extends generated schema classes
- Provides clean, easy-to-use interfaces
- Simplifies data loading, serialization, and UI binding
- Eliminates the need for wrapper or adapter classes

### Key Components

#### 1. Schema Classes

```csharp
// Simple direct inheritance from generated classes
[Serializable]
public class Collection : Models.SchemaGenerated.Collection
{
    // Unity-specific extensions here
}

[Serializable]
public class Item : Models.SchemaGenerated.Item
{
    // Unity-specific fields and methods
    [NonSerialized] public Texture2D cover;
    [NonSerialized] public Collection parentCollection;
    
    // View system methods
    public void NotifyViewsOfUpdate() { /* ... */ }
    public event System.Action ModelUpdated;
}
```

#### 2. View System

The module includes a Model-View pattern for binding schema objects to UI:

```csharp
// Base class for Item views
public abstract class ItemView : MonoBehaviour
{
    [SerializeField] protected Item _item;
    
    // View handling with auto-registration
    public Item Item { get; set; } // Auto registers/unregisters
    
    // Override this to update your UI
    public abstract void HandleModelUpdated();
}

// Base class for Collection views
public abstract class CollectionView : MonoBehaviour
{
    [SerializeField] protected Collection _collection;
    
    // View handling
    public abstract void HandleModelUpdated();
}
```

#### 3. ModelLoader Utility

A utility class that simplifies loading schema objects from JSON:

```csharp
// Load directly from files
Collection collection = ModelLoader.LoadCollectionFromFile(jsonPath);
Item item = ModelLoader.LoadItemFromFile(jsonPath);

// Load from JSON strings
Collection collection = ModelLoader.LoadCollectionFromJson(jsonString);
Item item = ModelLoader.LoadItemFromJson(jsonString);

// Clear caches
ModelLoader.ClearCaches();
```

### Usage Example

```csharp
// Load a collection and its items
public void LoadCollection(string collectionId)
{
    // Load collection from JSON file
    string jsonPath = Path.Combine(contentPath, "collections", collectionId, "collection.json");
    Collection collection = ModelLoader.LoadCollectionFromFile(jsonPath);
    
    // Access properties directly
    Debug.Log($"Loaded: {collection.Name} ({collection.Id})");
    
    // Load associated items
    foreach (string itemDir in Directory.GetDirectories(itemsDir))
    {
        Item item = ModelLoader.LoadItemFromFile(Path.Combine(itemDir, "item.json"));
        item.parentCollection = collection;
    }
}
```

### Advantages

1. **Simplicity** - Direct inheritance with no complex adapter layers
2. **Performance** - No translation overhead between schema and Unity objects
3. **Type Safety** - Full type checking for all schema properties
4. **Unity Integration** - Schema objects are ScriptableObjects with Unity-specific extensions

### Directory Structure

```
Assets/Scripts/Schema/
├── Schema.cs            # Core schema classes and utilities
├── Examples/            # Example usage scripts
│   └── SimpleSchemaExample.cs
└── README.md            # Module-specific documentation
```

## Zod Schema Definition

[Zod](https://github.com/colinhacks/zod) serves as our primary schema definition tool, providing both TypeScript type generation and runtime validation.

### Basic Schema Definition

```typescript
// src/lib/schemas/collection.ts
import { z } from 'zod';

export const CollectionSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  query: z.string().optional(),
  description: z.string().optional(),
  created: z.string().datetime(),
  lastUpdated: z.string().datetime(),
  totalItems: z.number().int().nonnegative(),
  includeInUnity: z.boolean().optional().default(false),
  sort: z.string().optional(),
  limit: z.number().int().nonnegative().optional(),
  exportProfiles: z.array(z.string()).optional()
});

// Generate TypeScript type
export type Collection = z.infer<typeof CollectionSchema>;

// Create subschema for creation (subset of fields)
export const CollectionCreateSchema = CollectionSchema.omit({
  created: true,
  lastUpdated: true,
  totalItems: true
}).extend({
  // Add any creation-specific fields
});

export type CollectionCreate = z.infer<typeof CollectionCreateSchema>;
```

### Advanced Schema Patterns

#### Nested Objects

```typescript
// Item with metadata schema
export const ItemSchema = z.object({
  id: z.string().min(1),
  title: z.string().min(1),
  // Nested metadata object
  metadata: z.object({
    author: z.string().optional(),
    year: z.number().int().positive().optional(),
    tags: z.array(z.string()).default([]),
    ratings: z.record(z.number().min(0).max(5)).optional()
  }).optional()
});
```

#### Union Types and Discriminated Unions

```typescript
// Define a union type for different content types
const TextContentSchema = z.object({
  type: z.literal('text'),
  content: z.string()
});

const ImageContentSchema = z.object({
  type: z.literal('image'),
  url: z.string().url(),
  alt: z.string().optional()
});

// Discriminated union based on 'type' field
export const ContentBlockSchema = z.discriminatedUnion('type', [
  TextContentSchema,
  ImageContentSchema
]);
```

### Schema Organization

```
src/lib/schemas/
├── collection.ts      - Collection schemas
├── item.ts            - Item schemas
├── export-profile.ts  - Export profile schemas
├── connector.ts       - Connector schemas
└── index.ts           - Re-exports all schemas
```

## TypeScript Integration

### Type Inference

Zod automatically generates TypeScript types:

```typescript
import { CollectionSchema, type Collection } from '../lib/schemas/collection';

// Type-safe usage
const collection: Collection = {
  id: 'scifi',
  name: 'Science Fiction',
  query: 'subject:science fiction',
  created: new Date().toISOString(),
  lastUpdated: new Date().toISOString(),
  totalItems: 0
};
```

### Validation

```typescript
import { CollectionSchema } from '../lib/schemas/collection';
import { validateOrThrow } from '../lib/utils/validation';

// Runtime validation
function createCollection(data: unknown) {
  // This throws if validation fails
  const validCollection = validateOrThrow(CollectionSchema, data);
  
  // Now work with validated data
  saveCollection(validCollection);
}
```

### Form Validation Integration

Zod schemas can be used directly with form libraries:

```typescript
import { useForm } from 'svelte-forms';
import { toFormValidator } from '../lib/utils/form-validators';
import { CollectionCreateSchema } from '../lib/schemas/collection';

// In a Svelte component
const form = useForm({
  initialValues: {
    id: '',
    name: '',
    description: ''
  },
  validate: toFormValidator(CollectionCreateSchema),
  onSubmit: async (values) => {
    // Values are already validated against the schema
    await api.collections.create(values);
  }
});
```

## JSON Schema Export

We export Zod schemas to JSON Schema format for cross-platform use:

```typescript
// scripts/schema-export.ts
import { zodToJsonSchema } from 'zod-to-json-schema';
import fs from 'fs-extra';
import path from 'path';
import * as schemas from '../src/lib/schemas/index.js';

async function exportSchemas() {
  const outputDir = path.resolve('exports/schemas');
  await fs.ensureDir(outputDir);
  
  for (const [name, schema] of Object.entries(schemas)) {
    const jsonSchema = zodToJsonSchema(schema, { name });
    await fs.writeJSON(
      path.join(outputDir, `${name}.schema.json`),
      jsonSchema,
      { spaces: 2 }
    );
  }
}

exportSchemas().catch(console.error);
```

### Generated JSON Schema Example

A Zod schema like `CollectionSchema` will generate a JSON Schema like:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "minLength": 1
    },
    "name": {
      "type": "string",
      "minLength": 1
    },
    "query": {
      "type": "string"
    },
    "description": {
      "type": "string"
    },
    "created": {
      "type": "string",
      "format": "date-time"
    },
    "lastUpdated": {
      "type": "string",
      "format": "date-time"
    },
    "totalItems": {
      "type": "integer",
      "minimum": 0
    },
    "includeInUnity": {
      "type": "boolean",
      "default": false
    },
    "sort": {
      "type": "string"
    },
    "limit": {
      "type": "integer",
      "minimum": 0
    },
    "exportProfiles": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": ["id", "name", "created", "lastUpdated", "totalItems"],
  "additionalProperties": false
}
```

## Unity C# Integration

### C# Class Generation

We use [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) to generate C# classes from our JSON Schemas:

```csharp
// Unity Editor script to generate C# classes
using UnityEditor;
using UnityEngine;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using System.IO;
using System.Threading.Tasks;

public class SchemaImporter : EditorWindow
{
    [MenuItem("Tools/Import JSON Schema")]
    static async void GenerateModels()
    {
        string schemaDirectory = Path.Combine(Application.dataPath, "Content/Schemas");
        string outputDirectory = Path.Combine(Application.dataPath, "Generated/Schemas");
        
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
            
        foreach (string schemaFile in Directory.GetFiles(schemaDirectory, "*.json"))
        {
            await GenerateModelFromSchema(schemaFile, outputDirectory);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Model generation complete!");
    }
    
    static async Task GenerateModelFromSchema(string schemaPath, string outputDir)
    {
        string schemaJson = File.ReadAllText(schemaPath);
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            Namespace = "CraftSpace.Models.SchemaGenerated",
            ClassStyle = CSharpClassStyle.Poco,
            JsonLibrary = CSharpJsonLibrary.NewtonsoftJson
        });
        
        var code = generator.GenerateFile();
        string fileName = Path.GetFileNameWithoutExtension(schemaPath);
        File.WriteAllText(Path.Combine(outputDir, $"{fileName}.cs"), code);
        
        Debug.Log($"Generated {fileName}.cs");
    }
}
```

### Avoiding Namespace Conflicts

To avoid conflicts between our generated classes and our manual model classes, we follow these principles:

1. **Separate Namespaces**: 
   - Hand-written model classes: `CraftSpace.Models`
   - Generated schema classes: `CraftSpace.Models.SchemaGenerated`

2. **Partial Classes**: All generated classes are marked as `partial`, allowing us to extend them in separate files.

3. **Directory Organization**:
   - Hand-written models: `Assets/Scripts/Models/`
   - Generated schema classes: `Assets/Generated/Schemas/`

### Generated C# Class Example

For the `Collection` schema, the generated C# class would look like:

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CraftSpace.Models.SchemaGenerated
{
    public partial class Collection
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("query", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string? Query { get; set; }

        [JsonProperty("description", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("created", Required = Required.Always)]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("lastUpdated", Required = Required.Always)]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty("totalItems", Required = Required.Always)]
        public int TotalItems { get; set; }

        [JsonProperty("includeInUnity", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncludeInUnity { get; set; } = false;

        [JsonProperty("sort", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string Sort { get; set; }

        [JsonProperty("limit", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public int? Limit { get; set; }

        [JsonProperty("exportProfiles", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ExportProfiles { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Collection? FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<Collection>(json);
        }
    }
}
```

### Extending Generated Classes

You can extend these generated partial classes with your own functionality:

```csharp
// Assets/Scripts/Models/Extensions/CollectionExtensions.cs
using UnityEngine;

namespace CraftSpace.Models.SchemaGenerated
{
    // Add Unity-specific functionality to the generated class
    public partial class Collection : ScriptableObject
    {
        // Unity-specific fields
        [System.NonSerialized] public Texture2D previewImage;
        
        // Additional methods
        public void LoadPreview()
        {
            // Implementation
        }
        
        // Other Unity-specific functionality
    }
}
```

### JSON.NET Integration

The generated C# classes work seamlessly with JSON.NET:

```csharp
using Newtonsoft.Json;
using CraftSpace.Models;
using UnityEngine;
using System.Threading.Tasks;

public class CollectionLoader : MonoBehaviour
{
    public async Task<Collection> LoadCollection(string collectionId)
    {
        string json = await FetchCollectionData(collectionId);
        
        // Deserialize with JSON.NET
        Collection collection = JsonConvert.DeserializeObject<Collection>(json);
        
        // Use the strongly-typed object
        Debug.Log($"Loaded collection: {collection.Name} with {collection.TotalItems} items");
        
        return collection;
    }
    
    private async Task<string> FetchCollectionData(string collectionId)
    {
        // Implementation of data fetching
        // ...
    }
}
```

### Unity-BackSpace Bridge Integration

The schema system works with our P/Invoke bridge for WebGL communication:

```csharp
using CraftSpace.Models;
using Newtonsoft.Json;

public class BackSpaceBridge : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void _Bridge_SendToBackSpace(string messageType, string data);
    
    public void SendCollectionToBackSpace(Collection collection)
    {
        string json = JsonConvert.SerializeObject(collection);
        _Bridge_SendToBackSpace("collection_update", json);
    }
    
    // Receive data from BackSpace
    public void ReceiveCollectionFromBackSpace(string json)
    {
        try {
            Collection collection = JsonConvert.DeserializeObject<Collection>(json);
            // Process the received collection
            ProcessCollection(collection);
        }
        catch (JsonException ex) {
            Debug.LogError($"Failed to deserialize collection: {ex.Message}");
        }
    }
}
```

## Web/Client Integration

### Browser-Side Validation

For client-side validation, we use [Ajv](https://github.com/ajv-validator/ajv) with our exported JSON schemas:

```javascript
// client/src/validation.js
import Ajv from 'ajv';
import collectionSchema from '../schemas/CollectionSchema.schema.json';
import itemSchema from '../schemas/ItemSchema.schema.json';

const ajv = new Ajv();
const validateCollection = ajv.compile(collectionSchema);
const validateItem = ajv.compile(itemSchema);

export function validateCollectionData(data) {
  const valid = validateCollection(data);
  return {
    valid,
    errors: validateCollection.errors
  };
}
```

### SvelteKit API Validation

Server-side API endpoints use Zod for validation:

```typescript
// src/routes/api/collections/+server.ts
import { json } from '@sveltejs/kit';
import { CollectionCreateSchema } from '$lib/schemas/collection';

export async function POST({ request }) {
  const data = await request.json();
  
  // Validate using the schema
  const result = CollectionCreateSchema.safeParse(data);
  
  if (!result.success) {
    return json({ error: result.error.format() }, { status: 400 });
  }
  
  // Proceed with validated data
  const collection = result.data;
  // ... create collection logic
  
  return json({ success: true, id: collection.id });
}
```

## Schema Versioning

As schemas evolve, we maintain backward compatibility:

1. **Add, don't remove**: Add optional fields rather than removing fields
2. **Version in filename**: For breaking changes, version the schema (`collection_v2.schema.json`)
3. **Migration utilities**: Provide migration utilities for schema changes

### Versioning Strategy

When a breaking change is needed:

1. Create a new schema version (e.g., `CollectionSchemaV2`)
2. Create migration utilities between versions
3. Update the export process to generate both versions
4. Gradually transition code to use the new version

```typescript
// src/lib/schemas/collection.ts
export const CollectionSchemaV1 = z.object({
  // Original schema definition
});

export const CollectionSchemaV2 = z.object({
  // New schema with breaking changes
});

// Current active version
export const CollectionSchema = CollectionSchemaV2;

// Migration utility
export function migrateV1ToV2(v1Collection): z.infer<typeof CollectionSchemaV2> {
  // Transform from v1 to v2 format
  return {
    ...v1Collection,
    // Add new required fields or transform existing ones
  };
}
```

## Schema Update Workflow

When updating a schema:

1. Update the Zod schema definition in `SvelteKit/BackSpace/src/lib/schemas/`
2. Run the complete schema generation process:
   ```bash
   cd SvelteKit/BackSpace
   npm run schema:generate-all
   ```
   This will:
   - Export JSON Schemas to BackSpace schemas directory
   - Copy JSON Schemas to central Content/schemas directory
   - Copy JSON Schemas to Unity's Content/Schemas directory
   - Generate C# classes
3. Open Unity and go to `Tools > Import JSON Schema` to update C# classes
4. Update any affected TypeScript code
5. Test validation across all platforms

### Typical Schema Evolution Workflow

1. **Discussion Phase**: Discuss and document the needed schema changes
2. **Implementation**: Update Zod schema in TypeScript
3. **Testing**: Create tests for new schema validation
4. **Export**: Run the schema generation process
5. **Unity Import**: Import schemas in Unity and verify C# classes
6. **Integration**: Update any code that uses the schema
7. **End-to-End Testing**: Test the full data pipeline

## Best Practices

1. **Incremental Schema Changes**: Make small, incremental changes
2. **Documentation**: Document all schema changes in a changelog
3. **Test Coverage**: Ensure validation tests for all platforms
4. **Strict Validation**: Use stricter validation during development
5. **Schema Visualization**: Use tools like [JSON Schema Viewer](https://github.com/networknt/json-schema-viewer) for visualization
6. **Follow Naming Conventions**: Be consistent with property naming
7. **Keep Schemas DRY**: Extract common patterns into reusable schema components
8. **Include Descriptions**: Add clear descriptions for each property

### Schema Design Patterns

#### Reusable Sub-Schemas

```typescript
// Define reusable schema fragments
const TimestampFields = z.object({
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime()
});

// Use them in multiple schemas
const UserSchema = z.object({
  id: z.string().uuid(),
  name: z.string()
}).merge(TimestampFields);

const PostSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  content: z.string()
}).merge(TimestampFields);
```

#### Enum Validation

```typescript
// Define allowed values as an enum
export const CollectionTypeEnum = z.enum(['public', 'private', 'featured']);
export type CollectionType = z.infer<typeof CollectionTypeEnum>;

// Use in schema
export const CollectionSchema = z.object({
  // ...other properties
  type: CollectionTypeEnum,
  // ...more properties
});
```

## Resources

- [Zod Documentation](https://zod.dev/)
- [JSON Schema](https://json-schema.org/)
- [NJsonSchema](https://github.com/RicoSuter/NJsonSchema)
- [JSON.NET](https://www.newtonsoft.com/json)
- [Ajv Validator](https://ajv.js.org/)
- [zod-to-json-schema](https://github.com/StefanTerdell/zod-to-json-schema)

## Schema Design Principles

* All schemas are defined using Zod for TypeScript type safety
* Use `id` in model properties to align with JSON
* Use camelCase for variable and parameter names: `collectionId`, `itemId`
* Use snake_case for function names that handle these IDs: `get_collection()`, `update_item()`
* Keep schemas focused on one responsibility/entity 
* Include detailed descriptions for all properties to auto-generate documentation
* Prefer composition over inheritance for schema reuse 