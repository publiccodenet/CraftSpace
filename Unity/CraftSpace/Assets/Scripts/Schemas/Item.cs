//------------------------------------------------------------------------------
// <file_path>Unity/CraftSpace/Assets/Scripts/Schemas/Item.cs</file_path>
// <namespace>CraftSpace</namespace>
// <assembly>Assembly-CSharp</assembly>
//
// IMPORTANT: This is a MANUAL EXTENSION of a generated schema class.
// DO NOT DELETE this file - it extends the generated ItemSchema.cs.
//
// WARNING: KEEP THIS CLASS THIN AND SIMPLE!
// DO NOT PUT FUNCTIONALITY HERE THAT SHOULD GO IN THE SHARED BASE CLASS SchemaGeneratedObject.
// This class MUST agree with BOTH the SchemaGeneratedObject base class AND the generated ItemSchema.cs.
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
using System.Collections;

[Serializable]
public class Item : ItemSchema
{
    // Runtime-only state (not serialized)
    [NonSerialized] public Texture2D cover;
    // [NonSerialized] public Collection parentCollection; // REMOVED - Use ParentCollectionId and registry lookups
    [NonSerialized] private HashSet<IItemView> registeredViews = new HashSet<IItemView>();
    
    /// <summary>
    /// The ID of the collection this item belongs to.
    /// Set by the Brewster registry when the item is loaded via GetItem.
    /// </summary>
    public string ParentCollectionId { get; set; }
    
    public void RegisterView(IItemView view)
    {
        if (view != null && !registeredViews.Contains(view))
        {
            registeredViews.Add(view);
            Debug.Log($"[Item] Registered view for {Title}");
        }
    }
    
    public void UnregisterView(IItemView view)
    {
        if (view != null && registeredViews.Remove(view))
        {
            Debug.Log($"[Item] Unregistered view for {Title}. Remaining views: {registeredViews.Count}");
            
            // If this was the last view, cleanup the texture to avoid memory leaks
            if (registeredViews.Count == 0 && cover != null)
            {
                Debug.Log($"[Item/MEMORY] No more views registered for {Title}, destroying cover texture");
                UnityEngine.Object.Destroy(cover);
                cover = null;
            }
        }
    }
    
    public void NotifyViewsOfUpdate()
    {
        Debug.Log($"[Item] Notifying {registeredViews.Count} views of update for {Title}");
        
        foreach (var view in registeredViews)
        {
            try
            {
                view.OnItemUpdated(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Item] Error notifying view: {ex.Message}");
            }
        }
    }
    
    public static new Item CreateInstance<T>() where T : Item
    {
        return ScriptableObject.CreateInstance<T>();
    }
    
    public static new Item CreateInstance(Type type)
    {
        return ScriptableObject.CreateInstance(type) as Item;
    }

