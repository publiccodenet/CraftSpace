using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Arranges collection items in a grid layout
/// </summary>
[RequireComponent(typeof(CollectionView))]
public class GridLayoutCollection : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private float cellWidth = 2f;
    [SerializeField] private float cellHeight = 2f;
    [SerializeField] private int columns = 4;
    
    private CollectionView _collectionView;
    
    private void Awake()
    {
        _collectionView = GetComponent<CollectionView>();
        
        // Subscribe to model updates
        _collectionView.ModelUpdated += HandleModelUpdated;
    }
    
    private void OnDestroy()
    {
        if (_collectionView != null)
        {
            _collectionView.ModelUpdated -= HandleModelUpdated;
        }
    }
    
    public void ApplyLayout()
    {
        if (_collectionView == null) return;
        
        var containers = _collectionView.GetItemContainers();
        int itemCount = containers.Count;
        
        for (int i = 0; i < itemCount; i++)
        {
            int row = i / columns;
            int col = i % columns;
            
            Vector3 position = new Vector3(
                col * cellWidth, 
                0, 
                -row * cellHeight
            );
            
            containers[i].transform.localPosition = position;
        }
    }
    
    private void HandleModelUpdated()
    {
        // This would be called when the collection model changes
        // Could adjust layout based on number of items, etc.
        ApplyLayout();
    }
} 