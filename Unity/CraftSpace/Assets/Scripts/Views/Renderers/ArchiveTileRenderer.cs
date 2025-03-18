using UnityEngine;
using CraftSpace.Models.Schema.Generated;
using TMPro;
using System.Collections.Generic;
using CraftSpace.Utils;
using Type = CraftSpace.Utils.LoggerWrapper.Type;

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
        _titleText.textWrappingMode = TMPro.TextWrappingModes.Normal;
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
    
    public override void UpdateWithItemModel(CraftSpace.Models.Schema.Generated.Item model)
    {
        if (model == null)
            return;
        
        // Ensure we have the title at minimum
        if (string.IsNullOrEmpty(model.Title)) {
            model.Title = model.Id; // Use ID as title fallback
            LoggerWrapper.Warning("ArchiveTileRenderer", "UpdateWithItemModel", "Item missing title", new Dictionary<string, object> {
                { "itemId", model.Id },
                { "usingIdAsTitle", true }
            }, this.gameObject);
        }
        
        // Set title text
        if (_titleText != null)
        {
            _titleText.text = model.Title;  // Just set the title directly, let TMP handle it
        }
        
        // First show placeholder color while loading
        GenerateRandomColorForItem();
        
        // Check if model already has a texture
        if (model.cover != null)
        {
            LoggerWrapper.Info("ArchiveTileRenderer", "UpdateWithItemModel", "Using cached cover image", new Dictionary<string, object> {
                { "itemId", model.Id },
                { "textureSize", $"{model.cover.width}x{model.cover.height}" }
            });
            
            // Use the existing texture
            _tileRenderer.material.shader = Shader.Find("Unlit/Texture");
            _tileRenderer.material.mainTexture = model.cover;
            _tileRenderer.material.color = Color.white;
        }
        else
        {
            // Load from Resources
            string resourcePath = GetTileImageUrl(model);
            
            if (!string.IsNullOrEmpty(resourcePath))
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    OnImageLoaded(texture);
                }
                else 
                {
                    GenerateRandomColorForItem();
                }
            }
        }
    }
    
    private string GetTileImageUrl(Item model)
    {
        LoggerWrapper.Info("ArchiveTileRenderer", "GetTileImageUrl", "Getting resource path", new Dictionary<string, object> { { "itemId", model?.Id ?? "null" } }, this.gameObject);
        
        if (!string.IsNullOrEmpty(model.Id))
        {
            // Don't include file extension - Unity will find the right asset type
            string resourcePath = $"Content/collections/{model.collectionId}/items/{model.Id}/cover";
            return resourcePath;
        }
        
        LoggerWrapper.Warning("ArchiveTileRenderer", "GetTileImageUrl", "Cannot get resource path, no valid ID", new Dictionary<string, object> { { "itemId", model?.Id ?? "null" }, { "modelTitle", model?.Title } }, this.gameObject);
        return null;
    }
    
    private void OnImageLoaded(Texture2D texture)
    {
        if (texture != null && _tileRenderer != null)
        {
            // Apply texture from Resources
            _tileRenderer.material.shader = Shader.Find("Unlit/Texture");
            _tileRenderer.material.mainTexture = texture;
            _tileRenderer.material.color = Color.white;
            
            // Cache in model if possible
            if (_itemView?.Model != null)
            {
                _itemView.Model.cover = texture;
            }
        }
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
            _tileRenderer.material.color = randomColor;
            
            // Use a shader that doesn't require a texture
            if (_tileRenderer.material.shader.name != "Unlit/Color") {
                _tileRenderer.material.shader = Shader.Find("Unlit/Color");
            }
        }
    }
    
    private void GeneratePlaceholderForItem(Item model)
    {
        // Use different colors based on item properties
        _tileRenderer.material.mainTexture = null;
        
        if (!string.IsNullOrEmpty(model.Title) && model.Title.Length > 0)
        {
            // Generate color based on first character of title
            char firstChar = model.Title.Length > 0 ? char.ToUpperInvariant(model.Title[0]) : 'A';
            float hue = (firstChar - 'A') / 26f;
            _tileRenderer.material.color = Color.HSVToRGB(hue, 0.3f, 0.5f);
        }
        else
        {
            _tileRenderer.material.color = _placeholderColor;
        }
    }
} 