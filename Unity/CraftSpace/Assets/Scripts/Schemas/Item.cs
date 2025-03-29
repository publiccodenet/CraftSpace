using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Item : ItemSchema
{
    // Unity-specific fields
    [NonSerialized] public Texture2D cover;
    [NonSerialized] public Collection parentCollection;
    
    // Runtime view/observer support
    private List<ItemView> _views = new List<ItemView>();
    
    public void RegisterView(ItemView view) 
    { 
        if (!_views.Contains(view))
            _views.Add(view);
    }
    
    public void UnregisterView(ItemView view)
    {
        _views.Remove(view);
    }
    
    public void NotifyViewsOfUpdate()
    {
        foreach (var view in _views)
        {
            if (view != null)
                view.HandleModelUpdated();
        }
    }
    
    // JSON methods
    public string ToJsonString(bool prettyPrint = false)
    {
        return JsonConvert.SerializeObject(this, 
            prettyPrint ? Formatting.Indented : Formatting.None);
    }
    
    public static Item FromJsonString(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        try
        {
            var item = CreateInstance<Item>();
            JsonConvert.PopulateObject(json, item);
            return item;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing Item from JSON: {ex.Message}");
            return null;
        }
    }

    // Alias for API compatibility
    public static Item FromJson(string json)
    {
        Debug.Log($"[Item] FromJson: {json}");
        var item = FromJsonString(json);
        Debug.Log($"[Item] FromJson: {item}");
        return item;
    }
} 