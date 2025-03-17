using UnityEngine;
using System;
using System.Collections.Generic;

namespace CraftSpace.Models.Schema.Generated
{
    [System.Serializable]
    public partial class Item : ScriptableObject
    {
        // Unity-specific fields
        [NonSerialized] public Texture2D cover;
        [NonSerialized] public string collectionId;
        public Collection parentCollection;
        
        // Runtime list of views
        private List<ItemView> _views = new List<ItemView>();
        
        // View registration methods
        public void RegisterView(ItemView view) 
        { 
            if (!_views.Contains(view))
                _views.Add(view);
        }
        
        public void UnregisterView(ItemView view)
        {
            _views.Remove(view);
        }
        
        // Notification methods
        public void NotifyViewsOfUpdate()
        {
            var viewsCopy = new List<ItemView>(_views);
            foreach (var view in viewsCopy)
            {
                if (view != null)
                    view.HandleModelUpdated();
            }
            InvokeModelUpdated();
        }
        
        // Event handling
        public event System.Action ModelUpdated;
        
        protected void InvokeModelUpdated()
        {
            ModelUpdated?.Invoke();
        }

    }
} 