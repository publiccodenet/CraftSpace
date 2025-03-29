using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ModelLoader : MonoBehaviour
{
    private static readonly Dictionary<string, Collection> _collectionCache = new Dictionary<string, Collection>();
    private static readonly Dictionary<string, Item> _itemCache = new Dictionary<string, Item>();
    
    /// <summary>
    /// Load a collection from a JSON file
    /// </summary>
    public static Collection LoadCollectionFromFile(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            return LoadCollectionFromJson(json, Path.GetFileNameWithoutExtension(filePath));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading collection from file {filePath}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Load a collection from JSON string
    /// </summary>
    public static Collection LoadCollectionFromJson(string json, string id = null)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        
        // Check cache first if ID is provided
        if (!string.IsNullOrEmpty(id) && _collectionCache.TryGetValue(id, out Collection cachedCollection))
        {
            return cachedCollection;
        }
        
        try
        {
            Collection collection = Collection.FromJsonString(json);
            
            // Cache if ID is provided
            if (!string.IsNullOrEmpty(id) && collection != null)
            {
                _collectionCache[id] = collection;
                
                // Set name for Unity inspector
                collection.name = $"Collection_{id}";
            }
            
            return collection;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing collection from JSON: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Load an item from a JSON file
    /// </summary>
    public static Item LoadItemFromFile(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            return LoadItemFromJson(json, Path.GetFileNameWithoutExtension(filePath));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading item from file {filePath}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Load an item from JSON string
    /// </summary>
    public static Item LoadItemFromJson(string json, string id = null)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        
        // Check cache first if ID is provided
        if (!string.IsNullOrEmpty(id) && _itemCache.TryGetValue(id, out Item cachedItem))
        {
            return cachedItem;
        }
        
        try
        {
            Item item = Item.FromJsonString(json);
            
            // Cache if ID is provided
            if (!string.IsNullOrEmpty(id) && item != null)
            {
                _itemCache[id] = item;
                
                // Set name for Unity inspector
                item.name = $"Item_{id}";
            }
            
            return item;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing item from JSON: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Clear the model caches
    /// </summary>
    public static void ClearCaches()
    {
        _collectionCache.Clear();
        _itemCache.Clear();
    }
    
    /// <summary>
    /// Save a collection to a JSON file
    /// </summary>
    public static void SaveCollectionToFile(Collection collection, string filePath, bool prettyPrint = true)
    {
        if (collection == null)
            return;
            
        try
        {
            string json = collection.ToJsonString(prettyPrint);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving collection to file {filePath}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Save an item to a JSON file
    /// </summary>
    public static void SaveItemToFile(Item item, string filePath, bool prettyPrint = true)
    {
        if (item == null)
            return;
            
        try
        {
            string json = item.ToJsonString(prettyPrint);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving item to file {filePath}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Create a ScriptableObject asset for a collection
    /// </summary>
    public static void CreateCollectionAsset(Collection collection, string path)
    {
        #if UNITY_EDITOR
        if (collection == null)
            return;
            
        UnityEditor.AssetDatabase.CreateAsset(collection, path);
        UnityEditor.AssetDatabase.SaveAssets();
        #else
        Debug.LogWarning("CreateCollectionAsset is only available in the editor");
        #endif
    }
    
    /// <summary>
    /// Create a ScriptableObject asset for an item
    /// </summary>
    public static void CreateItemAsset(Item item, string path)
    {
        #if UNITY_EDITOR
        if (item == null)
            return;
            
        UnityEditor.AssetDatabase.CreateAsset(item, path);
        UnityEditor.AssetDatabase.SaveAssets();
        #else
        Debug.LogWarning("CreateItemAsset is only available in the editor");
        #endif
    }
} 