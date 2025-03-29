using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

[AddComponentMenu("Views/Item View")]
public class ItemView : MonoBehaviour, IModelView<Item>
{
    [Header("Model Reference")]
    [SerializeField] private Item _model;
    
    [Header("Renderer Settings")]
    [SerializeField] private bool _autoInitializeRenderers = true;
    [SerializeField] private float _closeDistance = 5f;
    [SerializeField] private float _mediumDistance = 20f;
    [SerializeField] private float _farDistance = 100f;
    
    [Header("UI References")]
    [SerializeField] private ItemLabel _itemLabel;
    
    [Header("Item Display")]
    [SerializeField] private float _itemWidth = 1.4f;
    [SerializeField] private float _itemHeight = 1.0f;

    [Header("Materials")]
    [SerializeField] private Material _loadingMaterial;
    
    [Header("Highlighting")]
    [SerializeField] private Material _highlightMaterial;
    private GameObject _highlightMesh;
    
    [Header("Highlight Margins")]
    [SerializeField] private float _highlightMarginTop = 0.3f;    // Extra space for title
    [SerializeField] private float _highlightMarginBottom = 0f;
    [SerializeField] private float _highlightMarginLeft = 0f;
    [SerializeField] private float _highlightMarginRight = 0f;
    
    // Tracked renderers
    private Dictionary<System.Type, BaseViewRenderer> _renderers = new Dictionary<System.Type, BaseViewRenderer>();
    private List<BaseViewRenderer> _activeRenderers = new List<BaseViewRenderer>();
    
    // Distance tracking
    private Camera _mainCamera;
    private float _lastDistanceCheck = 0f;
    private const float DISTANCE_CHECK_INTERVAL = 0.5f;
    
    // Property to get/set the model (implementing IModelView)
    public Item Model 
    {
        get => _model;
        set => SetModel(value);
    }
    
    // For convenience, add an Item property that maps to Model
    public Item Item => _model;
    
    // Event for notifying renderers of model updates
    public delegate void ModelUpdatedHandler();
    public event ModelUpdatedHandler ModelUpdated;
    
    public CollectionView ParentCollectionView { get; set; }
    
    private Dictionary<System.Type, BaseViewRenderer> _closeRenderers = new Dictionary<System.Type, BaseViewRenderer>();
    
    [SerializeField] private UnityEvent<Item> _onItemChanged = new UnityEvent<Item>();
    
    // Reference to the loading material
    public Material LoadingMaterial => _loadingMaterial;
    
    // Unity event for item changes
    public UnityEvent<Item> OnItemChanged => _onItemChanged;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
        
        if (_model != null)
        {
            // Register with model on awake
            _model.RegisterView(this);
        }
        
