using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Arranges collection items in a 3D grid layout with configurable dimensions and spacing.
/// Works directly with standard 3D transforms - no UI/RectTransform dependency.
/// </summary>
[RequireComponent(typeof(CollectionView))]
public class CollectionGridLayout : MonoBehaviour
{
    #region Inspector Settings

    [Header("Grid Settings")]
    [Tooltip("Size of each cell in the grid")]
    [SerializeField] private float _cellSize = 1f;
    
    [Tooltip("Horizontal spacing between cells")]
    [SerializeField] private float _spacingHorizontal = 0.1f;
    
    [Tooltip("Vertical spacing between cells")]
    [SerializeField] private float _spacingVertical = 0.1f;
    
    [Tooltip("Number of columns in the grid")]
    [SerializeField] private int _columns = 4;
    
    [Header("Advanced Settings")]
    [Tooltip("Should layout update automatically when items are added/removed")]
    [SerializeField] private bool _autoUpdateLayout = true;
    
    [Tooltip("Center the grid horizontally relative to the transform")]
    [SerializeField] private bool _centerHorizontally = true;

    #endregion

    #region Private Fields

    private List<Transform> _itemTransforms = new List<Transform>();
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
        try 
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
            _itemTransforms.Clear();
            
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
                if (container != null)
                {
                    _itemTransforms.Add(container.transform);
                }
            }
            
            // Calculate optimal columns based on item count
            CalculateOptimalColumns();
            
            // Update the layout
            UpdateLayout();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionGridLayout] Error in ApplyLayout: {ex.Message}\n{ex.StackTrace}");
        }
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
        
        float width = _columns * _cellSize + (_columns - 1) * _spacingHorizontal;
        float height = rows * _cellSize + (rows - 1) * _spacingVertical;
        
        return new Vector2(width, height);
    }
    
    /// <summary>
    /// Adds a single transform to the grid and updates layout if autoUpdateLayout is true
    /// </summary>
    /// <param name="transform">The Transform to add</param>
    public void AddItem(Transform transform)
    {
        if (transform == null) return;
        
        _itemTransforms.Add(transform);
        
        if (_autoUpdateLayout)
        {
            UpdateLayout();
        }
    }
    
    /// <summary>
    /// Removes a single transform from the grid and updates layout if autoUpdateLayout is true
    /// </summary>
    /// <param name="transform">The Transform to remove</param>
    public void RemoveItem(Transform transform)
    {
        if (transform == null) return;
        
        _itemTransforms.Remove(transform);
        
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
        _itemTransforms.Clear();
        
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
        // Calculate base position - use different spacing for horizontal and vertical
        float xPos = gridPosition.x * (_cellSize + _spacingHorizontal);
        float zPos = -gridPosition.y * (_cellSize + _spacingVertical); // Negative to grow downward in z
        
        // Apply horizontal centering if enabled
        if (_centerHorizontally)
        {
            float gridWidth = _columns * (_cellSize + _spacingHorizontal) - _spacingHorizontal; // Subtract trailing spacing
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
        try
        {
            if (_itemTransforms == null || _itemTransforms.Count == 0) return;
            
            // Recalculate optimal columns before layout
            CalculateOptimalColumns();
            
            for (int i = 0; i < _itemTransforms.Count; i++)
            {
                var item = _itemTransforms[i];
                if (item == null) continue;
                
                Vector2Int gridPos = GetGridPosition(i);
                Vector3 position = GetPositionForGridPosition(gridPos);
                
                // Apply position directly to the transform
                // Use localPosition to position relative to the grid's parent
                item.localPosition = position;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CollectionGridLayout] Error in UpdateLayout: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the total item count, either from direct items list or from the collection
    /// </summary>
    private int GetItemCount()
    {
        // First check our direct items list
        if (_itemTransforms != null && _itemTransforms.Count > 0)
            return _itemTransforms.Count;
            
        // Otherwise check the collection
        if (_collection != null) 
        {
            return _collection.Items.Count();
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

    /// <summary>
    /// Calculates the optimal number of columns based on item count
    /// </summary>
    private void CalculateOptimalColumns()
    {
        int itemCount = GetItemCount();
        
        // Handle special cases to avoid crashes
        if (itemCount <= 0)
        {
            _columns = 1; // Default to 1 column for empty collections
            return;
        }
        
        if (itemCount == 1)
        {
            _columns = 1; // Single item needs just 1 column
            return;
        }
        
        // Calculate columns as ceiling of square root of item count
        // This creates a grid that's roughly square
        _columns = Mathf.CeilToInt(Mathf.Sqrt(itemCount));
    }

    #endregion
} 