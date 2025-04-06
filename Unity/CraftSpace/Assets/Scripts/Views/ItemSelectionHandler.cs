using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Handles selection events for Item Views
/// </summary>
[RequireComponent(typeof(ItemView))]
public class ItemSelectionHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool _triggerOnClick = true;
    [SerializeField] private CollectionDisplay _collectionDisplay;
    
    [Header("Events")]
    [SerializeField] private UnityEvent<ItemView> _onItemSelected = new UnityEvent<ItemView>();
    
    private ItemView _itemView;
    
    private void Awake()
    {
        _itemView = GetComponent<ItemView>();
        
        // If no collection display is assigned, try to find one in the scene
        if (_collectionDisplay == null)
        {
            _collectionDisplay = FindAnyObjectByType<CollectionDisplay>();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_triggerOnClick)
        {
            SelectItem();
        }
    }
    
    /// <summary>
    /// Select this item and notify listeners
    /// </summary>
    public void SelectItem()
    {
        if (_itemView == null || _itemView.Model == null) return;
        
        // Highlight the item
        _itemView.SetHighlighted(true);
        
        // Notify the collection display
        if (_collectionDisplay != null)
        {
            _collectionDisplay.OnItemSelected(_itemView);
        }
        
        // Invoke the event
        _onItemSelected.Invoke(_itemView);
        
        Debug.Log($"[ItemSelectionHandler] Selected item: {_itemView.Model.Title}");
    }
    
    /// <summary>
    /// Deselect this item
    /// </summary>
    public void DeselectItem()
    {
        if (_itemView != null)
        {
            _itemView.SetHighlighted(false);
        }
    }
} 