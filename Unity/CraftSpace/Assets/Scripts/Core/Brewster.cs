using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Add this for ToList()
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Brewster acts as the central Content Registry.
/// Responsible for loading, caching, and providing access to Collections and Items on demand.
/// </summary>
[DefaultExecutionOrder(-200)] 
public class Brewster : MonoBehaviour
{
    // Static instance for global access
    public static Brewster Instance { get; private set; }

    // Public property to check if core index is loaded
    public bool IsInitialized { get; private set; }

    // Content paths and settings
    [Header("Content Settings")]
    public string baseResourcePath = "Content";
    public bool loadOnStart = true;
    public bool loadCollectionsAutomatically = true;
    public bool loadItemsAutomatically = true;

    [Header("Debug")]
    public bool verbose = false;
    
    // --- Central Registry Caches ---
    private List<string> _collectionIds = new List<string>();
    private Dictionary<string, Collection> _loadedCollections = new Dictionary<string, Collection>();
    private Dictionary<string, Item> _loadedItems = new Dictionary<string, Item>(); // Key: Item ID, Value: Item object
    // Note: Item lookup might need collection context if IDs aren't globally unique,
    // or use a composite key like "collectionId/itemId". For now, assume item IDs are unique.
    
    void Awake()
    {
        try
        {
            Debug.Log("[Brewster/Awake] Starting - Setting up singleton instance.");
            
            // Set up singleton instance
            if (Instance != null && Instance != this)
            {
                Debug.Log("[Brewster/Awake] Another instance already exists. Destroying this instance.");
                Destroy(gameObject);
                return;
            }
            
            Debug.Log("[Brewster/Awake] Setting singleton instance reference.");
            Instance = this;
            
            Debug.Log("[Brewster/Awake] Calling DontDestroyOnLoad on this instance.");
            DontDestroyOnLoad(gameObject);

            // Load core content index if enabled
            if (loadOnStart)
            { 
                Debug.Log("[Brewster/Awake] loadOnStart is true, about to initialize registry.");
                try
                {
                    // Load only the collection index initially
                    InitializeRegistry();
                    Debug.Log("[Brewster/Awake] InitializeRegistry completed successfully.");
                }
                catch (Exception initEx)
                {
                    Debug.LogError($"[Brewster/Awake] FATAL ERROR in InitializeRegistry: {initEx.Message}");
                    Debug.LogError($"[Brewster/Awake] Exception type: {initEx.GetType().FullName}");
                    Debug.LogError($"[Brewster/Awake] Stack trace: {initEx.StackTrace}");
                }
            }
            else
            {
                Debug.Log("[Brewster/Awake] loadOnStart is false, skipping initialization.");
            }
            
            Debug.Log("[Brewster/Awake] Awake completed successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Brewster/Awake] FATAL ERROR in Awake: {e.Message}");
            Debug.LogError($"[Brewster/Awake] Exception type: {e.GetType().FullName}");
            Debug.LogError($"[Brewster/Awake] Stack trace: {e.StackTrace}");
        }
    }
    
    void OnDestroy()
    {
        IsInitialized = false;
        // Consider clearing caches if needed
        _loadedCollections.Clear();
        _loadedItems.Clear();
        _collectionIds.Clear();
    }

