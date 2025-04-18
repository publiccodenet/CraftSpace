# Content Directory Structure and Rules

FILE PATH: `Unity/CraftSpace/Assets/StreamingAssets/Content/README.md`

This directory serves as the central location for all runtime content consumed by the Unity application and potentially other tools or platforms. It acts as a **Single Source of Truth (SSOT)** for this content within the Unity project context, although the ultimate SSOT is often upstream (e.g., BackSpace for schemas).

## Core Philosophy: File System First

- **Identifiers as Paths:** The primary identifier for content items (like collections or individual items) is their **directory name**. This allows for easy browsing, management, and integration with version control and external tools.
- **Standardized Naming:**
    - Within a collection or item directory, the main data file is **always** named consistently (e.g., `collection.json`, `item.json`), regardless of the parent directory's ID. This avoids redundancy and simplifies loading logic.
    - Repeating the ID in the filename (e.g., `<item_id>.json`) is **discouraged** as it violates the SSOT principle (ID is already in the path).
- **ID Consistency:** While the directory name is the primary ID, the corresponding `.json` file *also* contains an `id` field. Pipelines and tools **must** validate that the directory name and the internal `id` field match. Discrepancies should trigger warnings or automated corrections to maintain consistency.
    - This allows for easy content duplication (copy/paste/rename directory) followed by automated ID fixing.

## Directory Structure Example

```
Content/
├── README.md             # This file
│
├── collections/          # Root directory for all collections
│   ├── collection_id_1/  # Directory named with the unique collection ID
│   │   ├── collection.json   # Main data file (contains "id": "collection_id_1")
│   │   ├── items/            # Directory containing items for this collection
│   │   │   ├── item_id_A/    # Directory named with unique item ID
│   │   │   │   └── item.json # Main item data (contains "id": "item_id_A")
│   │   │   ├── item_id_B/
│   │   │   │   └── item.json
│   │   │   └── ...
│   │   ├── items-index.json  # <<< DERIVED FILE for items in collection_id_1
│   │   ├── images/           # Optional: Images specific to the collection (e.g., cover)
│   │   │   └── cover.png
│   │   └── ...             # Other collection-specific assets
│   │
│   ├── collection_id_2/
│   │   ├── collection.json
│   │   ├── items/
│   │   │   └── ...
│   │   ├── items-index.json
│   │   └── ...
│   │
│   └── ...
│
├── collections-index.json # <<< DERIVED FILE for collections directory
│                          #     Contains: ["collection_id_1", "collection_id_2", ...]
│
├── schemas/              # JSON Schema definitions (SSOT is BackSpace)
│   │                     # NOTE: These schemas have metadata embedded via the description hack.
│   ├── Collection.json
│   ├── Item.json
│   └── ...
│
└── ...                   # Other top-level content types (e.g., textures, models)
```

## Derived Index Files (`*-index.json`)

- **Problem:** Unity's runtime (especially on platforms like WebGL or consoles) cannot reliably enumerate directory contents from `StreamingAssets`.
- **Solution:** For any directory containing multiple items that need to be discovered at runtime (like the `collections` directory or an `items` directory within a collection), a corresponding **index file** must be generated by the content pipeline.
- **Naming Convention:** The index file sits **next to** the directory it indexes and is named `<directory_name>-index.json` (e.g., `collections-index.json`, `items-index.json`).
- **Content:** The index file contains a simple JSON array of strings, where each string is the **directory name** (i.e., the ID) of the items within the indexed directory.
- **Source of Truth:** These index files are **derived data**, generated based on the *actual* directory contents in the upstream SSOT repository. They **should not** be manually edited or checked into the primary source control for the content itself, but rather generated as part of the pipeline that copies/filters content for a specific target (like Unity).

## Schema Content

- **Origin:** The JSON schemas in the `schemas/` directory are derived from Zod TypeScript definitions in the BackSpace project.
- **Metadata Embedding:** They have undergone a preprocessing step (`schema-export.js`) where metadata (like type converters) was extracted from specially formatted description strings (JSON appended after a newline in the Zod `.describe()`) and injected into an `x_meta` field. The original descriptions were cleaned up during this process.
    - **Note:** Consumers of the *original Zod schemas* must be aware of this description format, but consumers of *these JSON schemas* only need to look at the `description` and `x_meta.converter` fields.
- **Consumption:** These schemas are consumed by various tools, including the Unity C# generator (`SchemaGenerator.cs`). **Crucially**, consumers must be aware of the `x_meta` field and the strict rules against using reflection for runtime processing.

## Schema Consumption Rules (Critical for Runtime Safety)

When consuming the JSON schemas (e.g., in Unity C# code generation or other runtime tools):

1.  **NEVER USE REFLECTION-BASED JSON.NET METHODS OR ATTRIBUTES**:
    *   ⛔ `JToken.ToObject<T>()` - **CRASHES** IN WEBGL/IL2CPP
    *   ⛔ `JToken.FromObject(obj)` - **CRASHES** IN WEBGL/IL2CPP
    *   ⛔ `JsonConvert.DeserializeObject<T>(json)` when T is a user-defined class/struct (risk of reflection/stripping) - **CRASH RISK**
    *   ⛔ `JsonConvert.SerializeObject(obj)` when obj is a user-defined class/struct - **CRASH RISK**
    *   ⛔ `[JsonProperty]` attribute - **FORBIDDEN** (reflection-based)
    *   ⛔ `[JsonConverter]` attribute - **FORBIDDEN** (reflection-based activation)
    *   ⛔ Any method or attribute that uses .NET reflection on types during runtime serialization/deserialization.

2.  **ALWAYS USE DIRECT ACCESS METHODS INSTEAD**:
    *   ✅ Direct property assignment: `obj.Property = token.Value<Type>()` or explicit casts like `(int)token`.
    *   ✅ Manual iteration for lists/arrays.
    *   ✅ Direct type checking: `token.Type == JTokenType.X`.
    *   ✅ Direct manual construction of `JObject`/`JArray` for export.

3.  **FOR CONVERTERS (Specified in `x_meta.converter`)**:
    *   ✅ Define specific, IL2CPP-safe `JsonConverter` classes that **DO NOT USE REFLECTION INTERNALLY**.
    *   ✅ **CALL CONVERTERS EXPLICITLY** in generated code or runtime logic via direct helper methods (e.g., `new ConverterName().ReadJson(token)`), **NOT** via `[JsonConverter]` attribute or general `JsonConvert` methods.
    *   ⛔ Never use generic reflection-based converters.

## Future Content

This directory structure and the principles of File System First identifiers and derived index files will be applied to future content types added to the project. 