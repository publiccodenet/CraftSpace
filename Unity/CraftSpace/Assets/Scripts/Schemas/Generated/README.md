# Generated Schema Classes

This directory contains C# classes automatically generated from JSON schemas. **DO NOT EDIT** these files directly as your changes will be overwritten.

## Schema Pipeline

1. **JSON Schemas** are generated from TypeScript types in BackSpace and placed in:
   ```
   Assets/StreamingAssets/Content/schemas/*.json
   ```

2. **C# Classes** are generated from these JSON schemas and placed in:
   ```
   Assets/Scripts/Schemas/Generated/*.cs
   ```

## How to Generate

### 1. JSON Schema Files
From BackSpace TypeScript types to Unity StreamingAssets:
```bash
# In the BackSpace directory:
npm run schema:generate-all
```
This will:
1. Generate JSON schemas from TypeScript types using Zod
2. Copy them to `Unity/CraftSpace/Assets/StreamingAssets/Content/schemas`

### 2. C# Classes
From JSON schemas to C# classes:
1. Open Unity Editor
2. Click `Tools > Import JSON Schema` in the menu or use the Schema Generator window

This will:
1. Read schemas from `StreamingAssets/Content/schemas`
2. Generate C# classes in this directory (`Scripts/Schemas/Generated`) with "Schema" suffix (e.g., `ItemSchema.cs`, `CollectionSchema.cs`)
3. Apply Unity-specific attributes and type converters
4. Generate proper ScriptableObject classes with full Inspector integration

## Schema Features

### Type Converters
The schema generator supports custom type converters for special field types:
- `StringOrStringArrayToString`: Handles fields that can be either string or string[]
- `StringOrNullToString`: Handles fields that might be null
- `NullOrStringToStringArray`: Converts nullable or string values to string arrays
- `StringToDateTime`: Handles date/time conversions
- `UnixTimestampToDateTime`: Converts Unix timestamps to DateTime
- `Base64ToBinary`: Handles base64 string/byte array conversion

### Unity Inspector Integration
Schemas can include Unity-specific attributes for better Inspector integration:
- Headers and tooltips
- Multiline text areas
- Range validation
- Spacing controls
- Field ordering
- Grouping
- Width and height
- Read-only and delayed input

### Type/Class Attributes
Schemas can also include Unity-specific type attributes:
- Custom icons
- Help URLs
- Menu paths
- Create asset menu customization
- Color coding

### Additional Fields
All schemas include an `extraFields` property (renamed from `additionalProperties`) that stores any additional Internet Archive metadata fields not explicitly defined in the schema. This approach:

- Preserves all original data
- Handles evolving schemas and inconsistent field names
- Ensures backward compatibility
- Supports dynamic field access

## Base Class
All generated schema classes inherit from `SchemaGeneratedObject` (in the `CraftSpace` namespace), which provides:
- JSON serialization/deserialization
- Dynamic field access
- Field name normalization
- Validation support
- Event notifications for field changes

## Source of Truth Flow
```
[BackSpace TypeScript] → [StreamingAssets JSON Schemas] → [Generated C# Classes]
```

## Requirements
- JSON Schema files must include a top-level `title` property (e.g., "Item", "Collection")
- The generator will append "Schema" to the class names (e.g., "ItemSchema", "CollectionSchema")
- Field types are automatically mapped to appropriate C# types
- System.Object is used for object types to avoid ambiguity with UnityEngine.Object

## Namespace Organization
Generated classes use the namespace `CraftSpace.Schemas.Generated` and are inherited by our main classes in the `CraftSpace` namespace. 