    /// <summary>
    /// Initializes the registry by loading the list of available collection IDs.
    /// Also loads all collections (if loadCollectionsAutomatically is true)
    /// and their items (if loadItemsAutomatically is true).
    /// </summary>
    public void InitializeRegistry()
    {
        Debug.Log("[Brewster/Registry] InitializeRegistry starting...");
        try
        {
            // Debug.Log("[Brewster/Registry] Initializing - Loading collection index.");
            
            // Debug.Log("[Brewster/Registry] Clearing existing collections and items...");
            _loadedCollections.Clear();
            _loadedItems.Clear(); 
            _collectionIds.Clear();
            IsInitialized = false;
            // Debug.Log("[Brewster/Registry] Caches cleared successfully.");
            
            // Load collection IDs from index file
            Debug.Log("[Brewster/Registry] About to load collection IDs from index...");
            try
            {
                LoadCollectionIdsFromIndex();
                Debug.Log("[Brewster/Registry] Returned from LoadCollectionIdsFromIndex successfully.");
            }
            catch (Exception loadEx)
            {
                Debug.LogError($"[Brewster/Registry] ERROR: Exception during LoadCollectionIdsFromIndex: {loadEx.Message}");
                Debug.LogError($"[Brewster/Registry] Exception type: {loadEx.GetType().FullName}");
                Debug.LogError($"[Brewster/Registry] Stack trace: {loadEx.StackTrace}");
                throw;
            }
            
            // Debug.Log("[Brewster/Registry] About to check collection IDs count...");
            int idCount = 0;
            try
            {
                idCount = _collectionIds.Count;
                Debug.Log("[Brewster/Registry] Collection IDs count: " + idCount);
            }
            catch (Exception countEx)
            {
                Debug.LogError($"[Brewster/Registry] Error checking collection IDs count: {countEx.Message}");
                Debug.LogError($"[Brewster/Registry] Exception type: {countEx.GetType().FullName}");
                Debug.LogError($"[Brewster/Registry] Stack trace: {countEx.StackTrace}");
                throw;
            }
            
            // Debug.Log("[Brewster/Registry] Setting IsInitialized based on count...");
            IsInitialized = idCount > 0;
            
            // Eagerly load all collections (and items if auto-loading is enabled)
            if (IsInitialized && loadCollectionsAutomatically)
            {
                string loadMessage = "Eagerly loading all collections";
                if (loadItemsAutomatically) 
                    loadMessage += " and their items...";
                else
                    loadMessage += " (items will be loaded on demand)...";
                
                Debug.Log($"[Brewster/Registry] {loadMessage}");
                LoadAllCollections();
            }
            else if (IsInitialized)
            {
                Debug.Log("[Brewster/Registry] Collection auto-loading disabled - collections will be loaded on demand");
            }
            
            Debug.Log($"[Brewster/Registry] Initialization complete. {idCount} collection IDs found. IsInitialized={IsInitialized}");
        }
        catch (Exception e)
        {
            Debug.LogError("[Brewster/Registry] Error during initialization: " + e.Message);
            Debug.LogError("[Brewster/Registry] Exception type: " + e.GetType().FullName);
            Debug.LogError("[Brewster/Registry] Stack trace: " + e.StackTrace);
            IsInitialized = false;
        }
        // Debug.Log("[Brewster/Registry] InitializeRegistry completed.");
    }

