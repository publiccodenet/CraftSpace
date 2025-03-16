using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using CraftSpace.Models;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Brewster : MonoBehaviour
{
    // Singleton pattern
    public static Brewster Instance { get; private set; }
    
    [Header("Content")]
    public List<CollectionData> collections = new List<CollectionData>();
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
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (loadOnStart)
        {
            LoadAllContent();
        }
    }
    
    public void LoadAllContent()
    {
        collections.Clear();
        
        // Load collections index
        List<string> collectionIds = LoadCollectionIds();
        Debug.Log($"Found {collectionIds.Count} collections in index");
        
        // Load each collection
        foreach (var collectionId in collectionIds)
        {
            LoadCollection(collectionId);
        }
        
        Debug.Log($"Loaded {collections.Count} collections with a total of {GetTotalItemCount()} items");
    }
    
    private List<string> LoadCollectionIds()
    {
        string indexPath = Path.Combine(baseResourcePath, "collections-index.json");
        TextAsset indexAsset = Resources.Load<TextAsset>(indexPath);
        
        if (indexAsset == null)
        {
            Debug.LogError($"Failed to load collections index at {indexPath}");
            return new List<string>();
        }
        
        // Parse the simple string array
        return JsonConvert.DeserializeObject<List<string>>(indexAsset.text);
    }
    
    private void LoadCollection(string collectionId)
    {
        // Load collection data
        string collectionPath = Path.Combine(baseResourcePath, "collections", collectionId, "collection.json");
        TextAsset collectionAsset = Resources.Load<TextAsset>(collectionPath);
        
        if (collectionAsset == null)
        {
            Debug.LogError($"Collection data not found for {collectionId}!");
            return;
        }
        
        // Parse collection JSON using Newtonsoft.Json
        Collection jsonCollectionData = JsonConvert.DeserializeObject<Collection>(collectionAsset.text);
        
        // Create ScriptableObject
        CollectionData collectionData = null;
        
        #if UNITY_EDITOR
        if (createScriptableObjects)
        {
            collectionData = ScriptableObject.CreateInstance<CollectionData>();
            collectionData.PopulateFromJson(jsonCollectionData);
            
            // Create asset in editor
            if (!AssetDatabase.IsValidFolder("Assets/GeneratedData"))
                AssetDatabase.CreateFolder("Assets", "GeneratedData");
            
            if (!AssetDatabase.IsValidFolder("Assets/GeneratedData/Collections"))
                AssetDatabase.CreateFolder("Assets/GeneratedData", "Collections");
                
            string assetPath = $"Assets/GeneratedData/Collections/{collectionId}.asset";
            AssetDatabase.CreateAsset(collectionData, assetPath);
        }
        else
        {
        #endif
            // Runtime-only path (no persistence)
            collectionData = ScriptableObject.CreateInstance<CollectionData>();
            collectionData.PopulateFromJson(jsonCollectionData);
        #if UNITY_EDITOR
        }
        #endif
        
        collections.Add(collectionData);
        
        // Try to load collection thumbnail
        collectionData.thumbnail = Resources.Load<Texture2D>($"{baseResourcePath}/collections/{collectionId}/thumbnail");
        if (collectionData.thumbnail == null)
        {
            // Load placeholder
            collectionData.thumbnail = Resources.Load<Texture2D>($"{baseResourcePath}/placeholders/collection-thumbnail");
        }
        
        // Load items index
        LoadItemsForCollection(collectionData, collectionId);
        
        if (verbose)
        {
            Debug.Log($"Loaded collection: {collectionData.name} with {collectionData.items.Count} items");
        }
    }
    
    private void LoadItemsForCollection(CollectionData collectionData, string collectionId)
    {
        // Load items index
        string indexPath = Path.Combine(baseResourcePath, "collections", collectionId, "items-index.json");
        TextAsset indexAsset = Resources.Load<TextAsset>(indexPath);
        
        if (indexAsset == null)
        {
            Debug.LogWarning($"Items index not found for collection {collectionId}!");
            return;
        }
        
        // Parse the simple string array
        List<string> itemIds = JsonConvert.DeserializeObject<List<string>>(indexAsset.text);
        
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
            LoadItem(itemId, collectionData, collectionId);
        }
    }
    
    private void LoadItem(string itemId, CollectionData collectionData, string collectionId)
    {
        // Load item data
        string itemPath = Path.Combine(baseResourcePath, "collections", collectionId, "items", itemId, "item.json");
        TextAsset itemAsset = Resources.Load<TextAsset>(itemPath);
        
        if (itemAsset == null)
        {
            Debug.LogError($"Item data not found for {itemId}!");
            return;
        }
        
        // Parse item JSON using Newtonsoft.Json
        Item jsonItemData = JsonConvert.DeserializeObject<Item>(itemAsset.text);
        
        // Create ScriptableObject
        ItemData itemData = null;
        
        #if UNITY_EDITOR
        if (createScriptableObjects)
        {
            itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.PopulateFromJson(jsonItemData);
            itemData.parentCollection = collectionData;
            
            // Create asset in editor
            string assetPath = $"Assets/GeneratedData/Collections/{collectionId}_Items/{itemId}.asset";
            AssetDatabase.CreateAsset(itemData, assetPath);
        }
        else
        {
        #endif
            // Runtime-only path
            itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.PopulateFromJson(jsonItemData);
            itemData.parentCollection = collectionData;
        #if UNITY_EDITOR
        }
        #endif
        
        collectionData.items.Add(itemData);
        
        // Try to load item cover
        string coverPath = itemPath.Replace("/item.json", "/cover");
        itemData.cover = Resources.Load<Texture2D>(coverPath);
        
        if (itemData.cover == null)
        {
            // Load placeholder
            itemData.cover = Resources.Load<Texture2D>($"{baseResourcePath}/placeholders/cover");
        }
        else
        {
            // Notify the model that its cover was loaded
            itemData.OnCoverLoaded();
        }
        
        if (verbose)
        {
            Debug.Log($"Loaded item: {itemData.title}");
        }
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
    public CollectionData GetCollection(string collectionId)
    {
        return collections.Find(c => c.id == collectionId);
    }
    
    public ItemData GetItem(string collectionId, string itemId)
    {
        CollectionData collection = GetCollection(collectionId);
        if (collection != null)
        {
            return collection.items.Find(i => i.id == itemId);
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