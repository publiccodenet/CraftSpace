//------------------------------------------------------------------------------
// <file_path>Unity/CraftSpace/Assets/Scripts/Schemas/Collection.cs</file_path>
// <namespace>CraftSpace</namespace>
// <assembly>Assembly-CSharp</assembly>
//
// IMPORTANT: This is a MANUAL EXTENSION of a generated schema class.
// DO NOT DELETE this file - it extends the generated CollectionSchema.cs.
//
// WARNING: KEEP THIS CLASS THIN AND SIMPLE!
// DO NOT PUT FUNCTIONALITY HERE THAT SHOULD GO IN THE SHARED BASE CLASS SchemaGeneratedObject.
// This class MUST agree with BOTH the SchemaGeneratedObject base class AND the generated CollectionSchema.cs.
//
// ALWAYS verify against schema generator when encountering errors.
// Fix errors in the schema generator FIRST before modifying this file or any generated code.
// Generated files may be out of date, and the best fix is regenerating them with updated generator.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO; // Required for Path.Combine
using System.Collections;

[Serializable]
public class Collection : CollectionSchema
{    
    // Runtime-only state (not serialized)
    [NonSerialized] private List<string> _itemIds = new List<string>(); // Stores IDs, not direct Item references
    
    /// <summary>
    /// Gets the items associated with this collection.
    /// Performs on-demand loading via the Brewster registry.
    /// </summary>
    public IEnumerable<Item> Items 
    { 
        get 
        { 
            if (_itemIds == null || _itemIds.Count == 0)
            {
                Debug.LogWarning($"[Collection:{Id}] No item IDs available. LoadItemIndex may not have been called.");
                yield break;
            }
            
            int processedCount = 0;
            
            foreach (string itemId in _itemIds)
            {
                if (Brewster.Instance == null)
                {
                    Debug.LogError($"[Collection:{Id}] Brewster.Instance is null! Cannot load items.");
                    yield break;
                }
                
                Item item = Brewster.Instance?.GetItem(this.Id, itemId);
                if (item != null)
                {
                    yield return item;
                    processedCount++;
                }
                else 
                {
                    Debug.LogWarning($"[Collection:{Id}] Failed to retrieve Item with ID '{itemId}' from registry.");
                }
            }
        }
    }

    [NonSerialized] private HashSet<ICollectionView> registeredViews = new HashSet<ICollectionView>();
    
