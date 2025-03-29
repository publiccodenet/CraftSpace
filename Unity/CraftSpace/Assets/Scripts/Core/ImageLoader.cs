using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class ImageLoader : MonoBehaviour
{
    // Cache for images to prevent redundant downloads
    private static Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();
    
    // Maximum cache size to prevent memory issues
    private static int _maxCacheSize = 100;
    
    // Check if an image is already cached
    public static bool IsCached(string url)
    {
        return _imageCache.ContainsKey(url) && _imageCache[url] != null;
    }
    
    // Get an image from cache
    public static Texture2D GetCachedImage(string url)
    {
        if (IsCached(url))
        {
            return _imageCache[url];
        }
        return null;
    }
    
    // Coroutine to load an image from a URL
    public static IEnumerator LoadImageFromUrl(string url, System.Action<Texture2D> onSuccess, System.Action<string> onError)
    {
        // Check cache first
        if (IsCached(url))
        {
            Debug.Log($"ImageLoader: Cache Hit for {url}");
            
            onSuccess?.Invoke(_imageCache[url]);
            yield break;
        }
        
        Debug.Log($"ImageLoader: Loading image from {url}");
        
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"ImageLoader: Failed to load {url} - {webRequest.error}");
                onError?.Invoke(webRequest.error);
                yield break;
            }
            
            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            
            if (texture != null)
            {
                // Update cache (manage cache size)
                if (_imageCache.Count >= _maxCacheSize)
                {
                    // Remove oldest entry (or least recently used in a more sophisticated implementation)
                    string oldestKey = null;
                    foreach (var key in _imageCache.Keys)
                    {
                        oldestKey = key;
                        break;
                    }
                    
                    if (oldestKey != null)
                    {
                        _imageCache.Remove(oldestKey);
                    }
                }
                
                _imageCache[url] = texture;
                
                Debug.Log($"ImageLoader: Successfully loaded {url} ({texture.width}x{texture.height})");
                
                onSuccess?.Invoke(texture);
            }
            else
            {
                Debug.LogError($"ImageLoader: Downloaded texture is null for {url}");
                onError?.Invoke("Downloaded texture is null");
            }
        }
    }
    
    // Clear the entire cache
    public static void ClearCache()
    {
        Debug.Log($"ImageLoader: Clearing cache (previous size: {_imageCache.Count})");
        _imageCache.Clear();
    }
    
    // Set maximum cache size
    public static void SetMaxCacheSize(int size)
    {
        _maxCacheSize = Mathf.Max(1, size);
        
        // Trim cache if needed
        if (_imageCache.Count > _maxCacheSize)
        {
            int removeCount = _imageCache.Count - _maxCacheSize;
            int removed = 0;
            
            List<string> keysToRemove = new List<string>();
            foreach (var key in _imageCache.Keys)
            {
                keysToRemove.Add(key);
                removed++;
                
                if (removed >= removeCount)
                    break;
            }
            
            foreach (var key in keysToRemove)
            {
                _imageCache.Remove(key);
            }
            
            Debug.Log($"ImageLoader: Resized cache to {_maxCacheSize} (removed {removed} entries)");
        }
    }
} 