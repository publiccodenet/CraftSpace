using UnityEngine;
using System;
using System.Collections.Generic;

namespace CraftSpace.Models.Schema.Generated
{
    [System.Serializable]
    public partial class Collection : ScriptableObject
    {
        // Unity-specific fields
        [NonSerialized] public Texture2D thumbnail;
        
        // References to items in this collection
        [Header("References")]
        public List<Item> items = new List<Item>();
        
        // Runtime list of views displaying this collection
        [System.NonSerialized]
        private List<CollectionView> _views = new List<CollectionView>();
        
        // View registration methods
        public void RegisterView(CollectionView view)
        {
            if (!_views.Contains(view))
            {
                _views.Add(view);
            }
        }
        
        public void UnregisterView(CollectionView view)
        {
            _views.Remove(view);
        }
        
        // Notification methods
        public void NotifyViewsOfUpdate()
        {
            // Create a copy to avoid issues if views register/unregister during notification
            var viewsCopy = new List<CollectionView>(_views);
            foreach (var view in viewsCopy)
            {
                if (view != null)
                {
                    // Call view's HandleModelUpdated method
                    view.HandleModelUpdated();
                }
            }
        }
        
        // Utility methods
        public static Collection FindByCollectionId(string collectionId)
        {
            return Brewster.Instance.GetCollection(collectionId);
        }
        
        // Add any Unity-specific initialization or event handling
        void OnEnable()
        {
            // Initialize default values if needed
            if (items == null)
                items = new List<Item>();
            
            if (string.IsNullOrEmpty(_id))
                _id = System.Guid.NewGuid().ToString();
            
            if (string.IsNullOrEmpty(_name))
                _name = "New Collection";
        }
    }
}