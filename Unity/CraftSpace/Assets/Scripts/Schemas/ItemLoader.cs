//------------------------------------------------------------------------------
// <file_path>Unity/CraftSpace/Assets/Scripts/Schemas/ItemLoader.cs</file_path>
// <namespace>CraftSpace</namespace>
// <assembly>Assembly-CSharp</assembly>
//
// IMPORTANT: This is a MANUAL helper class for loading Item instances.
// It is NOT generated and should be maintained manually.
//
// Full absolute path: /Users/a2deh/GroundUp/SpaceCraft/CraftSpace/Unity/CraftSpace/Assets/Scripts/Schemas/ItemLoader.cs
//------------------------------------------------------------------------------

using UnityEngine;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ItemLoader : MonoBehaviour
{
    // Item metadata properties
    private string _collectionId;
    private string _itemId;
    
    // Properties for accessing metadata
    public string CollectionId => _collectionId;
    public string ItemId => _itemId;
    public string Title { get; private set; }
    public string ObjectId { get => _itemId; private set => _itemId = value; }
    
    // Synchronous wrapper for LoadItemModelAsync to be used by ItemManager
    public GameObject LoadItemModel(string collectionId, string itemId)
    {
        GameObject result = null;
        StartCoroutine(LoadItemModelAsync(collectionId, itemId, (go) => {
            result = go;
        }));
        return result;
    }
    
    public IEnumerator LoadItemModelAsync(string collectionId, string itemId, System.Action<GameObject> onComplete)
    {
        // Store item metadata
        _collectionId = collectionId;
        _itemId = itemId;
        ObjectId = itemId;
        
        string contentBase = Path.Combine(Application.streamingAssetsPath, "Content");
        string itemPath = Path.Combine(contentBase, "collections", collectionId, "items", itemId);
        
        bool success = true;
        GameObject itemObject = null;
        Exception caughtException = null;
        
        // Create a book-like object to represent the item
        itemObject = new GameObject($"Item_{itemId}");
        itemObject.transform.parent = transform;
        itemObject.transform.localPosition = Vector3.zero;
        
        // Add components
        MeshFilter meshFilter = itemObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = itemObject.AddComponent<MeshRenderer>();
        
        // Add an ItemView to get access to the loading material
        ItemView itemView = itemObject.AddComponent<ItemView>();
            
        // Set up the mesh for the item
        meshFilter.mesh = CreateBookCoverMesh();
        
        // Try to load the cover.jpg
        string coverPath = Path.Combine(itemPath, "cover.jpg");
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + coverPath))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Texture2D coverTexture = DownloadHandlerTexture.GetContent(request);
                    if (coverTexture != null && itemView.LoadingMaterial != null)
                    {
                        Material materialInstance = new Material(itemView.LoadingMaterial);
                        materialInstance.mainTexture = coverTexture;
                        meshRenderer.material = materialInstance;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ItemLoader] Error processing cover texture: {ex.Message}");
                    success = false;
                    caughtException = ex;
                    
                    if (itemView.LoadingMaterial != null)
                    {
                        meshRenderer.material = itemView.LoadingMaterial;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[ItemLoader] Failed to load cover image from {coverPath}");
                
                if (itemView.LoadingMaterial != null)
                {
                    meshRenderer.material = itemView.LoadingMaterial;
                }
            }
        }
        
        // Load metadata from item.json
        if (success)
        {
            string itemJsonPath = Path.Combine(itemPath, "item.json");
            using (UnityWebRequest request = UnityWebRequest.Get("file://" + itemJsonPath))
            {
                yield return request.SendWebRequest();
                
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string itemJson = request.downloadHandler.text;
                        Item item = LoadFromJson<Item>(itemJson, itemId);
                        if (item != null)
                        {
                            Title = item.Title;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemLoader] Failed to load item JSON from {itemJsonPath}");
                        Title = itemId; // Default to ID if we can't load JSON
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ItemLoader] Error loading item JSON: {ex.Message}");
                    success = false;
                    caughtException = ex;
                    Title = itemId;
                }
            }
        }
        
        // Add this ItemLoader component to the object for reference
        ItemLoader loader = itemObject.AddComponent<ItemLoader>();
        loader.Title = Title;
        loader._itemId = _itemId;
        loader._collectionId = _collectionId;
        
        if (success)
        {
            onComplete?.Invoke(itemObject);
        }
        else
        {
            Debug.LogError($"[ItemLoader] Error loading item {itemId}: {caughtException?.Message}");
            GameObject errorObject = CreateErrorObject(Vector3.zero, $"Error: {itemId}");
            onComplete?.Invoke(errorObject);
            
            // Clean up the failed item object
            if (itemObject != null)
            {
                Destroy(itemObject);
            }
        }
    }
    
    // Helper method to load objects from JSON
    private T LoadFromJson<T>(string json, string id) where T : class
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ItemLoader] Error deserializing {typeof(T).Name} from JSON: {ex.Message}");
            return null;
        }
    }
    
    // Create a basic book cover mesh
    public static Mesh CreateBookCoverMesh()
    {
        Mesh mesh = new Mesh();
        
        // Book-like proportions
        float width = 0.7f;
        float height = 1.0f;
        float depth = 0.1f;
        
        // Create vertices for a book-shaped box
        Vector3[] vertices = new Vector3[24]
        {
            // Front face
            new Vector3(-width/2, -height/2, depth/2),
            new Vector3(width/2, -height/2, depth/2),
            new Vector3(width/2, height/2, depth/2),
            new Vector3(-width/2, height/2, depth/2),
            
            // Back face
            new Vector3(-width/2, -height/2, -depth/2),
            new Vector3(width/2, -height/2, -depth/2),
            new Vector3(width/2, height/2, -depth/2),
            new Vector3(-width/2, height/2, -depth/2),
            
            // Top face
            new Vector3(-width/2, height/2, -depth/2),
            new Vector3(width/2, height/2, -depth/2),
            new Vector3(width/2, height/2, depth/2),
            new Vector3(-width/2, height/2, depth/2),
            
            // Bottom face
            new Vector3(-width/2, -height/2, -depth/2),
            new Vector3(width/2, -height/2, -depth/2),
            new Vector3(width/2, -height/2, depth/2),
            new Vector3(-width/2, -height/2, depth/2),
            
            // Left face
            new Vector3(-width/2, -height/2, -depth/2),
            new Vector3(-width/2, -height/2, depth/2),
            new Vector3(-width/2, height/2, depth/2),
            new Vector3(-width/2, height/2, -depth/2),
            
            // Right face
            new Vector3(width/2, -height/2, -depth/2),
            new Vector3(width/2, -height/2, depth/2),
            new Vector3(width/2, height/2, depth/2),
            new Vector3(width/2, height/2, -depth/2)
        };
        
        // Create triangles
        int[] triangles = new int[36]
        {
            // Front face
            0, 1, 2,
            0, 2, 3,
            
            // Back face
            5, 4, 7,
            5, 7, 6,
            
            // Top face
            8, 9, 10,
            8, 10, 11,
            
            // Bottom face
            13, 12, 15,
            13, 15, 14,
            
            // Left face
            16, 17, 18,
            16, 18, 19,
            
            // Right face
            21, 20, 23,
            21, 23, 22
        };
        
        // Create UVs (simple mapping)
        Vector2[] uv = new Vector2[24];
        for (int i = 0; i < 24; i += 4)
        {
            uv[i] = new Vector2(0, 0);
            uv[i + 1] = new Vector2(1, 0);
            uv[i + 2] = new Vector2(1, 1);
            uv[i + 3] = new Vector2(0, 1);
        }
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        
        // Calculate normals
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    // Helper method for other scripts to get bounds info for positioning UI elements
    public Bounds GetItemBounds()
    {
        Renderer renderer = GetComponent<Renderer>();
        return renderer != null ? renderer.bounds : new Bounds(transform.position, Vector3.one);
    }
    
    private GameObject CreateErrorObject(Vector3 position, string errorMessage)
    {
        // Create a simple object with our book cover mesh
        GameObject errorObj = new GameObject($"Error_{errorMessage}");
        errorObj.transform.position = position;
        
        // Add components
        MeshFilter meshFilter = errorObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = errorObj.AddComponent<MeshRenderer>();
        
        // Create book mesh
        meshFilter.mesh = CreateBookCoverMesh();
        
        // Add ItemView to get access to the loading material
        ItemView itemView = errorObj.AddComponent<ItemView>();
        
        // Use the loading material as-is without any modifications
        if (itemView.LoadingMaterial != null)
        {
            meshRenderer.material = itemView.LoadingMaterial;
        }
        
        // Store error metadata
        Title = errorMessage;
        
        return errorObj;
    }
} 