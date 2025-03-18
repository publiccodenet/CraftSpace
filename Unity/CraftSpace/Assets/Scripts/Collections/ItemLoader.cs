using UnityEngine;
using System;

public class ItemLoader : MonoBehaviour
{
    // Item metadata properties
    private string _itemTitle;
    private string _itemId;
    private string _collectionId;
    
    // Properties for accessing metadata
    public string ItemTitle => _itemTitle;
    public string ItemId => _itemId;
    public string CollectionId => _collectionId;
    
    public GameObject LoadItemModel(string collectionId, string itemId)
    {
        try
        {
            // Store item metadata
            _collectionId = collectionId;
            _itemId = itemId;
            
            string itemPath = $"Content/collections/{collectionId}/items/{itemId}";
            
            // Create a new game object
            GameObject itemObject = new GameObject($"Item_{itemId}");
            
            // Add a mesh filter and renderer
            MeshFilter meshFilter = itemObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = itemObject.AddComponent<MeshRenderer>();
            
            // Add the ItemView component to get access to the loading material
            CraftSpace.Collections.CollectionItemView itemView = itemObject.AddComponent<CraftSpace.Collections.CollectionItemView>();
            
            // Create the default book cover mesh since we don't load custom meshes
            meshFilter.mesh = CreateBookCoverMesh();
            
            // Try to load the cover.jpg
            Texture2D coverTexture = Resources.Load<Texture2D>($"{itemPath}/cover");
            if (coverTexture != null && itemView.LoadingMaterial != null)
            {
                Material materialInstance = new Material(itemView.LoadingMaterial);
                materialInstance.mainTexture = coverTexture;
                meshRenderer.material = materialInstance;
            }
            else if (itemView.LoadingMaterial != null)
            {
                meshRenderer.material = itemView.LoadingMaterial;
            }
            
            // Load metadata from item.json
            TextAsset itemJson = Resources.Load<TextAsset>($"{itemPath}/item");
            _itemTitle = itemId; // Default to ID if we can't parse the JSON
            if (itemJson != null)
            {
                // TODO: Parse the item.json to get the title
                // For now just using itemId as the title
            }
            
            // Add this ItemLoader component to the object for ItemView to access
            ItemLoader loader = itemObject.AddComponent<ItemLoader>();
            loader._itemTitle = _itemTitle;
            loader._itemId = _itemId;
            loader._collectionId = _collectionId;
            
            return itemObject;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading item {itemId}: {ex.Message}");
            return CreateErrorObject(Vector3.zero, $"Error: {itemId}");
        }
    }
    
    // Create a basic book cover mesh
    private Mesh CreateBookCoverMesh()
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
        CraftSpace.Collections.CollectionItemView itemView = errorObj.AddComponent<CraftSpace.Collections.CollectionItemView>();
        
        // Use the loading material as-is without any modifications
        if (itemView.LoadingMaterial != null)
        {
            meshRenderer.material = itemView.LoadingMaterial;
        }
        
        // Store error metadata
        _itemTitle = errorMessage;
        
        return errorObj;
    }
} 