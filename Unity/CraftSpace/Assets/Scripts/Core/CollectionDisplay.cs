using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System; // Needed for Action

/// <summary>
/// Manages the display of a collection and item details.
/// Waits for Brewster to fully load content before displaying.
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
    
    // Cache for collections and pending display
    private Dictionary<string, Collection> cachedCollections = new Dictionary<string, Collection>();
    private Item selectedItem;
    
    private void Start()
    {
        // Ensure Brewster instance exists
        if (Brewster.Instance == null)
        {
            Debug.LogError("CollectionDisplay requires Brewster instance in the scene!");
            enabled = false;
            return;
        }

        // Subscribe to Brewster event *before* potentially loading
        Brewster.Instance.OnAllContentLoaded += HandleAllContentLoaded;
        
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
        
        // Unsubscribe from Brewster event
        if (Brewster.Instance != null)
        {
            Brewster.Instance.OnAllContentLoaded -= HandleAllContentLoaded;
        }
    }
    
    // Event handlers
    private void HandleItemHoverStart(ItemView itemView)
    {
        // Debug.Log($"[CollectionDisplay] HandleItemHoverStart for item: {itemView?.Model?.Title ?? "NULL"}");
        if (itemView != null && itemView.Model != null)
        {
            DisplayItemDetails(itemView.Model);
        }
    }
    
    private void HandleItemHoverEnd(ItemView itemView)
    {
        // Debug.Log($"[CollectionDisplay] HandleItemHoverEnd for item: {itemView?.Model?.Title ?? "NULL"}");
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
    /// Display a collection by ID - Now only stores ID, display happens on content loaded
    /// </summary>
    public void DisplayCollection(string collectionId)
    {
        if (string.IsNullOrEmpty(collectionId))
        {
            Debug.LogWarning("Cannot display collection: collectionId is null or empty");
            this.collectionId = null; // Clear potentially invalid ID
            return;
        }
        
        // Store the ID. The actual display will happen in HandleAllContentLoaded
        this.collectionId = collectionId;
        Debug.Log($"CollectionDisplay: Queued display for collection ID: {collectionId}. Waiting for OnAllContentLoaded.");
    }
    
    /// <summary>
    /// Called when Brewster finishes loading all content.
    /// Now responsible for getting and displaying the collection.
    /// </summary>
    private void HandleAllContentLoaded()
    {
        Debug.Log("CollectionDisplay: Received OnAllContentLoaded event.");
        
        // Hide details panel initially now that content is loaded
        HideItemDetails();

        // If we are set to load on start and have a valid ID
        if (loadOnStart && !string.IsNullOrEmpty(collectionId))
        {    
            Debug.Log($"CollectionDisplay: Attempting to get collection '{collectionId}' from Brewster...");
            Collection collection = Brewster.Instance.GetCollection(this.collectionId);

            if (collection != null)
            {
                // Set the collection on the view now that all content is loaded
                if (collectionView != null)
                {
                    Debug.Log($"CollectionDisplay: Setting model on CollectionView for collection '{collection.Id}'.");
                    collectionView.SetModel(collection);
                }
                else
                {    
                    Debug.LogWarning("No CollectionView assigned. Cannot display collection even after load.");
                }
            }
            else
            {
                 Debug.LogError($"CollectionDisplay: Failed to get collection '{this.collectionId}' from Brewster even after OnAllContentLoaded.");
            }
        }
        else
        {
            Debug.Log("CollectionDisplay: Not configured to load a collection on start or collectionId is invalid.");
        }
    }
    
    /// <summary>
    /// Display item title in the InfoText panel
    /// </summary>
    public void DisplayItemDetails(Item item)
    {
        // Debug.Log($"[CollectionDisplay] DisplayItemDetails for item: {item?.Title ?? "NULL"}");
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
        // Debug.Log("[CollectionDisplay] HideItemDetails called.");
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