using UnityEngine;
using CraftSpace.Models;
using System.Collections.Generic;

public class CollectionGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float _itemWidth = 2.0f;
    [SerializeField] private float _itemHeight = 2.5f; // Includes title area
    [SerializeField] private float _itemSpacing = 0.5f;
    [SerializeField] private bool _centerGrid = true;
    
    [Header("References")]
    [SerializeField] private Transform _container;
    [SerializeField] private GameObject _itemViewPrefab;
    [SerializeField] private CollectionData _collection;
    
    private List<ItemView> _itemViews = new List<ItemView>();
    private Vector2Int _gridDimensions;
    
    private void Start()
    {
        if (_collection != null)
        {
            CreateItemGrid();
        }
    }
    
    public void SetCollection(CollectionData collection)
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
            
        // Calculate grid dimensions
        int itemCount = _collection.items.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(itemCount));
        
        // Ensure we have enough rows
        int rows = Mathf.CeilToInt((float)itemCount / columns);
        
        _gridDimensions = new Vector2Int(columns, rows);
        
        // Calculate total grid size
        float gridWidth = columns * _itemWidth + (columns - 1) * _itemSpacing;
        float gridHeight = rows * _itemHeight + (rows - 1) * _itemSpacing;
        
        // Calculate start position (top left of grid)
        Vector3 startPos = Vector3.zero;
        if (_centerGrid)
        {
            startPos.x = -gridWidth / 2 + _itemWidth / 2;
            startPos.z = gridHeight / 2 - _itemHeight / 2;
        }
        
        // Create items
        for (int i = 0; i < itemCount; i++)
        {
            int row = i / columns;
            int col = i % columns;
            
            Vector3 itemPosition = new Vector3(
                startPos.x + col * (_itemWidth + _itemSpacing),
                0,
                startPos.z - row * (_itemHeight + _itemSpacing)
            );
            
            ItemView itemView = CreateItemView(_collection.items[i], itemPosition);
            if (itemView != null)
            {
                _itemViews.Add(itemView);
            }
        }
    }
    
    private ItemView CreateItemView(ItemData itemData, Vector3 position)
    {
        if (_itemViewPrefab == null)
            return null;
            
        Transform container = _container != null ? _container : transform;
        
        GameObject itemObject = Instantiate(_itemViewPrefab, container);
        itemObject.transform.localPosition = position;
        
        ItemView itemView = itemObject.GetComponent<ItemView>();
        if (itemView != null)
        {
            itemView.Model = itemData;
            
            // Ensure the ArchiveTileRenderer is added
            if (itemView.GetComponent<ArchiveTileRenderer>() == null)
            {
                ArchiveTileRenderer renderer = itemView.gameObject.AddComponent<ArchiveTileRenderer>();
                renderer.Activate();
            }
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
            _gridDimensions.x * _itemWidth + (_gridDimensions.x - 1) * _itemSpacing,
            _gridDimensions.y * _itemHeight + (_gridDimensions.y - 1) * _itemSpacing
        );
    }
} 