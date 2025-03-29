using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Collection : CollectionSchema
{
    // Runtime-only list of actual Item objects (not serialized)
    [NonSerialized] public List<Item> items = new List<Item>();
    
    // Runtime-only thumbnail texture (not serialized)
    [NonSerialized] public Texture2D thumbnail;
    
    // View management
    private List<CollectionView> _views = new List<CollectionView>();
    
    // Register a view with this model
    public void RegisterView(CollectionView view)
    {
        if (!_views.Contains(view))
        {
            _views.Add(view);
        }
    }
    
    // Unregister a view from this model
    public void UnregisterView(CollectionView view)
    {
        _views.Remove(view);
    }
    
    // Notify all registered views that the model has changed
    public void NotifyViewsOfUpdate()
    {
        foreach (var view in _views)
        {
            if (view != null)
            {
                view.HandleModelUpdated();
            }
        }
    }
    
    // Default constructor
    public Collection()
    {
        items = new List<Item>();
        _views = new List<CollectionView>();
    }
    
    // JSON methods
    public string ToJsonString(bool prettyPrint = false)
    {
        return JsonConvert.SerializeObject(this, 
            prettyPrint ? Formatting.Indented : Formatting.None);
    }
    
    public static Collection FromJsonString(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        try
        {
            var collection = CreateInstance<Collection>();
            JsonConvert.PopulateObject(json, collection);
            return collection;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing Collection from JSON: {ex.Message}");
            return null;
        }
    }

    // Alias for API compatibility
    public static Collection FromJson(string json)
    {
        Debug.Log($"[Collection] FromJson: {json}");
        var collection = FromJsonString(json);
        Debug.Log($"[Collection] FromJson: {collection}");
        return collection;
    }
} 