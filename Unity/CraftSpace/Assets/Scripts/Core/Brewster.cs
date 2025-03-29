using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Run this script after ApplicationManager but before other components
[DefaultExecutionOrder(-100)]
public class Brewster : MonoBehaviour
{
    
    // Public property to check if content is loaded
    public bool IsInitialized { get; private set; } = false;
    
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
        if (loadOnStart)
        {
            Debug.Log("[Brewster] Auto-loading content at startup. Base path: " + baseResourcePath);
            LoadAllContent();
        }
    }
    
    void OnDestroy()
    {
        IsInitialized = false;
    }
    
    public void LoadAllContent()
    {
        Debug.Log("[Brewster] Starting to load all collections. Base path: " + baseResourcePath);
        
        collections.Clear();
        IsInitialized = false;
        
        try
        {
            // Load collections index
            List<string> collectionIds = LoadCollectionIds();
            Debug.Log($"[Brewster] Collections found in index: {collectionIds.Count}");
            
            // Load each collection
            foreach (var collectionId in collectionIds)
            {
                Debug.Log($"[Brewster] Loading collection: {collectionId}");
                LoadCollection(collectionId);
            }
            
            // Mark initialization as complete if we loaded at least some content
            IsInitialized = true;
            
            Debug.Log($"[Brewster] Completed loading all collections. Total collections: {collections.Count}, Total items: {GetTotalItemCount()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brewster] Failed to load content: {ex.Message}\n{ex.StackTrace}");
            IsInitialized = false;
        }
    }

    private List<string> LoadCollectionIds()
    {
        Debug.Log("[Brewster] Starting to load collections index");
        
        // Use StreamingAssets path instead of Resources
        string indexPath = Path.Combine(Application.streamingAssetsPath, "Content", "collections-index.json");
        
        // Debug the actual path being used
        Debug.Log($"[Brewster] Attempting to load collections index from: '{indexPath}'");
        
        // Check if file exists
        if (!File.Exists(indexPath))
        {
            Debug.LogError($"[Brewster] Collections index file not found at path: {indexPath}");
            return new List<string>();
        }
        
        // Read the file directly instead of using Resources.Load
        string json;
        try
        {
            json = File.ReadAllText(indexPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brewster] Failed to read collections index file: {ex.Message}");
            return new List<string>();
        }
        
        try 
        {
            // Direct parsing with Newtonsoft.Json
            JArray collectionArray = JArray.Parse(json);
            List<string> collectionIds = new List<string>();
            foreach (JToken token in collectionArray)
            {
                collectionIds.Add(token.ToString());
            }
            
            Debug.Log($"[Brewster] Successfully loaded collections from index: {collectionIds.Count} collections found");
            
            if (collectionIds == null || collectionIds.Count == 0) 
            {
                Debug.LogWarning($"[Brewster] Parsed collection index is empty or null. JSON content: {json}");
                return new List<string>();
            }
            
            return collectionIds;
        } 
        catch (System.Exception ex) 
        {
            // Old format fallback - try to deserialize as object with collections property using JObject instead
            Debug.LogWarning($"[Brewster] Failed to parse as string array, attempting legacy format. Exception: {ex.Message}");
            
            try 
            {
                JObject oldFormat = JObject.Parse(json);
                if (oldFormat != null && oldFormat["collections"] != null) 
                {
                    JArray collectionsArray = (JArray)oldFormat["collections"];
                    List<string> collectionIds = new List<string>();
                    foreach (JToken token in collectionsArray)
                    {
                        collectionIds.Add(token.ToString());
                    }
                    return collectionIds;
                }
            } 
            catch (Exception fallbackEx) 
            {
                Debug.LogError($"[Brewster] Failed to parse legacy format. Exception: {fallbackEx.Message}");
            }
            
            Debug.LogError($"[Brewster] Failed to parse collection index in any format. Exception: {ex.Message}");
            return new List<string>();
        }
    }
    
    private void LoadCollection(string collectionId)
    {
        Debug.Log($"[Brewster] Starting to load collection data. Collection ID: {collectionId}");
        
        // Use StreamingAssets path
        string collectionPath = Path.Combine(Application.streamingAssetsPath, "Content", "collections", collectionId, "collection.json");
        Debug.Log($"[Brewster] Attempting to load collection from: '{collectionPath}'");
        
        if (!File.Exists(collectionPath))
        {
            Debug.LogError($"[Brewster] Collection data not found. Collection ID: {collectionId}, Path: {collectionPath}");
            return;
        }
        
        // Read the file directly
        string json;
        try
        {
            json = File.ReadAllText(collectionPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brewster] Failed to read collection file: {ex.Message}");
            return;
        }

        Debug.Log($"[Brewster] Collection JSON loaded. Collection ID: {collectionId}, JSON length: {json.Length}, JSON preview: {(json.Length > 100 ? json.Substring(0, 100) + "..." : json)}");

        // Let the Collection parse its own JSON - create directly from JSON
        Collection collection = Collection.FromJson(json);

        Debug.Log($"[Brewster] Collection parsed. Collection ID: {collectionId}, Name: {collection.Name}, Items count: {collection.items.Count}");

#if false
        // Ensure minimum required fields
        if (string.IsNullOrEmpty(collection.Id)) {
            Debug.LogWarning($"[Brewster] Collection missing required 'Id' field. Collection ID: {collectionId}, Using directory name as fallback.");
            collection.Id = collectionId; // Use directory name as fallback
        }
        
        if (string.IsNullOrEmpty(collection.Name)) {
            Debug.LogWarning($"[Brewster] Collection missing required 'Name' field. Collection ID: {collectionId}, Using directory name as fallback.");
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
        #if UNITY_EDITOR
        }
        #endif
        
        collections.Add(collection);
        
        // Try to load collection thumbnail - still use Resources for thumbnails as they're textures
        string thumbnailPath = Path.Combine(Application.streamingAssetsPath, "Content", "collections", collectionId, "thumbnail.jpg");
        Texture2D thumbnail = null;
        
        if (File.Exists(thumbnailPath))
        {
            thumbnail = new Texture2D(2, 2);
            byte[] imageData = File.ReadAllBytes(thumbnailPath);
            thumbnail.LoadImage(imageData);
            collection.thumbnail = thumbnail;
        }
        
        if (collection.thumbnail == null)
        {
            // Load placeholder - keep using Resources for placeholders
            collection.thumbnail = Resources.Load<Texture2D>($"{baseResourcePath}/placeholders/collection-thumbnail");
            Debug.Log($"[Brewster] Using placeholder thumbnail for collection {collectionId}");
        }
        
        // Load items index
        Debug.Log($"[Brewster] Loading items for collection: {collectionId}");
        //LoadItemsForCollection(collection, collectionId);
        
        Debug.Log($"[Brewster] Loaded collection: {collection.Name} with {collection.items.Count} items");
        
        if (verbose)
        {
            Debug.Log($"Loaded collection: {collection.Name} with {collection.items.Count} items");
        }
#endif
    }
    
