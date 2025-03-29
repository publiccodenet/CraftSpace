using UnityEngine;
using System;
using System.Collections.Generic;

public class CollectionView : MonoBehaviour, IModelView<Collection>
{
    [Header("Model Reference")]
    [SerializeField] private Collection _model;
    
    [Header("Child Item Views")]
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _itemViewsContainerPrefab;
    [SerializeField] private List<GameObject> _itemViewPrefabs = new List<GameObject>();
    [SerializeField] private bool _createItemViewsAutomatically = false;
    
    // List of item view containers this collection view is managing
    private List<ItemViewsContainer> _itemContainers = new List<ItemViewsContainer>();
    
    // Property to get the model (implementing IModelView)
    public Collection Model 
    { 
        get { return _model; }
    }
    
    // Implement the IModelView.SetModel method
    public void SetModel(Collection value) 
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
    
    // Get Collection property as an alias for Model
    public Collection Collection => _model;
    
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
            gameObject.name = $"Collection: {_model.Name}";
        }
        else
        {
            gameObject.name = "Collection: [No Model]";
        }
        
        ModelUpdated?.Invoke();
        
        Debug.Log($"[CollectionView] View updated. Collection ID: {_model?.Id ?? "null"}, Collection name: {_model?.Name ?? "null"}, Item count: {_model?.items?.Count ?? 0}");
    }
    
    // Implement IModelView interface
    public void HandleModelUpdated()
    {
        // Create item views if needed
        if (_createItemViewsAutomatically && _model != null && _model.items.Count > 0)
        {
            CreateItemViews();
        }
        
        // Update view UI elements
        UpdateViewElements();
    }
    
    // Create item views for all items in the collection
    public void CreateItemViews()
    {
        // Clear any existing item views
        ClearItemViews();
        
        if (_model == null || _itemViewsContainerPrefab == null || _itemContainer == null)
            return;
            
        foreach (var item in _model.items)
        {
            CreateItemViewContainer(item, Vector3.zero);
        }
        
        // Apply layout if there's a layout component
        var layoutComponent = GetComponent<CollectionGridLayout>();
        if (layoutComponent != null)
        {
            layoutComponent.ApplyLayout();
        }
    }
    
    // Create a container with views for a specific item
    public ItemViewsContainer CreateItemViewContainer(Item itemModel, Vector3 position)
    {
        if (itemModel == null || _itemViewsContainerPrefab == null)
            return null;
            
        // Create container
        GameObject containerObj = Instantiate(_itemViewsContainerPrefab, _itemContainer);
        containerObj.name = $"ItemViews_{itemModel.Id}";
        containerObj.transform.localPosition = position;
        
        // Get or add container component
        ItemViewsContainer container = containerObj.GetComponent<ItemViewsContainer>();
        if (container == null)
        {
            container = containerObj.AddComponent<ItemViewsContainer>();
        }
        
        // Set the item model
        container.Item = itemModel;
        
        // Create child views inside the container
        foreach (var prefab in _itemViewPrefabs)
        {
            if (prefab != null)
            {
                AddItemViewToPrefab(container, prefab);
            }
        }
        
        // Keep track of the container
        _itemContainers.Add(container);
        
        return container;
    }
    
    // Add a view to an existing container
    private ItemView AddItemViewToPrefab(ItemViewsContainer container, GameObject prefab)
    {
        // Create the view inside the container
        GameObject viewObj = Instantiate(prefab, container.transform);
        
        // Get or add ItemView component
        ItemView view = viewObj.GetComponent<ItemView>();
        if (view == null)
        {
            view = viewObj.AddComponent<ItemView>();
        }
        
        // The container will handle setting the item model
        
        return view;
    }
    
    // Clear all item containers
    public void ClearItemViews()
    {
        foreach (var container in _itemContainers)
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }
        
        _itemContainers.Clear();
    }
    
    // Get all item containers
    public List<ItemViewsContainer> GetItemContainers()
    {
        return new List<ItemViewsContainer>(_itemContainers);
    }
    
    // Protected method for UI updates to be overridden by subclasses
    protected virtual void UpdateViewElements()
    {
        // For subclasses to implement UI updates
        if (_model != null)
        {
            gameObject.name = $"Collection: {_model.Name}";
            
            Debug.Log($"[CollectionView] View updated. Collection ID: {_model.Id}, Collection name: {_model.Name}, Item count: {_model.items.Count}, Container count: {_itemContainers.Count}");
        }
        else
        {
            gameObject.name = "Collection: [No Model]";
        }
    }
} 