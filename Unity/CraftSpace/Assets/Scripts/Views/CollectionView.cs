using UnityEngine;
using CraftSpace.Models.Schema.Generated;
using System.Collections.Generic;
using CraftSpace.Utils;
using Type = CraftSpace.Utils.LoggerWrapper.Type;

public class CollectionView : MonoBehaviour
{
    [Header("Model Reference")]
    [SerializeField] private Collection _model;
    
    [Header("Child Item Views")]
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _itemViewPrefab;
    [SerializeField] private bool _createItemViewsAutomatically = false;
    
    // List of item views this collection view has created/is managing
    private List<ItemView> _childItemViews = new List<ItemView>();
    
    // Property to get/set the model
    public Collection Model 
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
    public event System.Action ModelUpdated;
    
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
        
        ModelUpdated?.Invoke();
        
        LoggerWrapper.Success("CollectionView", "UpdateView", $"{Type.VIEW}{Type.SUCCESS} View updated", new Dictionary<string, object> {
            { "collectionId", _model?.Id ?? "null" },
            { "collectionName", _model?.Name ?? "null" },
            { "itemCount", _model?.items?.Count ?? 0 }
        }, this.gameObject);
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
            CreateItemView(item, Vector3.zero);
        }
    }
    
    // Create a view for a specific item
    public ItemView CreateItemView(Item itemModel, Vector3 position)
    {
        // Create a new GameObject for the item
        GameObject itemViewObj = new GameObject($"ItemView_{itemModel.Id}");
        itemViewObj.transform.parent = transform;
        itemViewObj.transform.localPosition = position;
        
        // Add ItemView component
        ItemView itemView = itemViewObj.AddComponent<ItemView>();
        itemView.ParentCollectionView = this;
        itemView.SetModel(itemModel);
        
        return itemView;
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

    // Add a method for model to call
    public virtual void HandleModelUpdated()
    {
        UpdateView();
    }
} 