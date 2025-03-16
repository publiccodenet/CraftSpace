using UnityEngine;
using CraftSpace.Models;
using System.Collections.Generic;

/// <summary>
/// Renders an item as a simple pixel icon
/// </summary>
[RequireComponent(typeof(ItemView))]
public class PixelIconRenderer : ItemViewRenderer
{
    [Header("Icon Settings")]
    [SerializeField] private int _pixelSize = 16;
    [SerializeField] private float _iconSize = 1f;
    
    [Header("Color Settings")]
    [SerializeField] private bool _useSubjectColors = true;
    [SerializeField] private Color _defaultColor = Color.white;
    
    private GameObject _iconObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private ItemView _itemView;
    
    // Color mapping for subjects
    private Dictionary<string, Color> _subjectColors = new Dictionary<string, Color>
    {
        { "fiction", new Color(0.3f, 0.5f, 0.9f) },
        { "science", new Color(0.2f, 0.8f, 0.3f) },
        { "history", new Color(0.8f, 0.7f, 0.2f) },
        { "fantasy", new Color(0.7f, 0.3f, 0.9f) },
        { "biography", new Color(0.9f, 0.5f, 0.3f) },
        { "art", new Color(0.5f, 0.8f, 0.9f) },
        { "philosophy", new Color(0.6f, 0.6f, 0.9f) }
    };
    
    protected override void Awake()
    {
        base.Awake();
        _itemView = GetComponent<ItemView>();
        
        CreateIconObject();
    }
    
    private void CreateIconObject()
    {
        // Create container for icon
        _iconObject = new GameObject("Pixel_Icon");
        _iconObject.transform.SetParent(transform);
        _iconObject.transform.localPosition = Vector3.zero;
        
        // Add mesh components
        _meshFilter = _iconObject.AddComponent<MeshFilter>();
        _meshRenderer = _iconObject.AddComponent<MeshRenderer>();
        
        // Create simple quad mesh
        Mesh mesh = new Mesh();
        float halfSize = _iconSize * 0.5f;
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-halfSize, -halfSize, 0),
            new Vector3(halfSize, -halfSize, 0),
            new Vector3(-halfSize, halfSize, 0),
            new Vector3(halfSize, halfSize, 0)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        
        _meshFilter.mesh = mesh;
        
        // Create material
        _meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        _meshRenderer.material.color = _defaultColor;
        
        // Initially hide
        _iconObject.SetActive(false);
    }
    
    private Texture2D GeneratePixelIcon(ItemData model)
    {
        // Create texture
        Texture2D texture = new Texture2D(_pixelSize, _pixelSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // Crisp pixel art
        
        // Get primary color from model
        Color primaryColor = _defaultColor;
        if (_useSubjectColors && model.subject.Count > 0)
        {
            foreach (string subject in model.subject)
            {
                foreach (var key in _subjectColors.Keys)
                {
                    if (subject.ToLowerInvariant().Contains(key))
                    {
                        primaryColor = _subjectColors[key];
                        break;
                    }
                }
            }
        }
        
        // Generate a simple icon based on first character
        char firstChar = model.title.Length > 0 ? char.ToUpperInvariant(model.title[0]) : 'A';
        int charValue = (int)firstChar - 65; // A=0, B=1, etc.
        
        // Create a pattern based on character value
        for (int y = 0; y < _pixelSize; y++)
        {
            for (int x = 0; x < _pixelSize; x++)
            {
                // Border
                if (x == 0 || y == 0 || x == _pixelSize - 1 || y == _pixelSize - 1)
                {
                    texture.SetPixel(x, y, new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.7f));
                    continue;
                }
                
                // Create a procedural pattern based on the character value
                bool setPixel = false;
                
                // Center character pixels
                if (x > _pixelSize / 4 && x < _pixelSize * 3 / 4 && 
                    y > _pixelSize / 4 && y < _pixelSize * 3 / 4)
                {
                    // Basic pixel fonts for A-Z (very simplified)
                    switch (firstChar)
                    {
                        case 'A':
                            setPixel = (x == _pixelSize/2 || y == _pixelSize/2 || y == _pixelSize*3/4);
                            break;
                        case 'B':
                        case 'D':
                        case 'P':
                        case 'R':
                            setPixel = (x == _pixelSize/3 || y == _pixelSize/3 || y == _pixelSize*2/3);
                            break;
                        case 'C':
                        case 'G':
                        case 'O':
                        case 'Q':
                            setPixel = (x == _pixelSize/3 || y == _pixelSize/3 || y == _pixelSize*2/3 || x == _pixelSize*2/3);
                            break;
                        default:
                            // Use hash of title for other characters
                            setPixel = ((x + y + charValue) % 3 == 0);
                            break;
                    }
                }
                
                // Set pixel based on pattern
                if (setPixel)
                {
                    texture.SetPixel(x, y, primaryColor);
                }
                else
                {
                    // Background - slightly transparent
                    Color bgColor = new Color(primaryColor.r * 0.3f, primaryColor.g * 0.3f, primaryColor.b * 0.3f, 0.3f);
                    texture.SetPixel(x, y, bgColor);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    public override void Activate()
    {
        base.Activate();
        if (_iconObject != null)
        {
            _iconObject.SetActive(true);
        }
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        if (_iconObject != null)
        {
            _iconObject.SetActive(false);
        }
    }
    
    protected override void OnAlphaChanged(float alpha)
    {
        base.OnAlphaChanged(alpha);
        
        if (_meshRenderer != null)
        {
            Color color = _meshRenderer.material.color;
            color.a = alpha;
            _meshRenderer.material.color = color;
        }
    }
    
    public override void UpdateWithItemModel(ItemData model)
    {
        if (model == null || _meshRenderer == null)
            return;
        
        // Generate pixel art texture
        Texture2D iconTexture = GeneratePixelIcon(model);
        
        // Apply to material
        _meshRenderer.material.mainTexture = iconTexture;
    }
} 