using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the display of a collection and item details
/// </summary>
public class CollectionDisplay : MonoBehaviour
{
    [Header("Collection Display")]
    public CollectionView collectionView;
    
    [Header("Item Detail Display")]
    public GameObject itemInfoPanel;
    public TextMeshProUGUI itemTitleText;
    
    [Header("Settings")]
    public bool loadOnStart = true;
    public string collectionId;
    
    [Header("Input References")]
    public InputManager inputManager;
    
    // Cache for collections
    private Dictionary<string, Collection> cachedCollections = new Dictionary<string, Collection>();
    private Item selectedItem;
    
    private void Start()
    {
        if (loadOnStart)
        {
            InitializeDisplay();
        }
        
        // Subscribe to InputManager events if available
        
        if (inputManager != null)
        {
            inputManager.OnItemHoverStart.AddListener(HandleItemHoverStart);
            inputManager.OnItemHoverEnd.AddListener(HandleItemHoverEnd);
            inputManager.OnItemSelected.AddListener(HandleItemSelected);
            inputManager.OnItemDeselected.AddListener(HandleItemDeselected);
        }
        else
        {
            Debug.LogWarning("No InputManager found. Item hover/selection display will not function.");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (inputManager != null)
        {
            inputManager.OnItemHoverStart.RemoveListener(HandleItemHoverStart);
            inputManager.OnItemHoverEnd.RemoveListener(HandleItemHoverEnd);
            inputManager.OnItemSelected.RemoveListener(HandleItemSelected);
            inputManager.OnItemDeselected.RemoveListener(HandleItemDeselected);
        }
    }
    
    // Event handlers
    private void HandleItemHoverStart(ItemView itemView)
    {
        if (itemView != null && itemView.Model != null)
        {
            DisplayItemDetails(itemView.Model);
        }
    }
    
    private void HandleItemHoverEnd(ItemView itemView)
    {
        // Only hide if we're not currently showing a selected item
        if (selectedItem == null)
        {
            HideItemDetails();
        }
    }
    
    private void HandleItemSelected(ItemView itemView)
    {
        if (itemView != null && itemView.Model != null)
        {
            selectedItem = itemView.Model;
            DisplayItemDetails(itemView.Model);
        }
    }
    
    private void HandleItemDeselected(ItemView itemView)
    {
        selectedItem = null;
        HideItemDetails();
    }
    
    /// <summary>
    /// Initialize the collection display
    /// </summary>
    public void InitializeDisplay()
    {
        // Show the specified collection
        if (!string.IsNullOrEmpty(collectionId))
        {
            DisplayCollection(collectionId);
        }
        else
        {
            Debug.LogError("No collection ID specified.");
        }
        
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
            Debug.LogWarning("Cannot display collection: collectionId is null or empty");
            return;
        }
        
        // Get collection asynchronously using callback
        Collection collection = Brewster.Instance.GetCollection(collectionId);
        OnCollectionLoaded(collection);
    }
    
    /// <summary>
    /// Callback when collection is loaded
    /// </summary>
    private void OnCollectionLoaded(Collection collection)
    {
        if (collection == null)
        {
            Debug.LogError($"Failed to load collection");
            return;
        }
        
        // Set the collection on the view
        if (collectionView != null)
        {
            collectionView.SetModel(collection);
        }
        else
        {
            Debug.LogWarning("No CollectionView assigned. Cannot display collection.");
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
        
        // Show title in the InfoText component
        if (itemTitleText != null)
        {
            itemTitleText.text = item.Title;
            
            // Make panel visible if it exists
            if (itemInfoPanel != null)
            {
                itemInfoPanel.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Hide item details panel
    /// </summary>
    public void HideItemDetails()
    {
        // Clear title text if present
        if (itemTitleText != null)
        {
            itemTitleText.text = "";
        }
        
        // Hide panel if present
        if (itemInfoPanel != null && itemInfoPanel.activeSelf)
        {
            itemInfoPanel.SetActive(false);
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
    /// Handle item selection from UI or input
    /// </summary>
    public void OnItemSelected(ItemView itemView)
    {
        if (itemView != null && itemView.Model != null)
        {
            selectedItem = itemView.Model;
            DisplayItemDetails(itemView.Model);
        }
        else
        {
            HideItemDetails();
        }
    }
} 