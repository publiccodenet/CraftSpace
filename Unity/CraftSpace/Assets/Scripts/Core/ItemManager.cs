using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Create this class to manage loading items in batches
public class ItemManager : MonoBehaviour
{
    [SerializeField] private int batchSize = 10;
    [SerializeField] private float delayBetweenBatches = 0.1f;
    
    private Queue<KeyValuePair<string, string>> _pendingItems = new Queue<KeyValuePair<string, string>>();
    private List<GameObject> _loadedItems = new List<GameObject>();
    
    [SerializeField] private ItemLoader _itemLoader;
    
    private void Start()
    {
        if (_itemLoader == null)
        {
            _itemLoader = GetComponent<ItemLoader>();
            if (_itemLoader == null)
            {
                _itemLoader = gameObject.AddComponent<ItemLoader>();
            }
        }
    }
    
    public void QueueItems(string collectionId, string[] itemIds)
    {
        foreach (string itemId in itemIds)
        {
            _pendingItems.Enqueue(new KeyValuePair<string, string>(collectionId, itemId));
        }
        
        // Start loading if not already loading
        if (!IsInvoking(nameof(LoadNextBatch)))
        {
            InvokeRepeating(nameof(LoadNextBatch), 0.1f, delayBetweenBatches);
        }
    }
    
    private void LoadNextBatch()
    {
        int count = 0;
        
        Debug.Log($"Loading batch, {_pendingItems.Count} items remaining in queue");
        
        while (_pendingItems.Count > 0 && count < batchSize)
        {
            var item = _pendingItems.Dequeue();
            StartCoroutine(LoadItemAsync(item.Key, item.Value));
            count++;
        }
        
        if (_pendingItems.Count == 0)
        {
            CancelInvoke(nameof(LoadNextBatch));
            Debug.Log("All items loaded");
        }
    }
    
    private IEnumerator LoadItemAsync(string collectionId, string itemId)
    {
        // Load each item in a separate frame to prevent stuttering
        yield return null;
        
        string resourcePath = $"Content/collections/{collectionId}/items/{itemId}/cover";
        bool exists = Resources.Load<Texture2D>(resourcePath) != null;
        Debug.Log($"Loading item {itemId}, cover exists: {exists}");
        
        // Start the coroutine to load the item model asynchronously
        yield return StartCoroutine(_itemLoader.LoadItemModelAsync(collectionId, itemId, (obj) => {
            if (obj != null)
            {
                _loadedItems.Add(obj);
                // Position the object
                obj.transform.position = GetGridPosition(_loadedItems.Count - 1);
            }
        }));
    }
    
    private Vector3 GetGridPosition(int index)
    {
        int rowSize = 10;
        int row = index / rowSize;
        int col = index % rowSize;
        
        return new Vector3(col * 2.0f, 0, row * 2.0f);
    }
    
    public void ClearItems()
    {
        foreach (var item in _loadedItems)
        {
            Destroy(item);
        }
        _loadedItems.Clear();
    }
} 