using UnityEngine;
using CraftSpace.Models.Schema.Generated;
using System.Collections.Generic;
using CraftSpace.Utils;
using Type = CraftSpace.Utils.LoggerWrapper.Type;
using TMPro;
using CraftSpace.Views;

public class ItemView : MonoBehaviour
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
    [SerializeField] private Texture2D _loadingTexture;
    [SerializeField] private Material _imageMaterial;
    
    // Tracked renderers
    private Dictionary<System.Type, BaseViewRenderer> _renderers = new Dictionary<System.Type, BaseViewRenderer>();
    private List<BaseViewRenderer> _activeRenderers = new List<BaseViewRenderer>();
    
    // Distance tracking
    private Camera _mainCamera;
    private float _lastDistanceCheck = 0f;
    private const float DISTANCE_CHECK_INTERVAL = 0.5f;
    
    // Property to get/set the model
    public Item Model 
    { 
        get { return _model; }
        set 
        { 
            if (_model != value)
            {
                // Unregister from old model
                if (_model != null)
                {
                    _model.UnregisterView(this);
                }
                
                _model = value;
                
                // Register with new model
                if (_model != null)
                {
                    _model.RegisterView(this);
                }
                
                // Update the view with the new model data
                UpdateView();
            }
        }
    }
    
    // Event for notifying renderers of model updates
    public delegate void ModelUpdatedHandler();
    public event ModelUpdatedHandler ModelUpdated;
    
    public CollectionView ParentCollectionView { get; set; }
    
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
        
        // Activate appropriate renderers based on distance
        if (distance <= _closeDistance)
        {
            // Close up - detailed view
            ShowRenderer<TextMetadataRenderer>(true);
            ShowRenderer<SingleImageRenderer>(true);
            ShowRenderer<PixelIconRenderer>(false);
            
            // Show highlight for closest items
            if (distance <= _closeDistance * 0.5f && _model != null && (_model.IsFavorite ?? false))
            {
                ShowRenderer<HighlightParticleRenderer>(true);
            }
            else
            {
                ShowRenderer<HighlightParticleRenderer>(false);
            }
        }
        else if (distance <= _mediumDistance)
        {
            // Medium distance - just image
            ShowRenderer<TextMetadataRenderer>(false);
            ShowRenderer<SingleImageRenderer>(true);
            ShowRenderer<PixelIconRenderer>(false);
            ShowRenderer<HighlightParticleRenderer>(false);
        }
        else if (distance <= _farDistance)
        {
            // Far distance - pixel icon
            ShowRenderer<TextMetadataRenderer>(false);
            ShowRenderer<SingleImageRenderer>(false);
            ShowRenderer<PixelIconRenderer>(true);
            ShowRenderer<HighlightParticleRenderer>(false);
        }
        else
        {
            // Very far away - disable all renderers or use simplest representation
            ShowRenderer<TextMetadataRenderer>(false);
            ShowRenderer<SingleImageRenderer>(false);
            ShowRenderer<PixelIconRenderer>(false);
            ShowRenderer<HighlightParticleRenderer>(false);
        }
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
    
    // Called by model when it's updated
    public virtual void HandleModelUpdated()
    {
        UpdateView();
    }
    
    // Update all active renderers
    protected virtual void UpdateView()
    {
        LoggerWrapper.ModelUpdated("ItemView", "UpdateView", "Item", new Dictionary<string, object> {
            { "itemId", _model?.Id ?? "null" },
            { "title", _model?.Title ?? "null" },
            { "activeRenderers", _activeRenderers.Count },
            { "viewName", gameObject.name }
        }, this.gameObject);
        
        // Base class just updates name for debugging
        if (_model != null)
        {
            gameObject.name = $"Item: {_model.Title}";
            
            // Update all renderers with new model data
            foreach (var renderer in _renderers.Values)
            {
                if (renderer != null)
                {
                    renderer.UpdateWithModel(_model);
                }
            }
            
            // Notify event subscribers
            ModelUpdated?.Invoke();
            
            LoggerWrapper.Success("ItemView", "UpdateView", $"{Type.VIEW}{Type.SUCCESS} View updated successfully", new Dictionary<string, object> {
                { "activeRenderers", _activeRenderers.Count },
                { "viewPosition", transform.position.ToString("F2") },
                { "isVisible", IsVisible() }
            }, this.gameObject);
            
            // Load image after model is set
            LoadItemImage();
            
            // Configure the item label
            if (_itemLabel != null && Model != null)
            {
                _itemLabel.SetText(Model.Title);
            }
        }
        else
        {
            gameObject.name = "Item: [No Model]";
            LoggerWrapper.Warning("ItemView", "UpdateView", $"{Type.MODEL}{Type.ERROR} Cannot update view, model is null", new Dictionary<string, object> {
                { "objectName", gameObject.name },
                { "objectPath", GetGameObjectPath() }
            }, this.gameObject);
        }
    }
    
    // Initialize default renderers
    private void InitializeDefaultRenderers()
    {
        // Add renderers but don't activate them yet - distance check will do that
        GetOrAddRenderer<TextMetadataRenderer>();
        GetOrAddRenderer<SingleImageRenderer>();
        GetOrAddRenderer<PixelIconRenderer>();
        GetOrAddRenderer<HighlightParticleRenderer>();
        
        // Initial distance check
        UpdateRenderersBasedOnDistance();
    }
    
    // Get or add a renderer component
    public T GetOrAddRenderer<T>() where T : BaseViewRenderer
    {
        System.Type rendererType = typeof(T);
        
        if (_renderers.TryGetValue(rendererType, out BaseViewRenderer existingRenderer))
        {
            return (T)existingRenderer;
        }
        
        // Add the renderer component if it doesn't exist
        T newRenderer = GetComponent<T>();
        if (newRenderer == null)
        {
            newRenderer = gameObject.AddComponent<T>();
        }
        
        _renderers[rendererType] = newRenderer;
        return newRenderer;
    }
    
    // Show or hide a specific renderer
    public void ShowRenderer<T>(bool show) where T : BaseViewRenderer
    {
        T renderer = GetOrAddRenderer<T>();
        
        if (show)
        {
            if (!_activeRenderers.Contains(renderer))
            {
                LoggerWrapper.Info("ItemView", "ShowRenderer", $"{Type.RENDER}{Type.CREATE} Activating renderer", new Dictionary<string, object> {
                    { "rendererType", typeof(T).Name },
                    { "itemId", _model?.Id ?? "null" },
                    { "distance", _mainCamera != null ? Vector3.Distance(transform.position, _mainCamera.transform.position).ToString("F2") : "unknown" }
                }, this.gameObject);
                _activeRenderers.Add(renderer);
                renderer.Activate();
                renderer.UpdateWithModel(_model);
            }
        }
        else
        {
            if (_activeRenderers.Contains(renderer))
            {
                LoggerWrapper.Info("ItemView", "ShowRenderer", $"{Type.RENDER}{Type.DELETE} Deactivating renderer", new Dictionary<string, object> {
                    { "rendererType", typeof(T).Name },
                    { "itemId", _model?.Id ?? "null" },
                    { "wasActive", renderer is BaseViewRenderer ? ((BaseViewRenderer)renderer).IsActive : false },
                    { "reason", "Distance or visibility change" }
                }, this.gameObject);
                _activeRenderers.Remove(renderer);
                renderer.Deactivate();
            }
        }
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
        Model = model;
        
        // Register with the model
        if (Model != null)
        {
            Model.RegisterView(this);
            // Ensure we have the collectionId
            if (Model.parentCollection != null && string.IsNullOrEmpty(Model.collectionId)) {
                Model.collectionId = Model.parentCollection.Id;
            }
        }
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
        
        // Create new mesh or update existing one
        if (meshFilter.sharedMesh == null)
        {
            Mesh newMesh = MeshGenerator.CreateFlatQuad(width, height);
            meshFilter.sharedMesh = newMesh;
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
        if (Model == null) return;

        // If we don't have a texture yet, create a standard book-shaped mesh
        if (GetComponent<MeshRenderer>()?.material?.mainTexture == null)
        {
            // Get a mesh filter or add one
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            
            // Use our helper method to create a standard book cover shape
            float maxWidth = _itemWidth * 0.85f;  // Leave some margin
            float maxHeight = _itemHeight * 0.85f; // Leave room for title
            meshFilter.sharedMesh = MeshGenerator.CreateStandardBookCoverMesh(maxWidth, maxHeight);
        }
        
        // Set the label text
        if (_itemLabel != null)
        {
            _itemLabel.SetText(Model.Title ?? "Unknown");
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
        return $"Content/collections/{Model.collectionId}/items/{itemId}/cover";
    }

    // Method to load an image
    public void LoadItemImage()
    {
        if (Model == null || string.IsNullOrEmpty(Model.Id))
            return;

        // Set initial loading state
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (_loadingMaterial != null)
            {
                renderer.material = _loadingMaterial;
            }
            else
            {
                Material loadingMat = new Material(Shader.Find("Unlit/Texture"));
                if (_loadingTexture != null)
                {
                    loadingMat.mainTexture = _loadingTexture;
                }
                else
                {
                    loadingMat.color = new Color(0.8f, 0.8f, 0.8f);
                }
                renderer.material = loadingMat;
            }
            
            // Create standard book cover mesh for loading state
            float loadingWidth = _itemWidth * 0.8f;
            float loadingHeight = loadingWidth * 1.5f;
            if (loadingHeight > _itemHeight)
            {
                loadingHeight = _itemHeight * 0.9f;
                loadingWidth = loadingHeight / 1.5f;
            }
            CreateOrUpdateCoverMesh(loadingWidth, loadingHeight);
        }

        // ONLY load from Resources, never from web
        string resourcePath = GetItemThumbnailUrl(Model.Id);
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
        if (texture == null)
        {
            Debug.LogWarning($"Null texture received for item {Model?.Id}");
            return;
        }
        
        // Calculate aspect ratio of the image
        float aspectRatio = (float)texture.width / texture.height;
        
        // Calculate dimensions to fit within item area while preserving aspect ratio
        float width, height;
        
        if (aspectRatio >= 1f) // Landscape or square
        {
            width = _itemWidth * 0.9f; // Slightly smaller than max width
            height = width / aspectRatio;
            
            // Ensure height doesn't exceed max
            if (height > _itemHeight * 0.9f)
            {
                height = _itemHeight * 0.9f;
                width = height * aspectRatio;
            }
        }
        else // Portrait (like book covers)
        {
            height = _itemHeight * 0.9f; // Slightly smaller than max height
            width = height * aspectRatio;
            
            // Ensure width doesn't exceed max
            if (width > _itemWidth * 0.9f)
            {
                width = _itemWidth * 0.9f;
                height = width / aspectRatio;
            }
        }
        
        // Log the dimensions for debugging
        Debug.Log($"Image dimensions for {Model?.Id}: {texture.width}x{texture.height}, " +
                  $"Aspect ratio: {aspectRatio}, Displayed at: {width}x{height}");
        
        // Resize mesh to match image aspect ratio
        CreateOrUpdateCoverMesh(width, height);
        
        // Apply texture to material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material instanceMaterial;
            
            // Use custom material if provided, otherwise create a new one with built-in shader
            if (_imageMaterial != null)
            {
                instanceMaterial = new Material(_imageMaterial);
            }
            else
            {
                // Use the built-in Unlit/Texture shader
                instanceMaterial = new Material(Shader.Find("Unlit/Texture"));
            }
            
            // Apply the downloaded texture
            instanceMaterial.mainTexture = texture;
            renderer.material = instanceMaterial;
        }
        
        // Update UI
        SetupItemUI();
    }
} 