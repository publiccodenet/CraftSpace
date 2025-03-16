using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CraftSpace.Models
{
    /// <summary>
    /// Schema for Item
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Unique identifier for the item
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// ID of the collection this item belongs to
        /// </summary>
        [JsonProperty("collection_id")]
        public string CollectionId { get; set; }

        /// <summary>
        /// Display title of the item
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Author or creator of the item
        /// </summary>
        [JsonProperty("creator")]
        public string? Creator { get; set; }

        /// <summary>
        /// Human-readable description of the item contents
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Publication or creation date
        /// </summary>
        [JsonProperty("date")]
        public string? Date { get; set; }

        /// <summary>
        /// Type of media (texts, video, audio, etc.)
        /// </summary>
        [JsonProperty("mediaType")]
        public string? MediaType { get; set; }

        /// <summary>
        /// Subject categories or tags
        /// </summary>
        [JsonProperty("subjects")]
        public List<string>? Subjects { get; set; }

        /// <summary>
        /// Creation timestamp in the system
        /// </summary>
        [JsonProperty("created")]
        public string? Created { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        [JsonProperty("lastUpdated")]
        public string? LastUpdated { get; set; }

        [JsonProperty("files")]
        public List<Dictionary<string, object>>? Files { get; set; }

    }
}
