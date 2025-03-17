using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using CraftSpace.Utils;
using Type = CraftSpace.Utils.LoggerWrapper.Type;

namespace CraftSpace.Utils
{
    public static class ImageLoader
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
                LoggerWrapper.Info("ImageLoader", "LoadImageFromUrl", "Cache Hit", new Dictionary<string, object> {
                    { "url", url }
                });
                
                onSuccess?.Invoke(_imageCache[url]);
                yield break;
            }
            
            LoggerWrapper.Info("ImageLoader", "LoadImageFromUrl", "Image", new Dictionary<string, object> {
                { "url", url }
            });
            
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    LoggerWrapper.Info("ImageLoader", "LoadImageFromUrl", "Failed", new Dictionary<string, object> {
                        { "url", url },
                        { "error", webRequest.error }
                    });
                    
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
                    
                    LoggerWrapper.Info("ImageLoader", "LoadImageFromUrl", "Success", new Dictionary<string, object> {
                        { "url", url },
                        { "textureSize", $"{texture.width}x{texture.height}" },
                        { "cacheSize", _imageCache.Count }
                    });
                    
                    onSuccess?.Invoke(texture);
                }
                else
                {
                    LoggerWrapper.Info("ImageLoader", "LoadImageFromUrl", "Failed", new Dictionary<string, object> {
                        { "url", url },
                        { "reason", "Texture is null" }
                    });
                    
                    onError?.Invoke("Downloaded texture is null");
                }
            }
        }
        
        // Clear the entire cache
        public static void ClearCache()
        {
            LoggerWrapper.ObjectCache("ImageLoader", "ClearCache", "Clear", new Dictionary<string, object> {
                { "previousSize", _imageCache.Count }
            });
            
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
                
                LoggerWrapper.Info("ImageLoader", "SetMaxCacheSize", "Resize", new Dictionary<string, object> {
                    { "newMaxSize", _maxCacheSize },
                    { "entriesRemoved", removed }
                });
            }
        }
    }
} 