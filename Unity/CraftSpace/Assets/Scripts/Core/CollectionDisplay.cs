using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the display of collections and item details in the InfoText panel
/// </summary>
public class CollectionDisplay : MonoBehaviour
{
    [Header("Collection Display")]
    [SerializeField] private CollectionView _collectionView;
    
    [Header("Item Detail Display")]
    [SerializeField] private GameObject _itemInfoPanel;
    [SerializeField] private TextMeshProUGUI _itemTitleText;
    
    [Header("Settings")]
    [SerializeField] private bool _loadOnStart = true;
    [SerializeField] private string _defaultCollectionId;
    
    // Cache for collections
    private List<string> _availableCollectionIds = new List<string>();
    private Dictionary<string, Collection> _cachedCollections = new Dictionary<string, Collection>();
    private Item _selectedItem;
    
    private void Start()
    {
        if (_loadOnStart)
        {
            InitializeDisplay();
        }
    }
    
    /// <summary>
    /// Initialize the collection display
    /// </summary>
    public void InitializeDisplay()
    {
        // Check if Brewster is available
        if (Brewster.Instance == null)
        {
            Debug.LogWarning("[CollectionDisplay] Brewster instance not found. Cannot initialize display.");
            return;
        }
        
        // Ensure Brewster is initialized
        if (!Brewster.Instance.IsInitialized)
        {
            Debug.Log("[CollectionDisplay] Brewster is not initialized yet. Initializing now.");
            Brewster.Instance.InitializeRegistry();
        }
        
        // Get available collections
        _availableCollectionIds = Brewster.Instance.GetAllCollectionIds().ToList();
        
        if (_availableCollectionIds.Count == 0)
        {
            Debug.LogWarning("[CollectionDisplay] No collections available to display.");
            return;
        }
        
        // Show default collection or first available
        string collectionToShow = !string.IsNullOrEmpty(_defaultCollectionId) && 
                                _availableCollectionIds.Contains(_defaultCollectionId) 
                                ? _defaultCollectionId 
                                : _availableCollectionIds[0];
                                
        DisplayCollection(collectionToShow);
        
        // Hide item details initially
        HideItemDetails();
    }
    
    /// <summary>
    /// Display a collection by ID
    /// </summary>
    public void DisplayCollection(string collectionId)
    {
        if (string.IsNullOrEmpty(collectionId))
        {
            Debug.LogWarning("[CollectionDisplay] Cannot display collection: collectionId is null or empty");
            return;
        }
        
        Collection collection = GetCollection(collectionId);
        if (collection == null)
        {
            Debug.LogError($"[CollectionDisplay] Failed to load collection with ID: {collectionId}");
            return;
        }
        
        Debug.Log($"[CollectionDisplay] Displaying collection: {collection.Title} (ID: {collection.Id})");
        
        // Set the collection on the view
        if (_collectionView != null)
        {
            _collectionView.SetModel(collection);
        }
        else
        {
            Debug.LogWarning("[CollectionDisplay] No CollectionView assigned. Cannot display collection.");
        }
        
        // Hide detail panel when changing collections
        HideItemDetails();
    }
    
    /// <summary>
    /// Display item title in the InfoText panel
    /// </summary>
    public void DisplayItemDetails(Item item)
    {
        if (item == null)
        {
            HideItemDetails();
            return;
        }
        
        _selectedItem = item;
        
        // Show title in the InfoText component
        if (_itemTitleText != null)
        {
            _itemTitleText.text = item.Title;
            
            // Make panel visible if it exists
            if (_itemInfoPanel != null)
            {
                _itemInfoPanel.SetActive(true);
            }
        }
        else
        {
            Debug.Log($"[CollectionDisplay] No title text component assigned, but would show: '{item.Title}'");
        }
    }
    
    /// <summary>
    /// Hide item details panel
    /// </summary>
    public void HideItemDetails()
    {
        _selectedItem = null;
        
        // Clear title text if present
        if (_itemTitleText != null)
        {
            _itemTitleText.text = "";
        }
        
        // Hide panel if present
        if (_itemInfoPanel != null && _itemInfoPanel.activeSelf)
        {
            _itemInfoPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Clear the detail panel
    /// </summary>
    public void CloseDetailPanel()
    {
        HideItemDetails();
    }
    
    /// <summary>
    /// Get a collection by ID (with caching)
    /// </summary>
    private Collection GetCollection(string collectionId)
    {
        if (string.IsNullOrEmpty(collectionId) || Brewster.Instance == null)
            return null;
            
        // Check cache first
        if (_cachedCollections.TryGetValue(collectionId, out Collection cachedCollection))
        {
            return cachedCollection;
        }
        
        // Load from Brewster if not cached
        Collection collection = Brewster.Instance.GetCollection(collectionId);
        if (collection != null)
        {
            _cachedCollections[collectionId] = collection;
        }
        
        return collection;
    }
    
    /// <summary>
    /// Handle item selection from UI or input
    /// </summary>
    public void OnItemSelected(ItemView itemView)
    {
        if (itemView != null && itemView.Item != null)
        {
            DisplayItemDetails(itemView.Item);
        }
        else
        {
            HideItemDetails();
        }
    }
} 