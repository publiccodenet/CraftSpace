using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class ItemSelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private LayerMask _selectableLayerMask = -1;
    [SerializeField] private float _maxSelectionDistance = 100f;
    [SerializeField] private Color _hoverHighlightColor = new Color(1f, 1f, 0.5f, 0.7f);
    [SerializeField] private Color _selectedHighlightColor = new Color(1f, 0.8f, 0.2f, 0.9f);
    
    [Header("Events")]
    public UnityEvent<ItemView> OnItemSelected;
    public UnityEvent<ItemView> OnItemDeselected;
    public UnityEvent<ItemView> OnItemHoverStart;
    public UnityEvent<ItemView> OnItemHoverEnd;
    
    private Camera _mainCamera;
    private ItemView _currentHoveredItem;
    private ItemView _currentSelectedItem;
    
    // Track original materials for restoring later
    private Dictionary<GameObject, Material[]> _originalMaterials = new Dictionary<GameObject, Material[]>();
    
    private void Start()
    {
        _mainCamera = Camera.main;
    }
    
    private void Update()
    {
        HandleMouseHover();
        HandleMouseClick();
    }
    
    private void HandleMouseHover()
    {
        // Cast ray from mouse position
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, _maxSelectionDistance, _selectableLayerMask))
        {
            // Try to get item view from hit object
            ItemView hitItemView = hit.collider.GetComponentInParent<ItemView>();
            
            if (hitItemView != null)
            {
                // Different from current hover
                if (_currentHoveredItem != hitItemView)
                {
                    // Remove hover from previous item
                    if (_currentHoveredItem != null)
                    {
                        RemoveHoverHighlight();
                        OnItemHoverEnd?.Invoke(_currentHoveredItem);
                    }
                    
                    // Set new hovered item
                    _currentHoveredItem = hitItemView;
                    ApplyHoverHighlight(_currentHoveredItem);
                    OnItemHoverStart?.Invoke(_currentHoveredItem);
                }
            }
        }
        else if (_currentHoveredItem != null)
        {
            // No longer hovering on anything
            RemoveHoverHighlight();
            OnItemHoverEnd?.Invoke(_currentHoveredItem);
            _currentHoveredItem = null;
        }
    }
    
    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Select the currently hovered item
            if (_currentHoveredItem != null)
            {
                SelectItem(_currentHoveredItem);
            }
            else
            {
                // Click on empty space - deselect
                if (_currentSelectedItem != null)
                {
                    DeselectItem();
                }
            }
        }
    }
    
    public void SelectItem(ItemView itemView)
    {
        // Don't re-select same item
        if (_currentSelectedItem == itemView)
            return;
            
        // Deselect current item if exists
        if (_currentSelectedItem != null)
        {
            DeselectItem();
        }
        
        // Set new selection
        _currentSelectedItem = itemView;
        ApplySelectionHighlight(_currentSelectedItem);
        OnItemSelected?.Invoke(_currentSelectedItem);
    }
    
    public void DeselectItem()
    {
        if (_currentSelectedItem != null)
        {
            RemoveSelectionHighlight();
            OnItemDeselected?.Invoke(_currentSelectedItem);
            _currentSelectedItem = null;
        }
    }
    
    private void ApplyHoverHighlight(ItemView item)
    {
        // Simple hover highlight - just use debug log for now
        Debug.Log($"Hovering over {item.name}");
        
        // For a real implementation, we would change material color or add a highlight effect
        // ApplyHighlightColor(item.gameObject, _hoverHighlightColor);
    }
    
    private void RemoveHoverHighlight()
    {
        // Only remove hover highlight if it's not the selected item
        if (_currentHoveredItem != null && _currentHoveredItem != _currentSelectedItem)
        {
            // RestoreOriginalMaterials(_currentHoveredItem.gameObject);
        }
    }
    
    private void ApplySelectionHighlight(ItemView item)
    {
        // Simple selection highlight - just use debug log for now
        Debug.Log($"Selected {item.name}");
        
        // For a real implementation, we would change material color or add a highlight effect
        // ApplyHighlightColor(item.gameObject, _selectedHighlightColor);
    }
    
    private void RemoveSelectionHighlight()
    {
        if (_currentSelectedItem != null)
        {
            // RestoreOriginalMaterials(_currentSelectedItem.gameObject);
        }
    }
    
    // Utility methods for future implementation
    
    private void ApplyHighlightColor(GameObject obj, Color color)
    {
        // Store original materials if we haven't already
        if (!_originalMaterials.ContainsKey(obj))
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                _originalMaterials[obj] = renderer.materials;
                
                // Create new highlight materials
                Material[] highlightMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    highlightMaterials[i] = new Material(renderer.materials[i]);
                    highlightMaterials[i].color = color;
                }
                
                // Apply highlight materials
                renderer.materials = highlightMaterials;
            }
        }
    }
    
    private void RestoreOriginalMaterials(GameObject obj)
    {
        if (_originalMaterials.TryGetValue(obj, out Material[] originalMats))
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.materials = originalMats;
            }
            _originalMaterials.Remove(obj);
        }
    }
    
    public ItemView GetSelectedItem()
    {
        return _currentSelectedItem;
    }
} 