using UnityEngine;
using CraftSpace.Models;
using System.Collections.Generic;

public class CollectionBrowserManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _collectionsContainer;
    [SerializeField] private GameObject _itemViewPrefab;
    [SerializeField] private GameObject _collectionViewPrefab;
    [SerializeField] private CameraController _cameraController;
    
    [Header("Layout Settings")]
    [SerializeField] private float _collectionSpacing = 5f;
    
    private List<CollectionGridLayout> _collectionLayouts = new List<CollectionGridLayout>();
    private Brewster _brewster;
    
    private void Start()
    {
        _brewster = GetComponent<Brewster>();
        if (_brewster == null)
        {
            Debug.LogError("Brewster component not found!");
            return;
        }
        
        // Subscribe to Brewster's loading completion
        // Since Brewster might not have a built-in event, we can check in Update
    }
    
    private void Update()
    {
        // Only run once when collections are loaded
        if (_brewster.collections.Count > 0 && _collectionLayouts.Count == 0)
        {
            CreateCollectionLayouts();
        }
    }
    
    private void CreateCollectionLayouts()
    {
        if (_collectionsContainer == null || _collectionViewPrefab == null)
            return;
            
        // Clear any existing layouts
        foreach (var layout in _collectionLayouts)
        {
            if (layout != null)
            {
                Destroy(layout.gameObject);
            }
        }
        _collectionLayouts.Clear();
        
        // Calculate total width
        float totalWidth = 0;
        List<Vector2> gridSizes = new List<Vector2>();
        
        foreach (var collection in _brewster.collections)
        {
            // Create temporary layout to calculate size
            GameObject tempLayout = Instantiate(_collectionViewPrefab);
            CollectionGridLayout gridLayout = tempLayout.GetComponent<CollectionGridLayout>();
            gridLayout.SetCollection(collection);
            
            Vector2 gridSize = gridLayout.GetGridSize();
            gridSizes.Add(gridSize);
            
            totalWidth += gridSize.x;
            
            // Store for later configuration
            _collectionLayouts.Add(gridLayout);
        }
        
        // Add spacing between collections
        totalWidth += (_brewster.collections.Count - 1) * _collectionSpacing;
        
        // Position layouts
        float currentX = -totalWidth / 2;
        
        for (int i = 0; i < _collectionLayouts.Count; i++)
        {
            CollectionGridLayout layout = _collectionLayouts[i];
            Vector2 gridSize = gridSizes[i];
            
            // Position at current X coordinate
            float layoutCenterX = currentX + gridSize.x / 2;
            layout.transform.position = new Vector3(layoutCenterX, 0, 0);
            layout.transform.SetParent(_collectionsContainer, false);
            
            // Update for next layout
            currentX += gridSize.x + _collectionSpacing;
        }
        
        // Focus camera on first collection if available
        if (_collectionLayouts.Count > 0 && _cameraController != null)
        {
            _cameraController.FocusOnCollection(_collectionLayouts[0]);
        }
    }
    
    // Method for selecting a collection (e.g., called from UI)
    public void FocusOnCollection(int collectionIndex)
    {
        if (_cameraController != null && 
            collectionIndex >= 0 && 
            collectionIndex < _collectionLayouts.Count)
        {
            _cameraController.FocusOnCollection(_collectionLayouts[collectionIndex]);
        }
    }
} 