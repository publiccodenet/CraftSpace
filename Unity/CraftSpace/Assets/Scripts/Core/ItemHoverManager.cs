using UnityEngine;

/// <summary>
/// Handles hovering over items in the scene and updates the CollectionDisplay
/// </summary>
public class ItemHoverManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask _itemLayerMask = -1;  // Default to "Everything"
    [SerializeField] private float _maxRaycastDistance = 100f;
    [SerializeField] private string _itemLayerName = "Items";
    [SerializeField] private float _hoverCheckFrequency = 0.1f;  // How often to check for hovering (seconds)
    
    [Header("Required References")]
    public Camera targetCamera;  // Direct reference to camera to use
    public CollectionDisplay collectionDisplay;
    
    private ItemView _currentHoveredItem;
    private float _nextCheckTime;
    
    void Awake()
    {
        // Validate required components
        if (targetCamera == null)
        {
            Debug.LogError("[ItemHoverManager] Camera reference must be assigned in the Inspector.");
            enabled = false;
            return;
        }
        
        if (collectionDisplay == null)
        {
            Debug.LogError("[ItemHoverManager] CollectionDisplay reference must be assigned in the Inspector.");
            enabled = false;
            return;
        }
        
        // Try to use the Items layer
        if (_itemLayerName == "Items")
        {
            _itemLayerMask = LayerMask.GetMask("Items");
            if (_itemLayerMask == 0) // No layer found
            {
                Debug.LogWarning("[ItemHoverManager] 'Items' layer not found. You need to create this layer in Unity's Tags & Layers settings.");
                _itemLayerMask = -1; // Use all layers as fallback
            }
        }
    }
    
    void Update()
    {
        // Only check periodically to save performance
        if (Time.time < _nextCheckTime)
            return;
            
        _nextCheckTime = Time.time + _hoverCheckFrequency;
        
        // Simple raycast from camera through mouse position
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, _maxRaycastDistance, _itemLayerMask))
        {
            // Check if we hit an item
            ItemView itemView = hit.collider.GetComponentInParent<ItemView>();
            if (itemView != null && itemView.Item != null)
            {
                // Only update if it's a different item
                if (_currentHoveredItem != itemView)
                {
                    _currentHoveredItem = itemView;
                    collectionDisplay.DisplayItemDetails(itemView.Item);
                }
            }
            else
            {
                // If we hit something but it's not an item, clear the display
                if (_currentHoveredItem != null)
                {
                    _currentHoveredItem = null;
                    collectionDisplay.CloseDetailPanel();
                }
            }
        }
        else
        {
            // If we didn't hit anything, clear the display
            if (_currentHoveredItem != null)
            {
                _currentHoveredItem = null;
                collectionDisplay.CloseDetailPanel();
            }
        }
    }
} 