#if false    
    private void LoadItemsForCollection(Collection collection, string collectionId)
    {
        Debug.Log($"[Brewster] Starting to load items index. Collection ID: {collectionId}");
        
        // Use StreamingAssets path
        string indexPath = Path.Combine(Application.streamingAssetsPath, "Content", "collections", collectionId, "items-index.json");
        Debug.Log($"[Brewster] Attempting to load items index from: '{indexPath}'");
        
        if (!File.Exists(indexPath))
        {
            Debug.LogWarning($"[Brewster] Items index not found. Collection ID: {collectionId}, Path: {indexPath}");
            return;
        }
        
        // Read the file directly
        string json;
        try
        {
            json = File.ReadAllText(indexPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brewster] Failed to read items index file: {ex.Message}");
            return;
        }
        
        // Parse the simple string array
        List<string> itemIds = JsonConvert.DeserializeObject<List<string>>(json);
        
        Debug.Log($"[Brewster] Parsed items index. Collection ID: {collectionId}, Item count: {itemIds.Count}");
        
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
        
        Debug.Log($"[Brewster] Loaded items. Collection ID: {collectionId}, Count: {itemIds.Count}");
    }
    
    private void LoadItem(string itemId, Collection collection, string collectionId)
    {
        // Always log item loading during initial development, commented out later
        Debug.Log($"[Brewster] Loading item data. Collection ID: {collectionId}, Item ID: {itemId}");
        
        // Use StreamingAssets path
        string itemPath = Path.Combine(Application.streamingAssetsPath, "Content", "collections", collectionId, "items", itemId, "item.json");
        Debug.Log($"[Brewster] Attempting to load item from: '{itemPath}'");
        
        if (!File.Exists(itemPath))
        {
            Debug.LogError($"[Brewster] Failed to load item data. Collection ID: {collectionId}, Item ID: {itemId}, Path: {itemPath}");
            return;
        }
        
        // Read the file directly
        string json;
        try
        {
            json = File.ReadAllText(itemPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brewster] Failed to read item file: {ex.Message}");
            return;
        }
        
        // Log raw JSON for debugging
        Debug.Log($"[Brewster] Item JSON loaded. Collection ID: {collectionId}, Item ID: {itemId}, JSON length: {json.Length}, JSON preview: {(json.Length > 100 ? json.Substring(0, 100) + "..." : json)}");
        
        // Create directly from JSON
        Item item = Item.FromJson(json);
        
        // Ensure minimum required fields
        if (string.IsNullOrEmpty(item.Id)) {
            Debug.LogWarning($"[Brewster] Item missing required 'Id' field. Collection ID: {collectionId}, Item ID: {itemId}, Using directory name as fallback.");
            item.Id = itemId; // Use directory name as fallback
        }
        
        if (string.IsNullOrEmpty(item.Title)) {
            Debug.LogWarning($"[Brewster] Item missing required 'Title' field. Collection ID: {collectionId}, Item ID: {itemId}, ID: {item.Id}, Source path: {itemPath}, Using ID as title.");
            item.Title = itemId; // Use directory name as fallback for title too
        }
        
        // Create ScriptableObject with detailed logging
        
        #if UNITY_EDITOR
        if (createScriptableObjects)
        {
            Debug.Log($"[Brewster] Creating ScriptableObject in Editor. Collection ID: {collectionId}, Item ID: {itemId}, Asset path: Assets/GeneratedData/Collections/{collectionId}_Items/{itemId}.asset");
            
            try {
                item.parentCollection = collection;
                item.CollectionId = collectionId;
                
                // Create asset in editor
                string assetPath = $"Assets/GeneratedData/Collections/{collectionId}_Items/{itemId}.asset";
                AssetDatabase.CreateAsset(item, assetPath);
                Debug.Log($"[Brewster] Created ScriptableObject asset successfully. Collection ID: {collectionId}, Item ID: {itemId}, Asset path: {assetPath}");
            }
            catch (Exception ex) {
                Debug.LogError($"[Brewster] Failed to create ScriptableObject. Collection ID: {collectionId}, Item ID: {itemId}, Exception: {ex.Message}");
                return;
            }
        }
        else
        {
        #endif
            // Runtime-only path
            Debug.Log($"[Brewster] Creating runtime Item. Collection ID: {collectionId}, Item ID: {itemId}");
            
            try {
                item.parentCollection = collection;
                item.CollectionId = collectionId;
                Debug.Log($"[Brewster] Created runtime Item successfully. Collection ID: {collectionId}, Item ID: {itemId}, Title: {item.Title}");
            }
            catch (Exception ex) {
                Debug.LogError($"[Brewster] Failed to create runtime Item. Collection ID: {collectionId}, Item ID: {itemId}, Exception: {ex.Message}");
                return;
            }
        #if UNITY_EDITOR
        }
        #endif
        
        // Add to collection with logging
        collection.items.Add(item);
        Debug.Log($"[Brewster] Added item to collection. Collection ID: {collectionId}, Item ID: {itemId}, Collection item count: {collection.items.Count}");
        
        // Try to load item cover using StreamingAssets
        string coverPath = itemPath.Replace("item.json", "cover.jpg");
        Debug.Log($"[Brewster] Attempting to load cover. Collection ID: {collectionId}, Item ID: {itemId}, Cover path: {coverPath}");
        
        if (File.Exists(coverPath))
        {
            Texture2D coverTexture = new Texture2D(2, 2);
            byte[] imageData = File.ReadAllBytes(coverPath);
            if (coverTexture.LoadImage(imageData))
            {
                item.cover = coverTexture;
                Debug.Log($"[Brewster] Loaded cover texture successfully. Collection ID: {collectionId}, Item ID: {itemId}, Cover dimensions: {item.cover.width}x{item.cover.height}");
            }
            else
            {
                Debug.LogWarning($"[Brewster] Failed to load cover image data. Collection ID: {collectionId}, Item ID: {itemId}");
            }
        }
        
        if (item.cover == null)
        {
            // Load placeholder from Resources
            Debug.Log($"[Brewster] Cover not found, using placeholder. Collection ID: {collectionId}, Item ID: {itemId}, Placeholder path: {baseResourcePath}/placeholders/cover");
            item.cover = Resources.Load<Texture2D>($"{baseResourcePath}/placeholders/cover");
            
            if (item.cover == null) {
                Debug.LogWarning($"[Brewster] Placeholder cover not found. Collection ID: {collectionId}, Item ID: {itemId}, Placeholder path: {baseResourcePath}/placeholders/cover");
            }
        }
        
        // Notify the model that its cover was loaded
        item.NotifyViewsOfUpdate();
        
        // Log complete item data
        Debug.Log($"[Brewster] Item loaded. Item ID: {itemId}, Title: {item.Title}, Collection ID: {collectionId}, Creator: {item.Creator}, Has cover: {item.cover != null}, Description: {(item.Description?.Length > 30 ? item.Description.Substring(0, 30) + "..." : item.Description)}");
        
        // When loading an item, set the collectionId
        item.CollectionId = collectionId;
    }
    #endif
    
    public int GetTotalItemCount()
    {
        int count = 0;
        foreach (var collection in collections)
        {
            count += collection.items.Count;
        }
        return count;
    }

#if false    
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

#endif

}
