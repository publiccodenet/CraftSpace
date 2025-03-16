using UnityEngine;
using CraftSpace.Models;
using System.Collections.Generic;

public class ItemView : MonoBehaviour
{
    [Header("Model Reference")]
    [SerializeField] private ItemData _model;
    
    [Header("Renderer Settings")]
    [SerializeField] private bool _autoInitializeRenderers = true;
    [SerializeField] private float _closeDistance = 5f;
    [SerializeField] private float _mediumDistance = 20f;
    [SerializeField] private float _farDistance = 100f;
    
    // Tracked renderers
    private Dictionary<System.Type, BaseViewRenderer> _renderers = new Dictionary<System.Type, BaseViewRenderer>();
    private List<BaseViewRenderer> _activeRenderers = new List<BaseViewRenderer>();
    
    // Distance tracking
    private Camera _mainCamera;
    private float _lastDistanceCheck = 0f;
    private const float DISTANCE_CHECK_INTERVAL = 0.5f;
    
    // Property to get/set the model
    public ItemData Model 
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
    public event ModelUpdatedHandler OnModelUpdated;
    
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
            if (distance <= _closeDistance * 0.5f && _model != null && _model.isFavorite)
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
    public virtual void OnModelUpdated()
    {
        UpdateView();
    }
    
    // Update all active renderers
    protected virtual void UpdateView()
    {
        // Base class just updates name for debugging
        if (_model != null)
        {
            gameObject.name = $"Item: {_model.title}";
            
            // Update all renderers with new model data
            foreach (var renderer in _renderers.Values)
            {
                if (renderer != null)
                {
                    renderer.UpdateWithModel(_model);
                }
            }
            
            // Notify event subscribers
            OnModelUpdated?.Invoke();
        }
        else
        {
            gameObject.name = "Item: [No Model]";
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
                _activeRenderers.Add(renderer);
                renderer.Activate();
                renderer.UpdateWithModel(_model);
            }
        }
        else
        {
            if (_activeRenderers.Contains(renderer))
            {
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
} 