    /// <summary>
    /// Loads all collections based on the collection IDs
    /// </summary>
    private void LoadAllCollections()
    {
        try
        {
            if (_collectionIds == null || _collectionIds.Count == 0) return;
            
            Debug.Log($"[Brewster/Registry] Starting eager load of all {_collectionIds.Count} collections");
            
            int loadedCount = 0;
            int errorCount = 0;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            foreach (string collectionId in _collectionIds)
            {
                try
                {
                    Collection collection = GetCollection(collectionId);
                    if (collection != null)
                    {
                        loadedCount++;
                        if (verbose) Debug.Log($"[Brewster/Registry] Eagerly loaded collection: {collectionId}");
                    }
                    else
                    {
                        errorCount++;
                        Debug.LogWarning($"[Brewster/Registry] Failed to load collection: {collectionId}");
                    }
                }
                catch (Exception collEx)
                {
                    errorCount++;
                    Debug.LogError($"[Brewster/Registry] Error loading collection '{collectionId}': {collEx.Message}");
                    // Continue with other collections despite error
                }
            }
            
            float elapsedSec = sw.ElapsedMilliseconds / 1000f;
            Debug.Log($"[Brewster/Registry] Completed eager loading of {loadedCount}/{_collectionIds.Count} collections " +
                     $"({(errorCount > 0 ? $"{errorCount} errors" : "no errors")}) in {elapsedSec:F2}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Brewster/Registry] Error during eager collection loading: {e.Message}");
            Debug.LogError($"[Brewster/Registry] Exception type: {e.GetType().FullName}");
            Debug.LogError($"[Brewster/Registry] Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Loads the collection IDs from the index file into the internal list.
    /// </summary>
    private void LoadCollectionIdsFromIndex()
    {
        // (This logic is mostly the same as the old LoadCollectionIds method)
        try
        {
            _collectionIds.Clear();
            
            if(verbose) Debug.Log("[Brewster/Registry] Loading collections index file.");
            
            string indexFilePath = "";
            try 
            {
                indexFilePath = Path.Combine(Application.streamingAssetsPath, baseResourcePath, "collections-index.json");
                if(verbose) Debug.Log($"[Brewster/Registry] Built index file path: '{indexFilePath}'.");
            }
            catch (Exception pathEx)
            {
                Debug.LogError($"[Brewster/Registry] ERROR in path construction: {pathEx.Message}");
                Debug.LogError($"[Brewster/Registry] Path exception type: {pathEx.GetType().FullName}");
                Debug.LogError($"[Brewster/Registry] Path exception stack trace: {pathEx.StackTrace}");
                throw;
            }
            
            if(verbose) Debug.Log("[Brewster/Registry] Attempting to load collections index from: '" + indexFilePath + "'");
            
            // WebGL requires special handling
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StartCoroutine(LoadCollectionIndexWithWebRequest(indexFilePath));
                return; // Early return, coroutine will continue processing
            }
            
            // Standard file handling for non-WebGL platforms
            if (!File.Exists(indexFilePath))
            {
                Debug.LogWarning("[Brewster/Registry] Collections index not found at: " + indexFilePath);
                return;
            }
            
            string jsonContent = "";
            try
            {
                jsonContent = File.ReadAllText(indexFilePath);
                if(verbose) Debug.Log($"[Brewster/Registry] Successfully read file content. Length: {jsonContent.Length}");
            }
            catch (Exception fileEx)
            {
                Debug.LogError($"[Brewster/Registry] ERROR reading file: {fileEx.Message}");
                Debug.LogError($"[Brewster/Registry] File exception type: {fileEx.GetType().FullName}");
                Debug.LogError($"[Brewster/Registry] File exception stack trace: {fileEx.StackTrace}");
                throw;
            }
            
            ProcessCollectionIndexJson(jsonContent);
        }
        catch (Exception e)
        {
            Debug.LogError("[Brewster/Registry] Error loading collection IDs from index: " + e.Message);
            Debug.LogError("[Brewster/Registry] Exception type: " + e.GetType().FullName);
            Debug.LogError("[Brewster/Registry] Stack trace: " + e.StackTrace);
            _collectionIds.Clear(); // Ensure list is empty on error
        }
    }
    
    // WebGL-specific loading using UnityWebRequest
    private IEnumerator LoadCollectionIndexWithWebRequest(string indexFilePath)
    {
        Debug.Log("[Brewster/Registry] Using WebRequest to load collections index (WebGL mode)");
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(indexFilePath))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[Brewster/Registry] Collections index not found at: " + indexFilePath);
                Debug.LogWarning("[Brewster/Registry] WebRequest error: " + www.error);
                yield break;
            }
            
            string jsonContent = www.downloadHandler.text;
            if(verbose) Debug.Log($"[Brewster/Registry] Successfully read file content via WebRequest. Length: {jsonContent.Length}");
            
            ProcessCollectionIndexJson(jsonContent);
            
            // Now that we're initialized, notify any listeners
            if (_collectionIds.Count > 0)
            {
                IsInitialized = true;
                if (loadCollectionsAutomatically)
                {
                    EagerLoadCollections();
                }
            }
        }
    }
    
    // Common JSON processing logic extracted to avoid duplication
    private void ProcessCollectionIndexJson(string jsonContent)
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            Debug.LogError("[Brewster/Registry] ERROR: JSON content is empty or null.");
            return;
        }
        
        if (verbose) 
        {
            try
            {
                Debug.Log($"[Brewster/Registry] Index JSON Preview: {Truncate(jsonContent, 100)}");
            }
            catch (Exception previewEx)
            {
                Debug.LogError($"[Brewster/Registry] ERROR in preview generation: {previewEx.Message}");
            }
        }
        
        // Parse as array
        Debug.Log("[Brewster/Registry] About to deserialize JSON content...");
        List<string> parsedIds = null;
        
        try
        {
            parsedIds = JsonConvert.DeserializeObject<List<string>>(jsonContent);
            if (parsedIds != null)
            {
                Debug.Log($"[Brewster/Registry] Successfully parsed {parsedIds.Count} collection IDs from JSON.");
            }
        }
        catch (Exception jsonEx)
        {
            Debug.LogError($"[Brewster/Registry] Error deserializing collections index: {jsonEx.Message}");
            
            // Try a different deserialization approach
            try
            {
                var collectionIndex = JsonConvert.DeserializeObject<CollectionIndex>(jsonContent);
                if (collectionIndex != null && collectionIndex.collections != null)
                {
                    parsedIds = new List<string>(collectionIndex.collections);
                    Debug.Log($"[Brewster/Registry] Successfully parsed {parsedIds.Count} collection IDs from CollectionIndex object.");
                }
            }
            catch (Exception altEx)
            {
                Debug.LogError($"[Brewster/Registry] Error in alternate deserialization: {altEx.Message}");
            }
        }
        
        if (parsedIds != null)
        {
            try
            {
                foreach (string id in parsedIds)
                {
                    if (!string.IsNullOrEmpty(id) && !_collectionIds.Contains(id))
                    {
                        _collectionIds.Add(id);
                    }
                }
                
                if (verbose && _collectionIds.Count > 0) {
                    try {
                        string joinedIds = string.Join(", ", _collectionIds);
                        Debug.Log("[Brewster/Registry] Collection IDs: " + joinedIds);
                    }
                    catch (Exception joinEx) {
                        Debug.LogError($"[Brewster/Registry] Error joining IDs: {joinEx.Message}");
                        // Continue despite this non-critical error
                    }
                }
            }
            catch (Exception processEx) {
                Debug.LogError("[Brewster/Registry] Error processing parsed IDs: " + processEx.Message);
                Debug.LogError("[Brewster/Registry] Exception type: " + processEx.GetType().FullName);
                Debug.LogError("[Brewster/Registry] Stack trace: " + processEx.StackTrace);
                throw;
            }
        }
        else 
        {
             Debug.LogError("[Brewster/Registry] Failed to deserialize collection index JSON into a list of strings.");
        }
    }

    /// <summary>
    /// Gets a collection by ID. Loads from file/cache on demand.
    /// </summary>
    public Collection GetCollection(string collectionId)
    {
        if (string.IsNullOrEmpty(collectionId))
        {
            Debug.LogWarning("[Brewster/Registry] GetCollection called with null or empty ID.");
            return null;
        }

        // 1. Check cache
        if (_loadedCollections.TryGetValue(collectionId, out Collection cachedCollection))
        {
            // if (verbose) Debug.Log($"[Brewster/Registry] Cache hit for Collection: {collectionId}");
            return cachedCollection;
        }

        // 2. Load from file if not cached
        if (verbose) Debug.Log($"[Brewster/Registry] Cache miss for Collection: {collectionId}. Attempting to load from file.");
        try
        {
            string collectionPath = Path.Combine(Application.streamingAssetsPath, baseResourcePath, "collections", collectionId, "collection.json");
            if (verbose) Debug.Log("[Brewster/Registry] Loading Collection from: '" + collectionPath + "'");
            
            if (!File.Exists(collectionPath))
            {
                Debug.LogWarning($"[Brewster/Registry] Collection file not found: {collectionPath}");
                return null;
            }
            
            string jsonContent = File.ReadAllText(collectionPath);
            if (verbose) Debug.Log($"[Brewster/Registry] Collection JSON loaded ({jsonContent.Length} chars), Preview: {Truncate(jsonContent, 100)}");
            
            Collection collection = Collection.FromJson(jsonContent);
            
            if (collection != null)
            {
                if (collection.Id != collectionId)
                {
                     Debug.LogWarning($"[Brewster/Registry] Collection ID mismatch! Directory ID '{collectionId}' does not match collection.json ID '{collection.Id}'. Using directory ID for caching key, but object has its own ID.");
                     // Consider whether to force the ID on the object: collection.Id = collectionId;
                }

                // Add to cache BEFORE loading item index
                _loadedCollections[collectionId] = collection;
                if (verbose) Debug.Log($"[Brewster/Registry] Loaded and cached Collection: {collection.Id}");

                // --- Load Item Index (but not items themselves) ---
                // This should happen lazily when Collection.Items is accessed,
                // OR we can trigger it here if we want the index loaded when the collection is.
                // Let's trigger it here for now, simplifying Collection.Items getter.
                string collectionBasePath = Path.GetDirectoryName(collectionPath);
                if (!string.IsNullOrEmpty(collectionBasePath))
                {
                     if (verbose) Debug.Log($"[Brewster/Registry] Triggering item *index* load for collection '{collectionId}' from path: {collectionBasePath}");
                     collection.LoadItemIndex(collectionBasePath); // Renamed method!
                     
                     // Eagerly load all items for the collection if auto-loading is enabled
                     if (loadItemsAutomatically)
                     {
                         Debug.Log($"[Brewster/Registry] Auto-loading all items for collection '{collectionId}'");
                         LoadAllItemsForCollection(collection);
                     }
                     else if (verbose)
                     {
                         Debug.Log($"[Brewster/Registry] Item auto-loading disabled - items will be loaded on demand for collection '{collectionId}'");
                     }
                }
                else
                {
                     Debug.LogError($"[Brewster/Registry] Could not determine base path for collection '{collectionId}'. Cannot load item index.");
                }
                // --- End Item Index Load ---

                return collection;
            }
            else
            {
                Debug.LogError($"[Brewster/Registry] Failed to parse collection JSON from: {collectionPath}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Brewster/Registry] Error loading collection '{collectionId}' from file: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Eagerly loads all items for a collection
    /// </summary>
    private void LoadAllItemsForCollection(Collection collection)
    {
        try
        {
            if (collection == null) return;
            
            if (verbose) Debug.Log($"[Brewster/Registry] Starting eager load of all items for collection '{collection.Id}'");
            
            // Use the Items property to force loading of all items
            // This is a bit of a hack - we're using the enumerator to load all items
            int loadedItemCount = 0;
            int errorCount = 0;
            int totalCount = 0;
            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            foreach (Item item in collection.Items)
            {
                totalCount++;
                if (item != null)
                {
                    loadedItemCount++;
                    // Only log individual items if in verbose mode
                    Debug.Log($"[Brewster/Registry] Successfully loaded item: {item.Id}");
                }
                else
                {
                    errorCount++;
                    Debug.LogWarning($"[Brewster/Registry] Failed to load item #{totalCount} from collection '{collection.Id}'");
                }
            }
            
            // Log summary statistics
            float elapsedSec = sw.ElapsedMilliseconds / 1000f;
            Debug.Log($"[Brewster/Registry] Collection '{collection.Id}' - Successfully loaded {loadedItemCount}/{totalCount} items " + 
                      $"({(errorCount > 0 ? $"{errorCount} errors" : "no errors")}) in {elapsedSec:F2}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Brewster/Registry] Error during eager item loading for collection '{collection?.Id}': {e.Message}");
            Debug.LogError($"[Brewster/Registry] Exception type: {e.GetType().FullName}");
            Debug.LogError($"[Brewster/Registry] Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Gets an item by its ID. Loads from file/cache on demand.
    /// Requires collection context to build the file path.
    /// </summary>
    public Item GetItem(string collectionId, string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(collectionId))
        {
            Debug.LogWarning($"[Brewster/Registry] GetItem called with null or empty ID (Collection: '{collectionId}', Item: '{itemId}').");
            return null;
        }

        // --- Use Item ID as the primary key for the item cache --- 
        // Assuming item IDs are globally unique or unique enough for runtime.
        // If not, a composite key like $"{collectionId}/{itemId}" might be needed.
        string itemCacheKey = itemId;

        // 1. Check cache
        if (_loadedItems.TryGetValue(itemCacheKey, out Item cachedItem))
        {
            // if (verbose) Debug.Log($"[Brewster/Registry] Cache hit for Item: {itemCacheKey}");
            return cachedItem;
        }

        // 2. Load from file if not cached
        if (verbose) Debug.Log($"[Brewster/Registry] Cache miss for Item: {itemCacheKey}. Attempting to load from file (Context: Collection '{collectionId}').");
        try
        {
            // SINGULAR DEFINITIVE PATH CONSTRUCTION - This is the ONLY place that should construct this path
            string itemFilePath = Path.Combine(Application.streamingAssetsPath, baseResourcePath, 
                "collections", collectionId, "items", itemId, "item.json");

            if (verbose) Debug.Log("[Brewster/Registry] Loading Item from: '" + itemFilePath + "'");
            
            // STRICTLY CHECK EXISTENCE - Fatal error if missing
            if (!File.Exists(itemFilePath))
            {
                // FATAL ERROR - item.json is missing
                Debug.LogError($"[Brewster/Registry] FATAL ERROR: Item file not found: {itemFilePath}");
                
                // Create placeholder item with MISSING title
                Item placeholderItem = ScriptableObject.CreateInstance<Item>();
                placeholderItem.Id = itemId;
                placeholderItem.Title = "MISSING";  // Use MISSING as title
                placeholderItem.ParentCollectionId = collectionId;
                
                // Add to cache so we don't keep trying to load it
                _loadedItems[itemCacheKey] = placeholderItem;
                
                return placeholderItem;
            }
            
            string jsonContent = File.ReadAllText(itemFilePath);
            if (verbose) Debug.Log($"[Brewster/Registry] Item JSON loaded ({jsonContent.Length} chars), Preview: {Truncate(jsonContent, 100)}");
            
            Item item = Item.FromJson(jsonContent);
            
            if (item != null)
            {
                if (item.Id != itemId)
                {
                    Debug.LogWarning($"[Brewster/Registry] Item ID mismatch! Directory ID '{itemId}' does not match item.json ID '{item.Id}'. Using directory ID for caching key, but object has its own ID.");
                    // Consider forcing ID: item.Id = itemId;
                }

                // Set Parent Collection ID before caching/returning
                item.ParentCollectionId = collectionId; 

                // Add to cache
                _loadedItems[itemCacheKey] = item;
                if (verbose) Debug.Log($"[Brewster/Registry] Loaded and cached Item: {item.Id} from Collection: {collectionId}");

                // --- Trigger Cover Image Load ---
                // Load cover image after item is loaded and cached.
                if (verbose) Debug.Log($"[Brewster/Registry] Triggering cover image load for item: {item.Id}");
                item.LoadCoverImage(); 
                // --- End Cover Image Load ---
                
                return item;
            }
            else
            {
                Debug.LogError($"[Brewster/Registry] FATAL ERROR: Failed to parse item JSON from: {itemFilePath}");
                
                // Create placeholder item with MISSING title
                Item placeholderItem = ScriptableObject.CreateInstance<Item>();
                placeholderItem.Id = itemId;
                placeholderItem.Title = "MISSING"; // Use MISSING as title for parse failures too
                placeholderItem.ParentCollectionId = collectionId;
                
                // Add to cache so we don't keep trying to load it
                _loadedItems[itemCacheKey] = placeholderItem;
                
                return placeholderItem;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Brewster/Registry] FATAL ERROR: Exception loading item '{itemId}' (Collection '{collectionId}'): {e.Message}");
            
            // Create placeholder item with MISSING title
            Item placeholderItem = ScriptableObject.CreateInstance<Item>();
            placeholderItem.Id = itemId;
            placeholderItem.Title = "MISSING"; // Use MISSING as title for exceptions too
            placeholderItem.ParentCollectionId = collectionId;
            
            // Add to cache so we don't keep trying to load it
            _loadedItems[itemCacheKey] = placeholderItem;
            
            return placeholderItem;
        }
    }

    /// <summary>
    /// Converts an item ID from directory name format to a more readable title
    /// </summary>
    private string CleanupItemName(string itemId)
    {
        // Replace hyphens, underscores and dots with spaces
        string cleanTitle = itemId.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');
        
        // Title case the result (capitalize first letter of each word)
        System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;
        cleanTitle = textInfo.ToTitleCase(cleanTitle);
        
        return cleanTitle;
    }

    /// <summary>
    /// Gets all known collection IDs.
    /// </summary>
    public IReadOnlyList<string> GetAllCollectionIds()
    {
        // Ensure initialized? Maybe call InitializeRegistry() if !IsInitialized?
        return _collectionIds.AsReadOnly();
    }

    /// <summary>
    /// Gets all currently loaded collections from the cache.
    /// Does not trigger loading of collections not already requested.
    /// </summary>
    public IEnumerable<Collection> GetAllLoadedCollections()
    {
        return _loadedCollections.Values;
    }

    /// <summary>
    /// Helper to truncate strings for logging.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
    
    // --- Old Loading Methods Removed --- 
    // LoadAllContent (replaced by InitializeRegistry and on-demand gets)
    // LoadCollection (replaced by GetCollection)
    // LoadItemsForCollection (logic moved to Collection.LoadItemIndex and GetItem)
    // LoadItemCoverImage (logic moved to Item.LoadCoverImage, triggered by GetItem)
    
    // GetTotalItemCount might need rework to iterate IDs and potentially trigger loads, or just count loaded items.
}

/// <summary>
/// Helper class for deserializing the collections index
/// </summary>
[Serializable]
public class CollectionIndex
{
    public string[] collections;
}
