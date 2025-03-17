using UnityEngine;
using CraftSpace.Models.Schema.Generated;
using System.Collections.Generic;

/// <summary>
/// Renders a particle effect to highlight items, useful for search results and selections
/// </summary>
[RequireComponent(typeof(ItemView))]
public class HighlightParticleRenderer : ItemViewRenderer
{
    [Header("Particle Settings")]
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private float _particleSize = 0.3f;
    [SerializeField] private float _emissionRadius = 0.5f;
    [SerializeField] private int _maxParticles = 50;
    [SerializeField] private float _particleLifetime = 2.0f;
    
    [Header("Highlight Effect")]
    [SerializeField] private float _pulseSpeed = 1.0f;
    [SerializeField] private float _pulseIntensity = 0.5f;
    
    private GameObject _particleContainer;
    private ParticleSystem _particleSystem;
    private ParticleSystem.MainModule _mainModule;
    private ParticleSystem.EmissionModule _emissionModule;
    private ItemView _itemView;
    
    // Color mapping for highlighting specific types
    private Dictionary<string, Color> _highlightColors = new Dictionary<string, Color>
    {
        { "selected", Color.yellow },
        { "search", new Color(0.0f, 0.8f, 1.0f) },
        { "new", new Color(0.0f, 1.0f, 0.5f) },
        { "popular", new Color(1.0f, 0.5f, 0.0f) }
    };
    
    protected override void Awake()
    {
        base.Awake();
        _itemView = GetComponent<ItemView>();
        
        CreateParticleSystem();
    }
    
    private void CreateParticleSystem()
    {
        // Create container
        _particleContainer = new GameObject("Highlight_Particles");
        _particleContainer.transform.SetParent(transform);
        _particleContainer.transform.localPosition = Vector3.zero;
        
        // Add particle system
        _particleSystem = _particleContainer.AddComponent<ParticleSystem>();
        
        // Configure particle system
        _mainModule = _particleSystem.main;
        _mainModule.startSize = _particleSize;
        _mainModule.startColor = _defaultColor;
        _mainModule.startLifetime = _particleLifetime;
        _mainModule.startSpeed = 0.5f;
        _mainModule.maxParticles = _maxParticles;
        _mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Emission settings
        _emissionModule = _particleSystem.emission;
        _emissionModule.rateOverTime = 10;
        
        // Shape module for emission area
        var shapeModule = _particleSystem.shape;
        shapeModule.shapeType = ParticleSystemShapeType.Sphere;
        shapeModule.radius = _emissionRadius;
        shapeModule.radiusThickness = 0.1f; // Emit from shell
        
        // Size over lifetime
        var sizeModule = _particleSystem.sizeOverLifetime;
        sizeModule.enabled = true;
        
        AnimationCurve sizeOverLifetime = new AnimationCurve();
        sizeOverLifetime.AddKey(0f, 0f);
        sizeOverLifetime.AddKey(0.2f, 1f);
        sizeOverLifetime.AddKey(0.8f, 1f);
        sizeOverLifetime.AddKey(1f, 0f);
        
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLifetime);
        
        // Color over lifetime
        var colorModule = _particleSystem.colorOverLifetime;
        colorModule.enabled = true;
        
        Gradient colorOverLifetime = new Gradient();
        colorOverLifetime.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(_defaultColor, 0f), 
                new GradientColorKey(_defaultColor, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f), 
                new GradientAlphaKey(1f, 0.2f), 
                new GradientAlphaKey(1f, 0.8f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        
        colorModule.color = new ParticleSystem.MinMaxGradient(colorOverLifetime);
        
        // Initially stop
        _particleSystem.Stop();
        _particleContainer.SetActive(false);
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Animate particle system if active
        if (_isActivated && _particleSystem != null)
        {
            // Pulse emission rate
            float pulseFactor = 1.0f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseIntensity;
            _emissionModule.rateOverTime = 10 * pulseFactor;
        }
    }
    
    public override void Activate()
    {
        base.Activate();
        if (_particleContainer != null)
        {
            _particleContainer.SetActive(true);
            _particleSystem.Play();
        }
    }
    
    public override void Deactivate()
    {
        base.Deactivate();
        if (_particleContainer != null)
        {
            _particleSystem.Stop();
            // Delay disabling to allow particles to fade out
            Invoke("DisableContainer", _particleLifetime);
        }
    }
    
    private void DisableContainer()
    {
        if (_particleContainer != null)
        {
            _particleContainer.SetActive(false);
        }
    }
    
    protected override void OnAlphaChanged(float alpha)
    {
        base.OnAlphaChanged(alpha);
        
        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            Color startColor = main.startColor.color;
            startColor.a = alpha;
            main.startColor = startColor;
            
            // Adjust emission rate based on alpha
            _emissionModule.rateOverTime = 10 * alpha;
        }
    }
    
    public void SetHighlightType(string highlightType)
    {
        if (_highlightColors.TryGetValue(highlightType.ToLowerInvariant(), out Color color))
        {
            SetColor(color);
        }
        else
        {
            SetColor(_defaultColor);
        }
    }
    
    public void SetColor(Color color)
    {
        if (_particleSystem != null)
        {
            _mainModule.startColor = color;
            
            // Update color gradient
            var colorModule = _particleSystem.colorOverLifetime;
            Gradient colorOverLifetime = new Gradient();
            colorOverLifetime.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(color, 0f), 
                    new GradientColorKey(color, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(1f, 0.2f), 
                    new GradientAlphaKey(1f, 0.8f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            
            colorModule.color = new ParticleSystem.MinMaxGradient(colorOverLifetime);
        }
    }
    
    public override void UpdateWithItemModel(CraftSpace.Models.Schema.Generated.Item model)
    {
        if (model == null)
            return;
            
        // Determine appropriate color based on model properties
        Color highlightColor = _defaultColor;
        
        // Example of highlighting based on properties:
        // Featured items
        if (model.IsFavorite ?? false)
        {
            highlightColor = _highlightColors["selected"];
        }
        // Popular items
        else if ((model.Downloads ?? 0) > 5000)
        {
            highlightColor = _highlightColors["popular"];
        }
        
        SetColor(highlightColor);
    }
} 