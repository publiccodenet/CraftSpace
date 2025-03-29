using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Grid layout for collection items
/// </summary>
public class CollectionGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private float _spacing = 0.1f;
    [SerializeField] private int _columns = 4;
    
    private List<RectTransform> _items = new List<RectTransform>();
    private Collection _collection;
    
    public void SetCollection(Collection collection)
    {
        _collection = collection;
        
        // Get the CollectionView component and update it
        CollectionView collectionView = GetComponent<CollectionView>();
        if (collectionView != null)
        {
            collectionView.SetModel(collection);
        }
    }
    
    // Public method to force the layout to update (required by CollectionView)
    public void ApplyLayout()
    {
        // Get all item containers from the CollectionView
        CollectionView view = GetComponent<CollectionView>();
        if (view != null)
        {
            // Clear existing items
            _items.Clear();
            
            // Get all containers
            var containers = view.GetItemContainers();
            
            // Add their transforms to our layout items
            foreach (var container in containers)
            {
                if (container != null && container.transform as RectTransform != null)
                {
                    _items.Add(container.transform as RectTransform);
                }
            }
            
            // Update the layout
            UpdateLayout();
        }
    }
    
    public Vector2 GetGridSize()
    {
        int itemCount = 0;
        
        // Safely get item count avoiding null reference errors
        if (_collection != null) 
        {
            if (_collection.items != null && _collection.items.Count > 0)
            {
                itemCount = _collection.items.Count;
            }
            else if (_collection.ItemIds != null && _collection.ItemIds.Count > 0)
            {
                itemCount = _collection.ItemIds.Count;
            }
        }
        
        // Default to a single cell if no items
        if (itemCount == 0)
            return new Vector2(_cellSize, _cellSize);
            
        int rows = Mathf.CeilToInt((float)itemCount / _columns);
        
        float width = _columns * _cellSize + (_columns - 1) * _spacing;
        float height = rows * _cellSize + (rows - 1) * _spacing;
        
        return new Vector2(width, height);
    }
    
    public void AddItem(RectTransform item)
    {
        _items.Add(item);
        UpdateLayout();
    }
    
    public void RemoveItem(RectTransform item)
    {
        _items.Remove(item);
        UpdateLayout();
    }
    
    public void Clear()
    {
        _items.Clear();
        UpdateLayout();
    }
    
    private void UpdateLayout()
    {
        float xOffset = 0;
        float yOffset = 0;
        int currentColumn = 0;
        
        foreach (var item in _items)
        {
            item.anchoredPosition = new Vector2(xOffset, yOffset);
            item.sizeDelta = new Vector2(_cellSize, _cellSize);
            
            currentColumn++;
            xOffset += _cellSize + _spacing;
            
            if (currentColumn >= _columns)
            {
                currentColumn = 0;
                xOffset = 0;
                yOffset -= _cellSize + _spacing;
            }
        }
    }
} 