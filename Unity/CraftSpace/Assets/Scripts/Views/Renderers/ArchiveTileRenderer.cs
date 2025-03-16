using UnityEngine;
using CraftSpace.Models;
using TMPro;
using System.Collections;

[RequireComponent(typeof(ItemView))]
public class ArchiveTileRenderer : ItemViewRenderer
{
    // Standard tile dimensions used by Internet Archive
    public static readonly Vector2 STANDARD_TILE_SIZE = new Vector2(180, 180);
    
    [Header("Tile Settings")]
    [SerializeField] private float _tileSize = 1.5f;
    [SerializeField] private float _titleOffset = 0.1f;
    [SerializeField] private float _titleHeight = 0.4f;
    [SerializeField] private int _maxTitleLength = 25;
    [SerializeField] private Color _textColor = Color.white;
    [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    
    [Header("Placeholder Settings")]
    [SerializeField] private Color _placeholderColor = new Color(0.3f, 0.3f, 0.3f);
    
    private GameObject _tileObject;
    private GameObject _titleObject;
    private MeshRenderer _tileRenderer;
    private TextMeshPro _titleText;
    private ItemView _itemView;
    private Coroutine _loadImageCoroutine;
    private bool _isImageLoading = false;
    
    protected override void Awake()
    {
        base.Awake();
        _itemView = GetComponent<ItemView>();
        
        CreateTileObjects();
    }
    
    private void CreateTileObjects()
    {
        // Create container for tile image
        _tileObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _tileObject.name = "Tile_Image";
        _tileObject.transform.SetParent(transform);
        _tileObject.transform.localPosition = Vector3.zero;
        _tileObject.transform.localScale = new Vector3(_tileSize, _tileSize, 1);
        
        // Remove collider
        Destroy(_tileObject.GetComponent<Collider>());
        
        // Configure renderer
        _tileRenderer = _tileObject.GetComponent<MeshRenderer>();
        _tileRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        _tileRenderer.material.color = _placeholderColor;
        
        // Create title text
        _titleObject = new GameObject("Tile_Title");
        _titleObject.transform.SetParent(transform);
        _titleObject.transform.localPosition = new Vector3(0, -(_tileSize/2 + _titleOffset + _titleHeight/2), 0);
        
        _titleText = _titleObject.AddComponent<TextMeshPro>();
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.fontSize = 8;
        _titleText.color = _textColor;
        _titleText.rectTransform.sizeDelta = new Vector2(_tileSize * 1.2f, _titleHeight);
        _titleText.enableWordWrapping = true;
        _titleText.overflowMode = TextOverflowModes.Ellipsis;
        
        // Add background to text
        GameObject textBackground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        textBackground.name = "Title_Background";
        textBackground.transform.SetParent(_titleObject.transform);
        textBackground.transform.localPosition = new Vector3(0, 0, 0.01f);
        textBackground.transform.localScale = new Vector3(_tileSize * 1.2f, _titleHeight, 1);
        
        // Remove collider from background
        Destroy(textBackground.GetComponent<Collider>());
        
        // Configure background renderer
        MeshRenderer bgRenderer = textBackground.GetComponent<MeshRenderer>();
        bgRenderer.material = new Material(Shader.Find("Unlit/Color"));
        bgRenderer.material.color = _backgroundColor;
        
        // Initially hide
        _tileObject.SetActive(false);
        _titleObject.SetActive(false);
    }
    
    public override void Activate()
    {
        base.Activate();
        if (_tileObject != null && _titleObject != null)
        {
            _tileObject.SetActive(true);
            _titleObject.SetActive(true);
        }
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        if (_tileObject != null && _titleObject != null)
        {
            _tileObject.SetActive(false);
            _titleObject.SetActive(false);
        }
        
        // Cancel image loading if in progress
        if (_loadImageCoroutine != null)
        {
            StopCoroutine(_loadImageCoroutine);
            _loadImageCoroutine = null;
            _isImageLoading = false;
        }
    }
    
    protected override void OnAlphaChanged(float alpha)
    {
        base.OnAlphaChanged(alpha);
        
        if (_tileRenderer != null)
        {
            Color tileColor = _tileRenderer.material.color;
            tileColor.a = alpha;
            _tileRenderer.material.color = tileColor;
        }
        
        if (_titleText != null)
        {
            Color textColor = _titleText.color;
            textColor.a = alpha;
            _titleText.color = textColor;
        }
    }
    
    public override void UpdateWithItemModel(ItemData model)
    {
        if (model == null)
            return;
        
        // Set title text
        if (_titleText != null)
        {
            string titleText = model.title;
            if (titleText.Length > _maxTitleLength)
            {
                titleText = titleText.Substring(0, _maxTitleLength) + "...";
            }
            _titleText.text = titleText;
        }
        
        // Check if model already has a texture
        if (model.cover != null)
        {
            // Use the existing texture
            _tileRenderer.material.mainTexture = model.cover;
            _tileRenderer.material.color = Color.white;
        }
        else
        {
            // Get tile image URL
            string imageUrl = GetTileImageUrl(model);
            
            if (!string.IsNullOrEmpty(imageUrl) && !_isImageLoading)
            {
                // Show random color while loading
                GenerateRandomColorForItem();
                
                // Load image asynchronously
                _isImageLoading = true;
                _loadImageCoroutine = StartCoroutine(
                    ImageLoader.LoadImageFromUrl(
                        imageUrl,
                        OnImageLoaded,
                        OnImageLoadError
                    )
                );
            }
            else if (string.IsNullOrEmpty(imageUrl))
            {
                // No image URL available, use random color
                GenerateRandomColorForItem();
            }
        }
    }
    
    private string GetTileImageUrl(ItemData model)
    {
        // First try the standard Internet Archive thumbnail
        if (!string.IsNullOrEmpty(model.id))
        {
            // Construct URL from item ID (Internet Archive standard service)
            return $"https://archive.org/services/img/{model.id}";
        }
        
        return null;
    }
    
    private void OnImageLoaded(Texture2D texture)
    {
        _isImageLoading = false;
        _loadImageCoroutine = null;
        
        if (texture != null && _tileRenderer != null)
        {
            // Apply texture
            _tileRenderer.material.mainTexture = texture;
            _tileRenderer.material.color = Color.white;
            
            // Update model to cache the texture if possible
            if (_itemView?.Model != null)
            {
                _itemView.Model.cover = texture;
            }
        }
    }
    
    private void OnImageLoadError(string error)
    {
        _isImageLoading = false;
        _loadImageCoroutine = null;
        
        Debug.LogWarning($"Failed to load image: {error}");
        
        // Fall back to placeholder with random color
        GenerateRandomColorForItem();
    }
    
    private void GenerateRandomColorForItem()
    {
        // Generate a pleasant random color
        float hue = UnityEngine.Random.value;
        float saturation = UnityEngine.Random.Range(0.3f, 0.7f); // Mid-range saturation
        float value = UnityEngine.Random.Range(0.5f, 0.9f);      // Brighter values
        
        Color randomColor = Color.HSVToRGB(hue, saturation, value);
        
        if (_tileRenderer != null)
        {
            _tileRenderer.material.mainTexture = null;
            _tileRenderer.material.color = randomColor;
        }
    }
    
    private void GeneratePlaceholderForItem(ItemData model)
    {
        // Use different colors based on item properties
        _tileRenderer.material.mainTexture = null;
        
        if (!string.IsNullOrEmpty(model.title) && model.title.Length > 0)
        {
            // Generate color based on first character of title
            char firstChar = char.ToUpper(model.title[0]);
            float hue = (firstChar - 'A') / 26f;
            _tileRenderer.material.color = Color.HSVToRGB(hue, 0.3f, 0.5f);
        }
        else
        {
            _tileRenderer.material.color = _placeholderColor;
        }
    }
} 