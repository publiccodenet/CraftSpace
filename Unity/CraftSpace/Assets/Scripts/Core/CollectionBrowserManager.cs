using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// Run this script after Brewster
[DefaultExecutionOrder(0)]
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
    public Brewster brewster; // Public field to assign Brewster in the editor
    private bool _initialized = false;
    private bool _isInitializing = false;
    
    private void Start()
    {
        if (brewster == null)
        {
            Debug.LogError("[CollectionBrowserManager] Brewster reference is not assigned in the editor!");
            return;
        }
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        // Validate critical component references
        if (_collectionsContainer == null)
        {
            Debug.LogError("[CollectionBrowserManager] Collections container reference is missing!");
            return;
        }
        
        if (_collectionViewPrefab == null)
        {
            Debug.LogError("[CollectionBrowserManager] Collection view prefab reference is missing!");
            return;
        }
        
        if (_cameraController == null)
        {
            Debug.LogWarning("[CollectionBrowserManager] Camera controller reference is missing. Camera focusing features will be disabled.");
        }
        
        _initialized = true;
        Debug.Log("[CollectionBrowserManager] Successfully connected to Brewster.");
    }
    
    private void Update()
    {
        // Only run once when collections are loaded and everything is properly initialized
        if (_initialized && brewster != null && brewster.collections.Count > 0 && _collectionLayouts.Count == 0
            && _collectionsContainer != null && _collectionViewPrefab != null)
        {
            try
            {
                CreateCollectionLayouts();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CollectionBrowserManager] Error creating collection layouts: {ex.Message}\n{ex.StackTrace}");
            }
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
        
        foreach (var collection in brewster.collections)
        {
            // Create temporary layout to calculate size
            GameObject tempLayout = Instantiate(_collectionViewPrefab);
            CollectionGridLayout gridLayout = tempLayout.GetComponent<CollectionGridLayout>();
            if (gridLayout == null)
            {
                Debug.LogError("[CollectionBrowserManager] Failed to get CollectionGridLayout component from prefab.");
                Destroy(tempLayout);
                continue;
            }
            
            gridLayout.SetCollection(collection);
            
            Vector2 gridSize = gridLayout.GetGridSize();
            gridSizes.Add(gridSize);
            
            totalWidth += gridSize.x;
            
            // Store for later configuration
            _collectionLayouts.Add(gridLayout);
        }
        
        // Add spacing between collections
        totalWidth += (brewster.collections.Count - 1) * _collectionSpacing;
        
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
            // Get the collection from the layout
            var layout = _collectionLayouts[0];
            var collectionView = layout.GetComponent<CollectionView>();
            var collection = collectionView != null ? collectionView.Model : null;
            
            if (collection != null)
            {
                _cameraController.FocusOnCollection(collection);
            }
            else
            {
                // If no collection is available, still focus on something
                Debug.LogWarning("[CollectionBrowserManager] No Collection model found in layout, using default focus");
                _cameraController.FocusOnCollection(null);
            }
        }
        
        Debug.Log($"[CollectionBrowserManager] Created {_collectionLayouts.Count} collection layouts");
    }
    
    // Method for selecting a collection (e.g., called from UI)
    public void FocusOnCollection(int collectionIndex)
    {
        if (_cameraController == null)
        {
            Debug.LogWarning("[CollectionBrowserManager] Cannot focus on collection - Camera controller is null");
            return;
        }
        
        if (collectionIndex < 0 || collectionIndex >= _collectionLayouts.Count)
        {
            Debug.LogWarning($"[CollectionBrowserManager] Cannot focus on collection - Invalid index: {collectionIndex}");
            return;
        }
        
        // Get the collection from the layout
        var layout = _collectionLayouts[collectionIndex];
        var collectionView = layout.GetComponent<CollectionView>();
        var collection = collectionView != null ? collectionView.Model : null;
        
        if (collection != null)
        {
            _cameraController.FocusOnCollection(collection);
        }
        else
        {
            // If no collection is available, still focus on something
            Debug.LogWarning("[CollectionBrowserManager] No Collection model found in layout, using default focus");
            _cameraController.FocusOnCollection(null);
        }
    }
} 