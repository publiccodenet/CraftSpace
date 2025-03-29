using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders an item as a single image/texture (book cover, etc)
/// </summary>
[RequireComponent(typeof(ItemView))]
public class SingleImageRenderer : ItemViewRenderer
{
    [Header("Image Settings")]
    [SerializeField] private float _width = 1.0f;
    [SerializeField] private float _height = 1.5f;
    [SerializeField] private bool _preserveAspectRatio = true;
    [SerializeField] private float _billboardDistance = 20f;
    
    private GameObject _imageObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private ItemView _itemView;
    private Camera _mainCamera;
    [SerializeField] private Material _defaultMaterial;
    
    protected override void Awake()
    {
        base.Awake();
        _itemView = GetComponent<ItemView>();
        _mainCamera = Camera.main;
        
        CreateImageObject();
        
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
        
        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshFilter>();
        }
    }
    
    private void CreateImageObject()
    {
        // Create container for image
        _imageObject = new GameObject("Cover_Image");
        _imageObject.transform.SetParent(transform);
        _imageObject.transform.localPosition = Vector3.zero;
        
        // Add mesh components
        _meshFilter = _imageObject.AddComponent<MeshFilter>();
        _meshRenderer = _imageObject.AddComponent<MeshRenderer>();
        
        // Create simple quad mesh
        Mesh mesh = new Mesh();
        float halfWidth = _width * 0.5f;
        float halfHeight = _height * 0.5f;
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, -halfHeight, 0),
            new Vector3(-halfWidth, halfHeight, 0),
            new Vector3(halfWidth, halfHeight, 0)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        
        _meshFilter.mesh = mesh;
        
        // Create material
        _meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        
        // Initially hide
        _imageObject.SetActive(false);
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Auto-billboard when far away
        if (_mainCamera != null && _imageObject != null && _imageObject.activeSelf)
        {
            float distance = Vector3.Distance(transform.position, _mainCamera.transform.position);
            
            if (distance > _billboardDistance)
            {
                // Look at camera
                _imageObject.transform.rotation = Quaternion.LookRotation(
                    _imageObject.transform.position - _mainCamera.transform.position
                );
            }
        }
    }
    
    public override void Activate()
    {
        base.Activate();
        if (_imageObject != null)
        {
            _imageObject.SetActive(true);
        }
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        if (_imageObject != null)
        {
            _imageObject.SetActive(false);
        }
    }
    
    protected override void OnAlphaChanged(float alpha)
    {
        base.OnAlphaChanged(alpha);
        
        if (_meshRenderer != null)
        {
            Color color = _meshRenderer.material.color;
            color.a = alpha;
            _meshRenderer.material.color = color;
        }
    }
    
    public override void UpdateWithItemModel(Item item)
    {
        if (item == null) return;
        
        // Load the cover image if available
        if (!string.IsNullOrEmpty(item.CoverImage))
        {
            // Load texture from Resources or AssetBundle
            Texture2D texture = Resources.Load<Texture2D>(item.CoverImage);
            if (texture != null)
            {
                ApplyTexture(texture);
            }
            else if (_defaultMaterial != null)
            {
                _meshRenderer.material = _defaultMaterial;
            }
        }
    }
    
    private void ApplyTexture(Texture2D texture)
    {
        if (texture == null || _meshRenderer == null) return;
        
        // Create material if needed
        if (_meshRenderer.material == null)
        {
            _meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        
        // Apply texture
        _meshRenderer.material.mainTexture = texture;
        
        // Update the mesh based on texture dimensions
        CreateOrUpdateMesh(texture);
    }
    
    private void CreateOrUpdateMesh(Texture2D texture)
    {
        float aspectRatio = (float)texture.width / texture.height;
        float width, height;
        
        // Calculate dimensions while preserving aspect ratio
        if (aspectRatio >= 1.0f)
        {
            width = _width;
            height = width / aspectRatio;
        }
        else
        {
            height = _height;
            width = height * aspectRatio;
        }
        
        // Create mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = CreateQuadMesh(width, height);
        }
        else
        {
            UpdateQuadMesh(meshFilter.sharedMesh, width, height);
        }
    }
    
    private Mesh CreateQuadMesh(float width, float height)
    {
        Mesh mesh = new Mesh();
        
        // Vertices
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width/2, 0, -height/2),  // Bottom left
            new Vector3(width/2, 0, -height/2),   // Bottom right
            new Vector3(-width/2, 0, height/2),   // Top left
            new Vector3(width/2, 0, height/2)     // Top right
        };
        
        // UVs
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        // Triangles
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private void UpdateQuadMesh(Mesh mesh, float width, float height)
    {
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width/2, 0, -height/2),  // Bottom left
            new Vector3(width/2, 0, -height/2),   // Bottom right
            new Vector3(-width/2, 0, height/2),   // Top left
            new Vector3(width/2, 0, height/2)     // Top right
        };
        
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
} 