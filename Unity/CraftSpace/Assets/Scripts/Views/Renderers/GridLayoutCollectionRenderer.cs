using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CollectionView))]
public class GridLayoutCollectionRenderer : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private int _columns = 3;
    [SerializeField] private Vector2 _spacing = new Vector2(10, 10);
    [SerializeField] private Vector2 _itemSize = new Vector2(200, 300);
    
    private CollectionView _collectionView;
    private GridLayoutGroup _gridLayout;
    
    private void Awake()
    {
        _collectionView = GetComponent<CollectionView>();
        
        // Make sure we have a grid layout component
        _gridLayout = GetComponent<GridLayoutGroup>();
        if (_gridLayout == null)
        {
            _gridLayout = gameObject.AddComponent<GridLayoutGroup>();
        }
        
        // Set up layout
        ConfigureLayout();
        
        // Subscribe to model updates
        _collectionView.OnModelUpdated += UpdateLayout;
    }
    
    private void OnDestroy()
    {
        if (_collectionView != null)
        {
            _collectionView.OnModelUpdated -= UpdateLayout;
        }
    }
    
    private void ConfigureLayout()
    {
        if (_gridLayout != null)
        {
            _gridLayout.cellSize = _itemSize;
            _gridLayout.spacing = _spacing;
            _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            _gridLayout.childAlignment = TextAnchor.UpperLeft;
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = _columns;
        }
    }
    
    private void UpdateLayout()
    {
        // This would be called when the collection model changes
        // Could adjust layout based on number of items, etc.
        ConfigureLayout();
    }
} 