using UnityEngine;
using TMPro;
using CraftSpace.Models;
using System.Text;

/// <summary>
/// Renders item metadata using TextMeshPro
/// </summary>
[RequireComponent(typeof(ItemView))]
public class TextMetadataRenderer : ItemViewRenderer
{
    [Header("Text Settings")]
    [SerializeField] private Vector3 _offset = new Vector3(0, 0.5f, 0);
    [SerializeField] private float _width = 2.5f;
    [SerializeField] private float _height = 1.5f;
    [SerializeField] private bool _alwaysFaceCamera = true;
    [SerializeField] private Color _textColor = Color.white;
    [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);
    
    [Header("Content Settings")]
    [SerializeField] private bool _showTitle = true;
    [SerializeField] private bool _showAuthor = true;
    [SerializeField] private bool _showDate = true;
    [SerializeField] private bool _showDescription = true;
    [SerializeField] private bool _showSubjects = true;
    [SerializeField] private int _maxDescriptionLength = 150;
    
    private GameObject _textContainer;
    private TextMeshPro _titleText;
    private TextMeshPro _metadataText;
    private MeshRenderer _backgroundRenderer;
    private Camera _mainCamera;
    private ItemView _itemView;
    
    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
        _itemView = GetComponent<ItemView>();
        
        CreateTextObjects();
    }
    
    private void CreateTextObjects()
    {
        // Create container for text elements
        _textContainer = new GameObject("Metadata_Text");
        _textContainer.transform.SetParent(transform);
        _textContainer.transform.localPosition = _offset;
        
        // Create background
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        background.transform.SetParent(_textContainer.transform);
        background.transform.localPosition = Vector3.zero;
        background.transform.localScale = new Vector3(_width, _height, 1);
        
        _backgroundRenderer = background.GetComponent<MeshRenderer>();
        _backgroundRenderer.material = new Material(Shader.Find("Standard"));
        _backgroundRenderer.material.color = _backgroundColor;
        
        // Remove collider
        Destroy(background.GetComponent<Collider>());
        
        // Create title text
        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(_textContainer.transform);
        titleObj.transform.localPosition = new Vector3(0, _height * 0.35f, -0.01f);
        titleObj.transform.localRotation = Quaternion.identity;
        
        _titleText = titleObj.AddComponent<TextMeshPro>();
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.fontSize = 5;
        _titleText.color = _textColor;
        _titleText.rectTransform.sizeDelta = new Vector2(_width * 0.9f, _height * 0.3f);
        _titleText.enableWordWrapping = true;
        
        // Create metadata text
        GameObject metadataObj = new GameObject("Metadata_Text");
        metadataObj.transform.SetParent(_textContainer.transform);
        metadataObj.transform.localPosition = new Vector3(0, -_height * 0.1f, -0.01f);
        metadataObj.transform.localRotation = Quaternion.identity;
        
        _metadataText = metadataObj.AddComponent<TextMeshPro>();
        _metadataText.alignment = TextAlignmentOptions.TopLeft;
        _metadataText.fontSize = 3;
        _metadataText.color = _textColor;
        _metadataText.rectTransform.sizeDelta = new Vector2(_width * 0.9f, _height * 0.6f);
        _metadataText.enableWordWrapping = true;
        
        // Initially hide the container
        _textContainer.SetActive(false);
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Make text always face camera
        if (_alwaysFaceCamera && _mainCamera != null && _textContainer != null && _textContainer.activeSelf)
        {
            _textContainer.transform.rotation = Quaternion.LookRotation(
                _textContainer.transform.position - _mainCamera.transform.position
            );
        }
    }
    
    public override void Activate()
    {
        base.Activate();
        if (_textContainer != null)
        {
            _textContainer.SetActive(true);
        }
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        if (_textContainer != null)
        {
            _textContainer.SetActive(false);
        }
    }
    
    protected override void OnAlphaChanged(float alpha)
    {
        base.OnAlphaChanged(alpha);
        
        if (_titleText != null)
        {
            Color titleColor = _titleText.color;
            titleColor.a = alpha;
            _titleText.color = titleColor;
        }
        
        if (_metadataText != null)
        {
            Color metaColor = _metadataText.color;
            metaColor.a = alpha;
            _metadataText.color = metaColor;
        }
        
        if (_backgroundRenderer != null)
        {
            Color bgColor = _backgroundColor;
            bgColor.a = _backgroundColor.a * alpha;
            _backgroundRenderer.material.color = bgColor;
        }
    }
    
    public override void UpdateWithItemModel(ItemData model)
    {
        if (model == null)
            return;
            
        // Update title text
        if (_titleText != null)
        {
            _titleText.text = model.title;
        }
        
        // Update metadata text
        if (_metadataText != null)
        {
            StringBuilder sb = new StringBuilder();
            
            if (_showAuthor && !string.IsNullOrEmpty(model.creator))
            {
                sb.AppendLine($"<b>By:</b> {model.creator}");
            }
            
            if (_showDate && !string.IsNullOrEmpty(model.date))
            {
                sb.AppendLine($"<b>Date:</b> {System.DateTime.Parse(model.date).Year}");
            }
            
            if (_showSubjects && model.subject.Count > 0)
            {
                sb.Append("<b>Subjects:</b> ");
                for (int i = 0; i < Mathf.Min(3, model.subject.Count); i++)
                {
                    sb.Append(model.subject[i]);
                    if (i < Mathf.Min(3, model.subject.Count) - 1)
                        sb.Append(", ");
                }
                sb.AppendLine();
            }
            
            if (_showDescription && !string.IsNullOrEmpty(model.description))
            {
                string description = model.description;
                if (description.Length > _maxDescriptionLength)
                {
                    description = description.Substring(0, _maxDescriptionLength) + "...";
                }
                sb.AppendLine($"<b>Description:</b> {description}");
            }
            
            _metadataText.text = sb.ToString();
        }
    }
} 