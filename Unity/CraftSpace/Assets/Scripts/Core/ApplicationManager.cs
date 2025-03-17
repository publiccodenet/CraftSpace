using UnityEngine;
using CraftSpace.Utils;
using System.Collections.Generic;

namespace CraftSpace.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        public static ApplicationManager Instance { get; private set; }

        [Header("Configuration")]
        public bool showDebugInfo = true;
        public bool trackPerformance = true;
        
        private float _startupTime;
        
        void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _startupTime = Time.realtimeSinceStartup;
                
                LoggerWrapper.Info("ApplicationManager", "Awake", "CraftSpace application initializing", new Dictionary<string, object> {
                    { "buildVersion", Application.version },
                    { "platform", Application.platform.ToString() },
                    { "debug", Debug.isDebugBuild }
                });
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            float startupDuration = Time.realtimeSinceStartup - _startupTime;
            
            LoggerWrapper.Success("ApplicationManager", "Start", "CraftSpace application started", new Dictionary<string, object> {
                { "startupTimeMs", Mathf.RoundToInt(startupDuration * 1000) },
                { "screenSize", $"{Screen.width}x{Screen.height}" },
                { "quality", QualitySettings.GetQualityLevel() }
            });
            
            // Log system info if debug is enabled
            if (showDebugInfo)
            {
                LoggerWrapper.Info("ApplicationManager", "Start", "System information", new Dictionary<string, object> {
                    { "deviceModel", SystemInfo.deviceModel },
                    { "deviceName", SystemInfo.deviceName },
                    { "memory", $"{SystemInfo.systemMemorySize}MB" },
                    { "processorType", SystemInfo.processorType },
                    { "processorCount", SystemInfo.processorCount },
                    { "graphicsDeviceName", SystemInfo.graphicsDeviceName },
                    { "graphicsMemory", $"{SystemInfo.graphicsMemorySize}MB" }
                });
            }
        }
        
        void OnApplicationQuit()
        {
            LoggerWrapper.Info("ApplicationManager", "OnApplicationQuit", "Application shutting down");
        }
    }
} 