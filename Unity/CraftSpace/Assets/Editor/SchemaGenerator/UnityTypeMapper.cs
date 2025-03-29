using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CraftSpace.Editor.SchemaGenerator
{
    /// <summary>
    /// Maps JSON Schema types to Unity/C# types with appropriate defaults
    /// </summary>
    public static class UnityTypeMapper
    {
        private static readonly Dictionary<string, TypeInfo> JsonToUnityTypes = new()
        {
            { "string", new TypeInfo("string", "\"\"") },
            { "integer", new TypeInfo("int", "0") },
            { "number", new TypeInfo("float", "0f") },
            { "boolean", new TypeInfo("bool", "false") },
            { "array", new TypeInfo("List<{0}>", "new()") },
            { "object", new TypeInfo(null, "null") },
            { "any", new TypeInfo("JToken", "null") }  // Support for arbitrary JSON data
        };

        public struct TypeInfo
        {
            public string Type { get; }
            public string DefaultValue { get; }

            public TypeInfo(string type, string defaultValue)
            {
                Type = type;
                DefaultValue = defaultValue;
            }
        }

        /// <summary>
        /// Gets the Unity/C# type and default value for a given schema type
        /// </summary>
        public static TypeInfo GetUnityType(SchemaType schema, string className = null)
        {
            // Handle "any" type explicitly
            if (schema.Type == "any")
            {
                return JsonToUnityTypes["any"];
            }

            if (schema.Enum != null)
            {
                return new TypeInfo(
                    className ?? "Enum",
                    schema.Enum.Length > 0 ? $"{className}.{schema.Enum[0]}" : "0"
                );
            }

            if (!JsonToUnityTypes.TryGetValue(schema.Type, out var unityType))
            {
                // Default to JToken for unknown types to support arbitrary JSON
                return JsonToUnityTypes["any"];
            }

            if (schema.Type == "array" && schema.Items != null)
            {
                var itemType = GetUnityType(schema.Items);
                return new TypeInfo(
                    string.Format(unityType.Type, itemType.Type),
                    unityType.DefaultValue
                );
            }

            return unityType.Type == null && className != null
                ? new TypeInfo(className, "null")
                : unityType;
        }
    }
} 