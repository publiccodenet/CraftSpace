using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ViewFactory : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _collectionViewPrefab;
    [SerializeField] private GameObject _itemViewPrefab;
    [SerializeField] private GameObject _itemViewsContainerPrefab;
    [SerializeField] private List<GameObject> _itemViewVariantPrefabs = new List<GameObject>();
    
    [Header("Configuration")]
    [SerializeField] private bool _createWithColliders = true;
    [SerializeField] private string _defaultConfigPath = "Configs/DefaultViewConfig";
    
    [Header("Container References")]
    [SerializeField] private Transform _defaultCollectionContainer;
    [SerializeField] private Transform _defaultItemContainer;
    
    // JSON serializer settings with Unity converters
    private JsonSerializerSettings _serializerSettings;
    
    private void Awake()
    {
        // Initialize serializer settings with custom converters
        _serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace
            // Your custom Unity converters will be registered when JsonConvert is used
        };
    }
    
    // Create a new collection view
    public CollectionView CreateCollectionView(Collection model, Transform container = null, string configJson = null)
    {
        if (_collectionViewPrefab == null)
            return null;
            
        if (container == null)
            container = _defaultCollectionContainer;
            
        if (container == null)
            container = transform;
            
        GameObject viewObj = Instantiate(_collectionViewPrefab, container);
        CollectionView view = viewObj.GetComponent<CollectionView>();
        
        if (view != null)
        {
            // Use SetModel method instead of trying to set Model property directly
            view.SetModel(model);
            
            // Then apply any JSON configuration if provided
            if (!string.IsNullOrEmpty(configJson))
            {
                ConfigureFromJson(view, configJson);
            }
            // Otherwise check for default config
            else if (!string.IsNullOrEmpty(_defaultConfigPath))
            {
                TextAsset defaultConfig = Resources.Load<TextAsset>(_defaultConfigPath + "/collection_view");
                if (defaultConfig != null && !string.IsNullOrEmpty(defaultConfig.text))
                {
                    ConfigureFromJson(view, defaultConfig.text);
                }
            }
        }
        
        return view;
    }
    
    // Create a new item view
    public ItemView CreateItemView(Item model, Transform container = null, string configJson = null)
    {
        if (_itemViewPrefab == null)
            return null;
            
        if (container == null)
            container = _defaultItemContainer;
            
        if (container == null)
            container = transform;
            
        GameObject viewObj = Instantiate(_itemViewPrefab, container);
        ItemView view = viewObj.GetComponent<ItemView>();
        
        if (view != null)
        {
            // Use SetModel method
            view.SetModel(model);
            
            // Apply configuration if provided
            if (!string.IsNullOrEmpty(configJson))
            {
                ConfigureFromJson(view, configJson);
            }
        }
        
        return view;
    }
    
    // Create a container with multiple views for an item
    public ItemViewsContainer CreateItemViewsContainer(Item model, Transform container = null, string configJson = null)
    {
        if (_itemViewsContainerPrefab == null)
            return null;
            
        if (container == null)
            container = _defaultItemContainer;
            
        if (container == null)
            container = transform;
            
        // Create container
        GameObject containerObj = Instantiate(_itemViewsContainerPrefab, container);
        containerObj.name = $"ItemViews_{model?.Id ?? "Unknown"}";
        
        ItemViewsContainer containerComponent = containerObj.GetComponent<ItemViewsContainer>();
        if (containerComponent == null)
        {
            containerComponent = containerObj.AddComponent<ItemViewsContainer>();
        }
        
        // Set the model
        containerComponent.Item = model;
        
        // Apply any JSON configuration
        if (!string.IsNullOrEmpty(configJson))
        {
            ConfigureFromJson(containerComponent, configJson);
        }
        
        // Create child views if not populated through config
        if (containerObj.transform.childCount == 0 && _itemViewVariantPrefabs.Count > 0)
        {
            foreach (var prefab in _itemViewVariantPrefabs)
            {
                if (prefab != null)
                {
                    AddViewToPrefab(containerComponent, prefab);
                }
            }
        }
        
        return containerComponent;
    }
    
    // Add a view to a container
    public ItemView AddViewToPrefab(ItemViewsContainer container, GameObject prefab)
    {
        if (container == null || prefab == null)
            return null;
            
        GameObject viewObj = Instantiate(prefab, container.transform);
        ItemView view = viewObj.GetComponent<ItemView>();
        
        // Items will get their model from the container
        
        return view;
    }
    
    // Configure an object from JSON
    public void ConfigureFromJson<T>(T target, string json) where T : MonoBehaviour
    {
        if (target == null || string.IsNullOrEmpty(json))
            return;
            
        try
        {
            // Use PopulateObject to update existing instance (rather than creating new)
            JsonConvert.PopulateObject(json, target, _serializerSettings);
            
            // For collection view, might need special handling of prefab references
            if (target is CollectionView collectionView)
            {
                // Any special handling for collection view
            }
            else if (target is ItemViewsContainer container)
            {
                // Any special handling for container
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error configuring {typeof(T).Name} from JSON: {e.Message}");
        }
    }
    
    // Load a view configuration from a resource
    public string LoadViewConfigJson(string resourcePath)
    {
        TextAsset config = Resources.Load<TextAsset>(resourcePath);
        return config?.text;
    }
} 