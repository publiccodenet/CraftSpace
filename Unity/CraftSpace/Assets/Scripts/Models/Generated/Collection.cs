using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CraftSpace.Models
{
    /// <summary>
    /// Schema for Collection
    /// </summary>
    public class Collection
    {
        /// <summary>
        /// Unique identifier for the collection
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Display name for the collection
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Internet Archive query string that defines the collection
        /// </summary>
        [JsonProperty("query")]
        public string Query { get; set; }

        /// <summary>
        /// ISO date of last update
        /// </summary>
        [JsonProperty("lastUpdated")]
        public string LastUpdated { get; set; }

        /// <summary>
        /// Total number of items in the collection
        /// </summary>
        [JsonProperty("totalItems")]
        public int TotalItems { get; set; }

        /// <summary>
        /// Human-readable description of the collection
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Sort order for items
        /// </summary>
        [JsonProperty("sort")]
        public string? Sort { get; set; }

        /// <summary>
        /// Maximum items (0 = unlimited)
        /// </summary>
        [JsonProperty("limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// Export profile configurations to use
        /// </summary>
        [JsonProperty("exportProfiles")]
        public object? ExportProfiles { get; set; }

        /// <summary>
        /// Relative path to cache directory
        /// </summary>
        [JsonProperty("cache_dir")]
        public string? CacheDir { get; set; }

        /// <summary>
        /// Path to cached index file
        /// </summary>
        [JsonProperty("index_cached")]
        public string? IndexCached { get; set; }

        /// <summary>
        /// Additional metadata for the collection
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

    }
}
