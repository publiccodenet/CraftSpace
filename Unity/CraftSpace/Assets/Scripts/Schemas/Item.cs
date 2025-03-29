using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class Item : ScriptableObject
{
    [SerializeField] public string Id { get; set; }
    [SerializeField] public string Title { get; set; }
    [SerializeField] public string Description { get; set; }
    [SerializeField] public List<string> Subjects { get; set; }
    [SerializeField] public bool IsFavorite { get; set; }
    [SerializeField] public string ThumbnailUrl { get; set; }
    [SerializeField] public string ModelUrl { get; set; }
    [SerializeField] public string CollectionId { get; set; }
    [SerializeField] public string Creator { get; set; }
    [SerializeField] public int Downloads { get; set; }
    
    // Alias for ThumbnailUrl to maintain compatibility
    public string CoverImage 
    { 
        get => ThumbnailUrl; 
        set => ThumbnailUrl = value; 
    }
    
    // Alias for lowercase collectionId (for JSON compatibility)
    public string collectionId 
    { 
        get => CollectionId; 
        set => CollectionId = value; 
    }
    
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
        return FromJsonString(json);
    }
} 