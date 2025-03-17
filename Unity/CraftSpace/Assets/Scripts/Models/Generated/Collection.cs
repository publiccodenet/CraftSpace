using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

#nullable enable

namespace CraftSpace.Models.Schema.Generated
{
    /// <summary>
    /// Schema for Collection
    /// </summary>
    public partial class Collection : ScriptableObject
    {
        /// <summary>
        /// Unique identifier for the collection
        /// </summary>
        [JsonProperty("id")]
        [SerializeField] private string? _id;
        public string? Id { get => _id; set => _id = value; }

        /// <summary>
        /// Display name for the collection
        /// </summary>
        [JsonProperty("name")]
        [SerializeField] private string? _name;
        public string? Name { get => _name; set => _name = value; }

        /// <summary>
        /// Internet Archive query string that defines the collection
        /// </summary>
        [JsonProperty("query")]
        [SerializeField] private string? _query;
        public string? Query { get => _query; set => _query = value; }

        /// <summary>
        /// ISO date of last update
        /// </summary>
        [JsonProperty("lastUpdated")]
        [SerializeField] private string? _lastUpdated;
        public string? LastUpdated { get => _lastUpdated; set => _lastUpdated = value; }

        /// <summary>
        /// Total number of items in the collection
        /// </summary>
        [JsonProperty("totalItems")]
        [SerializeField] private int? _totalItems;
        public int? TotalItems { get => _totalItems; set => _totalItems = value; }

        /// <summary>
        /// Human-readable description of the collection
        /// </summary>
        [JsonProperty("description")]
        [SerializeField] private string? _description;
        public string? Description { get => _description; set => _description = value; }

        /// <summary>
        /// Sort order for items
        /// </summary>
        [JsonProperty("sort")]
        [SerializeField] private string? _sort;
        public string? Sort { get => _sort; set => _sort = value; }

        /// <summary>
        /// Maximum items (0 = unlimited)
        /// </summary>
        [JsonProperty("limit")]
        [SerializeField] private int? _limit;
        public int? Limit { get => _limit; set => _limit = value; }

        /// <summary>
        /// IDs of items that failed validation and should be excluded
        /// </summary>
        [JsonProperty("excludedItemIds")]
        [SerializeField] private List<string>? _excludedItemIds;
        public List<string>? ExcludedItemIds { get => _excludedItemIds; set => _excludedItemIds = value; }

        /// <summary>
        /// Export profile configurations to use
        /// </summary>
        [JsonProperty("exportProfiles")]
        [SerializeField] private object? _exportProfiles;
        public object? ExportProfiles { get => _exportProfiles; set => _exportProfiles = value; }

        /// <summary>
        /// Relative path to cache directory
        /// </summary>
        [JsonProperty("cache_dir")]
        [SerializeField] private string? _cache_dir;
        public string? CacheDir { get => _cache_dir; set => _cache_dir = value; }

        /// <summary>
        /// Path to cached index file
        /// </summary>
        [JsonProperty("index_cached")]
        [SerializeField] private string? _index_cached;
        public string? IndexCached { get => _index_cached; set => _index_cached = value; }

        /// <summary>
        /// Additional metadata for the collection
        /// </summary>
        [JsonProperty("metadata")]
        [SerializeField] private Dictionary<string, object>? _metadata;
        public Dictionary<string, object>? Metadata { get => _metadata; set => _metadata = value; }



        /// <summary>
        /// Populate this object from JSON deserialization
        /// </summary>
        public void PopulateFromJson(CraftSpace.Models.Schema.Generated.Collection jsonData)
        {
            _id = jsonData.Id;
            _name = jsonData.Name;
            _query = jsonData.Query;
            _lastUpdated = jsonData.LastUpdated;
            _totalItems = jsonData.TotalItems;
            _description = jsonData.Description;
            _sort = jsonData.Sort;
            _limit = jsonData.Limit;
            _excludedItemIds = jsonData.ExcludedItemIds;
            _exportProfiles = jsonData.ExportProfiles;
            _cache_dir = jsonData.CacheDir;
            _index_cached = jsonData.IndexCached;
            _metadata = jsonData.Metadata;

            // Notify views of update
            NotifyViewsOfUpdate();
        }

    }
}
