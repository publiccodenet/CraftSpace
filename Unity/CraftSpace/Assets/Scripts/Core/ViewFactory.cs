using UnityEngine;
using CraftSpace.Models.Schema.Generated;

public class ViewFactory : MonoBehaviour
{
    [Header("View Prefabs")]
    [SerializeField] private GameObject _collectionViewPrefab;
    [SerializeField] private GameObject _itemViewPrefab;
    
    [Header("Container References")]
    [SerializeField] private Transform _defaultCollectionContainer;
    [SerializeField] private Transform _defaultItemContainer;
    
    // Create a collection view
    public CollectionView CreateCollectionView(CraftSpace.Models.Schema.Generated.Collection model, Transform container = null)
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
            view.Model = model;
        }
        
        return view;
    }
    
    // Create an item view
    public ItemView CreateItemView(CraftSpace.Models.Schema.Generated.Item model, Transform container = null)
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
            view.Model = model;
        }
        
        return view;
    }
} 