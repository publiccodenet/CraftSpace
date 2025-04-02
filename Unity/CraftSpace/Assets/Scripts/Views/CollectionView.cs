using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class CollectionView : MonoBehaviour, IModelView<Collection>, ICollectionView
{
    [Header("Model Reference")]
    [SerializeField] private Collection _model;
    
    [Header("Child Item Views")]
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private GameObject _itemViewsContainerPrefab;
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
        try
        {
            // Base class just updates name for debugging
            if (_model != null)
            {
                gameObject.name = $"Collection: {_model.Title}";
            }
            else
            {
                gameObject.name = "Collection: [No Model]";
            }
            
            ModelUpdated?.Invoke();
            
            Debug.Log($"[CollectionView] View updated. Collection ID: {_model?.Id ?? "null"}, Collection title: {_model?.Title ?? "null"}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionView] Error in UpdateView: {ex.Message}");
        }
    }
    
    // Implement IModelView interface for backward compatibility
    public void HandleModelUpdated()
    {
        try
        {
            // Create item views if needed, using Any() for efficiency
            if (_createItemViewsAutomatically && _model != null && _model.Items.Any())
            {
                CreateItemViews();
            }
            
            // Update view UI elements
            UpdateViewElements();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionView] Error in HandleModelUpdated: {ex.Message}");
        }
    }
    
    // Implement ICollectionView interface
    public void OnCollectionUpdated(Collection collection)
    {
        try
        {
            // Verify this is our model
            if (_model != collection)
            {
                Debug.LogWarning($"[CollectionView] Received update for different collection: {collection?.Title ?? "null"}");
                return;
            }
            
            // Use existing handler for backward compatibility
            HandleModelUpdated();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionView] Error in OnCollectionUpdated: {ex.Message}");
        }
    }
    
    // Create item views for all items in the collection
    public void CreateItemViews()
    {
        try
        {
            // Clear any existing item views
            ClearItemViews();
            
            if (_model == null || _itemViewsContainerPrefab == null || _itemContainer == null)
                return;
                
            foreach (var item in _model.Items)
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
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionView] Error in CreateItemViews: {ex.Message}");
        }
    }
    
    // Create a container with views for a specific item
    public ItemViewsContainer CreateItemViewContainer(Item itemModel, Vector3 position)
    {
        try
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
            
            // Set the item model - the container will create its own item view
            container.Item = itemModel;
            
            // Keep track of the container
            _itemContainers.Add(container);
            
            return container;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionView] Error in CreateItemViewContainer: {ex.Message}");
            return null;
        }
    }
    
    // Clear all item containers
    public void ClearItemViews()
    {
        try
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
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionView] Error in ClearItemViews: {ex.Message}");
        }
    }
    
    // Get all item containers
    public List<ItemViewsContainer> GetItemContainers()
    {
        return new List<ItemViewsContainer>(_itemContainers);
    }
    
    // Protected method for UI updates to be overridden by subclasses
    protected virtual void UpdateViewElements()
    {
        // Override in subclasses to update UI elements
    }
} 