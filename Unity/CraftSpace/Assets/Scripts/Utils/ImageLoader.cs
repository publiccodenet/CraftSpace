using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

public static class ImageLoader
{
    // Dictionary to cache loaded textures to prevent redundant downloads
    private static readonly Dictionary<string, Texture2D> _textureCache = 
        new Dictionary<string, Texture2D>();
    
    // Loads an image from URL with optional callback when complete
    public static IEnumerator LoadImageFromUrl(
        string url, 
        Action<Texture2D> onComplete, 
        Action<string> onError = null)
    {
        // Check cache first
        if (_textureCache.TryGetValue(url, out Texture2D cachedTexture))
        {
            onComplete?.Invoke(cachedTexture);
            yield break;
        }
        
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(www.error);
                yield break;
            }
            
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            _textureCache[url] = texture;
            onComplete?.Invoke(texture);
        }
    }
    
    // Clear the texture cache
    public static void ClearCache()
    {
        _textureCache.Clear();
    }
} 