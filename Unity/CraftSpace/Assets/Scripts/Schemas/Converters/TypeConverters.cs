using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CraftSpace.Schemas.Converters
{
    /// <summary>
    /// Base class for all type converters
    /// </summary>
    public abstract class BaseTypeConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return GetDefaultValue();
                
            JToken token = JToken.Load(reader);
            return ConvertFromJToken(token);
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken token = ConvertToJToken(value);
            token.WriteTo(writer);
        }
        
        protected abstract T GetDefaultValue();
        protected abstract T ConvertFromJToken(JToken token);
        protected abstract JToken ConvertToJToken(object value);
    }
    
    /// <summary>
    /// Converts null/undefined/string to a non-null string
    /// </summary>
    public class StringOrNullToStringConverter : BaseTypeConverter<string>
    {
        protected override string GetDefaultValue() => string.Empty;
        
        protected override string ConvertFromJToken(JToken token)
        {
            return token?.ToString() ?? string.Empty;
        }
        
        protected override JToken ConvertToJToken(object value)
        {
            return new JValue(value?.ToString() ?? string.Empty);
        }
    }
    
    /// <summary>
    /// Converts string or string[] to a single string, joining arrays with newlines
    /// </summary>
    public class StringOrStringArrayToStringConverter : BaseTypeConverter<string>
    {
        protected override string GetDefaultValue() => string.Empty;
        
        protected override string ConvertFromJToken(JToken token)
        {
            if (token == null)
                return string.Empty;
                
            if (token.Type == JTokenType.Array)
            {
                var values = token.ToObject<string[]>();
                return string.Join("\n", values);
            }
            
            return token.ToString();
        }
        
        protected override JToken ConvertToJToken(object value)
        {
            return new JValue(value?.ToString() ?? string.Empty);
        }
    }
    
    /// <summary>
    /// Converts null/string/string[] to string[]
    /// </summary>
    public class NullOrStringToStringArrayConverter : BaseTypeConverter<List<string>>
    {
        protected override List<string> GetDefaultValue() => new List<string>();
        
        protected override List<string> ConvertFromJToken(JToken token)
        {
            if (token == null)
                return new List<string>();
                
            if (token.Type == JTokenType.Array)
                return token.ToObject<List<string>>();
                
            return new List<string> { token.ToString() };
        }
        
        protected override JToken ConvertToJToken(object value)
        {
            var list = value as List<string>;
            if (list == null)
                return new JArray();
                
            return JArray.FromObject(list);
        }
    }
    
    /// <summary>
    /// Converts ISO-8601 string to DateTime in C#
    /// </summary>
    public class StringToDateTimeConverter : BaseTypeConverter<DateTime>
    {
        protected override DateTime GetDefaultValue() => DateTime.MinValue;
        
        protected override DateTime ConvertFromJToken(JToken token)
        {
            if (token == null)
                return DateTime.MinValue;
                
            if (DateTime.TryParse(token.ToString(), out DateTime result))
                return result;
                
            return DateTime.MinValue;
        }
        
        protected override JToken ConvertToJToken(object value)
        {
            if (value is DateTime dateTime)
                return new JValue(dateTime.ToString("o"));
                
            return new JValue(DateTime.MinValue.ToString("o"));
        }
    }
    
    /// <summary>
    /// Converts Unix timestamp to DateTime
    /// </summary>
    public class UnixTimestampToDateTimeConverter : BaseTypeConverter<DateTime>
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        protected override DateTime GetDefaultValue() => DateTime.MinValue;
        
        protected override DateTime ConvertFromJToken(JToken token)
        {
            if (token == null)
                return DateTime.MinValue;
                
            if (long.TryParse(token.ToString(), out long seconds))
                return Epoch.AddSeconds(seconds);
                
            return DateTime.MinValue;
        }
        
        protected override JToken ConvertToJToken(object value)
        {
            if (value is DateTime dateTime)
            {
                long seconds = (long)(dateTime.ToUniversalTime() - Epoch).TotalSeconds;
                return new JValue(seconds);
            }
            
            return new JValue(0);
        }
    }
    
    /// <summary>
    /// Converts base64 string to byte array
    /// </summary>
    public class Base64ToBinaryConverter : BaseTypeConverter<byte[]>
    {
        protected override byte[] GetDefaultValue() => new byte[0];
        
        protected override byte[] ConvertFromJToken(JToken token)
        {
            if (token == null)
                return new byte[0];
                
            try
            {
                return Convert.FromBase64String(token.ToString());
            }
            catch
            {
                return new byte[0];
            }
        }
        
        protected override JToken ConvertToJToken(object value)
        {
            if (value is byte[] bytes)
                return new JValue(Convert.ToBase64String(bytes));
                
            return new JValue(string.Empty);
        }
    }
} 