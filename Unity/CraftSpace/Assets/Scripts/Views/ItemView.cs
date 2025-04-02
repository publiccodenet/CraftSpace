using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Displays a single Item model as a 3D object in the scene.
/// Features lazy loading of textures, LOD based on distance, and memory-efficient texture management.
/// Only loads cover images when the item is actually visible to the camera.
/// </summary>
[AddComponentMenu("Views/Item View")]
public class ItemView : MonoBehaviour, IModelView<Item>, IItemView
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
    
    // Cached component references
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private BoxCollider _boxCollider;
    
    // Tracked renderers - update to use the base non-generic type
    private Dictionary<System.Type, MonoBehaviour> _renderers = new Dictionary<System.Type, MonoBehaviour>();
    private List<MonoBehaviour> _activeRenderers = new List<MonoBehaviour>();
    
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
    
    private Dictionary<System.Type, MonoBehaviour> _closeRenderers = new Dictionary<System.Type, MonoBehaviour>();
    
    [SerializeField] private UnityEvent<Item> _onItemChanged = new UnityEvent<Item>();
    
    // Reference to the loading material
    public Material LoadingMaterial => _loadingMaterial;
    
    // Unity event for item changes
    public UnityEvent<Item> OnItemChanged => _onItemChanged;
    
    // Add a new method to report visibility changes
    private bool _wasVisibleLastFrame = false;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
        
        // Cache component references
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        _boxCollider = GetComponent<BoxCollider>();
        
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
    
    /// <summary>
    /// Simplified Update method that doesn't do periodic polling
    /// </summary>
    private void Update()
    {
        // If texture is loaded but not applied to renderer, apply it
        if (_model != null && _model.cover != null && 
            _meshRenderer != null && _meshRenderer.material != null && 
            _meshRenderer.material.mainTexture != _model.cover)
        {
            ApplyLoadedTexture(_model.cover);
        }
    }
    
    private void UpdateRenderersBasedOnDistance()
    {
        float distance = Vector3.Distance(transform.position, _mainCamera.transform.position);
        
        // Always use SingleImageRenderer, regardless of distance
        ShowRenderer<SingleImageRenderer>(true);
    }
    
    private void OnDestroy()
    {
        if (_model != null)
        {
            // Unregister when view is destroyed
            _model.UnregisterView(this);
            
            // If this is the last view for this item, we should consider cleaning up the texture
            // This is handled in the Item class's UnregisterView method
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
    
    // Implement the IItemView interface method
    public void OnItemUpdated(Item item)
    {
        try
        {
            // Verify this is our model
            if (_model != item)
            {
                Debug.LogWarning($"[ItemView] Received update for different item: {item?.Title ?? "null"}");
                return;
            }
            
            // Use existing handler for backward compatibility
            HandleModelUpdated();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ItemView] Error in OnItemUpdated: {ex.Message}");
        }
    }
    
    // Update view based on the current model
    public void UpdateView()
    {
        try
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
        catch (Exception ex)
        {
            Debug.LogError($"[ItemView] Error in UpdateView: {ex.Message}");
        }
    }
    
    // Update all renderers with the current model
    private void UpdateRenderers()
    {
        if (_model == null) return;
        
        foreach (var renderer in _renderers.Values)
        {
            var baseRenderer = renderer as BaseViewRenderer<Item>;
            if (baseRenderer != null && baseRenderer.IsActive)
            {
                baseRenderer.UpdateWithModel(_model);
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
            // Always show the SingleImageRenderer
            var imageRenderer = GetOrAddRenderer<SingleImageRenderer>();
            if (imageRenderer != null)
            {
                imageRenderer.Activate();
                if (!_activeRenderers.Contains(imageRenderer))
                {
                    _activeRenderers.Add(imageRenderer);
                }
                
                // Update the renderer with the current model
                imageRenderer.UpdateWithModel(Item);
            }
        }
    }
    
    // Initialize default renderers
    private void InitializeDefaultRenderers()
    {
        // Add only SingleImageRenderer but don't activate it yet
        GetOrAddRenderer<SingleImageRenderer>();
        
        // Initial distance check
        UpdateRenderersBasedOnDistance();
    }
    
    // Get or add a renderer component
    public T GetOrAddRenderer<T>() where T : MonoBehaviour
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
    public T ShowRenderer<T>(bool show = true) where T : MonoBehaviour
    {
        var renderer = GetOrAddRenderer<T>();
        if (renderer != null)
        {
            var baseRenderer = renderer as BaseViewRenderer<Item>;
            if (baseRenderer != null)
            {
                if (show)
                {
                    baseRenderer.Activate();
                    baseRenderer.UpdateWithModel(_model);
                }
                else
                {
                    baseRenderer.Deactivate();
                }
            }
        }
        return renderer;
    }
    
    // Deactivate all renderers
    public void DeactivateAllRenderers()
    {
        foreach (var renderer in _activeRenderers)
        {
            var baseRenderer = renderer as BaseViewRenderer<Item>;
            if (baseRenderer != null)
            {
                baseRenderer.Deactivate();
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
        
        Debug.Log($"[ItemView/SET_MODEL] *** Setting model: {_model.Id} - {_model.Title} ***");
        
        // Apply loading material
        if (_loadingMaterial != null)
        {
            _meshRenderer.material = _loadingMaterial;
            
            // Create standard book cover mesh for loading state
            float loadingWidth = _itemWidth;
            float loadingHeight = _itemWidth * 1.5f; // Book cover aspect ratio
            if (loadingHeight > _itemHeight)
            {
                loadingHeight = _itemHeight;
                loadingWidth = loadingHeight / 1.5f;
            }
            CreateOrUpdateCoverMesh(loadingWidth, loadingHeight);
        }
        else
        {
            // Create a fallback material if loading material is not set
            Material fallbackMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fallbackMaterial.color = new Color(0.8f, 0.8f, 0.8f); // Light gray
            _meshRenderer.material = fallbackMaterial;
            
            // Create standard book cover mesh for loading state
            float loadingWidth = _itemWidth;
            float loadingHeight = _itemWidth * 1.5f; // Book cover aspect ratio
            if (loadingHeight > _itemHeight)
            {
                loadingHeight = _itemHeight;
                loadingWidth = loadingHeight / 1.5f;
            }
            CreateOrUpdateCoverMesh(loadingWidth, loadingHeight);
        }
        
        // Set up label text immediately
        if (_itemLabel != null)
        {
            _itemLabel.SetText(_model.Title);
        }
        
        // IMMEDIATELY load cover image without any delay or distance checks
        Debug.Log($"[ItemView/SET_MODEL] Immediately loading cover image for {_model.Id}");
        LoadItemImage();
        
        // Update view with the new model
        UpdateView();
        
        // Trigger event for external listeners
        _onItemChanged.Invoke(_model);
    }

    // New method to force immediate loading without any delay or visibility check
    private void ForceLoadCoverImage()
    {
        if (_model == null || string.IsNullOrEmpty(_model.Id)) return;

        // Construct the direct file path
        string coverPath = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "Content",
            "collections",
            _model.ParentCollectionId,
            "items", 
            _model.Id,
            "cover.jpg"
        );

        // Check if file exists
        if (!System.IO.File.Exists(coverPath)) return;

        try
        {
            // Direct file loading - simple and reliable
            byte[] imageData = System.IO.File.ReadAllBytes(coverPath);
            
            // Create texture from bytes
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                // Cache the texture in the model
                _model.cover = texture;
                
                // Apply the texture to this view
                ApplyLoadedTexture(texture);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ItemView] Exception loading cover: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if this item is currently visible to the camera.
    /// For now, always returns true to ensure loading.
    /// </summary>
    private bool IsVisible()
    {
        return true;  // Always consider visible
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
        // Use cached mesh filter/renderer
        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"[ItemView] Invalid mesh dimensions: {width}x{height}");
            return;
        }
        
        // Create or update mesh
        if (_meshFilter.sharedMesh == null)
        {
            _meshFilter.sharedMesh = MeshGenerator.CreateFlatQuad(width, height);
        }
        else
        {
            MeshGenerator.ResizeQuadMesh(_meshFilter.sharedMesh, width, height);
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
        
        // Setup material - use cached renderer
        if (_meshRenderer != null)
        {
            // Ensure we have a material
            if (_meshRenderer.material == null)
                _meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Apply texture
            _meshRenderer.material.mainTexture = texture;
        }
        
        // Update UI (including title positioning)
        SetupItemUI();
    }

    // Add this method to handle UI setup for the item
    private void SetupItemUI()
    {
        if (Item == null) return;

        // If we don't have a texture yet, create a standard book-shaped mesh
        if (_meshRenderer?.material?.mainTexture == null)
        {
            // Use cached box collider if available
            if (_boxCollider == null) return;

            // Get collider dimensions
            float colliderWidth = _boxCollider.size.x;
            float colliderHeight = _boxCollider.size.z;  // Using Z since we're flat on the ground

            // Use standard book aspect ratio for placeholder (2:3)
            float defaultAspect = 2f/3f;  // width:height ratio

            // Calculate dimensions to fill collider while maintaining aspect ratio
            float width, height;
            
            // Since defaultAspect is < 1.0 (tall book), we'll use full height
            height = colliderHeight;
            width = height * defaultAspect;

            // Create/update mesh - use cached mesh filter
            if (_meshFilter.sharedMesh == null)
            {
                _meshFilter.sharedMesh = MeshGenerator.CreateCoverMesh(width, height);
            }
            else
            {
                MeshGenerator.ResizeQuadMesh(_meshFilter.sharedMesh, width, height);
            }
            
            // Create a simple unlit material for the placeholder
            if (_meshRenderer != null)
            {
                Material material = new Material(Shader.Find("Unlit/Texture"));
                if (_loadingMaterial != null)
                {
                    _meshRenderer.material = _loadingMaterial;
                }
                else
                {
                    _meshRenderer.material = material;
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
        return $"Content/collections/{Item.ParentCollectionId}/items/{itemId}/cover";
    }

    /// <summary>
    /// Loads the cover image for the item
    /// </summary>
    public void LoadItemImage()
    {
        if (Item == null || string.IsNullOrEmpty(Item.Id))
            return;

        // Use placeholder material while loading
        if (_loadingMaterial != null)
            _meshRenderer.material = _loadingMaterial;

        // Create standard book cover mesh
        CreateOrUpdateCoverMesh(_itemWidth, _itemHeight);

        // Check if cover image is already loaded in the Item
        if (Item.cover != null)
        {
            ApplyLoadedTexture(Item.cover);
            return;
        }

        // Construct the path to the cover image
        string coverPath = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "Content",
            "collections",
            Item.ParentCollectionId,
            "items",
            Item.Id,
            "cover.jpg"
        );
        
        // If the direct cover.jpg file doesn't exist, try to locate any image in the folder
        if (!System.IO.File.Exists(coverPath))
        {
            string itemFolder = System.IO.Path.GetDirectoryName(coverPath);
            
            if (System.IO.Directory.Exists(itemFolder))
            {
                // Get all image files using separate calls to avoid Concat issues
                List<string> imageFiles = new List<string>();
                imageFiles.AddRange(System.IO.Directory.GetFiles(itemFolder, "*.jpg"));
                imageFiles.AddRange(System.IO.Directory.GetFiles(itemFolder, "*.png"));
                imageFiles.AddRange(System.IO.Directory.GetFiles(itemFolder, "*.jpeg"));
                
                if (imageFiles.Count > 0)
                {
                    coverPath = imageFiles[0]; // Use the first image file found
                }
            }
        }
        
        if (!System.IO.File.Exists(coverPath))
            return;

        // FORCE IMMEDIATE LOAD FIRST - try to load without callback 
        try {
            byte[] imageData = System.IO.File.ReadAllBytes(coverPath);
            Texture2D directTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (directTexture.LoadImage(imageData)) {
                // Force-set the texture on the model
                Item.cover = directTexture;
                
                // Apply to the renderer
                ApplyLoadedTexture(directTexture);
                
                // NOTIFY ALL OTHER VIEWS that we've updated the model texture
                Item.NotifyViewsOfUpdate();
                
                // Done - direct loading was successful
                return;
            }
        } catch (Exception ex) {
            // Silently fall back to async
        }
        
        // If direct loading failed, fall back to the async version
        // Start the download/load process
        ItemImageLoader.StartDownload(
            Item.Id,
            coverPath,
            (texture) => {
                // Store the texture in the model for caching
                if (texture != null && Item != null)
                {
                    // FORCEFULLY set the texture on the model
                    Item.cover = texture;
                    
                    // Double check that Item.cover is set
                    if (Item.cover == null) {
                        Item.cover = texture; // Try again
                    }
                    
                    // Apply the texture to our material
                    ApplyLoadedTexture(texture);
                    
                    // Force update any other views
                    Item.NotifyViewsOfUpdate();
                }
            },
            true // Try immediate loading first
        );
    }

    private void ApplyLoadedTexture(Texture2D texture)
    {
        if (texture == null) return;
        if (_meshRenderer == null) return;

        // Create a new material with an unlit texture shader - fast and WebGL compatible
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null) 
        {
            shader = Shader.Find("Diffuse"); // Minimal fallback
        }
        
        if (shader == null)
        {
            Material errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            errorMaterial.color = Color.magenta;
            _meshRenderer.material = errorMaterial;
            return;
        }
        
        Material material = new Material(shader);
        material.mainTexture = texture;
        _meshRenderer.material = material;

        // Update the mesh to match the texture's aspect ratio
        float aspectRatio = (float)texture.width / texture.height;
        float coverWidth = _itemWidth;
        float coverHeight = coverWidth / aspectRatio;
        
        // If height exceeds maximum, scale width down
        if (coverHeight > _itemHeight)
        {
            coverHeight = _itemHeight;
            coverWidth = coverHeight * aspectRatio;
        }
        
        CreateOrUpdateCoverMesh(coverWidth, coverHeight);
        
        // Force a redraw of the renderer
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = false;
            _meshRenderer.enabled = true;
        }
    }

    /// <summary>
    /// Static helper class for loading item cover images asynchronously
    /// </summary>
    private static class ItemImageLoader
    {
        private static Dictionary<string, int> _activeDownloads = new Dictionary<string, int>();
        private static Dictionary<string, List<ImageLoadedCallback>> _callbacks = new Dictionary<string, List<ImageLoadedCallback>>();
        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        private static Dictionary<string, int> _referenceCount = new Dictionary<string, int>();
        
        private static Dictionary<string, ImageLoadRunner> _runnerLookup = new Dictionary<string, ImageLoadRunner>();
        
        // Callback definition
        public delegate void ImageLoadedCallback(Texture2D texture);
        
        /// <summary>
        /// Initiates an image download with multiple optimizations:
        /// 1. Prevents duplicate downloads for the same item
        /// 2. Tries immediate loading before async for faster response
        /// 3. Handles reference counting for memory management
        /// </summary>
        public static void StartDownload(string itemId, string imagePath, ImageLoadedCallback callback, bool checkImmediate = true)
        {
            // Register for reference counting
            RegisterTextureUser(itemId);
            
            // Add callback to notify when download completes
            if (!_callbacks.ContainsKey(itemId))
            {
                _callbacks[itemId] = new List<ImageLoadedCallback>();
            }
            _callbacks[itemId].Add(callback);
            
            // If already completed and cached, return immediately
            if (_textureCache.ContainsKey(itemId) && _textureCache[itemId] != null)
            {
                NotifyCallbacks(itemId, _textureCache[itemId]);
                return;
            }
            
            // If already downloading, just wait for completion
            if (_activeDownloads.ContainsKey(itemId) && _activeDownloads[itemId] > 0)
            {
                return;
            }
            
            // Start fresh download
            _activeDownloads[itemId] = 1;
            
            // Check if the file exists
            if (!System.IO.File.Exists(imagePath))
            {
                NotifyCallbacks(itemId, null);
                _activeDownloads.Remove(itemId);
                return;
            }
            
            // Try immediate loading if requested
            if (checkImmediate)
            {
                bool immediateSuccess = TryLoadImageImmediate(itemId, imagePath);
                if (immediateSuccess)
                {
                    _activeDownloads.Remove(itemId);
                    return;
                }
            }
            
            // Start the coroutine for async loading
            ImageLoadRunner runner = new ImageLoadRunner();
            _runnerLookup[itemId] = runner;
            runner.StartCoroutine(itemId, imagePath);
        }
        
        /// <summary>
        /// Try to load the image synchronously for faster loading
        /// </summary>
        private static bool TryLoadImageImmediate(string itemId, string imagePath)
        {
            try
            {
                // Load all bytes from the file
                byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
                
                // Create texture with explicit format for WebGL compatibility
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear; // Make it look nicer
                
                bool loadSuccess = texture.LoadImage(imageData);
                
                if (loadSuccess)
                {
                    // Apply mipmap generation for better quality at distance
                    texture.Apply(true, false);
                    
                    // Cache the loaded texture
                    _textureCache[itemId] = texture;
                    
                    // Try to find matching Item in the Brewster registry and set its cover
                    if (Brewster.Instance != null)
                    {
                        // Look up the item directly from Brewster
                        Item itemRef = Brewster.Instance.GetItem(null, itemId);
                        if (itemRef != null)
                        {
                            // Found the item, directly set its cover property
                            itemRef.cover = texture;
                            
                            // NOTIFY ALL VIEWS of the update to ensure everyone gets the texture
                            itemRef.NotifyViewsOfUpdate();
                        }
                    }
                    
                    // Notify callbacks 
                    NotifyCallbacks(itemId, texture);
                    return true;
                }
                return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        
        // Helper class to run coroutines for async loading
        private class ImageLoadRunner
        {
            private UnityEngine.Coroutine _coroutine;
            
            public void StartCoroutine(string itemId, string imagePath)
            {
                // Use the main MonoBehaviour to start our coroutine
                if (Brewster.Instance != null)
                {
                    _coroutine = Brewster.Instance.StartCoroutine(LoadImageAsync(itemId, imagePath, this));
                }
                else
                {
                    NotifyCallbacks(itemId, null);
                }
            }
        }
        
        // Coroutine for loading images
        private static System.Collections.IEnumerator LoadImageAsync(string itemId, string imagePath, ImageLoadRunner runner)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                NotifyCallbacks(itemId, null);
                yield break;
            }

            // Add a frame delay before starting to avoid freezing UI
            yield return null;
            
            // Load the file data
            byte[] imageData = null;
            
            try
            {
                imageData = System.IO.File.ReadAllBytes(imagePath);
            }
            catch (System.Exception)
            {
                NotifyCallbacks(itemId, null);
                _activeDownloads.Remove(itemId);
                _runnerLookup.Remove(itemId);
                yield break;
            }

            // Add another frame delay after loading the file
            yield return null;
            
            // Check file data
            if (imageData == null || imageData.Length == 0)
            {
                NotifyCallbacks(itemId, null);
                _activeDownloads.Remove(itemId);
                _runnerLookup.Remove(itemId);
                yield break;
            }
            
            // Create and process the texture
            Texture2D texture = null;
            bool success = false;
            
            try
            {
                // Create texture with explicit format for WebGL compatibility
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear; // Make it look nicer
                
                success = texture.LoadImage(imageData);
            }
            catch (System.Exception)
            {
                NotifyCallbacks(itemId, null);
                _activeDownloads.Remove(itemId);
                _runnerLookup.Remove(itemId);
                yield break;
            }
            
            // Process the result
            if (success)
            {
                // Apply mipmap generation for better quality at distance
                texture.Apply(true, false);
                
                // Cache result
                _textureCache[itemId] = texture;
                
                // Try to find matching Item in the Brewster registry and set its cover
                if (Brewster.Instance != null)
                {
                    // Look up the item directly from Brewster
                    Item itemRef = Brewster.Instance.GetItem(null, itemId);
                    if (itemRef != null)
                    {
                        // Found the item, directly set its cover property
                        itemRef.cover = texture;
                        
                        // NOTIFY ALL VIEWS of the update to ensure everyone gets the texture
                        itemRef.NotifyViewsOfUpdate();
                    }
                }
                
                // Notify listeners
                NotifyCallbacks(itemId, texture);
            }
            else
            {
                NotifyCallbacks(itemId, null);
            }
            
            // Clean up
            _activeDownloads.Remove(itemId);
            _runnerLookup.Remove(itemId);
            
            yield break;
        }
        
        /// <summary>
        /// Register a view with this model
        /// </summary>
        private static void RegisterTextureUser(string itemId)
        {
            if (!_referenceCount.ContainsKey(itemId))
            {
                _referenceCount[itemId] = 0;
            }
            
            _referenceCount[itemId]++;
        }
        
        /// <summary>
        /// Unregister a view from this model
        /// </summary>
        public static void UnregisterTextureUser(string itemId)
        {
            if (_referenceCount.ContainsKey(itemId))
            {
                _referenceCount[itemId]--;
                
                // If no more references, release the texture
                if (_referenceCount[itemId] <= 0)
                {
                    _referenceCount.Remove(itemId);
                    
                    // Clear the cache
                    if (_textureCache.ContainsKey(itemId))
                    {
                        Texture2D texture = _textureCache[itemId];
                        if (texture != null)
                        {
                            UnityEngine.Object.Destroy(texture);
                        }
                        
                        _textureCache.Remove(itemId);
                    }
                }
            }
        }
        
        /// <summary>
        /// Notify all waiting callbacks for an item
        /// </summary>
        private static void NotifyCallbacks(string itemId, Texture2D texture)
        {
            if (!_callbacks.ContainsKey(itemId))
            {
                return;
            }
            
            foreach (var callback in _callbacks[itemId])
            {
                try
                {
                    callback(texture);
                }
                catch (System.Exception) { }
            }
            
            // Clear callbacks after notification
            _callbacks.Remove(itemId);
        }
        
        /// <summary>
        /// Check if an image is currently being downloaded
        /// </summary>
        public static bool IsDownloading(string itemId)
        {
            return _activeDownloads.ContainsKey(itemId) && _activeDownloads[itemId] > 0;
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
            
            // Use cached box collider
            if (_boxCollider == null)
                _boxCollider = GetComponent<BoxCollider>();
            
            // Get base size from collider
            float baseWidth = _boxCollider != null ? _boxCollider.size.x : 1f;
            float baseLength = _boxCollider != null ? _boxCollider.size.z : 1f;
            
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
            var baseRenderer = renderer as BaseViewRenderer<Item>;
            if (baseRenderer != null)
            {
                if (show)
                {
                    baseRenderer.Activate();
                    baseRenderer.UpdateWithModel(_model);
                }
                else
                {
                    baseRenderer.Deactivate();
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
        Debug.Log($"[ItemView/INIT] Initializing with item: {item?.Id ?? "null"} - {item?.Title ?? "unknown"}");
        SetModel(item);
    }

    // Clear the item reference
    public void Clear()
    {
        SetModel(null);
    }

    // Add a new method to report visibility changes
    private void CheckVisibilityChanged()
    {
        bool isVisible = IsVisible();
        
        // Just track the state change without logging
        if (isVisible != _wasVisibleLastFrame)
        {
            _wasVisibleLastFrame = isVisible;
        }
    }

    // Helper method to check if a file exists
    private bool CheckFileWithDiagnostics(string path, string description)
    {
        return System.IO.File.Exists(path);
    }
} 