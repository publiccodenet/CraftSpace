using UnityEngine;
using System.Collections.Generic;
using CraftSpace.Models;
using CraftSpace.Models.Schema.Generated;

public class CollectionGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float _itemWidth = 2.0f;  // Horizontal spacing
    [SerializeField] private float _itemHeight = 2.0f; // Forward/back spacing
    [SerializeField] private float _itemMarginTop = 0.2f;
    [SerializeField] private float _itemMarginBottom = 0.2f;
    [SerializeField] private float _itemMarginLeft = 0.2f;
    [SerializeField] private float _itemMarginRight = 0.2f;
    [SerializeField] private float _itemGapVertical = 0.5f;
    [SerializeField] private float _itemGapHorizontal = 0.5f;
    [SerializeField] private bool _centerGrid = true;
    
    [Header("References")]
    [SerializeField] private Transform _container;
    [SerializeField] private GameObject _itemViewPrefab;
    [SerializeField] private Collection _collection;
    
    private List<ItemView> _itemViews = new List<ItemView>();
    private Vector2Int _gridDimensions;
    
    private void Start()
    {
        if (_collection != null)
        {
            CreateItemGrid();
        }
    }
    
    public void SetCollection(CraftSpace.Models.Schema.Generated.Collection collection)
    {
        _collection = collection;
        ClearGrid();
        
        if (_collection != null)
        {
            CreateItemGrid();
        }
    }
    
    private void CreateItemGrid()
    {
        if (_collection == null || _itemViewPrefab == null)
            return;
            
        // Calculate total item dimensions including margins
        float totalItemWidth = _itemWidth + _itemMarginLeft + _itemMarginRight;
        float totalItemHeight = _itemHeight + _itemMarginTop + _itemMarginBottom;
        
        // Calculate grid dimensions
        int itemCount = _collection.items.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(itemCount));
        
        // Ensure we have enough rows
        int rows = Mathf.CeilToInt((float)itemCount / columns);
        
        _gridDimensions = new Vector2Int(columns, rows);
        
        // Calculate total grid size with gaps
        float gridWidth = columns * totalItemWidth + (columns - 1) * _itemGapHorizontal;
        float gridHeight = rows * totalItemHeight + (rows - 1) * _itemGapVertical;
        
        // Calculate start position (top left of grid)
        Vector3 startPos = Vector3.zero;
        if (_centerGrid)
        {
            startPos.x = -gridWidth / 2 + totalItemWidth / 2;
            startPos.z = gridHeight / 2 - totalItemHeight / 2;
        }
        
        // Create items
        for (int i = 0; i < itemCount; i++)
        {
            int row = i / columns;
            int col = i % columns;
            
            Vector3 itemPosition = new Vector3(
                startPos.x + col * (totalItemWidth + _itemGapHorizontal),
                0,
                startPos.z - row * (totalItemHeight + _itemGapVertical)
            );
            
            ItemView itemView = CreateItemView(_collection.items[i], itemPosition);
            if (itemView != null)
            {
                _itemViews.Add(itemView);
            }
        }
    }
    
    private ItemView CreateItemView(Item itemData, Vector3 position)
    {
        if (_itemViewPrefab == null)
            return null;
            
        Transform container = _container != null ? _container : transform;
        
        GameObject itemObject = Instantiate(_itemViewPrefab, container);
        itemObject.transform.localPosition = position;
        
        ItemView itemView = itemObject.GetComponent<ItemView>();
        if (itemView != null)
        {
            // Configure the ItemView with dimensions that match our layout
            if (itemView is ItemView typedItemView)
            {
                // Configure item dimensions if exposed through properties
                // This assumes you've added public properties or methods to ItemView
                // to adjust its dimensions
            }
            
            // Set the model data
            itemView.Model = itemData;
        }
        
        return itemView;
    }
    
    public void ClearGrid()
    {
        foreach (var itemView in _itemViews)
        {
            if (itemView != null)
            {
                Destroy(itemView.gameObject);
            }
        }
        
        _itemViews.Clear();
    }
    
    // Helper to get the total size of the grid
    public Vector2 GetGridSize()
    {
        return new Vector2(
            _gridDimensions.x * _itemWidth + (_gridDimensions.x - 1) * _itemGapHorizontal,
            _gridDimensions.y * _itemHeight + (_gridDimensions.y - 1) * _itemGapVertical
        );
    }
} 