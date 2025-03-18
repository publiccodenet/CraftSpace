using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using CraftSpace.Models.Schema.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CraftSpace.Utils;
using Type = CraftSpace.Utils.LoggerWrapper.Type;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Brewster : MonoBehaviour
{
    // Singleton pattern
    public static Brewster Instance { get; private set; }
    
    [Header("Content")]
    public List<Collection> collections = new List<Collection>();
    public string baseResourcePath = "Content";
    
    [Header("Settings")]
    public bool loadOnStart = true;
    public bool createScriptableObjects = true;
    
    [Header("Debug")]
    public bool verbose = false;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoggerWrapper.Info("Brewster", "Awake", "Brewster content manager initialized", new Dictionary<string, object> {
                { "basePath", baseResourcePath }
            });
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (loadOnStart)
        {
            LoggerWrapper.Info("Brewster", "Awake", "Auto-loading content at startup", new Dictionary<string, object> {
                { "basePath", baseResourcePath }
            });
            LoadAllContent();
        }
    }
    
    public void LoadAllContent()
    {
        LoggerWrapper.LoadStart("Brewster", "LoadAllContent", "all collections", new Dictionary<string, object> {
            { "basePath", baseResourcePath }
        });
        
        collections.Clear();
        
        // Load collections index
        List<string> collectionIds = LoadCollectionIds();
        LoggerWrapper.Info("Brewster", "LoadAllContent", "Collections found in index", new Dictionary<string, object> {
            { "count", collectionIds.Count }
        });
        
        // Load each collection
        foreach (var collectionId in collectionIds)
        {
            LoadCollection(collectionId);
        }
        
        LoggerWrapper.LoadComplete("Brewster", "LoadAllContent", "all collections", new Dictionary<string, object> {
            { "collections", collections.Count },
            { "totalItems", GetTotalItemCount() }
        });
    }
    
    private List<string> LoadCollectionIds()
    {
        LoggerWrapper.LoadStart("Brewster", "LoadCollectionIds", "collections index");
        
        // Fixed path construction - don't include the .json extension for Resources.Load
        string indexPath = Path.Combine(baseResourcePath, "collections-index");
        
        // Debug the actual path being used
        Debug.Log($"[Brewster] Attempting to load collections index from: '{indexPath}'");
        
        TextAsset indexAsset = Resources.Load<TextAsset>(indexPath);
        
        if (indexAsset == null)
        {
            LoggerWrapper.Error("Brewster", "LoadCollectionIds", "Failed to load collections index", new Dictionary<string, object> {
                { "path", indexPath },
                { "fullResourcePath", $"Assets/Resources/{indexPath}.json" } // Show the actual file path for debugging
            }, null, gameObject);
            return new List<string>();
        }
        
        try {
            // Direct parsing with Newtonsoft.Json
            JArray collectionArray = JArray.Parse(indexAsset.text);
            List<string> collectionIds = new List<string>();
            foreach (JToken token in collectionArray)
            {
                collectionIds.Add(token.ToString());
            }
            
            if (collectionIds == null || collectionIds.Count == 0) {
                LoggerWrapper.Error("Brewster", "LoadCollectionIds", "Deserialized collection index is empty", new Dictionary<string, object> {
                    { "path", indexPath },
                    { "jsonContent", indexAsset.text }
                }, null, gameObject);
                return new List<string>();
            }
            
            return collectionIds;
        } catch (System.Exception ex) {
            // Old format fallback - try to deserialize as object with collections property using JObject instead
            LoggerWrapper.Warning("Brewster", "LoadCollectionIds", "Failed to parse as string array, attempting legacy format", new Dictionary<string, object> {
                { "path", indexPath },
                { "exception", ex.Message }
            }, gameObject);
            
            try {
                JObject oldFormat = JObject.Parse(indexAsset.text);
                if (oldFormat != null && oldFormat["collections"] != null) {
                    JArray collectionsArray = (JArray)oldFormat["collections"];
                    List<string> collectionIds = new List<string>();
                    foreach (JToken token in collectionsArray)
                    {
                        collectionIds.Add(token.ToString());
                    }
                    return collectionIds;
                }
            } catch (Exception fallbackEx) {
                LoggerWrapper.Error("Brewster", "LoadCollectionIds", "Failed to parse legacy format", new Dictionary<string, object> {
                    { "path", indexPath }
                }, fallbackEx, gameObject);
            }
            
            LoggerWrapper.Error("Brewster", "LoadCollectionIds", "Failed to parse collection index in any format", new Dictionary<string, object> {
                { "path", indexPath }
            }, ex, gameObject);
            return new List<string>();
        }
    }
    
    private void LoadCollection(string collectionId)
    {
        LoggerWrapper.LoadStart("Brewster", "LoadCollection", "collection data", new Dictionary<string, object> {
            { "collectionId", collectionId }
        });
        
        // Load collection data - FIXED: removed .json extension
        string collectionPath = Path.Combine(baseResourcePath, "collections", collectionId, "collection");
        Debug.Log($"[Brewster] Attempting to load collection from: '{collectionPath}'");
        TextAsset collectionAsset = Resources.Load<TextAsset>(collectionPath);
        
        if (collectionAsset == null)
        {
            LoggerWrapper.Error("Brewster", "LoadCollection", "Collection data not found", new Dictionary<string, object> {
                { "collectionId", collectionId },
                { "path", collectionPath },
                { "fullResourcePath", $"Assets/Resources/{collectionPath}.json" }
            }, null, gameObject);
            return;
        }
        
        // Create the ScriptableObject instance
        Collection collection = ScriptableObject.CreateInstance<Collection>();
        
        // Let the Collection parse its own JSON
        collection.ParseFromJson(collectionAsset.text);
        
        // Ensure minimum required fields
        if (string.IsNullOrEmpty(collection.Id)) {
            LoggerWrapper.Warning("Brewster", "LoadCollection", "Collection missing required 'Id' field", new Dictionary<string, object> {
                { "collectionId", collectionId },
                { "usingDirectoryName", true }
            }, gameObject);
            collection.Id = collectionId; // Use directory name as fallback
        }
        
        if (string.IsNullOrEmpty(collection.Name)) {
            LoggerWrapper.Warning("Brewster", "LoadCollection", "Collection missing required 'Name' field", new Dictionary<string, object> {
                { "collectionId", collectionId },
                { "usingDirectoryName", true }
            }, gameObject);
            collection.Name = collectionId; // Use directory name as fallback for name too
        }
        
        #if UNITY_EDITOR
        if (createScriptableObjects)
        {
            // Create asset in editor
            if (!AssetDatabase.IsValidFolder("Assets/GeneratedData"))
                AssetDatabase.CreateFolder("Assets", "GeneratedData");
            
            if (!AssetDatabase.IsValidFolder("Assets/GeneratedData/Collections"))
                AssetDatabase.CreateFolder("Assets/GeneratedData", "Collections");
                
            string assetPath = $"Assets/GeneratedData/Collections/{collectionId}.asset";
            AssetDatabase.CreateAsset(collection, assetPath);
        }
        else
        {
        #endif
            // No need to create another instance since we already created it above
            // collection = ScriptableObject.CreateInstance<Collection>();
            // collection.PopulateFromJson(jsonCollection);
        #if UNITY_EDITOR
        }
        #endif
        
        collections.Add(collection);
        
        // Try to load collection thumbnail
        collection.thumbnail = Resources.Load<Texture2D>($"{baseResourcePath}/collections/{collectionId}/thumbnail");
        if (collection.thumbnail == null)
        {
            // Load placeholder
            collection.thumbnail = Resources.Load<Texture2D>($"{baseResourcePath}/placeholders/collection-thumbnail");
        }
        
        // Load items index
        LoadItemsForCollection(collection, collectionId);
        
        LoggerWrapper.CollectionLoaded("Brewster", "LoadCollection", collectionId, new Dictionary<string, object> {
            { "name", collection.Name },
            { "itemCount", collection.items.Count }
        });
        
        if (verbose)
        {
            Debug.Log($"Loaded collection: {collection.Name} with {collection.items.Count} items");
        }
    }
    
    private void LoadItemsForCollection(Collection collection, string collectionId)
    {
        LoggerWrapper.LoadStart("Brewster", "LoadItemsForCollection", "items index", new Dictionary<string, object> {
            { "collectionId", collectionId }
        });
        
        // Load items index - FIXED: removed .json extension
        string indexPath = Path.Combine(baseResourcePath, "collections", collectionId, "items-index");
        Debug.Log($"[Brewster] Attempting to load items index from: '{indexPath}'");
        TextAsset indexAsset = Resources.Load<TextAsset>(indexPath);
        
        if (indexAsset == null)
        {
            LoggerWrapper.Warning("Brewster", "LoadItemsForCollection", "Items index not found", new Dictionary<string, object> {
                { "collectionId", collectionId },
                { "path", indexPath }
            }, gameObject);
            return;
        }
        
        // Parse the simple string array
        List<string> itemIds = JsonConvert.DeserializeObject<List<string>>(indexAsset.text);
        
        LoggerWrapper.Info("Brewster", "LoadItemsForCollection", "Parsed items index", new Dictionary<string, object> {
            { "collectionId", collectionId },
            { "itemCount", itemIds.Count }
        });
        
        // Create folder for items if needed
        #if UNITY_EDITOR
        if (createScriptableObjects)
        {
            string itemsFolder = $"Assets/GeneratedData/Collections/{collectionId}_Items";
            if (!AssetDatabase.IsValidFolder(itemsFolder))
                AssetDatabase.CreateFolder("Assets/GeneratedData/Collections", $"{collectionId}_Items");
        }
        #endif
        
        // Load each item
        foreach (var itemId in itemIds)
        {
            LoadItem(itemId, collection, collectionId);
        }
        
        LoggerWrapper.Info("Brewster", "LoadItemsForCollection", "items", new Dictionary<string, object> {
            { "collectionId", collectionId },
            { "count", itemIds.Count }
        });
    }
    
    private void LoadItem(string itemId, Collection collection, string collectionId)
    {
        // Always log item loading during initial development, commented out later
        LoggerWrapper.Info("Brewster", "LoadItem", "item data", new Dictionary<string, object> { 
            { "collectionId", collectionId }, 
            { "itemId", itemId }, 
            { "path", Path.Combine(baseResourcePath, "collections", collectionId, "items", itemId, "item") } 
        }, gameObject);
        
        // Load item data - FIXED: removed .json extension
        string itemPath = Path.Combine(baseResourcePath, "collections", collectionId, "items", itemId, "item");
        Debug.Log($"[Brewster] Attempting to load item from: '{itemPath}'");
        TextAsset itemAsset = Resources.Load<TextAsset>(itemPath);
        
        if (itemAsset == null)
        {
            LoggerWrapper.Error("Brewster", "LoadItem", $"{Type.ITEM}{Type.ERROR} Failed to load item data", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "path", itemPath }, { "resourcesRootPath", baseResourcePath } }, null, gameObject);
            return;
        }
        
        // Log raw JSON for debugging
        LoggerWrapper.Info("Brewster", "LoadItem", "Item JSON loaded", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "jsonLength", itemAsset.text.Length }, { "jsonPreview", itemAsset.text.Length > 100 ? itemAsset.text.Substring(0, 100) + "..." : itemAsset.text } }, gameObject);
        
        // Create a proper ScriptableObject instance
        Item item = ScriptableObject.CreateInstance<Item>();
        
        // Let the Item parse its own JSON
        item.ParseFromJson(itemAsset.text);
        
        // Ensure minimum required fields
        if (string.IsNullOrEmpty(item.Id)) {
            LoggerWrapper.Warning("Brewster", "LoadItem", "Item missing required 'Id' field", new Dictionary<string, object> {
                { "collectionId", collectionId },
                { "itemId", itemId },
                { "usingDirectoryName", true }
            }, gameObject);
            item.Id = itemId; // Use directory name as fallback
        }
        
        if (string.IsNullOrEmpty(item.Title)) {
            LoggerWrapper.Warning("Brewster", "LoadItem", "Item missing required 'Title' field", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "id", item.Id }, { "sourcePath", itemPath }, { "usingIdAsTitle", true } }, gameObject);
            item.Title = itemId; // Use directory name as fallback for title too
        }
        
        // Create ScriptableObject with detailed logging
        
        #if UNITY_EDITOR
        if (createScriptableObjects)
        {
            LoggerWrapper.Info("Brewster", "LoadItem", "Creating ScriptableObject in Editor", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "assetPath", $"Assets/GeneratedData/Collections/{collectionId}_Items/{itemId}.asset" } }, gameObject);
            
            try {
                item.parentCollection = collection;
                item.collectionId = collectionId;
                
                // Create asset in editor
                string assetPath = $"Assets/GeneratedData/Collections/{collectionId}_Items/{itemId}.asset";
                AssetDatabase.CreateAsset(item, assetPath);
                LoggerWrapper.Success("Brewster", "LoadItem", "Created ScriptableObject asset", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "assetPath", assetPath } }, gameObject);
            }
            catch (Exception ex) {
                LoggerWrapper.Error("Brewster", "LoadItem", "Failed to create ScriptableObject", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "exception", ex.Message } }, ex, gameObject);
                return;
            }
        }
        else
        {
        #endif
            // Runtime-only path
            LoggerWrapper.Info("Brewster", "LoadItem", "Creating runtime CraftSpace.Models.Item", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId } }, gameObject);
            
            try {
                item.parentCollection = collection;
                item.collectionId = collectionId;
                LoggerWrapper.Success("Brewster", "LoadItem", "Created runtime CraftSpace.Models.Item", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "dataTitle", item.Title } }, gameObject);
            }
            catch (Exception ex) {
                LoggerWrapper.Error("Brewster", "LoadItem", "Failed to create runtime CraftSpace.Models.Item", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "exception", ex.Message } }, ex, gameObject);
                return;
            }
        #if UNITY_EDITOR
        }
        #endif
        
        // Add to collection with logging
        collection.items.Add(item);
        LoggerWrapper.Info("Brewster", "LoadItem", "Added item to collection", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "collectionItemCount", collection.items.Count } }, gameObject);
        
        // Try to load item cover
        string coverPath = itemPath.Replace("/item", "/cover");
        LoggerWrapper.Info("Brewster", "LoadItem", "Attempting to load cover", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "coverPath", coverPath } }, gameObject);
        
        item.cover = Resources.Load<Texture2D>(coverPath);
        
        if (item.cover == null)
        {
            // Load placeholder
            LoggerWrapper.Info("Brewster", "LoadItem", "Cover not found, using placeholder", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "placeholderPath", $"{baseResourcePath}/placeholders/cover" } }, gameObject);
            item.cover = Resources.Load<Texture2D>($"{baseResourcePath}/placeholders/cover");
            
            if (item.cover == null) {
                LoggerWrapper.Warning("Brewster", "LoadItem", "Placeholder cover not found", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "placeholderPath", $"{baseResourcePath}/placeholders/cover" } }, gameObject);
            }
        }
        else
        {
            // Notify the model that its cover was loaded
            LoggerWrapper.Success("Brewster", "LoadItem", "Loaded cover texture successfully", new Dictionary<string, object> { { "collectionId", collectionId }, { "itemId", itemId }, { "coverDimensions", $"{item.cover.width}x{item.cover.height}" } }, gameObject);
            item.NotifyViewsOfUpdate();
        }
        
        // Log complete item data
        LoggerWrapper.ItemLoaded("Brewster", "LoadItem", itemId, new Dictionary<string, object> { { "title", item.Title }, { "collectionId", collectionId }, { "creator", item.Creator }, { "hasCover", item.cover != null }, { "description", item.Description?.Length > 30 ? item.Description.Substring(0, 30) + "..." : item.Description } }, gameObject);
        
        // When loading an item, set the collectionId
        item.collectionId = collectionId;
    }
    
    private int GetTotalItemCount()
    {
        int count = 0;
        foreach (var collection in collections)
        {
            count += collection.items.Count;
        }
        return count;
    }
    
    // Utility methods to find items
    public Collection GetCollection(string collectionId)
    {
        return collections.Find(c => c.Id == collectionId);
    }
    
    public Item GetItem(string collectionId, string itemId)
    {
        Collection collection = GetCollection(collectionId);
        if (collection != null)
        {
            return collection.items.Find(i => i.Id == itemId);
        }
        return null;
    }
    
    // Clear all loaded data (useful for testing/reloading)
    public void ClearAllData()
    {
        foreach (var collection in collections)
        {
            foreach (var item in collection.items)
            {
                #if UNITY_EDITOR
                if (createScriptableObjects)
                {
                    string assetPath = AssetDatabase.GetAssetPath(item);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                }
                #endif
                
                if (Application.isPlaying)
                {
                    Destroy(item);
                }
            }
            
            #if UNITY_EDITOR
            if (createScriptableObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(collection);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
            #endif
            
            if (Application.isPlaying)
            {
                Destroy(collection);
            }
        }
        
        collections.Clear();
    }
    
    #if UNITY_EDITOR
    // Editor-only reload function
    [ContextMenu("Reload All Content")]
    private void EditorReloadContent()
    {
        ClearAllData();
        LoadAllContent();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
    #endif
} 