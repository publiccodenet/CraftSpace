using UnityEngine;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CraftSpace.Editor.SchemaGenerator
{
    /// <summary>
    /// Event arguments for dynamic field changes
    /// </summary>
    public class DynamicFieldChangeEventArgs : EventArgs
    {
        public string FieldName { get; }
        public JToken OldValue { get; }
        public JToken NewValue { get; }

        public DynamicFieldChangeEventArgs(string fieldName, JToken oldValue, JToken newValue)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Base class for all schema-generated types that supports both schema-defined and dynamic fields.
    /// 
    /// This class serves two critical use cases:
    /// 
    /// 1. JavaScript Bridge Integration:
    ///    - Allows JavaScript to dynamically add/modify fields through the bridge
    ///    - Supports transparent access to both schema-defined and dynamic fields
    ///    - Preserves all fields during serialization for bridge communication
    ///    - Enables real-time updates and queries from JavaScript
    /// 
    /// 2. Internet Archive Metadata Support:
    ///    - Handles evolving and inconsistent metadata fields
    ///    - Preserves unknown fields that aren't in the schema
    ///    - Supports mixed data types (e.g., description as string or string[])
    ///    - Maintains backward compatibility as metadata evolves
    /// </summary>
    public abstract class SchemaGeneratedObject : ScriptableObject, ISchemaObject
    {
        // Event for dynamic field changes (Unity-friendly)
        [NonSerialized]
        private UnityEvent<DynamicFieldChangeEventArgs> _onDynamicFieldChanged = new();
        public UnityEvent<DynamicFieldChangeEventArgs> OnDynamicFieldChanged => _onDynamicFieldChanged;

        // Serialized storage for unknown fields
        [SerializeField]
        private string _unknownFieldsJson = "{}";

        // Runtime storage for unknown fields (managed by Newtonsoft.Json)
        [JsonExtensionData]
        private IDictionary<string, JToken> _unknownFields;

        // Cache for field name mappings
        private Dictionary<string, string> _normalizedToOriginalMap;
        private Dictionary<string, string> _originalToNormalizedMap;

        // Optional validation callback for dynamic field updates
        private Func<string, JToken, bool> _validationCallback;

        /// <summary>
        /// Access to unknown fields not mapped to specific properties.
        /// These fields are preserved during serialization and can be accessed from JavaScript.
        /// </summary>
        public JObject UnknownFields
        {
            get => JObject.Parse(_unknownFieldsJson);
            set => _unknownFieldsJson = value?.ToString() ?? "{}";
        }

        /// <summary>
        /// Set a validation callback for dynamic field updates.
        /// Return true to allow the update, false to reject it.
        /// </summary>
        public void SetValidationCallback(Func<string, JToken, bool> callback)
        {
            _validationCallback = callback;
        }

        protected virtual void OnEnable()
        {
            InitializeFieldMappings();
            _onDynamicFieldChanged = new UnityEvent<DynamicFieldChangeEventArgs>();
        }

        /// <summary>
        /// Bulk import dynamic fields from a JSON object.
        /// Useful when receiving data from JavaScript or Internet Archive API.
        /// </summary>
        public void ImportDynamicFields(JObject data)
        {
            var unknownFields = UnknownFields;
            foreach (var prop in data.Properties())
            {
                // Skip properties that are schema-defined
                if (HasSchemaDefined(prop.Name)) continue;

                var oldValue = unknownFields[prop.Name];
                if (_validationCallback?.Invoke(prop.Name, prop.Value) ?? true)
                {
                    unknownFields[prop.Name] = prop.Value;
                    _onDynamicFieldChanged?.Invoke(new DynamicFieldChangeEventArgs(prop.Name, oldValue, prop.Value));
                }
            }
            UnknownFields = unknownFields;
        }

        /// <summary>
        /// Check if a field exists (either schema-defined or dynamic)
        /// </summary>
        public bool HasField(string fieldName)
        {
            var originalName = GetOriginalFieldName(fieldName);
            return HasSchemaDefined(originalName) || UnknownFields[originalName] != null;
        }

        /// <summary>
        /// Check if a field is schema-defined (not dynamic)
        /// </summary>
        public bool HasSchemaDefined(string fieldName)
        {
            var originalName = GetOriginalFieldName(fieldName);
            return GetType().GetProperty(originalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase) != null;
        }

        /// <summary>
        /// Get a field value with type conversion, returns default if not found
        /// </summary>
        public T GetFieldOrDefault<T>(string fieldName, T defaultValue = default)
        {
            return TryGetField<T>(fieldName, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Set a dynamic field value with validation and change notification
        /// </summary>
        public bool SetDynamicField(string fieldName, object value)
        {
            var originalName = GetOriginalFieldName(fieldName);
            if (HasSchemaDefined(originalName))
            {
                Debug.LogWarning($"Cannot set dynamic field '{fieldName}' as it is schema-defined");
                return false;
            }

            var jtoken = JToken.FromObject(value);
            if (_validationCallback?.Invoke(originalName, jtoken) ?? true)
            {
                var unknownFields = UnknownFields;
                var oldValue = unknownFields[originalName];
                unknownFields[originalName] = jtoken;
                UnknownFields = unknownFields;
                _onDynamicFieldChanged?.Invoke(new DynamicFieldChangeEventArgs(originalName, oldValue, jtoken));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a dynamic field
        /// </summary>
        public bool RemoveDynamicField(string fieldName)
        {
            var originalName = GetOriginalFieldName(fieldName);
            if (HasSchemaDefined(originalName))
            {
                Debug.LogWarning($"Cannot remove field '{fieldName}' as it is schema-defined");
                return false;
            }

            var unknownFields = UnknownFields;
            var oldValue = unknownFields[originalName];
            if (oldValue != null)
            {
                unknownFields.Remove(originalName);
                UnknownFields = unknownFields;
                _onDynamicFieldChanged?.Invoke(new DynamicFieldChangeEventArgs(originalName, oldValue, null));
                return true;
            }
            return false;
        }

        private void InitializeFieldMappings()
        {
            _normalizedToOriginalMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _originalToNormalizedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Map known properties
            foreach (var prop in GetType().GetProperties())
            {
                var jsonProp = prop.GetCustomAttributes(typeof(JsonPropertyAttribute), true)
                    .FirstOrDefault() as JsonPropertyAttribute;
                
                if (jsonProp != null)
                {
                    var originalName = jsonProp.PropertyName ?? prop.Name;
                    var normalizedName = NormalizeFieldName(originalName);
                    
                    _normalizedToOriginalMap[normalizedName] = originalName;
                    _originalToNormalizedMap[originalName] = normalizedName;
                }
            }

            // Map unknown fields
            var unknownFields = UnknownFields;
            foreach (var prop in unknownFields.Properties())
            {
                var normalizedName = NormalizeFieldName(prop.Name);
                _normalizedToOriginalMap[normalizedName] = prop.Name;
                _originalToNormalizedMap[prop.Name] = normalizedName;
            }
        }

        /// <summary>
        /// Normalizes field names to camelCase and handles special characters
        /// </summary>
        protected virtual string NormalizeFieldName(string name)
        {
            // Handle special characters and convert to camelCase
            var parts = name.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return name;

            // First part is lowercase
            var result = parts[0].ToLower();
            
            // Subsequent parts are PascalCase
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the original field name from a normalized name
        /// </summary>
        protected string GetOriginalFieldName(string normalizedName)
        {
            if (_normalizedToOriginalMap == null)
            {
                InitializeFieldMappings();
            }

            return _normalizedToOriginalMap.TryGetValue(normalizedName, out var originalName)
                ? originalName
                : normalizedName;
        }

        /// <summary>
        /// Gets the normalized field name from an original name
        /// </summary>
        protected string GetNormalizedFieldName(string originalName)
        {
            if (_originalToNormalizedMap == null)
            {
                InitializeFieldMappings();
            }

            return _originalToNormalizedMap.TryGetValue(originalName, out var normalizedName)
                ? normalizedName
                : NormalizeFieldName(originalName);
        }

        /// <summary>
        /// Dynamic access to all fields with JavaScript-like behavior.
        /// Supports both schema-defined and dynamic fields with normalized naming.
        /// </summary>
        public dynamic AsDynamic()
        {
            return new DynamicSchemaObject(this);
        }

        /// <summary>
        /// Try to get a field value by name, including both schema-defined and dynamic fields.
        /// Supports case-insensitive and normalized field names.
        /// </summary>
        public bool TryGetField<T>(string fieldName, out T value)
        {
            var originalName = GetOriginalFieldName(fieldName);

            // First check schema-defined properties
            var prop = GetType().GetProperty(originalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanRead)
            {
                var propValue = prop.GetValue(this);
                if (propValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
            }

            // Then check dynamic fields
            var token = UnknownFields[originalName];
            if (token != null)
            {
                try
                {
                    value = token.ToObject<T>();
                    return true;
                }
                catch
                {
                    // Conversion failed
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Dynamic object wrapper that provides JavaScript-like field access
        /// with support for both schema-defined and dynamic fields.
        /// </summary>
        private class DynamicSchemaObject : DynamicObject
        {
            private readonly SchemaGeneratedObject _parent;

            public DynamicSchemaObject(SchemaGeneratedObject parent)
            {
                _parent = parent;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var originalName = _parent.GetOriginalFieldName(binder.Name);

                // First try schema-defined properties
                var prop = _parent.GetType().GetProperty(originalName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanRead)
                {
                    result = prop.GetValue(_parent);
                    return true;
                }

                // Then try dynamic fields
                var token = _parent.UnknownFields[originalName];
                if (token != null)
                {
                    result = token;
                    return true;
                }

                // Return null for undefined fields (JavaScript-like behavior)
                result = null;
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                return _parent.SetDynamicField(binder.Name, value);
            }
        }
    }

    /// <summary>
    /// Interface for schema-generated objects
    /// </summary>
    public interface ISchemaObject
    {
        JObject UnknownFields { get; set; }
        dynamic AsDynamic();
        bool TryGetField<T>(string fieldName, out T value);
    }

    /// <summary>
    /// Represents a JSON Schema type with Unity-specific extensions
    /// </summary>
    public class SchemaType
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, SchemaType> Properties { get; set; } = new();

        [JsonProperty("required")]
        public string[] Required { get; set; }

        [JsonProperty("enum")]
        public string[] Enum { get; set; }

        [JsonProperty("items")]
        public SchemaType Items { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("unityAttributes")]
        public Dictionary<string, object> UnityAttributes { get; set; } = new();

        /// <summary>
        /// Unity-specific attribute to control how the field appears in the Inspector
        /// </summary>
        [JsonProperty("unityField")]
        public UnityFieldAttributes FieldAttributes { get; set; } = new();

        [JsonProperty("errors")]
        public JToken[] Errors { get; set; }  // Custom validation errors that may contain Unity annotations

        [JsonProperty("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Unity-specific field attributes for customizing the Inspector appearance
    /// </summary>
    public class UnityFieldAttributes
    {
        [JsonProperty("header")]
        public string Header { get; set; }

        [JsonProperty("range")]
        public float[] Range { get; set; }

        [JsonProperty("multiline")]
        public bool Multiline { get; set; }

        [JsonProperty("tooltip")]
        public string Tooltip { get; set; }

        [JsonProperty("space")]
        public float? Space { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("order")]
        public int? Order { get; set; }

        [JsonProperty("width")]
        public float? Width { get; set; }

        [JsonProperty("height")]
        public float? Height { get; set; }

        [JsonProperty("readOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("delayed")]
        public bool Delayed { get; set; }
    }

    public class SchemaProperty
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public SchemaType Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("typeConverter")]
        public string TypeConverter { get; set; }

        [JsonProperty("errors")]
        public JToken[] Errors { get; set; }  // Custom validation errors that may contain Unity annotations
    }

    public class UnityTypeAttributes
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("unityAttributes")]
        public Dictionary<string, object> UnityAttributes { get; set; }

        [JsonProperty("helpUrl")]
        public string HelpUrl { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("scriptableObjectCreateMenu")]
        public ScriptableObjectCreateMenu ScriptableObjectCreateMenu { get; set; }
    }

    public class ScriptableObjectCreateMenu
    {
        [JsonProperty("menuName")]
        public string MenuName { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }
    }

    public class TypeConverterInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("options")]
        public Dictionary<string, object> Options { get; set; }
    }

    public static class SchemaExtensions
    {
        public static UnityFieldAttributes GetUnityFieldAttributes(this SchemaProperty property)
        {
            if (property.Errors == null) return null;

            foreach (var error in property.Errors)
            {
                try
                {
                    var message = error.ToString();
                    var unityField = JsonConvert.DeserializeObject<Dictionary<string, UnityFieldAttributes>>(message);
                    if (unityField != null && unityField.ContainsKey("unityField"))
                    {
                        return unityField["unityField"];
                    }
                }
                catch
                {
                    // Not a Unity field annotation
                }
            }

            return null;
        }

        public static UnityTypeAttributes GetUnityTypeAttributes(this SchemaType type)
        {
            if (type.Errors == null) return null;

            foreach (var error in type.Errors)
            {
                try
                {
                    var message = error.ToString();
                    var unityType = JsonConvert.DeserializeObject<Dictionary<string, UnityTypeAttributes>>(message);
                    if (unityType != null && unityType.ContainsKey("unityType"))
                    {
                        return unityType["unityType"];
                    }
                }
                catch
                {
                    // Not a Unity type annotation
                }
            }

            return null;
        }

        public static TypeConverterInfo GetTypeConverter(this SchemaType type)
        {
            if (type.Errors == null) return null;

            foreach (var error in type.Errors)
            {
                try
                {
                    var message = error.ToString();
                    var converter = JsonConvert.DeserializeObject<Dictionary<string, TypeConverterInfo>>(message);
                    if (converter != null && converter.ContainsKey("typeConverter"))
                    {
                        return converter["typeConverter"];
                    }
                }
                catch
                {
                    // Not a type converter annotation
                }
            }

            return null;
        }
    }
} 