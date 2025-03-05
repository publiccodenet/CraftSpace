using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class CollectionIndex
{
    public string prefix;
    public string subject;
    public string mediatype;
    public int totalItems;
    public string lastUpdated;
    public List<string> chunks;
}

[Serializable]
public class TopLevelIndex
{
    [Serializable]
    public class Collection
    {
        public string prefix;
        public string subject;
        public string mediatype;
        public int totalItems;
        public string lastUpdated;
        public string indexFile;
        public string url;
        public string dataUrl;
    }
    
    public List<Collection> collections;
}

[Serializable]
public class BookMetadata
{
    public string id;
    public string title;
    public string creator;
    public string date;
    public List<string> subject;
    public List<string> collection;
    public string description;
    
    [Serializable]
    public class Icons
    {
        public string _1x1;
        public string _2x3;
    }
    
    public Icons icons;
}

public class CollectionLoader : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "https://spaceship.donhopkins.com/api";
    [SerializeField] private bool useBuiltInDataFirst = true;
    
    private TopLevelIndex _topLevelIndex;
    private Dictionary<string, CollectionIndex> _collectionIndices = new Dictionary<string, CollectionIndex>();
    private Dictionary<string, BookMetadata> _bookMetadata = new Dictionary<string, BookMetadata>();
    
    public event Action<TopLevelIndex> OnTopLevelIndexLoaded;
    public event Action<string, CollectionIndex> OnCollectionIndexLoaded;
    public event Action<string, BookMetadata> OnBookMetadataLoaded;
    
    private void Start()
    {
        StartCoroutine(LoadTopLevelIndex());
    }
    
    private IEnumerator LoadTopLevelIndex()
    {
        bool loaded = false;
        
        // First try to load from embedded Resources if enabled
        if (useBuiltInDataFirst)
        {
            TextAsset indexAsset = Resources.Load<TextAsset>("Collections/index");
            if (indexAsset != null)
            {
                _topLevelIndex = JsonUtility.FromJson<TopLevelIndex>(indexAsset.text);
                OnTopLevelIndexLoaded?.Invoke(_topLevelIndex);
                loaded = true;
                Debug.Log("Loaded top-level index from Resources");
            }
        }
        
        // If not loaded from Resources or that's disabled, try from API
        if (!loaded)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/collections"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    _topLevelIndex = JsonUtility.FromJson<TopLevelIndex>(request.downloadHandler.text);
                    OnTopLevelIndexLoaded?.Invoke(_topLevelIndex);
                    Debug.Log("Loaded top-level index from API");
                }
                else
                {
                    Debug.LogError($"Failed to load top-level index: {request.error}");
                }
            }
        }
    }
    
    public void LoadCollectionIndex(string prefix)
    {
        StartCoroutine(LoadCollectionIndexCoroutine(prefix));
    }
    
    private IEnumerator LoadCollectionIndexCoroutine(string prefix)
    {
        bool loaded = false;
        
        // First try to load from embedded Resources if enabled
        if (useBuiltInDataFirst)
        {
            TextAsset indexAsset = Resources.Load<TextAsset>($"Collections/{prefix}/{prefix}_index");
            if (indexAsset != null)
            {
                CollectionIndex collectionIndex = JsonUtility.FromJson<CollectionIndex>(indexAsset.text);
                _collectionIndices[prefix] = collectionIndex;
                OnCollectionIndexLoaded?.Invoke(prefix, collectionIndex);
                loaded = true;
                Debug.Log($"Loaded collection index for {prefix} from Resources");
            }
        }
        
        // If not loaded from Resources or that's disabled, try from API
        if (!loaded)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/collections/{prefix}"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    CollectionIndex collectionIndex = JsonUtility.FromJson<CollectionIndex>(request.downloadHandler.text);
                    _collectionIndices[prefix] = collectionIndex;
                    OnCollectionIndexLoaded?.Invoke(prefix, collectionIndex);
                    Debug.Log($"Loaded collection index for {prefix} from API");
                }
                else
                {
                    Debug.LogError($"Failed to load collection index for {prefix}: {request.error}");
                }
            }
        }
    }
    
    public void LoadBookMetadata(string prefix, string bookId)
    {
        StartCoroutine(LoadBookMetadataCoroutine(prefix, bookId));
    }
    
    private IEnumerator LoadBookMetadataCoroutine(string prefix, string bookId)
    {
        bool loaded = false;
        
        // First try to load from embedded Resources if enabled
        if (useBuiltInDataFirst)
        {
            TextAsset metadataAsset = Resources.Load<TextAsset>($"Collections/{prefix}/{bookId}/metadata");
            if (metadataAsset != null)
            {
                BookMetadata metadata = JsonUtility.FromJson<BookMetadata>(metadataAsset.text);
                _bookMetadata[bookId] = metadata;
                OnBookMetadataLoaded?.Invoke(bookId, metadata);
                loaded = true;
                Debug.Log($"Loaded metadata for {bookId} from Resources");
            }
        }
        
        // If not loaded from Resources or that's disabled, try from API
        if (!loaded)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/collections/{prefix}/{bookId}"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    BookMetadata metadata = JsonUtility.FromJson<BookMetadata>(request.downloadHandler.text);
                    _bookMetadata[bookId] = metadata;
                    OnBookMetadataLoaded?.Invoke(bookId, metadata);
                    Debug.Log($"Loaded metadata for {bookId} from API");
                }
                else
                {
                    Debug.LogError($"Failed to load metadata for {bookId}: {request.error}");
                }
            }
        }
    }
    
    // Add methods for generating texture atlases from color data
    public void GenerateAtlasForCollection(string prefix)
    {
        // Implementation for creating texture atlases from book metadata
        // using the techniques described in BookCoverVisualization.md
    }
} 