    // Register a view with this model
    public void RegisterView(object view)
    {
        try
        {
            if (view is ICollectionView collectionView && !registeredViews.Contains(collectionView))
            {
                registeredViews.Add(collectionView);
                Debug.Log($"[Collection] Registered view for {Title}");
            }
            else
            {
                Debug.LogWarning($"[Collection] Attempted to register unknown view type: {view?.GetType().Name ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Collection] Error registering view: {ex.Message}");
        }
    }
    
    // Unregister a view from this model
    public void UnregisterView(object view)
    {
        try
        {
            if (view is ICollectionView collectionView)
            {
                registeredViews.Remove(collectionView);
                Debug.Log($"[Collection] Unregistered view for {Title}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Collection] Error unregistering view: {ex.Message}");
        }
    }
    
    // Notify all registered views that the model has changed
    public void NotifyViewsOfUpdate()
    {
        try
        {
            Debug.Log($"[Collection] Notifying {registeredViews.Count} views of update for {Title}");
            
            foreach (var view in registeredViews)
            {
                if (view != null)
                {
                    try
                    {
                        view.OnCollectionUpdated(this);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Collection] Error notifying view: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Collection] Error in NotifyViewsOfUpdate: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Create a new Collection instance
    /// </summary>
    public static new Collection CreateInstance<T>() where T : Collection
    {
        return ScriptableObject.CreateInstance<T>();
    }
    
    /// <summary>
    /// Create a new Collection instance
    /// </summary>
    public static new Collection CreateInstance(Type type)
    {
        return ScriptableObject.CreateInstance(type) as Collection;
    }
    
    /// <summary>
    /// Parse a Collection from JSON string - Use SchemaGeneratedObject.ImportFromJson instead
    /// </summary>
    [Obsolete("Use Collection.FromJson instead.")]
    public static Collection FromJsonString(string json)
    {
        Debug.LogWarning("[Collection] FromJsonString is obsolete. Use FromJson instead.");
        return null; 
    }
    
    /// <summary>
    /// Parse a Collection from JSON string - public facade for backward compatibility
    /// </summary>
    public static Collection FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[Collection] Cannot create Collection from null or empty JSON.");
            return null;
        }

        Collection instance = ScriptableObject.CreateInstance<Collection>();
        try
        {
            instance.ImportFromJson(json); // Base class handles parsing, property import, name setting

             if (string.IsNullOrEmpty(instance.Id))
             {
                 Debug.LogError("[Collection] Created Collection is missing required 'id' field after import.");
                 ScriptableObject.DestroyImmediate(instance);
                 return null;
             }

             // Initialize the ID list, but don't populate yet (LoadItemIndex does that)
             instance._itemIds = new List<string>(); 

            // Debug.Log($"[Collection] Successfully created and imported Collection: {instance.Id}");
            return instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Collection] Failed to import Collection from JSON: {e.Message}\nJSON Preview: {Truncate(json, 200)}");
            if (instance != null) ScriptableObject.DestroyImmediate(instance);
            return null;
        }
    }
    
    /// <summary>
    /// Convert to JSON string - Use SchemaGeneratedObject.ExportToJson instead
    /// </summary>
    public new string ToJson()
    {
        return base.ExportToJson();
    }
    
    /// <summary>
    /// Original method for backward compatibility
    /// </summary>
    public new string ToJsonString(bool prettyPrint = false)
    {
        try
        {
            if (prettyPrint)
            {
                return base.ExportToJson();
            }
            else
            {
                return JObject.Parse(base.ExportToJson()).ToString(Formatting.None);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Collection] Error in ToJsonString: {e.Message}");
            return "{}";
        }
    }

#if false    
    /// <summary>
    /// Load the cover image from file
    /// </summary>
    public void LoadCoverImage()
    {
        if (string.IsNullOrEmpty(CoverImage))
        {
            return;
        }
        
        try
        {
            string coverImagePath = System.IO.Path.Combine(
                Application.streamingAssetsPath,
                "Content",
                "collections",
                Id,
                "images",
                CoverImage
            );
            
            if (System.IO.File.Exists(coverImagePath))
            {
                byte[] imageData = System.IO.File.ReadAllBytes(coverImagePath);
                Texture2D texture = new Texture2D(2, 2);
                
                if (texture.LoadImage(imageData))
                {
                    Debug.Log($"[Collection] Loaded cover image from {coverImagePath}");
                }
                else
                {
                    Debug.LogWarning($"[Collection] Failed to load cover image from {coverImagePath}");
                }
            }
            else
            {
                Debug.LogWarning($"[Collection] Cover image file not found at {coverImagePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Collection] Error loading cover image: {e.Message}");
        }
    }
#endif
    
    /// <summary>
    /// Handler for deserialization errors
    /// </summary>
    private static void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
    {
        Debug.LogWarning("[Collection] JSON deserialization issue detected");
        Debug.LogWarning("[Collection] Error message: " + args.ErrorContext.Error.Message);
        Debug.LogWarning("[Collection] Error path: " + args.ErrorContext.Path);
        Debug.LogWarning("[Collection] Current object: " + args.ErrorContext.OriginalObject?.GetType());
        Debug.LogWarning("[Collection] Member being deserialized: " + args.ErrorContext.Member);
        if (args.ErrorContext.Error.InnerException != null)
        {
            Debug.LogWarning("[Collection] Inner exception: " + args.ErrorContext.Error.InnerException.Message);
            Debug.LogWarning("[Collection] Inner stack trace: " + args.ErrorContext.Error.InnerException.StackTrace);
        }
        args.ErrorContext.Handled = true;
    }

    /// <summary>
    /// Loads the *index* of item IDs associated with this collection from 'items-index.json'.
    /// Does NOT load the actual Item objects.
    /// Called by Brewster after the Collection itself is loaded.
    /// </summary>
    /// <param name="basePath">The directory path containing the collection's 'items-index.json'.</param>
    public void LoadItemIndex(string basePath)
    {
        // Renamed from LoadItems, changed logic to only load IDs
        Debug.Log($"[Collection:{Id}] === STARTING ITEM INDEX LOAD ===");
        Debug.Log($"[Collection:{Id}] Runtime platform: {Application.platform}, IL2CPP mode: {UnityEngine.RuntimePlatform.WebGLPlayer == Application.platform}");

        // if (_itemIds != null && _itemIds.Count > 0) // Check if already loaded?
        // {
        //     Debug.Log($"[Collection:{Id}] Item index already loaded.");
        //     return;
        // }

        _itemIds = new List<string>(); // Initialize or clear
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        string indexFilePath = Path.Combine(basePath, "items-index.json");
        // Use verbose flag from Brewster? Or add local flag?
        bool verbose = Brewster.Instance?.verbose ?? false;
        Debug.Log($"[Collection:{Id}] Attempting to load items index from: '{indexFilePath}'");

        // WebGL requires special handling with UnityWebRequest
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (Brewster.Instance != null)
            {
                Brewster.Instance.StartCoroutine(LoadItemIndexWithWebRequest(indexFilePath));
            }
            else
            {
                Debug.LogError($"[Collection:{Id}] Cannot start WebRequest coroutine - Brewster.Instance is null");
            }
            return;
        }

        // Standard file handling for non-WebGL platforms
        if (!File.Exists(indexFilePath))
        {
            Debug.LogWarning($"[Collection:{Id}] Items index not found at path: {indexFilePath}. Collection will have no items.");
            return;
        }

        string indexJson = "";
        try
        {
            indexJson = File.ReadAllText(indexFilePath);
            Debug.Log($"[Collection:{Id}] File read completed in {sw.ElapsedMilliseconds}ms");
        }
        catch (System.Exception e)
        {
             Debug.LogError($"[Collection:{Id}] Failed to read items index file '{indexFilePath}': {e.Message}");
             Debug.LogError($"[Collection:{Id}] Stack trace: {e.StackTrace}");
             return;
        }

        // Process the loaded JSON
        ProcessItemIndexJson(indexJson, sw);
    }
    
    /// <summary>
    /// WebGL-specific loading using UnityWebRequest
    /// </summary>
    private IEnumerator LoadItemIndexWithWebRequest(string indexFilePath)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        
        Debug.Log($"[Collection:{Id}] Using WebRequest to load items index (WebGL mode)");
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(indexFilePath))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Collection:{Id}] Items index not found at path: {indexFilePath}. Collection will have no items.");
                Debug.LogWarning($"[Collection:{Id}] WebRequest error: {www.error}");
                yield break;
            }
            
            string indexJson = www.downloadHandler.text;
            Debug.Log($"[Collection:{Id}] File read via WebRequest completed in {sw.ElapsedMilliseconds}ms");
            
            // Process the loaded JSON
            ProcessItemIndexJson(indexJson, sw);
        }
    }
    
    /// <summary>
    /// Process item index JSON content
    /// </summary>
    private void ProcessItemIndexJson(string indexJson, System.Diagnostics.Stopwatch sw)
    {
        bool verbose = Brewster.Instance?.verbose ?? false;
        
        if (verbose)
        {
            Debug.Log($"[Collection:{Id}] Items index JSON loaded. Length: {indexJson.Length}, Preview: {Truncate(indexJson, 100)}");
            Debug.Log($"[Collection:{Id}] First 20 characters: '{Truncate(indexJson, 20).Replace("\n", "\\n").Replace("\r", "\\r")}'");
            
            // Check if the JSON starts with [ which indicates an array
            if (indexJson.Length > 0) 
            {
                string trimmed = indexJson.TrimStart();
                Debug.Log($"[Collection:{Id}] JSON starts with: '{trimmed[0]}' (Expected '[' for array)");
                if (trimmed[0] != '[')
                {
                    Debug.LogWarning($"[Collection:{Id}] JSON does not appear to start with an array bracket. First char: '{trimmed[0]}'");
                }
            }
        }

        // Try to parse the items index
        try
        {
            // Validate we have non-empty JSON
            if (string.IsNullOrWhiteSpace(indexJson))
            {
                throw new JsonException("Items index JSON is empty or whitespace");
            }
            
            Debug.Log($"[Collection:{Id}] Parsing JSON to JToken...");
            Newtonsoft.Json.Linq.JToken token = null;
            try {
                token = Newtonsoft.Json.Linq.JToken.Parse(indexJson);
                Debug.Log($"[Collection:{Id}] JToken.Parse completed in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception parseEx) {
                Debug.LogError($"[Collection:{Id}] JToken.Parse failed with exception: {parseEx.Message}");
                throw; // Re-throw to be caught by outer catch block
            }
            
            Debug.Log($"[Collection:{Id}] JSON successfully parsed to JToken of type: {token.Type}");
            
            // Check that we have a JArray
            Newtonsoft.Json.Linq.JArray array = null;
            if (token is Newtonsoft.Json.Linq.JArray jArray)
            {
                array = jArray;
                Debug.Log($"[Collection:{Id}] JArray found with {array.Count} elements");
            }
            else
            {
                Debug.LogError($"[Collection:{Id}] Expected array of item IDs, but found {token.Type}. Token value: {token}");
                return;
            }
            
            // Special case for empty array
            if (array.Count == 0)
            {
                Debug.LogWarning($"[Collection:{Id}] Empty array found in items-index.json - collection will have no items");
                _itemIds = new List<string>();
                return;
            }
            
            // Log some details about the first few elements (only if verbose)
            if (verbose)
            {
                // Show first 3 elements as sample
                int sampleSize = Mathf.Min(3, array.Count);
                for (int i = 0; i < sampleSize; i++)
                {
                    var elem = array[i];
                    Debug.Log($"[Collection:{Id}] Sample element {i}: Type={elem.Type}, Value={elem}");
                }
            }
            
            // Process the array of item IDs (should be an array of strings)
            List<string> parsedIds = new List<string>();
            int nonStringTokens = 0;
            
            foreach (var token2 in array)
            {
                // Only accept string tokens, warn about others
                if (token2.Type == JTokenType.String)
                {
                    string id = token2.Value<string>();
                    parsedIds.Add(id);
                }
                else
                {
                    nonStringTokens++;
                }
            }
            
            // Warn about non-string tokens if any were found
            if (nonStringTokens > 0)
            {
                Debug.LogWarning($"[Collection:{Id}] Found and skipped {nonStringTokens} non-string tokens in array");
            }
            
            Debug.Log($"[Collection:{Id}] JToken parsing complete in {sw.ElapsedMilliseconds}ms. Found {parsedIds.Count} valid item IDs out of {array.Count} total elements");
            
            // Assign the results
            _itemIds = parsedIds;
        }
        catch (System.Exception e) 
        {
             Debug.LogError($"[Collection:{Id}] Failed to parse items index JSON: {e.Message}");
             if (e.InnerException != null)
             {
                 Debug.LogError($"[Collection:{Id}] Inner exception: {e.InnerException.Message}");
             }
             Debug.LogError($"[Collection:{Id}] Exception type: {e.GetType().Name}");
             Debug.LogError($"[Collection:{Id}] Stack trace: {e.StackTrace}");
             return;
        }

        Debug.Log($"[Collection:{Id}] Successfully loaded item index: {_itemIds.Count} item IDs found.");
        Debug.Log($"[Collection:{Id}] === COMPLETED ITEM INDEX LOAD ===");
    }

    /// <summary>
    /// Helper to truncate strings for logging.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Gets a specific item by ID using the lazy-loading Items property.
    /// </summary>
    public Item GetItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;
        
        // Use Linq on the Items property (which uses the registry)
        return this.Items.FirstOrDefault(item => item.Id == itemId);
    }
} 