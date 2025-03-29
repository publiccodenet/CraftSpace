using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CraftSpace
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
    /// Interface for objects that can be serialized to and from JSON
    /// </summary>
    public interface ISchemaObject
    {
        void ImportFromJson(string json);
        string ExportToJson();
        void ImportFromJToken(JToken token);
        JToken ExportToJToken();
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
        /// Try to get a field value with type conversion
        /// </summary>
        public bool TryGetField<T>(string fieldName, out T value)
        {
            var originalName = GetOriginalFieldName(fieldName);
            
            // First try schema-defined properties
            var property = GetType().GetProperty(originalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null)
            {
                var propValue = property.GetValue(this);
                if (propValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
                try
                {
                    // Try conversion for compatible types
                    value = (T)Convert.ChangeType(propValue, typeof(T));
                    return true;
                }
                catch
                {
                    // Conversion failed, fall through to dynamic fields
                }
            }
            
            // Then try dynamic fields
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

        // ISchemaObject implementation
        public virtual void ImportFromJson(string json)
        {
            JsonConvert.PopulateObject(json, this);
        }

        public virtual string ExportToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public virtual void ImportFromJToken(JToken token)
        {
            JsonConvert.PopulateObject(token.ToString(), this);
        }

        public virtual JToken ExportToJToken()
        {
            return JToken.FromObject(this);
        }
    }
} 