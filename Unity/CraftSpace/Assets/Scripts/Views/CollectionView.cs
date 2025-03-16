using UnityEngine;
using CraftSpace.Models;
using System.Collections.Generic;

public class CollectionView : MonoBehaviour
{
    [Header("Model Reference")]
    [SerializeField] private CollectionData _model;
    
    [Header("Child Item Views")]
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _itemViewPrefab;
    [SerializeField] private bool _createItemViewsAutomatically = false;
    
    // List of item views this collection view has created/is managing
    private List<ItemView> _childItemViews = new List<ItemView>();
    
    // Property to get/set the model
    public CollectionData Model 
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
                    
                    // If set to automatically create item views, do so now
                    if (_createItemViewsAutomatically)
                    {
                        CreateItemViews();
                    }
                }
                
                // Update the view with the new model data
                UpdateView();
            }
        }
    }
    
    private void Awake()
    {
        if (_model != null)
        {
            // Register with model on awake
            _model.RegisterView(this);
        }
    }
    
    private void OnDestroy()
    {
        if (_model != null)
        {
            // Unregister when view is destroyed
            _model.UnregisterView(this);
        }
        
        // Clean up child item views
        ClearItemViews();
    }
    
    // Called by model when it's updated
    public virtual void OnModelUpdated()
    {
        UpdateView();
    }
    
    // Override in subclasses to provide specific visualization
    protected virtual void UpdateView()
    {
        // Base class just updates name for debugging
        if (_model != null)
        {
            gameObject.name = $"Collection: {_model.name}";
        }
        else
        {
            gameObject.name = "Collection: [No Model]";
        }
    }
    
    // Create item views for all items in the collection
    public void CreateItemViews()
    {
        // Clear any existing item views
        ClearItemViews();
        
        if (_model == null || _itemViewPrefab == null || _itemContainer == null)
            return;
            
        foreach (var item in _model.items)
        {
            CreateItemView(item);
        }
    }
    
    // Create a view for a specific item
    public ItemView CreateItemView(ItemData itemModel)
    {
        if (_itemViewPrefab == null || _itemContainer == null)
            return null;
            
        GameObject itemViewObj = Instantiate(_itemViewPrefab, _itemContainer);
        ItemView itemView = itemViewObj.GetComponent<ItemView>();
        
        if (itemView != null)
        {
            itemView.Model = itemModel;
            _childItemViews.Add(itemView);
            return itemView;
        }
        
        return null;
    }
    
    // Clear all item views
    public void ClearItemViews()
    {
        foreach (var itemView in _childItemViews)
        {
            if (itemView != null)
            {
                Destroy(itemView.gameObject);
            }
        }
        
        _childItemViews.Clear();
    }
} 