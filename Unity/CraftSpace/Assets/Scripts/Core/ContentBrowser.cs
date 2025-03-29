using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class ContentBrowser : MonoBehaviour
{
    [Header("UI References")]
    public Transform collectionsContainer;
    public Transform itemsContainer;
    public GameObject collectionPrefab;
    public GameObject itemPrefab;
    
    [Header("Item Detail")]
    public Image detailCover;
    public TextMeshProUGUI detailTitle;
    public TextMeshProUGUI detailAuthor;
    public TextMeshProUGUI detailDescription;
    
    private List<GameObject> instantiatedCollections = new List<GameObject>();
    private List<GameObject> instantiatedItems = new List<GameObject>();
    
    void Start()
    {
        if (Brewster.Instance != null)
        {
            PopulateCollections();
        }
        else
        {
            Debug.LogError("Brewster instance not found!");
        }
    }
    
    void PopulateCollections()
    {
        // Clear existing
        ClearCollections();
        
        foreach (var collection in Brewster.Instance.collections)
        {
            GameObject collectionObj = Instantiate(collectionPrefab, collectionsContainer);
            
            // Set up collection UI
            collectionObj.GetComponentInChildren<TextMeshProUGUI>().text = collection.Name;
            Image image = collectionObj.GetComponentInChildren<Image>();
            if (image != null)
            {
                // Check if thumbnail exists, otherwise try to load it
                if (collection.thumbnail == null && !string.IsNullOrEmpty(collection.ThumbnailUrl))
                {
                    // This is a placeholder - in a real implementation you'd load the texture from the URL
                    // For now, we'll log a warning
                    Debug.LogWarning($"Thumbnail not loaded for collection {collection.Name}. URL: {collection.ThumbnailUrl}");
                }
                
                if (collection.thumbnail != null)
                {
                    image.sprite = Sprite.Create(
                        collection.thumbnail,
                        new Rect(0, 0, collection.thumbnail.width, collection.thumbnail.height),
                        Vector2.one * 0.5f
                    );
                }
            }
            
            // Add click handler
            Button button = collectionObj.GetComponent<Button>();
            if (button != null)
            {
                var capturedCollection = collection; // Capture for closure
                button.onClick.AddListener(() => ShowCollectionItems(capturedCollection));
            }
            
            instantiatedCollections.Add(collectionObj);
        }
    }
    
    void ShowCollectionItems(Collection collection)
    {
        // Clear existing items
        ClearItems();
        
        foreach (var item in collection.items)
        {
            GameObject itemObj = Instantiate(itemPrefab, itemsContainer);
            
            // Set up item UI
            itemObj.GetComponentInChildren<TextMeshProUGUI>().text = item.Title;
            Image image = itemObj.GetComponentInChildren<Image>();
            if (image != null && item.cover != null)
            {
                image.sprite = Sprite.Create(
                    item.cover,
                    new Rect(0, 0, item.cover.width, item.cover.height),
                    Vector2.one * 0.5f
                );
            }
            
            // Add click handler
            Button button = itemObj.GetComponent<Button>();
            if (button != null)
            {
                var capturedItem = item; // Capture for closure
                button.onClick.AddListener(() => ShowItemDetail(capturedItem));
            }
            
            instantiatedItems.Add(itemObj);
        }
    }
    
    void ShowItemDetail(Item item)
    {
        if (detailCover != null && item.cover != null)
        {
            detailCover.sprite = Sprite.Create(
                item.cover,
                new Rect(0, 0, item.cover.width, item.cover.height),
                Vector2.one * 0.5f
            );
        }
        
        if (detailTitle != null)
            detailTitle.text = item.Title;
            
        if (detailAuthor != null)
            detailAuthor.text = item.Creator;
            
        if (detailDescription != null)
            detailDescription.text = item.Description;
    }
    
    void ClearCollections()
    {
        foreach (var obj in instantiatedCollections)
        {
            Destroy(obj);
        }
        instantiatedCollections.Clear();
    }
    
    void ClearItems()
    {
        foreach (var obj in instantiatedItems)
        {
            Destroy(obj);
        }
        instantiatedItems.Clear();
    }
} 