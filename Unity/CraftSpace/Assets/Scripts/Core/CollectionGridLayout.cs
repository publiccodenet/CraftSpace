using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Arranges collection items in a grid layout with configurable dimensions and spacing.
/// Handles both automatic layout based on CollectionView and manual item management.
/// </summary>
[RequireComponent(typeof(CollectionView))]
public class CollectionGridLayout : MonoBehaviour
{
    #region Inspector Settings

    [Header("Grid Settings")]
    [Tooltip("Size of each cell in the grid")]
    [SerializeField] private float _cellSize = 1f;
    
    [Tooltip("Spacing between cells")]
    [SerializeField] private float _spacing = 0.1f;
    
    [Tooltip("Number of columns in the grid")]
    [SerializeField] private int _columns = 4;
    
    [Header("Advanced Settings")]
    [Tooltip("Should layout update automatically when items are added/removed")]
    [SerializeField] private bool _autoUpdateLayout = true;
    
    [Tooltip("Center the grid horizontally relative to the transform")]
    [SerializeField] private bool _centerHorizontally = true;

    #endregion

    #region Private Fields

    private List<RectTransform> _items = new List<RectTransform>();
    private Collection _collection;
    private CollectionView _collectionView;
    private bool _initialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _collectionView = GetComponent<CollectionView>();
        if (_collectionView != null)
        {
            _collectionView.ModelUpdated += OnModelUpdated;
        }
        _initialized = true;
    }

    private void OnDestroy()
    {
        if (_collectionView != null)
        {
            _collectionView.ModelUpdated -= OnModelUpdated;
        }
    }

    private void OnValidate()
    {
        // Ensure columns is at least 1
        _columns = Mathf.Max(1, _columns);
        
        // If already initialized in play mode, update the layout
        if (_initialized && _autoUpdateLayout && Application.isPlaying)
        {
            UpdateLayout();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the collection and updates the associated CollectionView
    /// </summary>
    /// <param name="collection">The collection to display</param>
    public void SetCollection(Collection collection)
    {
        _collection = collection;
        
        // Get the CollectionView component and update it
        if (_collectionView != null)
        {
            _collectionView.SetModel(collection);
        }
        else 
        {
            Debug.LogWarning($"[CollectionGridLayout] Missing CollectionView component on {gameObject.name}");
        }
        
        if (_autoUpdateLayout)
        {
            ApplyLayout();
        }
    }
    
    /// <summary>
    /// Forces the layout to update with current items from the CollectionView
    /// </summary>
    public void ApplyLayout()
    {
        // Get all item containers from the CollectionView
        if (_collectionView == null)
        {
            _collectionView = GetComponent<CollectionView>();
            if (_collectionView == null)
            {
                Debug.LogError($"[CollectionGridLayout] Cannot apply layout - missing CollectionView component on {gameObject.name}");
                return;
            }
        }
        
        // Clear existing items
        _items.Clear();
        
        // Get all containers
        var containers = _collectionView.GetItemContainers();
        if (containers == null || containers.Count == 0)
        {
            Debug.Log($"[CollectionGridLayout] No item containers found in CollectionView on {gameObject.name}");
            return;
        }
        
        // Add their transforms to our layout items
        foreach (var container in containers)
        {
            if (container != null && container.transform as RectTransform != null)
            {
                _items.Add(container.transform as RectTransform);
            }
            else if (container != null)
            {
                // If it's not a RectTransform, we'll use its regular transform
                // This makes the class more flexible to work with both UI and world-space layouts
                var regularTransform = container.transform;
                var rectTransform = new GameObject($"{container.name}_LayoutProxy").AddComponent<RectTransform>();
                rectTransform.SetParent(transform, false);
                rectTransform.position = regularTransform.position;
                
                // Link the proxy to the original transform
                var proxy = rectTransform.gameObject.AddComponent<LayoutProxy>();
                proxy.SetTarget(regularTransform);
                
                _items.Add(rectTransform);
            }
        }
        
        // Update the layout
        UpdateLayout();
    }
    
    /// <summary>
    /// Gets the dimensions of the grid based on current settings and item count
    /// </summary>
    /// <returns>Vector2 with width and height of the entire grid</returns>
    public Vector2 GetGridSize()
    {
        int itemCount = GetItemCount();
        
        // Default to a single cell if no items
        if (itemCount == 0)
            return new Vector2(_cellSize, _cellSize);
            
        int rows = Mathf.CeilToInt((float)itemCount / _columns);
        
        float width = _columns * _cellSize + (_columns - 1) * _spacing;
        float height = rows * _cellSize + (rows - 1) * _spacing;
        
        return new Vector2(width, height);
    }
    
    /// <summary>
    /// Adds a single item to the grid and updates layout if autoUpdateLayout is true
    /// </summary>
    /// <param name="item">The RectTransform to add</param>
    public void AddItem(RectTransform item)
    {
        if (item == null) return;
        
        _items.Add(item);
        
        if (_autoUpdateLayout)
        {
            UpdateLayout();
        }
    }
    
    /// <summary>
    /// Removes a single item from the grid and updates layout if autoUpdateLayout is true
    /// </summary>
    /// <param name="item">The RectTransform to remove</param>
    public void RemoveItem(RectTransform item)
    {
        if (item == null) return;
        
        _items.Remove(item);
        
        if (_autoUpdateLayout)
        {
            UpdateLayout();
        }
    }
    
    /// <summary>
    /// Clears all items from the grid and updates layout if autoUpdateLayout is true
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        
        if (_autoUpdateLayout)
        {
            UpdateLayout();
        }
    }
    
    /// <summary>
    /// Gets the row and column for a specific item index
    /// </summary>
    /// <param name="index">Item index in the collection</param>
    /// <returns>Vector2Int where x is the column and y is the row</returns>
    public Vector2Int GetGridPosition(int index)
    {
        if (_columns <= 0) _columns = 1; // Safety check
        
        int row = index / _columns;
        int col = index % _columns;
        
        return new Vector2Int(col, row);
    }
    
    /// <summary>
    /// Gets the world position for a specific grid position
    /// </summary>
    /// <param name="gridPosition">Grid position (column, row)</param>
    /// <returns>Vector3 position in world space</returns>
    public Vector3 GetPositionForGridPosition(Vector2Int gridPosition)
    {
        float cellAndSpacing = _cellSize + _spacing;
        
        // Calculate base position
        float xPos = gridPosition.x * cellAndSpacing;
        float zPos = -gridPosition.y * cellAndSpacing; // Negative to grow downward in z
        
        // Apply horizontal centering if enabled
        if (_centerHorizontally)
        {
            float gridWidth = _columns * cellAndSpacing - _spacing; // Subtract trailing spacing
            float offset = gridWidth * 0.5f - _cellSize * 0.5f;
            xPos -= offset;
        }
        
        return new Vector3(xPos, 0, zPos);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the layout of all items based on current settings
    /// </summary>
    private void UpdateLayout()
    {
        if (_items == null || _items.Count == 0) return;
        
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item == null) continue;
            
            Vector2Int gridPos = GetGridPosition(i);
            Vector3 position = GetPositionForGridPosition(gridPos);
            
            item.anchoredPosition = new Vector2(position.x, position.z);
            item.sizeDelta = new Vector2(_cellSize, _cellSize);
            
            // Look for a LayoutProxy component to update the target transform
            var proxy = item.GetComponent<LayoutProxy>();
            if (proxy != null)
            {
                proxy.UpdateTargetPosition(position);
            }
        }
    }
    
    /// <summary>
    /// Gets the total item count, either from direct items list or from the collection
    /// </summary>
    private int GetItemCount()
    {
        // First check our direct items list
        if (_items != null && _items.Count > 0)
            return _items.Count;
            
        // Otherwise check the collection
        if (_collection != null) 
        {
            if (_collection.items != null && _collection.items.Count > 0)
            {
                return _collection.items.Count;
            }
            else if (_collection.ItemIds != null && _collection.ItemIds.Count > 0)
            {
                return _collection.ItemIds.Count;
            }
        }
        
        return 0;
    }
    
    /// <summary>
    /// Called when the CollectionView's model is updated
    /// </summary>
    private void OnModelUpdated()
    {
        if (_autoUpdateLayout)
        {
            ApplyLayout();
        }
    }

    #endregion
}

/// <summary>
/// Helper component to link a RectTransform used for layout to a regular Transform 
/// for positioning non-UI objects in a grid layout
/// </summary>
public class LayoutProxy : MonoBehaviour
{
    private Transform _target;
    
    public void SetTarget(Transform target)
    {
        _target = target;
    }
    
    public void UpdateTargetPosition(Vector3 position)
    {
        if (_target != null)
        {
            _target.position = position;
        }
    }
} 