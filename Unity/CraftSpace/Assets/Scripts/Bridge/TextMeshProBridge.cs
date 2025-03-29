using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Bridge for TextMeshPro with JSON-friendly properties and advanced text features
/// </summary>

[AddComponentMenu("Bridge/TextMeshPro Bridge")]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextMeshProBridge : BridgeObject
{
    // TextMeshPro component reference
    private TextMeshProUGUI _textComponent;
    
    // Animation and effect settings
    [SerializeField] private float _typewriterSpeed = 20f;
    [SerializeField] private bool _useTypewriterEffect = false;
    [SerializeField] private bool _rainbowText = false;
    [SerializeField] private float _rainbowSpeed = 0.5f;
    [SerializeField] private float _rainbowSaturation = 0.7f;
    [SerializeField] private float _rainbowBrightness = 0.8f;
    
    // Working variables
    private string _fullText = "";
    private Coroutine _typewriterCoroutine;
    private Coroutine _colorAnimationCoroutine;
    
    // Protected method to safely handle property changes
    protected void NotifyPropertyChanged(string key, object value)
    {
        // Use the base class SetProperty method which will properly handle the event
        base.SetProperty(key, value);
    }
    
    // Text property with typewriter effect support
    public string Text
    {
        get => _fullText;
        set
        {
            _fullText = value;
            
            if (_useTypewriterEffect && gameObject.activeInHierarchy)
            {
                StartTypewriter();
            }
            else
            {
                if (_textComponent != null)
                {
                    _textComponent.text = _fullText;
                }
            }
            
            // Notify property change
            NotifyPropertyChanged("Text", value);
        }
    }
    
    // JSON-friendly font style property
    public string FontStyle
    {
        get => _textComponent?.fontStyle.ToString() ?? "Normal";
        set
        {
            if (_textComponent != null && Enum.TryParse<FontStyles>(value, true, out var style))
            {
                _textComponent.fontStyle = style;
                NotifyPropertyChanged("FontStyle", value);
            }
        }
    }
    
    // Font size property
    public float FontSize
    {
        get => _textComponent?.fontSize ?? 36f;
        set
        {
            if (_textComponent != null)
            {
                _textComponent.fontSize = value;
                NotifyPropertyChanged("FontSize", value);
            }
        }
    }
    
    // Font color property
    public Color TextColor
    {
        get => _textComponent?.color ?? Color.white;
        set
        {
            if (_textComponent != null)
            {
                _textComponent.color = value;
                NotifyPropertyChanged("TextColor", value);
            }
        }
    }
    
    // Alignment property with string conversion
    public string Alignment
    {
        get => _textComponent?.alignment.ToString() ?? "TopLeft";
        set
        {
            if (_textComponent != null && Enum.TryParse<TextAlignmentOptions>(value, true, out var alignment))
            {
                _textComponent.alignment = alignment;
                NotifyPropertyChanged("Alignment", value);
            }
        }
    }
    
    // Typewriter effect property
    public bool UseTypewriterEffect
    {
        get => _useTypewriterEffect;
        set
        {
            _useTypewriterEffect = value;
            if (value && gameObject.activeInHierarchy && _textComponent != null)
            {
                StartTypewriter();
            }
            NotifyPropertyChanged("UseTypewriterEffect", value);
        }
    }
    
    // Typewriter speed property
    public float TypewriterSpeed
    {
        get => _typewriterSpeed;
        set
        {
            _typewriterSpeed = Mathf.Max(1, value);
            NotifyPropertyChanged("TypewriterSpeed", value);
        }
    }
    
    // Rainbow text property
    public bool RainbowText
    {
        get => _rainbowText;
        set
        {
            _rainbowText = value;
            
            if (value && gameObject.activeInHierarchy)
            {
                StartRainbowEffect();
            }
            else if (!value && _colorAnimationCoroutine != null)
            {
                StopCoroutine(_colorAnimationCoroutine);
                _colorAnimationCoroutine = null;
                
                // Reset to default color
                if (_textComponent != null)
                {
                    _textComponent.color = Color.white;
                }
            }
            
            NotifyPropertyChanged("RainbowText", value);
        }
    }
    
    // Initialize on awake
    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
        if (_textComponent == null)
        {
            _textComponent = gameObject.AddComponent<TextMeshProUGUI>();
        }
        
        _fullText = _textComponent.text;
    }
    
    // Initialize on enable
    private void OnEnable()
    {
        if (_useTypewriterEffect)
        {
            StartTypewriter();
        }
        
        if (_rainbowText)
        {
            StartRainbowEffect();
        }
    }
    
    // Clean up on disable
    private void OnDisable()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        
        if (_colorAnimationCoroutine != null)
        {
            StopCoroutine(_colorAnimationCoroutine);
            _colorAnimationCoroutine = null;
        }
    }
    
    // Override JSON configuration
    public override void ConfigureFromJson(string json)
    {
        base.ConfigureFromJson(json);
        
        // Special handling for rich text
        if (_properties.TryGetValue("richText", out var richTextObj) && richTextObj is string richText)
        {
            SetRichText(richText);
        }
        
        // Special handling for gradients
        if (_properties.TryGetValue("gradient", out var gradientObj) && gradientObj is JObject gradientJObj)
        {
            Color topColor = Color.white;
            Color bottomColor = Color.white;
            
            if (gradientJObj["topColor"] is JObject topColorObj)
            {
                topColor = new Color(
                    topColorObj["r"]?.Value<float>() ?? 1f,
                    topColorObj["g"]?.Value<float>() ?? 1f,
                    topColorObj["b"]?.Value<float>() ?? 1f,
                    topColorObj["a"]?.Value<float>() ?? 1f
                );
            }
            
            if (gradientJObj["bottomColor"] is JObject bottomColorObj)
            {
                bottomColor = new Color(
                    bottomColorObj["r"]?.Value<float>() ?? 1f,
                    bottomColorObj["g"]?.Value<float>() ?? 1f,
                    bottomColorObj["b"]?.Value<float>() ?? 1f,
                    bottomColorObj["a"]?.Value<float>() ?? 1f
                );
            }
            
            ApplyGradient(topColor, bottomColor);
        }
    }
    
    /// <summary>
    /// Start the typewriter effect
    /// </summary>
    public void StartTypewriter()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }
        
        _typewriterCoroutine = StartCoroutine(TypewriterEffect());
    }
    
    /// <summary>
    /// Start the rainbow text effect
    /// </summary>
    public void StartRainbowEffect()
    {
        if (_colorAnimationCoroutine != null)
        {
            StopCoroutine(_colorAnimationCoroutine);
        }
        
        _colorAnimationCoroutine = StartCoroutine(RainbowEffect());
    }
    
    /// <summary>
    /// Apply rich text formatting
    /// </summary>
    public void SetRichText(string richText)
    {
        if (_textComponent != null)
        {
            _textComponent.richText = true;
            _fullText = richText;
            
            if (_useTypewriterEffect)
            {
                StartTypewriter();
            }
            else
            {
                _textComponent.text = richText;
            }
        }
    }
    
    /// <summary>
    /// Apply a color gradient to the text
    /// </summary>
    public void ApplyGradient(Color topColor, Color bottomColor)
    {
        if (_textComponent != null)
        {
            _textComponent.enableVertexGradient = true;
            _textComponent.colorGradient = new VertexGradient(
                topColor,
                topColor,
                bottomColor,
                bottomColor
            );
        }
    }
    
    /// <summary>
    /// Set horizontal and vertical overflow modes
    /// </summary>
    public void SetOverflowModes(string horizontalMode, string verticalMode)
    {
        if (_textComponent != null)
        {
            if (Enum.TryParse<HorizontalAlignmentOptions>(horizontalMode, true, out var hAlign))
            {
                _textComponent.horizontalAlignment = hAlign;
            }
            
            if (Enum.TryParse<VerticalAlignmentOptions>(verticalMode, true, out var vAlign))
            {
                _textComponent.verticalAlignment = vAlign;
            }
        }
    }
    
    /// <summary>
    /// Apply a shaking effect to the text
    /// </summary>
    public void ShakeText(float duration = 1.0f, float intensity = 5.0f)
    {
        StartCoroutine(ShakeEffect(duration, intensity));
    }
    
    /// <summary>
    /// Typewriter coroutine
    /// </summary>
    private IEnumerator TypewriterEffect()
    {
        if (_textComponent == null) yield break;
        
        _textComponent.text = "";
        
        // Extract potential rich text tags
        List<(int index, string tag)> richTextTags = new List<(int index, string tag)>();
        for (int i = 0; i < _fullText.Length; i++)
        {
            if (_fullText[i] == '<')
            {
                int endIndex = _fullText.IndexOf('>', i);
                if (endIndex > i)
                {
                    richTextTags.Add((i, _fullText.Substring(i, endIndex - i + 1)));
                    i = endIndex;
                }
            }
        }
        
        // Type each character with appropriate tags
        int currentTagIndex = 0;
        string currentText = "";
        
        for (int i = 0; i < _fullText.Length; i++)
        {
            // Check if we need to insert a tag
            while (currentTagIndex < richTextTags.Count && richTextTags[currentTagIndex].index <= i)
            {
                currentText += richTextTags[currentTagIndex].tag;
                currentTagIndex++;
            }
            
            // Add the character if it's not part of a tag
            if (_fullText[i] != '<')
            {
                currentText += _fullText[i];
                _textComponent.text = currentText;
                
                // Wait for the next character
                if (_fullText[i] != ' ')
                {
                    float charDelay = 1.0f / _typewriterSpeed;
                    yield return new WaitForSeconds(charDelay);
                }
            }
            else
            {
                // Skip to end of tag
                int endIndex = _fullText.IndexOf('>', i);
                if (endIndex > i)
                {
                    i = endIndex;
                }
            }
        }
        
        // Ensure final text is complete
        _textComponent.text = _fullText;
        _typewriterCoroutine = null;
    }
    
    /// <summary>
    /// Rainbow text animation coroutine
    /// </summary>
    private IEnumerator RainbowEffect()
    {
        if (_textComponent == null) yield break;
        
        while (_rainbowText)
        {
            float hue = (Time.time * _rainbowSpeed) % 1.0f;
            _textComponent.color = Color.HSVToRGB(hue, _rainbowSaturation, _rainbowBrightness);
            yield return null;
        }
        
        _colorAnimationCoroutine = null;
    }
    
    /// <summary>
    /// Text shaking effect coroutine
    /// </summary>
    private IEnumerator ShakeEffect(float duration, float intensity)
    {
        if (_textComponent == null) yield break;
        
        Vector3 originalPosition = _textComponent.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f) * intensity,
                UnityEngine.Random.Range(-1f, 1f) * intensity,
                0
            );
            
            _textComponent.transform.localPosition = originalPosition + shakeOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _textComponent.transform.localPosition = originalPosition;
    }

}
