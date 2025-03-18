using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;

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
        /// Collection identifier
        /// </summary>
        [JsonProperty("collection_id")]
        [SerializeField] private string? _collectionId;
        public string? CollectionId { get => _collectionId; set => _collectionId = value; }

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
            _collectionId = jsonData.CollectionId;

            // Notify views of update
            NotifyViewsOfUpdate();
        }

        public void ParseFromJson(string json)
        {
            try
            {
                JObject jsonObj = JObject.Parse(json);
                ParseFromJObject(jsonObj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing Item JSON: {ex.Message}\n{json.Substring(0, Math.Min(json.Length, 100))}...");
            }
        }

        public void ParseFromJObject(JObject jsonObj)
        {
            try
            {
                // Set properties based on JSON - with null checks
                this.Id = jsonObj["id"]?.ToString();
                this.Title = jsonObj["title"]?.ToString();
                this.Description = jsonObj["description"]?.ToString();
                this.Creator = jsonObj["creator"]?.ToString();
                
                // Change CollectionIdentifier to a property that exists in the class
                if (jsonObj["collection_id"] != null)
                    this.CollectionId = jsonObj["collection_id"].ToString();
                
                // Handle arrays
                if (jsonObj["subject"] != null && jsonObj["subject"].Type == JTokenType.Array)
                {
                    this.Subjects = new List<string>();
                    foreach (var token in jsonObj["subject"])
                    {
                        this.Subjects.Add(token.ToString());
                    }
                }
                
                // Special handling for media type
                this.MediaType = jsonObj["mediatype"]?.ToString();
                
                // Add null check before string interpolation
                if (this.Id != null && this.Title != null)
                {
                    Debug.Log($"Successfully parsed Item JSON: {this.Id} - {this.Title}");
                }
                else
                {
                    Debug.Log("Successfully parsed Item JSON (ID or Title missing)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error mapping Item properties from JObject: {ex.Message}");
            }
        }

    }
}