        if (_autoInitializeRenderers)
        {
            InitializeDefaultRenderers();
        }
    }
    
    private void Update()
    {
        // Check distance to camera periodically to update renderers
        if (_mainCamera != null && Time.time > _lastDistanceCheck + DISTANCE_CHECK_INTERVAL)
        {
            _lastDistanceCheck = Time.time;
            UpdateRenderersBasedOnDistance();
        }
    }
    
    private void UpdateRenderersBasedOnDistance()
    {
        float distance = Vector3.Distance(transform.position, _mainCamera.transform.position);
        
        ShowRenderer<SingleImageRenderer>(true);
    }
    
    private void OnDestroy()
    {
        if (_model != null)
        {
            // Unregister when view is destroyed
            _model.UnregisterView(this);
        }
        
        // Clean up renderers
        DeactivateAllRenderers();
    }
    
    // Implement the IModelView interface method
    public void HandleModelUpdated()
    {
        // Call the original update view method
        UpdateView();
    }
    
    // Update view based on the current model
    public void UpdateView()
    {
        if (Item == null)
        {
            // No model, hide all renderers
            DeactivateAllRenderers();
            return;
        }
        
        // Update distance-based renderers
        UpdateBasedOnDistance();
        
        // Update all renderers with the model
        UpdateRenderers();
        
        // Notify subscribers that the model has been updated
        ModelUpdated?.Invoke();
    }
    
    // Update all renderers with the current model
    private void UpdateRenderers()
    {
        if (_model == null) return;
        
        foreach (var renderer in _renderers.Values)
        {
            if (renderer != null && renderer.IsActive)
            {
                renderer.UpdateWithModel(_model);
            }
        }
    }
    
    // Show renderers based on distance from camera
    private void UpdateBasedOnDistance()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        
        if (_mainCamera != null && Item != null)
        {
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            
            // Update renderers based on distance from camera
            foreach (var renderer in _renderers.Values)
            {
                if (renderer == null) continue;
                
                // Check if this is a close-range renderer
                bool isCloseRenderer = _closeRenderers.ContainsKey(renderer.GetType());
                
                if (isCloseRenderer)
                {
                    if (distance <= _closeDistance)
                    {
                        renderer.Activate();
                        if (!_activeRenderers.Contains(renderer))
                        {
                            _activeRenderers.Add(renderer);
                        }
                    }
                    else
                    {
                        renderer.Deactivate();
                        _activeRenderers.Remove(renderer);
                    }
                }
            }
            
            // Show highlight for closest items
            if (distance <= _closeDistance * 0.5f && Item != null && Item.IsFavorite)
            {
                var highlightRenderer = GetOrAddRenderer<HighlightParticleRenderer>();
                if (highlightRenderer != null)
                {
                    highlightRenderer.Activate();
                    highlightRenderer.UpdateWithModel(Item);
                }
            }
            else
            {
                var highlightRenderer = GetOrAddRenderer<HighlightParticleRenderer>();
                if (highlightRenderer != null)
                {
                    highlightRenderer.Deactivate();
                }
            }
        }
    }
    
    // Initialize default renderers
    private void InitializeDefaultRenderers()
    {
        // Add primary renderers but don't activate them yet
        GetOrAddRenderer<SingleImageRenderer>();
        GetOrAddRenderer<HighlightParticleRenderer>();
        
        // Initial distance check
        UpdateRenderersBasedOnDistance();
    }
    
    // Get or add a renderer component
    public T GetOrAddRenderer<T>() where T : BaseViewRenderer
    {
        System.Type type = typeof(T);
        
        // First check if we already have this renderer
        if (_renderers.ContainsKey(type))
        {
            return (T)_renderers[type];
        }
        
        // If not, try to get it from children
        T renderer = GetComponentInChildren<T>(true);
        
        // If not found, add it
        if (renderer == null)
        {
            GameObject rendererObj = new GameObject(type.Name);
            rendererObj.transform.SetParent(transform, false);
            renderer = rendererObj.AddComponent<T>();
        }
        
        // Register the renderer
        _renderers[type] = renderer;
        
        return renderer;
    }
    
    // Show or hide a specific renderer
    public T ShowRenderer<T>(bool show = true) where T : BaseViewRenderer
    {
        var renderer = GetOrAddRenderer<T>();
        if (renderer != null)
        {
            if (show)
            {
                renderer.Activate();
                renderer.UpdateWithModel(_model);
            }
            else
            {
                renderer.Deactivate();
            }
        }
        return renderer;
    }
    
    // Deactivate all renderers
    public void DeactivateAllRenderers()
    {
        foreach (var renderer in _activeRenderers)
        {
            if (renderer != null)
            {
                renderer.Deactivate();
            }
        }
        _activeRenderers.Clear();
    }

    public void SetModel(Item model)
    {
        if (model == null) 
        {
            _model = null;
            DeactivateAllRenderers();
            return;
        }
        
        // Store model reference
        _model = model;
        _model.RegisterView(this);
        
        // Log model assignment
        Debug.Log($"[ItemView] Model set for item view. Item ID: {_model.Id}, Collection ID: {_model.CollectionId}");
        
        // Update view with the new model
        UpdateView();
    }

    private bool IsVisible()
    {
        if (_mainCamera == null) return false;
        Vector3 screenPoint = _mainCamera.WorldToViewportPoint(transform.position);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    private string GetGameObjectPath()
    {
        string path = gameObject.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    private void CreateOrUpdateCoverMesh(float width, float height)
    {
        // Get or add mesh filter/renderer
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Create or update mesh
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = MeshGenerator.CreateFlatQuad(width, height);
        }
        else
        {
            MeshGenerator.ResizeQuadMesh(meshFilter.sharedMesh, width, height);
        }
    }

    private void UpdateCoverWithTexture(Texture2D texture)
    {
        if (texture == null)
            return;
        
        // Calculate max dimensions to fit within collider
        float maxWidth = 1.4f;  // Slightly smaller than collider
        float maxHeight = 1.0f; // Leave room for title
        
        // Calculate aspect ratio
        float aspectRatio = (float)texture.width / texture.height;
        
        float width, height;
        
        // Calculate dimensions to fit within max space while preserving aspect ratio
        if (aspectRatio >= 1f) // Landscape or square
        {
            width = maxWidth;
            height = width / aspectRatio;
            
            // Ensure height doesn't exceed max
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * aspectRatio;
            }
        }
        else // Portrait
        {
            height = maxHeight;
            width = height * aspectRatio;
            
            // Ensure width doesn't exceed max
            if (width > maxWidth)
            {
                width = maxWidth;
                height = width / aspectRatio;
            }
        }
        
        // Create/update mesh
        CreateOrUpdateCoverMesh(width, height);
        
        // Setup material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Ensure we have a material
            if (renderer.material == null)
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Apply texture
            renderer.material.mainTexture = texture;
        }
        
        // Update UI (including title positioning)
        SetupItemUI();
    }

    // Add this method to handle UI setup for the item
    private void SetupItemUI()
    {
        if (Item == null) return;

        // If we don't have a texture yet, create a standard book-shaped mesh
        if (GetComponent<MeshRenderer>()?.material?.mainTexture == null)
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;

            // Get collider dimensions
            float colliderWidth = boxCollider.size.x;
            float colliderHeight = boxCollider.size.z;  // Using Z since we're flat on the ground

            // Use standard book aspect ratio for placeholder (2:3)
            float defaultAspect = 2f/3f;  // width:height ratio

            // Calculate dimensions to fill collider while maintaining aspect ratio
            float width, height;
            
            // Since defaultAspect is < 1.0 (tall book), we'll use full height
            height = colliderHeight;
            width = height * defaultAspect;

            // Create/update mesh
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            
            if (meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = MeshGenerator.CreateCoverMesh(width, height);
            }
            else
            {
                MeshGenerator.ResizeQuadMesh(meshFilter.sharedMesh, width, height);
            }
            
            // Create a simple unlit material for the placeholder
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Unlit/Texture"));
                if (_loadingMaterial != null)
                {
                    renderer.material = _loadingMaterial;
                }
                else
                {
                    renderer.material = material;
                }
            }
        }
        
        // Set the label text
        if (_itemLabel != null)
        {
            _itemLabel.SetText(Item.Title);
        }
    }

    // Update this method to use SetText instead of Configure
    public void SetLabelText(string text)
    {
        if (_itemLabel != null)
        {
            _itemLabel.SetText(text);
        }
    }

    // Method to generate the correct thumbnail URL for an item
    private string GetItemThumbnailUrl(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return string.Empty;
            
        // Don't include file extension - Unity will find the right asset type
        return $"Content/collections/{Item.collectionId}/items/{itemId}/cover";
    }

    // Method to load an image
    public void LoadItemImage()
    {
        if (Item == null || string.IsNullOrEmpty(Item.Id))
            return;

        // Create initial mesh and renderer
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<MeshRenderer>();

        // Set initial loading state with proper book cover proportions
        if (_loadingMaterial != null)
        {
            renderer.material = _loadingMaterial;
        }

        // Create standard book cover mesh for loading state
        float loadingWidth = _itemWidth;
        float loadingHeight = _itemWidth * 1.5f; // Book cover aspect ratio
        if (loadingHeight > _itemHeight)
        {
            loadingHeight = _itemHeight;
            loadingWidth = loadingHeight / 1.5f;
        }
        CreateOrUpdateCoverMesh(loadingWidth, loadingHeight);

        // Load texture from Resources
        string resourcePath = GetItemThumbnailUrl(Item.Id);
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            ApplyLoadedTexture(texture);
        }
        else
        {
            Debug.LogWarning($"Failed to load texture from Resources: {resourcePath}");
        }
    }

    private void ApplyLoadedTexture(Texture2D texture)
    {
        if (texture == null) return;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) return;

        // Get collider dimensions
        float colliderWidth = boxCollider.size.x;
        float colliderHeight = boxCollider.size.z;  // This is our height in XZ plane

        // Get actual texture aspect ratio (width/height)
        float textureAspect = (float)texture.width / texture.height;
        
        float width, height;
        
        // Scale by the LONGEST dimension first, then let the other one be proportionally smaller
        if (textureAspect >= 1.0f)  // Width is longest dimension
        {
            width = colliderWidth;  // Fill width
            height = width / textureAspect;  // Height will be smaller, creating gaps top/bottom
        }
        else  // Height is longest dimension
        {
            height = colliderHeight;  // Fill height
            width = height * textureAspect;  // Width will be smaller, creating gaps left/right
        }

        // Create/update mesh with these dimensions
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = MeshGenerator.CreateCoverMesh(width, height);
        }
        else
        {
            MeshGenerator.ResizeQuadMesh(meshFilter.sharedMesh, width, height);
        }

        // Set up material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = texture;
            renderer.material = material;
        }
    }

    private void CreateHighlightMesh()
    {
        if (_highlightMesh == null)
        {
            _highlightMesh = new GameObject("Highlight");
            _highlightMesh.transform.SetParent(transform);
            
            MeshFilter meshFilter = _highlightMesh.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _highlightMesh.AddComponent<MeshRenderer>();
            
            Mesh mesh = new Mesh();
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            
            // Get base size from collider
            float baseWidth = boxCollider != null ? boxCollider.size.x : 1f;
            float baseLength = boxCollider != null ? boxCollider.size.z : 1f;
            
            // Calculate asymmetric positions
            float left = -baseWidth/2 - _highlightMarginLeft;
            float right = baseWidth/2 + _highlightMarginRight;
            float bottom = -baseLength/2 - _highlightMarginBottom;
            float top = baseLength/2 + _highlightMarginTop;
            
            // Create vertices with asymmetric margins
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(left, 0, bottom),   // Bottom left
                new Vector3(right, 0, bottom),  // Bottom right
                new Vector3(left, 0, top),      // Top left
                new Vector3(right, 0, top)      // Top right
            };
            
            // UV coordinates
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
            
            meshFilter.mesh = mesh;
            meshRenderer.material = _highlightMaterial;
            
            _highlightMesh.transform.localPosition = new Vector3(0, -0.02f, 0);
            _highlightMesh.SetActive(false);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_highlightMesh == null)
        {
            CreateHighlightMesh();
        }
        _highlightMesh.SetActive(highlighted);
    }

    private void LogWithContext(string methodName, string message, Dictionary<string, object> context = null)
    {
        if (context == null) context = new Dictionary<string, object>();
        
        // Add common context data
        context["itemId"] = _model?.Id ?? "null";
        context["title"] = _model?.Title ?? "null";
        
        // Log with standard Unity debug
        Debug.Log($"[ItemView] {methodName}: {message} - Item ID: {context["itemId"]}, Title: {context["title"]}");
    }

    private void ShowAllRenderers(bool show)
    {
        foreach (var renderer in _renderers.Values)
        {
            if (renderer != null)
            {
                if (show)
                {
                    renderer.Activate();
                    renderer.UpdateWithModel(_model);
                }
                else
                {
                    renderer.Deactivate();
                }
            }
        }
    }

    // Register with the item when enabled
    protected virtual void OnEnable()
    {
        if (_model != null)
        {
            HandleModelUpdated();
        }
    }

    // Initialize with an item
    public void Initialize(Item item)
    {
        SetModel(item);
    }

    // Clear the item reference
    public void Clear()
    {
        SetModel(null);
    }
} 