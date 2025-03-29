using System;
using System.Collections.Generic;

namespace CraftSpace.Editor.SchemaGenerator
{
    /// <summary>
    /// Represents a type converter for converting between data types
    /// </summary>
    public class TypeConverter
    {
        /// <summary>
        /// The name of the converter to use
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Optional configuration options for the converter
        /// </summary>
        public object Options { get; set; }
    }

    /// <summary>
    /// Attribute to specify a type converter for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class TypeConverterAttribute : Attribute
    {
        /// <summary>
        /// The name of the converter to use
        /// </summary>
        public string ConverterName { get; }
        
        /// <summary>
        /// Optional configuration options for the converter
        /// </summary>
        public object Options { get; }

        /// <summary>
        /// Creates a new TypeConverterAttribute with the specified converter name and options
        /// </summary>
        /// <param name="converterName">The name of the converter to use</param>
        /// <param name="options">Optional configuration options for the converter</param>
        public TypeConverterAttribute(string converterName, object options = null)
        {
            ConverterName = converterName;
            Options = options;
        }
    }
} 