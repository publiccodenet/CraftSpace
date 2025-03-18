using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Remove these if they don't exist
// using CraftSpace.Models;
// using CraftSpace.Models.Generated;

public class CollectionLoader : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "https://spaceship.donhopkins.com/api";
    [SerializeField] private bool useBuiltInDataFirst = true;
    [SerializeField] private bool verboseLogging = true;
    
    // Make these classes public to match event accessibility
    [Serializable]
    public class CollectionInfo
    {
        public string prefix;
        public string subject;
        public string mediatype;
        public string lastUpdated;
        public string indexFile;
        public string url;
        public string dataUrl;
    }
    
    [Serializable]
    public class CollectionIndex
    {
        public string prefix;
        public string subject;
        public string mediatype;
        public int totalItems;
        public string lastUpdated;
        public List<string> items = new List<string>();
    }
    
    [Serializable]
    public class TopLevelIndex
    {
        public List<CollectionInfo> collections = new List<CollectionInfo>();
    }
    
    [Serializable]
    public class ItemMetadata
    {
        public string id;
        public string title;
        public string creator;
        public string date;
        public List<string> subject = new List<string>();
        public List<string> collection = new List<string>();
        public string description;
    }
    
    private TopLevelIndex _topLevelIndex;
    private Dictionary<string, CollectionIndex> _collectionIndices = new Dictionary<string, CollectionIndex>();
    private Dictionary<string, ItemMetadata> _itemMetadata = new Dictionary<string, ItemMetadata>();
    
    public event Action<TopLevelIndex> OnTopLevelIndexLoaded;
    public event Action<string, CollectionIndex> OnCollectionIndexLoaded;
    public event Action<string, ItemMetadata> OnItemMetadataLoaded;
    
    private void Start()
    {
        StartCoroutine(LoadTopLevelIndex());
    }
    
    private IEnumerator LoadTopLevelIndex()
    {
        bool loaded = false;
        Debug.Log($"🔍 Starting load of top-level collection index");
        
        // First try to load from embedded Resources if enabled
        if (useBuiltInDataFirst)
        {
            // Load collections-index.json which is next to the collections directory
            if (verboseLogging) Debug.Log($"👀 Looking for collections-index.json in Resources/Content");
            TextAsset indexAsset = Resources.Load<TextAsset>("Content/collections-index");
            
            if (indexAsset != null)
            {
                Debug.Log($"✅ Found collections-index.json in Resources/Content");
                try
                {
                    if (verboseLogging) Debug.Log($"🔄 Parsing collections-index.json as JSON array");
                    // Parse the JSON array directly with JSON.net
                    JArray collectionArray = JArray.Parse(indexAsset.text);
                    
                    if (verboseLogging) Debug.Log($"📊 Creating TopLevelIndex structure with {collectionArray.Count} collections");
                    // Create the TopLevelIndex structure using your model class
                    _topLevelIndex = new TopLevelIndex();
                    _topLevelIndex.collections = new List<CollectionInfo>();
                    
                    // Add each collection from the index
                    foreach (JToken token in collectionArray)
                    {
                        string prefix = token.ToString();
                        if (verboseLogging) Debug.Log($"📚 Processing collection prefix: {prefix}");
                        
                        // Use your Collection.Info class instead of the old Collection class
                        var collection = new CollectionInfo();
                        collection.prefix = prefix;
                        
                        // Load the collection.json to get additional metadata
                        if (verboseLogging) Debug.Log($"👀 Looking for {prefix}/collection.json");
                        TextAsset collectionAsset = Resources.Load<TextAsset>($"Content/collections/{prefix}/collection");
                        if (collectionAsset != null)
                        {
                            if (verboseLogging) Debug.Log($"✅ Found collection.json for {prefix}");
                            try
                            {
                                // Deserialize directly to your model instead of manual property setting
                                if (verboseLogging) Debug.Log($"🔄 Deserializing collection.json for {prefix}");
                                var collectionInfo = JsonConvert.DeserializeObject<CollectionInfo>(collectionAsset.text);
                                // Copy properties from loaded collection to our info object
                                collection = collectionInfo;
                                if (verboseLogging) Debug.Log($"✅ Successfully deserialized collection data for {prefix}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"❌ Error parsing collection.json for {prefix}: {ex.Message}\nJSON: {collectionAsset.text.Substring(0, Math.Min(100, collectionAsset.text.Length))}...");
                                // Still proceed with basic info we already set
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ No collection.json found for {prefix}, using basic info only");
                        }
                        
                        _topLevelIndex.collections.Add(collection);
                        if (verboseLogging) Debug.Log($"✅ Added collection {prefix} to top level index");
                    }
                    
                    OnTopLevelIndexLoaded?.Invoke(_topLevelIndex);
                    loaded = true;
                    Debug.Log($"🎉 Successfully loaded {_topLevelIndex.collections.Count} collections from collections-index.json");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error parsing collections-index.json: {ex.Message}\nJSON: {indexAsset.text.Substring(0, Math.Min(100, indexAsset.text.Length))}...");
                }
            }
            else
            {
                // Fallback: Try loading directly from the scifi collection if no index exists
                Debug.LogWarning($"⚠️ No collections-index.json found, trying to load scifi collection directly");
                _topLevelIndex = new TopLevelIndex();
                _topLevelIndex.collections = new List<CollectionInfo>();
                
                TextAsset collectionAsset = Resources.Load<TextAsset>("Content/collections/scifi/collection");
                if (collectionAsset != null)
                {
                    Debug.Log($"✅ Found scifi/collection.json for fallback");
                    var collection = new CollectionInfo();
                    collection.prefix = "scifi";
                    collection.subject = "Science Fiction";
                    collection.mediatype = "texts";
                    collection.lastUpdated = System.DateTime.Now.ToString("o");
                    collection.indexFile = "";
                    collection.url = "";
                    collection.dataUrl = "";
                    _topLevelIndex.collections.Add(collection);
                    OnTopLevelIndexLoaded?.Invoke(_topLevelIndex);
                    loaded = true;
                    Debug.Log($"✅ Added fallback scifi collection to top level index");
                }
                else
                {
                    Debug.LogError($"❌ Failed to load collections-index.json AND scifi/collection.json fallback!");
                }
            }
        }
        
        // If not loaded from Resources or that's disabled, try from API
        if (!loaded)
        {
            Debug.Log($"🌐 Attempting to load collections from API: {apiBaseUrl}/collections");
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/collections"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (verboseLogging) Debug.Log($"🔄 Deserializing collections response from API");
                        _topLevelIndex = JsonConvert.DeserializeObject<TopLevelIndex>(request.downloadHandler.text);
                        OnTopLevelIndexLoaded?.Invoke(_topLevelIndex);
                        Debug.Log($"🎉 Successfully loaded {_topLevelIndex.collections.Count} collections from API");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Error deserializing collections from API: {ex.Message}\nJSON: {request.downloadHandler.text.Substring(0, Math.Min(100, request.downloadHandler.text.Length))}...");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Failed to load top-level index from API: {request.error}");
                }
            }
        }
    }
    
    public void LoadCollectionIndex(string prefix)
    {
        StartCoroutine(LoadCollectionIndexCoroutine(prefix));
    }
    
    private IEnumerator LoadCollectionIndexCoroutine(string prefix)
    {
        bool loaded = false;
        Debug.Log($"🔍 Starting load of collection index for '{prefix}'");
        
        // First try to load from embedded Resources if enabled
        if (useBuiltInDataFirst)
        {
            // Load the items-index.json file which is next to the items directory
            if (verboseLogging) Debug.Log($"👀 Looking for {prefix}/items-index.json");
            TextAsset indexAsset = Resources.Load<TextAsset>($"Content/collections/{prefix}/items-index");
            if (indexAsset != null)
            {
                Debug.Log($"✅ Found items-index.json for {prefix}");
                try
                {
                    // Parse the JSON array directly with JSON.net
                    if (verboseLogging) Debug.Log($"🔄 Parsing items-index.json for {prefix}");
                    JArray itemsArray = JArray.Parse(indexAsset.text);
                    
                    // Create the CollectionIndex structure using your model
                    if (verboseLogging) Debug.Log($"📊 Creating CollectionIndex with {itemsArray.Count} items");
                    CollectionIndex collectionIndex = new CollectionIndex();
                    collectionIndex.prefix = prefix;
                    collectionIndex.items = new List<string>();
                    
                    // Convert JArray to List<string> - adapt field name as needed for your model
                    foreach (JToken token in itemsArray)
                    {
                        string itemId = token.ToString();
                        collectionIndex.items.Add(itemId);
                        if (verboseLogging) Debug.Log($"📝 Added item '{itemId}' to collection '{prefix}'");
                    }
                    
                    // Also load collection.json for additional metadata
                    if (verboseLogging) Debug.Log($"👀 Looking for {prefix}/collection.json for additional metadata");
                    TextAsset collectionAsset = Resources.Load<TextAsset>($"Content/collections/{prefix}/collection");
                    if (collectionAsset != null)
                    {
                        if (verboseLogging) Debug.Log($"✅ Found collection.json for {prefix}");
                        try
                        {
                            // Deserialize to your model class
                            if (verboseLogging) Debug.Log($"🔄 Deserializing collection.json for {prefix}");
                            var collectionInfo = JsonConvert.DeserializeObject<CollectionInfo>(collectionAsset.text);
                            // Copy properties that exist in Index from Info
                            collectionIndex.subject = collectionInfo.subject;
                            collectionIndex.mediatype = collectionInfo.mediatype;
                            collectionIndex.lastUpdated = collectionInfo.lastUpdated;
                            collectionIndex.totalItems = itemsArray.Count;
                            if (verboseLogging) Debug.Log($"✅ Added metadata from collection.json to index");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"⚠️ Error parsing collection.json for metadata in {prefix}: {ex.Message}\nContinuing with basic index");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ No collection.json found for metadata in {prefix}, continuing with basic index");
                    }
                    
                    _collectionIndices[prefix] = collectionIndex;
                    OnCollectionIndexLoaded?.Invoke(prefix, collectionIndex);
                    loaded = true;
                    Debug.Log($"🎉 Successfully loaded collection index for '{prefix}' with {collectionIndex.items.Count} items");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error parsing items-index.json for {prefix}: {ex.Message}\nJSON: {indexAsset.text.Substring(0, Math.Min(100, indexAsset.text.Length))}...");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ No items-index.json found for {prefix}");
            }
        }
        
        // If not loaded from Resources or that's disabled, try from API
        if (!loaded)
        {
            Debug.Log($"🌐 Attempting to load collection index for '{prefix}' from API");
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/collections/{prefix}"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (verboseLogging) Debug.Log($"🔄 Deserializing collection index for {prefix} from API");
                        CollectionIndex collectionIndex = JsonConvert.DeserializeObject<CollectionIndex>(request.downloadHandler.text);
                        _collectionIndices[prefix] = collectionIndex;
                        OnCollectionIndexLoaded?.Invoke(prefix, collectionIndex);
                        Debug.Log($"🎉 Successfully loaded collection index for '{prefix}' from API with {collectionIndex.items.Count} items");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Error deserializing collection index from API: {ex.Message}\nJSON: {request.downloadHandler.text.Substring(0, Math.Min(100, request.downloadHandler.text.Length))}...");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Failed to load collection index for '{prefix}' from API: {request.error}");
                }
            }
        }
    }
    
    public void LoadItemMetadata(string prefix, string itemId)
    {
        StartCoroutine(LoadItemMetadataCoroutine(prefix, itemId));
    }
    
    private IEnumerator LoadItemMetadataCoroutine(string prefix, string itemId)
    {
        bool loaded = false;
        Debug.Log($"🔍 Starting load of item metadata for '{itemId}' in collection '{prefix}'");
        
        // First try to load from embedded Resources if enabled
        if (useBuiltInDataFirst)
        {
            if (verboseLogging) Debug.Log($"👀 Looking for {prefix}/items/{itemId}/item.json");
            TextAsset metadataAsset = Resources.Load<TextAsset>($"Content/collections/{prefix}/items/{itemId}/item");
            if (metadataAsset != null)
            {
                Debug.Log($"✅ Found item.json for {itemId}");
                try
                {
                    // Use JSON.net to deserialize to your Item class
                    if (verboseLogging) Debug.Log($"🔄 Deserializing item.json for {itemId}");
                    ItemMetadata metadata = JsonConvert.DeserializeObject<ItemMetadata>(metadataAsset.text);
                    _itemMetadata[itemId] = metadata;
                    OnItemMetadataLoaded?.Invoke(itemId, metadata);
                    loaded = true;
                    Debug.Log($"🎉 Successfully loaded metadata for '{itemId}' from Resources");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error parsing item.json for {itemId}: {ex.Message}\nJSON: {metadataAsset.text.Substring(0, Math.Min(100, metadataAsset.text.Length))}...");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ No item.json found for {itemId} in {prefix}");
            }
        }
        
        // If not loaded from Resources or that's disabled, try from API
        if (!loaded)
        {
            Debug.Log($"🌐 Attempting to load item metadata for '{itemId}' from API");
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/collections/{prefix}/{itemId}"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (verboseLogging) Debug.Log($"🔄 Deserializing item metadata for {itemId} from API");
                        ItemMetadata metadata = JsonConvert.DeserializeObject<ItemMetadata>(request.downloadHandler.text);
                        _itemMetadata[itemId] = metadata;
                        OnItemMetadataLoaded?.Invoke(itemId, metadata);
                        Debug.Log($"🎉 Successfully loaded metadata for '{itemId}' from API");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Error deserializing item metadata from API: {ex.Message}\nJSON: {request.downloadHandler.text.Substring(0, Math.Min(100, request.downloadHandler.text.Length))}...");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Failed to load metadata for '{itemId}' from API: {request.error}");
                }
            }
        }
    }
    
    // Add methods for generating texture atlases from color data
    public void GenerateAtlasForCollection(string prefix)
    {
        // Implementation for creating texture atlases from book metadata
        // using the techniques described in BookCoverVisualization.md
    }
} 