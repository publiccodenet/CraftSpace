using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace CraftSpace.Models
{
    [Serializable]
    public class Collection
    {
        public string id;
        public string name;
        public string description;
        public string query;
        // Other fields...
    }

    [CreateAssetMenu(fileName = "NewCollection", menuName = "CraftSpace/Collection", order = 1)]
    public class CollectionData : ScriptableObject
    {
        public string id;
        public string name;
        public string description;
        public string query;
        public string lastUpdated;
        public int totalItems;
        
        [Header("References")]
        public List<ItemData> items = new List<ItemData>();
        
        [Header("Runtime Data")]
        public Texture2D thumbnail;
        
        // Runtime list of views displaying this collection
        [System.NonSerialized]
        private List<CollectionView> _views = new List<CollectionView>();
        
        // Register a view that displays this collection
        public void RegisterView(CollectionView view)
        {
            if (!_views.Contains(view))
            {
                _views.Add(view);
            }
        }
        
        // Unregister a view that no longer displays this collection
        public void UnregisterView(CollectionView view)
        {
            _views.Remove(view);
        }
        
        // Notify all views of data changes
        public void NotifyViewsOfUpdate()
        {
            // Create a copy to avoid issues if views register/unregister during notification
            var viewsCopy = new List<CollectionView>(_views);
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
        public void PopulateFromJson(Collection jsonData)
        {
            // Map id to collection_id
            this.id = jsonData.id;
            this.name = jsonData.name;
            this.description = jsonData.description;
            this.query = jsonData.query;
            // Map other fields...
            
            // Notify views after data updates
            NotifyViewsOfUpdate();
        }
        #endif

        // Methods that interact with collections
        
        public static CollectionData FindByCollectionId(string collectionId)
        {
            return Brewster.Instance.GetCollection(collectionId);
        }
    }
} 