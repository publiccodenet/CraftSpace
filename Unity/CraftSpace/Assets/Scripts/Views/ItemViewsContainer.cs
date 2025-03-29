using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Container for managing multiple ItemView instances
/// </summary>
public class ItemViewsContainer : MonoBehaviour
{
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject itemViewPrefab;
    
    private List<ItemView> itemViews = new List<ItemView>();
    private Item _item;
    
    public Transform ContentContainer => contentContainer;
    public List<ItemView> ItemViews => itemViews;
    
    // Add property for accessing the item
    public Item Item
    {
        get => _item;
        set
        {
            _item = value;
            UpdateItemViews();
        }
    }
    
    private void Awake()
    {
        // Collect any existing item views
        ItemView[] existingViews = GetComponentsInChildren<ItemView>();
        foreach (var view in existingViews)
        {
            if (!itemViews.Contains(view))
            {
                itemViews.Add(view);
            }
        }
    }
    
    private void UpdateItemViews()
    {
        if (_item == null) return;
        
        foreach (var view in itemViews)
        {
            if (view != null)
            {
                view.Model = _item;
            }
        }
    }
    
    /// <summary>
    /// Initialize the container with items
    /// </summary>
    public void Initialize(List<Item> items)
    {
        Clear();
        
        if (items == null || items.Count == 0) return;
        
        foreach (var item in items)
        {
            AddItemView(item);
        }
    }
    
    /// <summary>
    /// Create and add a new item view
    /// </summary>
    public ItemView AddItemView(Item item)
    {
        if (item == null || itemViewPrefab == null) return null;
        
        GameObject viewObj = Instantiate(itemViewPrefab, contentContainer);
        ItemView itemView = viewObj.GetComponent<ItemView>();
        
        if (itemView != null)
        {
            itemView.SetModel(item);
            itemViews.Add(itemView);
        }
        
        return itemView;
    }
    
    /// <summary>
    /// Remove an item view
    /// </summary>
    public void RemoveItemView(ItemView itemView)
    {
        if (itemView == null) return;
        
        itemViews.Remove(itemView);
        Destroy(itemView.gameObject);
    }
    
    /// <summary>
    /// Clear all item views
    /// </summary>
    public void Clear()
    {
        foreach (var itemView in itemViews)
        {
            if (itemView != null && itemView.gameObject != null)
            {
                Destroy(itemView.gameObject);
            }
        }
        
        itemViews.Clear();
    }
    
    /// <summary>
    /// Apply position, rotation and scale to this container
    /// </summary>
    public void ApplyLayout(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = scale;
    }
} 