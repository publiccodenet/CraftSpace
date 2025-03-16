# BackSpace Schema System

This document outlines the schema-driven development approach used in BackSpace, focusing on how we maintain type safety and data consistency across multiple platforms.

## Core Principles

- **Single Source of Truth**: Zod schemas define all data structures
- **Cross-Platform Compatibility**: Schema definitions flow to all platforms
- **Static Type Safety**: Auto-generated types in TypeScript and C#
- **Runtime Validation**: Consistent validation across environments

## Zod Schema Definition

[Zod](https://github.com/colinhacks/zod) serves as our primary schema definition tool, providing both TypeScript type generation and runtime validation.

### Basic Schema Definition

```typescript
// src/lib/schemas/collection.ts
import { z } from 'zod';

export const CollectionSchema = z.object({
  collection_id: z.string().min(1),
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
  collection_id: 'scifi',
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
    [MenuItem("Tools/Generate Models from JSON Schemas")]
    static async void GenerateModels()
    {
        string schemaDirectory = Path.Combine(Application.dataPath, "../Schemas");
        string outputDirectory = Path.Combine(Application.dataPath, "Scripts/Models");
        
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
            
        foreach (string schemaFile in Directory.GetFiles(schemaDirectory, "*.schema.json"))
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
            Namespace = "BackSpace.Models",
            ClassStyle = CSharpClassStyle.Poco
        });
        
        var code = generator.GenerateFile();
        string fileName = Path.GetFileNameWithoutExtension(schemaPath).Replace(".schema", "");
        File.WriteAllText(Path.Combine(outputDir, $"{fileName}.cs"), code);
        
        Debug.Log($"Generated {fileName}.cs");
    }
}
```

### JSON.NET Integration

The generated C# classes work seamlessly with JSON.NET:

```csharp
using Newtonsoft.Json;
using BackSpace.Models;

public class CollectionLoader : MonoBehaviour
{
    public async void LoadCollection(string collectionId)
    {
        string json = await FetchCollectionData(collectionId);
        
        // Deserialize with JSON.NET
        Collection collection = JsonConvert.DeserializeObject<Collection>(json);
        
        // Use the strongly-typed object
        Debug.Log($"Loaded collection: {collection.Name} with {collection.TotalItems} items");
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

## Schema Versioning

As schemas evolve, we maintain backward compatibility:

1. **Add, don't remove**: Add optional fields rather than removing fields
2. **Version in filename**: For breaking changes, version the schema (`collection_v2.schema.json`)
3. **Migration utilities**: Provide migration utilities for schema changes

## Schema Update Workflow

When updating a schema:

1. Update the Zod schema definition
2. Run the export script to generate JSON Schema
3. Regenerate C# classes in Unity
4. Update any affected TypeScript code
5. Test validation across all platforms

## Best Practices

1. **Incremental Schema Changes**: Make small, incremental changes
2. **Documentation**: Document all schema changes in a changelog
3. **Test Coverage**: Ensure validation tests for all platforms
4. **Strict Validation**: Use stricter validation during development
5. **Schema Visualization**: Use tools like [JSON Schema Viewer](https://github.com/networknt/json-schema-viewer) for visualization

## Resources

- [Zod Documentation](https://zod.dev/)
- [JSON Schema](https://json-schema.org/)
- [NJsonSchema](https://github.com/RicoSuter/NJsonSchema)
- [JSON.NET](https://www.newtonsoft.com/json)
- [Ajv Validator](https://ajv.js.org/)

## Schema Design Principles

* All schemas are defined using Zod for TypeScript type safety
* Use `id` in model properties to align with JSON
* Use camelCase for variable and parameter names: `collectionId`, `itemId`
* Use snake_case for function names that handle these IDs: `get_collection()`, `update_item()`
* Keep schemas focused on one responsibility/entity 