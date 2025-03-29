using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

public class SchemaLoader : MonoBehaviour
{
    // Cache for loaded objects
    protected static readonly Dictionary<string, Collection> CollectionCache = new Dictionary<string, Collection>();
    protected static readonly Dictionary<string, Item> ItemCache = new Dictionary<string, Item>();
    
    // Common properties
    protected string ObjectId { get; set; }
    protected string Title { get; set; }
    
    // Common loading methods
    protected static T LoadFromJson<T>(string json, string id = null) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        try
        {
            // Use the appropriate cache and FromJsonString method based on type
            if (typeof(T) == typeof(Collection))
            {
                if (!string.IsNullOrEmpty(id) && CollectionCache.TryGetValue(id, out Collection cached))
                    return cached as T;
                    
                Collection collection = Collection.FromJsonString(json);
                if (!string.IsNullOrEmpty(id) && collection != null)
                {
                    CollectionCache[id] = collection;
                    collection.name = $"Collection_{id}";
                }
                return collection as T;
            }
            else if (typeof(T) == typeof(Item))
            {
                if (!string.IsNullOrEmpty(id) && ItemCache.TryGetValue(id, out Item cached))
                    return cached as T;
                    
                Item item = Item.FromJsonString(json);
                if (!string.IsNullOrEmpty(id) && item != null)
                {
                    ItemCache[id] = item;
                    item.name = $"Item_{id}";
                }
                return item as T;
            }
            
            throw new ArgumentException($"Unsupported type: {typeof(T)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deserializing {typeof(T)} from JSON: {ex.Message}");
            return null;
        }
    }
    
    protected static T LoadFromFile<T>(string filePath) where T : ScriptableObject
    {
        try
        {
            string json = File.ReadAllText(filePath);
            return LoadFromJson<T>(json, Path.GetFileNameWithoutExtension(filePath));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading {typeof(T)} from file {filePath}: {ex.Message}");
            return null;
        }
    }
    
    protected static void SaveToFile<T>(T obj, string filePath, bool prettyPrint = true) where T : ScriptableObject
    {
        if (obj == null)
            return;
            
        try
        {
            string json = string.Empty;
            
            if (obj is Collection collection)
                json = collection.ToJsonString(prettyPrint);
            else if (obj is Item item)
                json = item.ToJsonString(prettyPrint);
            else
                throw new ArgumentException($"Unsupported type: {typeof(T)}");
                
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving {typeof(T)} to file {filePath}: {ex.Message}");
        }
    }
    
    public static void ClearCaches()
    {
        CollectionCache.Clear();
        ItemCache.Clear();
    }
    
    #if UNITY_EDITOR
    protected static void CreateAsset<T>(T obj, string path) where T : ScriptableObject
    {
        if (obj == null)
            return;
            
        UnityEditor.AssetDatabase.CreateAsset(obj, path);
        UnityEditor.AssetDatabase.SaveAssets();
    }
    #endif
} 