    /// <summary>
    /// Static factory method to create and populate an Item from JSON.
    /// Handles ScriptableObject creation and calls the instance import method.
    /// </summary>
    public static Item FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[Item] Cannot create Item from null or empty JSON.");
            return null;
        }

        // Log JSON content for debugging (truncated to avoid overwhelming logs)
        // Debug.Log("[Item] FromJson received JSON: " + (json.Length > 150 ? json.Substring(0, 150) + "..." : json));

        Item instance = ScriptableObject.CreateInstance<Item>();
        try
        {
            // Import data using the base class method
            instance.ImportFromJson(json); 

            // Validate critical fields post-import
             if (string.IsNullOrEmpty(instance.Id))
             {
                 Debug.LogError("[Item] Created Item is missing required 'id' field after import.");
                 ScriptableObject.DestroyImmediate(instance); // Clean up invalid instance
                 return null;
             }

            // Set ScriptableObject name for Unity Editor identification - REMOVED, handled by base SetUnityObjectName
            // if (!string.IsNullOrEmpty(instance.Title))
            // {
            //     instance.name = instance.Title;
            // }
            // else
            // {
            //     instance.name = "Item_" + instance.Id;
            // }

            // ParentCollectionId is set externally by Collection.LoadItems
            // instance.parentCollection = null; // Ensure not set here

            // Debug.Log($"[Item] Successfully created and imported Item: {instance.name} (ID: {instance.Id})");
            return instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Item] Failed to import Item from JSON: {e.Message}\nJSON Preview: {(json.Length > 200 ? json.Substring(0, 200) + "..." : json)}");
            if (instance != null) ScriptableObject.DestroyImmediate(instance); // Clean up failed instance
            return null;
        }
    }

    // Mark FromJsonString as Obsolete and comment out its body or remove it
    [Obsolete("Use Item.FromJson instead.")]
    public static Item FromJsonString(string json)
    {
        // Body commented out or removed - redirects to new method if needed
        Debug.LogWarning("[Item] FromJsonString is obsolete. Use FromJson instead.");
        // return FromJson(json); // Optional redirect
        return null; // Or throw NotImplementedException
    }
    
    /// <summary>
    /// Loads the cover image for this item.
    /// </summary>
    public void LoadCoverImage()
    {
        // Ensure ParentCollectionId is set before trying to load
        if (string.IsNullOrEmpty(ParentCollectionId))
        {
            Debug.LogWarning($"[Item:{Id}] Cannot load cover image because ParentCollectionId is not set.");
            return;
        }
        
        if (string.IsNullOrEmpty(CoverImage)) // CoverImage is the *filename* from the schema
        {
            // Debug.Log($"[Item:{Id}] No cover image filename specified in schema.");
            return; 
        }
        
        // Construct path using ParentCollectionId and the item's own ID
        // According to README.md structure: collections/collection_id/items/item_id/
        string coverImagePath = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "Content",                     // Assuming Brewster.baseResourcePath is "Content"
            "collections",
            ParentCollectionId,            // Collection directory
            "items",                       // Items directory
            Id,                            // Item's directory
            CoverImage                     // The actual image filename from schema
        );
        
        // For debugging purposes
        Debug.Log($"[Item:{Id}] Attempting to load cover image from path: {coverImagePath}");
        
        // For WebGL, we need to use a coroutine with UnityWebRequest
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Find a MonoBehaviour to start the coroutine
            if (Brewster.Instance != null)
            {
                Brewster.Instance.StartCoroutine(LoadCoverImageWithWebRequest(coverImagePath));
            }
            else
            {
                Debug.LogError($"[Item:{Id}] Cannot start WebRequest coroutine - Brewster.Instance is null");
            }
            return;
        }
        
        // Standard file loading for non-WebGL platforms
        try
        {
            if (System.IO.File.Exists(coverImagePath))
            {
                // Read all bytes from the file
                byte[] imageData = System.IO.File.ReadAllBytes(coverImagePath);
                
                // Create a new texture and load the image data
                Texture2D texture = new Texture2D(2, 2); // Size will be replaced on load
                texture.LoadImage(imageData);
                
                // Set the texture as the CoverTexture
                cover = texture;
                
                Debug.Log($"[Item:{Id}] Successfully loaded cover image: {coverImagePath}");
                
                // Notify all registered views of the update
                NotifyViewsOfUpdate();
            }
            else
            {
                Debug.LogWarning($"[Item:{Id}] Cover image file not found: {coverImagePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Item:{Id}] Error loading cover image: {e.Message}");
        }
    }
    
    /// <summary>
    /// WebGL-specific method to load cover image using UnityWebRequest
    /// </summary>
    private IEnumerator LoadCoverImageWithWebRequest(string coverImagePath)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(coverImagePath))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Item:{Id}] Cover image file not found: {coverImagePath}");
                Debug.LogWarning($"[Item:{Id}] WebRequest error: {www.error}");
                yield break;
            }
            
            // Get texture from response
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
            
            // Set the texture
            cover = texture;
            
            Debug.Log($"[Item:{Id}] Successfully loaded cover image via WebRequest: {coverImagePath}");
            
            // Notify all registered views of the update
            NotifyViewsOfUpdate();
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
            Debug.LogError($"[Item] Error in ToJsonString: {e.Message}");
            return "{}";
        }
    }

    // Add an explicit cleanup method for when items are unloaded
    public void Cleanup()
    {
        Debug.Log($"[Item/MEMORY] Cleaning up item {Id} - {Title}");
        
        // Clean up texture
        if (cover != null)
        {
            Debug.Log($"[Item/MEMORY] Destroying cover texture for {Id}");
            UnityEngine.Object.Destroy(cover);
            cover = null;
        }
        
        // Clear registered views
        registeredViews.Clear();
    }
} 