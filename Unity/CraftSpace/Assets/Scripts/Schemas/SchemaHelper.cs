using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class SchemaHelper
{
    public class SimpleSchema
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public Dictionary<string, SimpleSchemaProperty> Properties { get; set; }
        public List<string> Required { get; set; }
        
        public static SimpleSchema Parse(string json)
        {
            try
            {
                // Parse JSON into dynamic object
                JObject schemaObj = JObject.Parse(json);
                
                SimpleSchema schema = new SimpleSchema();
                schema.Type = schemaObj["type"]?.ToString();
                schema.Title = schemaObj["title"]?.ToString();
                schema.Required = schemaObj["required"]?.ToObject<List<string>>() ?? new List<string>();
                
                // Parse properties
                JObject propsObj = schemaObj["properties"] as JObject;
                if (propsObj != null)
                {
                    schema.Properties = new Dictionary<string, SimpleSchemaProperty>();
                    
                    foreach (var prop in propsObj)
                    {
                        string propName = prop.Key;
                        JObject propObj = prop.Value as JObject;
                        
                        if (propObj != null)
                        {
                            var schemaProp = new SimpleSchemaProperty
                            {
                                Name = propName,
                                Type = propObj["type"]?.ToString(),
                                Description = propObj["description"]?.ToString(),
                                IsRequired = schema.Required.Contains(propName)
                            };
                            
                            schema.Properties.Add(propName, schemaProp);
                        }
                    }
                }
                
                return schema;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse schema: {ex.Message}");
                return null;
            }
        }
    }
    
    public class SimpleSchemaProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
    }
} 