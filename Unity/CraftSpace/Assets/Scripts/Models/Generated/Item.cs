using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

#nullable enable

namespace CraftSpace.Models.Schema.Generated
{
    /// <summary>
    /// Schema for Item
    /// </summary>
    public partial class Item : ScriptableObject
    {
        /// <summary>
        /// Unique identifier for the item
        /// </summary>
        [JsonProperty("id")]
        [SerializeField] private string? _id;
        public string? Id { get => _id; set => _id = value; }

        /// <summary>
        /// Display title of the item
        /// </summary>
        [JsonProperty("title")]
        [SerializeField] private string? _title;
        public string? Title { get => _title; set => _title = value; }

        /// <summary>
        /// Author or creator of the item
        /// </summary>
        [JsonProperty("creator")]
        [SerializeField] private string? _creator;
        public string? Creator { get => _creator; set => _creator = value; }

        /// <summary>
        /// Human-readable description of the item contents
        /// </summary>
        [JsonProperty("description")]
        [SerializeField] private string? _description;
        public string? Description { get => _description; set => _description = value; }

        /// <summary>
        /// Publication or creation date
        /// </summary>
        [JsonProperty("date")]
        [SerializeField] private string? _date;
        public string? Date { get => _date; set => _date = value; }

        /// <summary>
        /// Type of media (texts, video, audio, etc.)
        /// </summary>
        [JsonProperty("mediaType")]
        [SerializeField] private string? _mediaType;
        public string? MediaType { get => _mediaType; set => _mediaType = value; }

        /// <summary>
        /// Subject categories or tags
        /// </summary>
        [JsonProperty("subjects")]
        [SerializeField] private List<string>? _subjects;
        public List<string>? Subjects { get => _subjects; set => _subjects = value; }

        /// <summary>
        /// Whether the item is a favorite
        /// </summary>
        [JsonProperty("isFavorite")]
        [SerializeField] private bool? _isFavorite;
        public bool? IsFavorite { get => _isFavorite; set => _isFavorite = value; }

        /// <summary>
        /// Number of downloads
        /// </summary>
        [JsonProperty("downloads")]
        [SerializeField] private float? _downloads;
        public float? Downloads { get => _downloads; set => _downloads = value; }

        /// <summary>
        /// Creation timestamp in the system
        /// </summary>
        [JsonProperty("created")]
        [SerializeField] private string? _created;
        public string? Created { get => _created; set => _created = value; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        [JsonProperty("lastUpdated")]
        [SerializeField] private string? _lastUpdated;
        public string? LastUpdated { get => _lastUpdated; set => _lastUpdated = value; }

        /// <summary>
        /// Associated files
        /// </summary>
        [JsonProperty("files")]
        [SerializeField] private List<Dictionary<string, object>>? _files;
        public List<Dictionary<string, object>>? Files { get => _files; set => _files = value; }



        /// <summary>
        /// Populate this object from JSON deserialization
        /// </summary>
        public void PopulateFromJson(CraftSpace.Models.Schema.Generated.Item jsonData)
        {
            _id = jsonData.Id;
            _title = jsonData.Title;
            _creator = jsonData.Creator;
            _description = jsonData.Description;
            _date = jsonData.Date;
            _mediaType = jsonData.MediaType;
            _subjects = jsonData.Subjects;
            _isFavorite = jsonData.IsFavorite;
            _downloads = jsonData.Downloads;
            _created = jsonData.Created;
            _lastUpdated = jsonData.LastUpdated;
            _files = jsonData.Files;

            // Notify views of update
            NotifyViewsOfUpdate();
        }

    }
}
