using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace CraftSpace.Models
{
    [Serializable]
    public class Item
    {
        public string id;
        public string title;
        // Other fields...
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "CraftSpace/Item", order = 2)]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string title;
        public string creator;
        public string date;
        public string description;
        public string mediatype;
        public List<string> subject = new List<string>();
        public int downloads;
        
        [Header("References")]
        public CollectionData parentCollection;
        
        [Header("Runtime Data")]
        public Texture2D cover;
        public bool isFavorite;
        
        // Runtime list of views displaying this item
        [System.NonSerialized]
        private List<ItemView> _views = new List<ItemView>();
        
        // Register a view that displays this item
        public void RegisterView(ItemView view)
        {
            if (!_views.Contains(view))
            {
                _views.Add(view);
            }
        }
        
        // Unregister a view that no longer displays this item
        public void UnregisterView(ItemView view)
        {
            _views.Remove(view);
        }
        
        // Notify all views of data changes
        public void NotifyViewsOfUpdate()
        {
            // Create a copy to avoid issues if views register/unregister during notification
            var viewsCopy = new List<ItemView>(_views);
            foreach (var view in viewsCopy)
            {
                if (view != null)
                {
                    view.OnModelUpdated();
                }
            }
        }
        
        // Editor methods for debugging
        #if UNITY_EDITOR
        public void PopulateFromJson(Item jsonData)
        {
            // Map id to item_id
            this.id = jsonData.id;
            this.title = jsonData.title;
            creator = jsonData.Creator;
            date = jsonData.Date;
            description = jsonData.Description;
            mediatype = jsonData.MediaType;
            subject = new List<string>(jsonData.Subjects);
            downloads = jsonData.downloads;
            
            // Notify views after data updates
            NotifyViewsOfUpdate();
        }
        #endif
        
        // Called when cover image is loaded
        public void OnCoverLoaded()
        {
            NotifyViewsOfUpdate();
        }
    }
} 