using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Base abstract class for all view renderers
/// </summary>
public abstract class BaseViewRenderer : MonoBehaviour
{
    [Header("Renderer Settings")]
    [SerializeField] protected float _transitionSpeed = 1.0f;
    [SerializeField] protected bool _activeOnStart = false;
    
    protected bool _isActivated = false;
    protected float _currentAlpha = 0f;
    
    // Called when the renderer is first created
    protected virtual void Awake()
    {
        // Initialize in disabled state
        _isActivated = false;
        _currentAlpha = 0f;
        OnAlphaChanged(_currentAlpha);
        
        if (_activeOnStart)
        {
            Activate();
        }
    }
    
    // Update is called once per frame
    protected virtual void Update()
    {
        // Handle alpha transition
        UpdateTransition();
    }
    
    // Manage smooth transitions
    protected virtual void UpdateTransition()
    {
        float targetAlpha = _isActivated ? 1f : 0f;
        
        if (_currentAlpha != targetAlpha)
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, Time.deltaTime * _transitionSpeed);
            OnAlphaChanged(_currentAlpha);
        }
    }
    
    // Apply alpha changes to visuals
    protected virtual void OnAlphaChanged(float alpha)
    {
        // Base implementation does nothing, override in derived classes
    }
    
    // Show this renderer
    public virtual void Activate()
    {
        _isActivated = true;
        gameObject.SetActive(true);
    }
    
    // Hide this renderer
    public virtual void Deactivate()
    {
        _isActivated = false;
        gameObject.SetActive(false);
    }
    
    // Set visibility directly
    public virtual void SetVisibility(bool visible)
    {
        if (visible)
            Activate();
        else
            Deactivate();
    }
    
    // Update renderer with model data
    public abstract void UpdateWithModel(Item model);
    
    public bool IsActive => _isActivated;
}

/// <summary>
/// Base class for Item-specific renderers
/// </summary>
public abstract class ItemViewRenderer : BaseViewRenderer
{
    // Update with an Item model
    public virtual void UpdateWithItemModel(Item model)
    {
        // Override in derived classes
    }
    
    // Convert generic model to Item model
    public override void UpdateWithModel(Item model)
    {
        if (model is Item itemModel)
        {
            UpdateWithItemModel(itemModel);
        }
        else
        {
            Debug.LogWarning($"Renderer {GetType().Name} received invalid model type: {model?.GetType().Name ?? "null"}");
        }
    }
}

/// <summary>
/// Base class for Collection-specific renderers
/// </summary>
public abstract class CollectionViewRenderer : BaseViewRenderer
{
    // Update with a Collection model
    public virtual void UpdateWithCollectionModel(Collection model)
    {
        // Override in derived classes
    }
    
    // Convert generic model to Collection model
    public override void UpdateWithModel(Item model)
    {
        if (model is Collection collectionModel)
        {
            UpdateWithCollectionModel(collectionModel);
        }
        else
        {
            Debug.LogWarning($"Renderer {GetType().Name} received invalid model type: {model?.GetType().Name ?? "null"}");
        }
    }
} 