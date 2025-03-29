# CraftSpace Schema Generator

A lightweight, Unity-focused JSON schema to C# generator that creates ScriptableObject classes with full Unity Editor integration and robust support for both JavaScript bridge communication and Internet Archive metadata.

## Background

This tool was inspired by [NJsonSchema](https://github.com/RicoSuter/NJsonSchema), but was created as a simpler, Unity-specific alternative that excels at two critical use cases:

1. **JavaScript Bridge Integration**:
   - Real-time communication between Unity and JavaScript
   - Dynamic field updates through the bridge
   - Transparent access to all fields
   - Consistent serialization behavior

2. **Internet Archive Metadata Support**:
   - Handling of evolving metadata schemas
   - Support for inconsistent field types
   - Preservation of unknown fields
   - Backward compatibility

## Features

- Generates ScriptableObject classes from JSON schemas
- Full Unity Inspector integration with:
  - Headers
  - Tooltips
  - Range validation
  - Spacing controls
  - Multi-line text areas
- Comprehensive dynamic field support:
  - JavaScript-like field access
  - Field change notifications
  - Validation callbacks
  - Bulk import/export
  - Type-safe access methods
- Robust metadata handling:
  - Schema-defined fields with Unity integration
  - Dynamic fields for unknown metadata
  - Mixed type support
  - Case-insensitive field access
  - Field name normalization
- Bridge-friendly features:
  - Transparent field access
  - Automatic serialization
  - Change notifications
  - Query support
- Editor-only code generation (not included in builds)

## Usage

### Basic Setup
1. In Unity, go to `CraftSpace > Schema Generator`
2. Enter or paste your JSON schema
3. Set the target namespace (default: `CraftSpace.Generated`)
4. Choose output location (default: `Assets/Scripts/Generated`)
5. Click Generate

### JavaScript Bridge Integration

```javascript
// JavaScript side
// Access any field (schema-defined or dynamic)
const title = queryObject.get("title");
const metadata = queryObject.get("some-metadata-field");

// Add new fields dynamically
queryObject.set("new-field", "value");

// Update existing fields
queryObject.set("title", "New Title");

// Handle field updates
bridge.on("fieldChanged", (fieldName, newValue) => {
    console.log(`Field ${fieldName} changed to:`, newValue);
});
```

```csharp
// Unity side
// Dynamic access to all fields
dynamic item = archiveItem.AsDynamic();
string title = item.title;
JToken metadata = item.someMetadataField;

// Type-safe access
if (archiveItem.TryGetField<string>("title", out var safeTitle))
{
    // Use safeTitle
}

// Field change notifications
archiveItem.OnDynamicFieldChanged.AddListener((args) => {
    Debug.Log($"Field {args.FieldName} changed from {args.OldValue} to {args.NewValue}");
});

// Validation
archiveItem.SetValidationCallback((fieldName, value) => {
    // Return true to allow the change, false to reject it
    return true;
});

// Bulk import
var newData = JObject.Parse(jsonFromJavaScript);
archiveItem.ImportDynamicFields(newData);
```

### Internet Archive Metadata Handling

```json
{
  "title": "ArchiveItem",
  "type": "object",
  "description": "Represents an Internet Archive item",
  "properties": {
    "identifier": {
      "type": "string",
      "description": "Unique item identifier",
      "required": true
    },
    "title": {
      "type": "string",
      "description": "Item title"
    },
    "description": {
      "type": "any",
      "description": "Item description (handles both string and string[])"
    }
  }
}
```

```csharp
// Handle mixed types
var description = item.description as JToken;
if (description.Type == JTokenType.Array)
{
    var descriptions = description.ToObject<string[]>();
}
else
{
    var singleDescription = description.ToString();
}

// Access unknown metadata
var unknownFields = item.UnknownFields;
foreach (var field in unknownFields.Properties())
{
    // Handle arbitrary metadata fields
}

// Add new metadata fields
item.SetDynamicField("new-metadata", someValue);

// Check field existence
if (item.HasField("some-field"))
{
    // Field exists (either schema-defined or dynamic)
}
```

## Design Philosophy

1. **JavaScript-First Bridge Integration**:
   - Transparent field access
   - Dynamic field support
   - Consistent behavior with JavaScript objects
   - Real-time updates

2. **Robust Metadata Handling**:
   - Schema evolution support
   - Unknown field preservation
   - Mixed type handling
   - Backward compatibility

3. **Unity Integration**:
   - Editor support for schema-defined fields
   - Serialization of all fields
   - Change notifications
   - Type safety when needed

4. **Developer Experience**:
   - Clear separation of concerns
   - Intuitive API
   - Comprehensive documentation
   - Flexible extension points

## Installation

1. Ensure you have Newtonsoft.Json in your Unity project
2. Copy the SchemaGenerator folder to your project's Editor folder
3. Restart Unity if needed

## Contributing

Feel free to extend or modify this tool for your needs. The codebase is intentionally small and focused to make modifications straightforward. 

## Schema Design Approach

Our schema design follows these key principles for handling Internet Archive metadata:

### 1. Field Categorization

- **Essential IA Fields**: Core Internet Archive fields like `identifier`, `title`, `description`, `creator`, `date`, etc. are modeled as top-level schema properties with rich annotations.
- **Application-Specific Fields**: Fields critical to our Unity application get first-class schema support and rich Unity editor integration.
- **Passthrough Fields**: All other Internet Archive metadata fields are handled through a dedicated passthrough field to maintain compatibility without cluttering the schema.

### 2. Annotation System

We use a flexible annotation system that supports:

- **Field-Level Annotations**: Applied to individual schema properties
  - C# attributes for serialization control
  - Unity-specific attributes for editor integration
  - Type conversion information
- **Class-Level Annotations**: Applied to the entire schema
  - Base class specification
  - Interface implementations
  - ScriptableObject menu paths

### 3. Schema Example

```typescript
// Example schema with annotations
const ItemSchema = withAnnotations(
  z.object({
    // Essential IA field with annotations
    identifier: withAnnotations(
      z.string().describe('Unique Internet Archive identifier'),
      unity.field({
        header: 'Basic Info',
        tooltip: 'The unique identifier for this item'
      }),
      unity.serializeField()
    ),
    
    title: withAnnotations(
      z.string().describe('Title of this item'),
      unity.field({
        tooltip: 'The main title displayed for this item'
      }),
      unity.serializeField()
    ),
    
    // Mixed type handling with converter
    description: withAnnotations(
      z.union([z.string(), z.array(z.string())]).optional()
        .describe('Description of the item'),
      unity.field({
        tooltip: 'Description of the item',
        multiline: true
      }),
      unity.serializeField(),
      StringOrStringArray // Type converter
    ),
    
    // Date fields with specialized converter
    date: withAnnotations(
      z.string().optional().describe('Publication date'),
      unity.field({
        tooltip: 'When this item was published'
      }),
      unity.serializeField(),
      DateTimeConverter
    ),
    
    // All other fields pass through here
    additionalProperties: z.record(z.any()).optional()
      .describe('Additional Internet Archive metadata fields')
  }),
  
  // Class-level Unity annotations
  unity.type({
    title: 'Internet Archive Item',
    description: 'Represents an Internet Archive item with metadata',
    menuPath: 'Content'
  }),
  
  // Class-level C# annotations
  csharp.class({
    baseClass: 'ScriptableObject',
    interfaces: ['ISerializationCallbackReceiver'],
    attributes: [
      { name: 'Serializable' }
    ]
  })
);
```

### 4. Data Flow Architecture

Our data pipeline ensures metadata consistency between Internet Archive and our application:

1. **Import**: Internet Archive metadata is imported, with essential fields mapped to schema properties and remaining fields preserved in the passthrough field.
2. **Runtime Access**: All fields (essential and passthrough) are accessible through the same API.
3. **Export**: When exporting, an application-specific data exporter can filter and transform fields as needed.
4. **Filtering**: Exporters determine which fields to include, exclude, or transform based on the target platform.

This approach gives us the best of both worlds: rich editor integration for important fields and complete preservation of Internet Archive metadata. 