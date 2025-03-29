using UnityEngine;
using System;
using System.Collections.Generic;

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
            
            Debug.Log($"[ApplicationManager] CraftSpace application initializing. Build version: {Application.version}, Platform: {Application.platform}, Debug build: {Debug.isDebugBuild}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        float startupDuration = Time.realtimeSinceStartup - _startupTime;
        
        Debug.Log($"[ApplicationManager] CraftSpace application started. Startup time: {Mathf.RoundToInt(startupDuration * 1000)}ms, Screen size: {Screen.width}x{Screen.height}, Quality level: {QualitySettings.GetQualityLevel()}");
        
        // Log system info if debug is enabled
        if (showDebugInfo)
        {
            Debug.Log($"[ApplicationManager] System information - Device model: {SystemInfo.deviceModel}, Device name: {SystemInfo.deviceName}, Memory: {SystemInfo.systemMemorySize}MB, Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} cores), GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize}MB)");
        }
    }
    
    void OnApplicationQuit()
    {
        Debug.Log("[ApplicationManager] Application shutting down");
    }
} 