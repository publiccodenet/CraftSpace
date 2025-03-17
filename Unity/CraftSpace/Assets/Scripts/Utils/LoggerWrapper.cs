using UnityEngine;
using System.Collections.Generic;

namespace CraftSpace.Utils
{
    /// <summary>
    /// Wrapper class for Unity's Debug logging with additional formatting and metadata
    /// </summary>
    public static class LoggerWrapper
    {
        public static class Type
        {
            public const string INFO = "ℹ️";
            public const string SUCCESS = "✅";
            public const string WARNING = "⚠️";
            public const string ERROR = "❌";
            public const string START = "🚀";
            public const string COMPLETE = "🏁";
            public const string STATS = "📊";
            public const string LOAD = "📦";
            public const string SAVE = "💾";
            public const string SEARCH = "🔍";
            public const string PROCESS = "🔄";
            public const string CONFIG = "⚙️";
            public const string DATA = "📄";
            public const string COLLECTION = "📚";
            public const string ITEM = "🧩";
            public const string IMAGE = "🖼️";
            public const string NETWORK = "🌐";
            public const string RENDER = "🎨";
            public const string INSTANTIATE = "🔨";
            public const string VIEW = "👁️";
            public const string MODEL = "📊";
            public const string RESOURCE = "💎";
            public const string PHYSICS = "🧲";
            public const string NAVIGATION = "🧭";
            public const string CACHE = "💽";
            public const string CREATE = "🆕";
            public const string DELETE = "🗑️";
            public const string UPDATE = "🔄";
            public const string INIT = "🏗️";
        }

        private static string FormatLogMessage(string category, string method, string message, Dictionary<string, object> data = null)
        {
            string logMessage = $"[{category}.{method}] {message}";
            
            if (data != null && data.Count > 0)
            {
                logMessage += "\nData: {";
                foreach (var entry in data)
                {
                    logMessage += $"\n  {entry.Key}: {entry.Value}";
                }
                logMessage += "\n}";
            }
            
            return logMessage;
        }

        public static void Info(string category, string method, string message, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, message, data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }

        public static void Warning(string category, string method, string message, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, message, data);
            if (context != null)
                Debug.LogWarning(formattedMessage, context);
            else
                Debug.LogWarning(formattedMessage);
        }

        public static void Error(string category, string method, string message, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, message, data);
            if (context != null)
                Debug.LogError(formattedMessage, context);
            else
                Debug.LogError(formattedMessage);
        }

        public static void Error(string category, string method, string message, Dictionary<string, object> data = null, System.Exception exception = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, message, data);
            if (exception != null)
            {
                formattedMessage += $"\nException: {exception.Message}";
                formattedMessage += $"\nStackTrace: {exception.StackTrace}";
            }
            
            if (context != null)
                Debug.LogError(formattedMessage, context);
            else
                Debug.LogError(formattedMessage);
        }

        public static void Success(string category, string method, string message, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.SUCCESS} {message}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
        
        public static void ModelUpdated(string category, string method, string modelType, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.MODEL}{Type.UPDATE} Model updated: {modelType}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }

        // Methods for Brewster.cs
        public static void LoadStart(string category, string method, string message, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.LOAD}{Type.START} {message}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
        
        public static void LoadComplete(string category, string method, string message, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.LOAD}{Type.COMPLETE} {message}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
        
        // Methods for ArchiveTileRenderer.cs
        public static void ImageLoading(string category, string method, string url, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.IMAGE}{Type.LOAD} Loading image from URL: {url}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
        
        public static void ImageLoaded(string category, string method, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.IMAGE}{Type.COMPLETE} Image loaded successfully", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
        
        public static void ObjectCache(string category, string method, string operation, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.CACHE} Cache {operation}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
        
        public static void NetworkRequest(string category, string method, string url, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.NETWORK} Request to {url}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }

        public static void ItemLoaded(string category, string method, string itemId, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.ITEM}{Type.COMPLETE} Item loaded: {itemId}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }

        public static void CollectionLoaded(string category, string method, string collectionId, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.COLLECTION}{Type.COMPLETE} Collection loaded: {collectionId}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }

        public static void NetworkResponse(string category, string method, string status, Dictionary<string, object> data = null, GameObject context = null)
        {
            string formattedMessage = FormatLogMessage(category, method, $"{Type.NETWORK} Response: {status}", data);
            if (context != null)
                Debug.Log(formattedMessage, context);
            else
                Debug.Log(formattedMessage);
        }
    }
} 