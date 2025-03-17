using UnityEngine;
using CraftSpace.Models.Schema.Generated;

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
    
    protected override void Awake()
    {
        base.Awake();
        _itemView = GetComponent<ItemView>();
        _mainCamera = Camera.main;
        
        CreateImageObject();
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
    
    public override void UpdateWithItemModel(CraftSpace.Models.Schema.Generated.Item model)
    {
        if (model == null || _meshRenderer == null)
            return;
            
        if (model.cover != null)
        {
            // Apply texture
            _meshRenderer.material.mainTexture = model.cover;
            
            // Adjust aspect ratio if needed
            if (_preserveAspectRatio)
            {
                float texAspect = (float)model.cover.width / model.cover.height;
                float meshAspect = _width / _height;
                
                if (Mathf.Abs(texAspect - meshAspect) > 0.01f)
                {
                    // Update mesh to match texture aspect ratio
                    float halfWidth = _width * 0.5f;
                    float halfHeight = (_width / texAspect) * 0.5f;
                    
                    Vector3[] vertices = new Vector3[4]
                    {
                        new Vector3(-halfWidth, -halfHeight, 0),
                        new Vector3(halfWidth, -halfHeight, 0),
                        new Vector3(-halfWidth, halfHeight, 0),
                        new Vector3(halfWidth, halfHeight, 0)
                    };
                    
                    _meshFilter.mesh.vertices = vertices;
                    _meshFilter.mesh.RecalculateBounds();
                }
            }
        }
        else
        {
            // Apply default color
            _meshRenderer.material.mainTexture = null;
            
            // Use a color based on title
            if (!string.IsNullOrEmpty(model.Title))
            {
                char firstChar = !string.IsNullOrEmpty(model.Title) ? char.ToUpperInvariant(model.Title[0]) : 'A';
                float hue = (firstChar - 'A') / 26f;
                _meshRenderer.material.color = Color.HSVToRGB(hue, 0.7f, 0.8f);
            }
            else
            {
                _meshRenderer.material.color = Color.white;
            }
        }
    }
} 