using UnityEngine;
using CraftSpace.Models;

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
        
        // Self-activate if configured
        if (_activeOnStart)
        {
            Activate();
        }
    }
    
    // Update is called once per frame
    protected virtual void Update()
    {
        // Handle fade transitions
        UpdateTransition();
    }
    
    // Handle transition effects
    protected virtual void UpdateTransition()
    {
        float targetAlpha = _isActivated ? 1f : 0f;
        
        if (!Mathf.Approximately(_currentAlpha, targetAlpha))
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, Time.deltaTime * _transitionSpeed);
            OnAlphaChanged(_currentAlpha);
        }
    }
    
    // Override this to handle alpha changes
    protected virtual void OnAlphaChanged(float alpha)
    {
        // Default implementation does nothing
    }
    
    // Activate the renderer
    public virtual void Activate()
    {
        _isActivated = true;
    }
    
    // Deactivate the renderer
    public virtual void Deactivate()
    {
        _isActivated = false;
    }
    
    // Immediately show or hide without transition
    public virtual void SetVisibility(bool visible)
    {
        _isActivated = visible;
        _currentAlpha = visible ? 1f : 0f;
        OnAlphaChanged(_currentAlpha);
    }
    
    // Render the model data
    public abstract void UpdateWithModel(object model);
}

/// <summary>
/// Base class for item renderers
/// </summary>
public abstract class ItemViewRenderer : BaseViewRenderer
{
    // Called to update the renderer with item data
    public virtual void UpdateWithItemModel(ItemData model)
    {
        UpdateWithModel(model);
    }
    
    // Override to implement item-specific rendering
    public override void UpdateWithModel(object model)
    {
        if (model is ItemData itemData)
        {
            UpdateWithItemModel(itemData);
        }
    }
}

/// <summary>
/// Base class for collection renderers
/// </summary>
public abstract class CollectionViewRenderer : BaseViewRenderer
{
    // Called to update the renderer with collection data
    public virtual void UpdateWithCollectionModel(CollectionData model)
    {
        UpdateWithModel(model);
    }
    
    // Override to implement collection-specific rendering
    public override void UpdateWithModel(object model)
    {
        if (model is CollectionData collectionData)
        {
            UpdateWithCollectionModel(collectionData);
        }
    }
} 