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
    private HighlightParticleRenderer _currentHoverHighlight;
    private HighlightParticleRenderer _currentSelectionHighlight;
    
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
        // Get or add HighlightParticleRenderer
        _currentHoverHighlight = GetOrAddHighlightRenderer(item.gameObject);
        _currentHoverHighlight.SetColor(_hoverHighlightColor);
        _currentHoverHighlight.Activate();
    }
    
    private void RemoveHoverHighlight()
    {
        if (_currentHoverHighlight != null)
        {
            // Only deactivate if it's not the selection highlight
            if (_currentHoverHighlight != _currentSelectionHighlight)
            {
                _currentHoverHighlight.Deactivate();
            }
            _currentHoverHighlight = null;
        }
    }
    
    private void ApplySelectionHighlight(ItemView item)
    {
        // Get or add HighlightParticleRenderer
        _currentSelectionHighlight = GetOrAddHighlightRenderer(item.gameObject);
        _currentSelectionHighlight.SetColor(_selectedHighlightColor);
        _currentSelectionHighlight.Activate();
    }
    
    private void RemoveSelectionHighlight()
    {
        if (_currentSelectionHighlight != null)
        {
            _currentSelectionHighlight.Deactivate();
            _currentSelectionHighlight = null;
        }
    }
    
    private HighlightParticleRenderer GetOrAddHighlightRenderer(GameObject obj)
    {
        HighlightParticleRenderer renderer = obj.GetComponent<HighlightParticleRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<HighlightParticleRenderer>();
        }
        return renderer;
    }
    
    public ItemView GetSelectedItem()
    {
        return _currentSelectedItem;
    }
} 