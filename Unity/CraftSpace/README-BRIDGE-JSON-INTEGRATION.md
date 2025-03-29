# JSON Integration & BridgeObject Pattern

## Overview

This document explains our approach to configuring and managing Unity objects through JSON, enabling dynamic integration with third-party assets without modification. The system creates seamless bridges between content in our Schema system and any Unity component, including third-party assets.

## Key Features

- **JSON.NET Custom Converters**: Convert between Unity types (Vector3, Quaternion, etc.) and JSON
- **Dynamic BridgeObject Attachment**: Attach to unmodified prefabs at runtime
- **Configuration via JSON**: Apply settings to any object with a BridgeObject component
- **Schema Integration**: Works with our Schema system for content management
- **Specialized Bridge Extensions**: Custom bridges for popular libraries (LeanTween, TextMeshPro, Cinemachine)
- **Input Tracking System**: High-level mouse/touch tracking with optimized network traffic
- **Event System**: JSON-based events for inter-object communication

## JSON.NET Integration

### Custom Type Converters with Flexible Format Support

Our system registers custom converters for Unity-specific types with support for multiple input formats:

```csharp
// Custom converters registered with JSON.NET
public static void RegisterConverters()
{
    JsonConvert.DefaultSettings = () => new JsonSerializerSettings
    {
        Converters = new List<JsonConverter>
        {
            new Vector3Converter(),
            new QuaternionConverter(),
            new ColorConverter(),
            new Matrix4x4Converter(),
            new TransformConverter(),
            new MaterialConverter(),
            // Add other converters as needed
        },
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };
}
```

These converters support multiple input formats while maintaining consistent output:

#### Flexible Input Formats

```json
// Multiple ways to represent a color
{
  // Standard object format
  "color1": {"r": 1.0, "g": 0.5, "b": 0.2, "a": 1.0},
  
  // Hex string format (with or without alpha)
  "color2": "#FF8833",
  "color3": "#FF8833CC",
  
  // Array format [r,g,b,a]
  "color4": [1.0, 0.5, 0.2, 1.0],
  
  // Color name
  "color5": "red",
  
  // HSV format
  "color6": {"h": 0.1, "s": 0.5, "v": 0.9, "a": 1.0}
}

// Multiple ways to represent rotation
{
  // Quaternion format (always used for export)
  "rotation1": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
  
  // Euler angles (degrees)
  "rotation2": {"x": 0, "y": 45, "z": 0},
  
  // Alternative Euler format
  "rotation3": {"pitch": 0, "yaw": 45, "roll": 0},
  
  // Array format [x,y,z,w] for quaternion
  "rotation4": [0.0, 0.0, 0.0, 1.0],
  
  // Array format [x,y,z] for euler
  "rotation5": [0, 45, 0]
}
```

This format flexibility provides significant developer convenience, allowing content creators to use whichever notation feels most natural for their use case.

### Benefits Over Unity's JsonUtility

- Support for multiple input formats with consistent output
- Handles dictionaries, polymorphic types, and complex data structures
- Property-based serialization (not just fields)
- Custom converters for Unity and non-Unity types
- Fine-grained control over serialization behavior

## The BridgeObject Pattern

The BridgeObject component provides dynamic property storage and JSON configuration:

```csharp
public class BridgeObject : MonoBehaviour
{
    // Unique identifier for this bridge object
    public string Id;
    
    // Dynamic properties storage
    private Dictionary<string, object> _properties = new Dictionary<string, object>();
    
    // Event for property changes
    public event Action<string, object> OnPropertyChanged;
    
    // Configure from JSON
    public void ConfigureFromJson(string json)
    {
        var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        foreach (var kvp in settings)
        {
            SetProperty(kvp.Key, kvp.Value);
        }
    }
    
    // Get/Set properties with change notification
    public void SetProperty<T>(string key, T value)
    {
        _properties[key] = value;
        OnPropertyChanged?.Invoke(key, value);
    }
    
    public T GetProperty<T>(string key, T defaultValue = default)
    {
        if (_properties.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;
            
            try
            {
                // Try conversion for flexibility
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch 
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}
```

## Specialized Bridge Extensions

We're extending the base BridgeObject with specialized subclasses to provide seamless integration with popular Unity libraries. These bridges expose JSON-friendly properties and methods that map directly to the library's functionality.

### LeanTween Bridge

The LeanTweenBridge exposes animation capabilities through JSON:

```csharp
[AddComponentMenu("Bridge/Extensions/LeanTween Bridge")]
public class LeanTweenBridge : BridgeObject
{
    // JSON-friendly properties
    public float Duration { get; set; } = 1.0f;
    public string Ease { get; set; } = "easeInOutQuad";
    
    // Animation methods
    public void PlayAnimation(string animationName)
    {
        // Load animation JSON and apply it
    }
    
    // Convenience methods
    public void MoveTo(Vector3 position, float duration = 0)
    {
        // Implementation
    }
    
    public void FadeIn(float duration = 0)
    {
        // Implementation
    }
    
    // ... more methods
}
```

Example JSON animation definition:

```json
[
  {
    "id": "intro",
    "type": "move",
    "duration": 1.5,
    "ease": "easeOutBack",
    "to": {"x": 0, "y": 2, "z": 0},
    "delay": 0.3,
    "loop": false,
    "onComplete": "animationFinished"
  },
  {
    "id": "fade",
    "type": "alpha",
    "duration": 0.8,
    "to": {"value": 1.0},
    "delay": 0.1
  }
]
```

### TextMeshPro Bridge (Planned)

We plan to create a comprehensive TextMeshPro bridge that exposes all its functionality through JSON-friendly properties:

```csharp
[AddComponentMenu("Bridge/Extensions/TextMeshPro Bridge")]
public class TextMeshProBridge : BridgeObject
{
    // Core text properties
    public string Text { get; set; }
    public string Font { get; set; }
    public float FontSize { get; set; }
    public string Color { get; set; }
    
    // Advanced features
    public string Alignment { get; set; } = "center";
    public bool AutoSize { get; set; }
    public float CharacterSpacing { get; set; }
    public string OverflowMode { get; set; } = "truncate";
    
    // Rich text methods
    public void SetRichText(bool enabled) { /* implementation */ }
    
    // Style methods
    public void ApplyStyle(string styleName) { /* implementation */ }
    
    // Animation methods
    public void AnimateText(string animation) { /* implementation */ }
}
```

Example JSON configuration:

```json
{
  "text": "Welcome to <b>CraftSpace</b>",
  "font": "Fonts/Roboto-Bold",
  "fontSize": 24,
  "color": "#FFFFFF",
  "alignment": "center",
  "autoSize": true,
  "characterSpacing": 1.1,
  "overflowMode": "overflow",
  "richText": true,
  "wordWrapping": true,
  "style": "title",
  "animations": ["fadeIn", "pulse"]
}
```

### Cinemachine Bridge (Planned)

A bridge for Cinemachine to control virtual cameras through JSON:

```csharp
[AddComponentMenu("Bridge/Extensions/Cinemachine Bridge")]
public class CinemachineBridge : BridgeObject
{
    // Camera position and targeting
    public Vector3 LookAt { get; set; }
    public float Distance { get; set; }
    public Vector3 Offset { get; set; }
    
    // Camera settings
    public float FieldOfView { get; set; }
    public float NearClipPlane { get; set; }
    public float FarClipPlane { get; set; }
    
    // Camera motion
    public float FollowSpeed { get; set; }
    public string FollowTarget { get; set; }
    
    // Camera transitions
    public void TransitionTo(string cameraPreset) { /* implementation */ }
    public void BlendWithCamera(string cameraId, float blendTime) { /* implementation */ }
}
```

## Event Interest System

The Bridge system includes a powerful event interest mechanism that allows objects to subscribe to specific events and state changes:

```javascript
// Bridge.js format for updating interests
bridge.updateInterests(obj, {
    // Event name as key, interest configuration as value
    "textChanged": {
        // Interest properties
        "detail": "high",
        "throttle": 0.1,
        "conditions": [
            { "field": "text.length", "op": ">", "value": 10 }
        ]
    },
    
    // Simple flag interest (boolean true)
    "mouseMove": true,
    
    // Remove an interest by setting it to null
    "click": null
});
```

In Unity, use the BridgeObject's UpdateInterests method to subscribe to events:

```csharp
// C# code to update interests
JObject interests = new JObject();

// Simple boolean interest
interests["textChanged"] = true;

// Detailed interest with conditions
JObject mouseInterest = new JObject();
mouseInterest["detail"] = "high";
mouseInterest["throttle"] = 0.1f;
JArray conditions = new JArray();
JObject condition = new JObject();
condition["field"] = "position.x";
condition["op"] = ">";
condition["value"] = 10;
conditions.Add(condition);
mouseInterest["conditions"] = conditions;
interests["mouseMove"] = mouseInterest;

// Update the interests
bridgeObject.UpdateInterests(interests);
```

Events will be delivered to objects that have registered an interest in them:

```javascript
// Event data received by interested objects
{
    "event": "textChanged",
    "id": "textField_42", 
    "data": {
        "text": "Hello world",
        "length": 11,
        "visible": true
    }
}
```

The bridge system automatically filters events based on registered interests, ensuring that objects only receive events they care about, which optimizes network traffic and processing.

### TextMeshPro Bridge Events

When implementing the TextMeshPro bridge, emit events that match the interest system:

```csharp
// Emit a text changed event
JObject data = new JObject();
data["text"] = _textComponent.text;
data["length"] = _textComponent.text.Length;
data["visible"] = _textComponent.isActiveAndEnabled;
data["overflow"] = _textComponent.isTextOverflowing;

SendEventName("textChanged", data);
```

### Cinemachine Bridge Events

Similarly for Cinemachine events:

```csharp
// Emit a camera blend event
JObject data = new JObject();
data["blendTime"] = blendTime;
data["progress"] = currentBlendAmount;
data["source"] = sourceCamera.name;
data["target"] = targetCamera.name;

SendEventName("cameraBlend", data);
```

## Third-Party Library JSON Converters

We're building converters for specialized Unity structures and components:

1. **Animation Converters**: Convert between JSON and AnimationCurve/Keyframe
2. **UI Converters**: Handle RectTransform, Canvas, LayoutGroup properties
3. **Physics Converters**: Colliders, Rigidbodies, Joints
4. **Material/Shader Converters**: Properties, Keywords, Variants

These converters allow for complete JSON control over these complex types:

```json
{
  "animationCurve": {
    "keys": [
      {"time": 0, "value": 0, "inTangent": 0, "outTangent": 2},
      {"time": 0.5, "value": 1, "inTangent": 0, "outTangent": 0},
      {"time": 1, "value": 0, "inTangent": -2, "outTangent": 0}
    ],
    "preWrapMode": "clamp",
    "postWrapMode": "clamp"
  },
  
  "material": {
    "shader": "Standard",
    "mainTexture": "Textures/Wood",
    "color": "#FFD699",
    "metallic": 0.2,
    "smoothness": 0.5
  }
}
```

## Future Plans

1. **Complete TextMeshPro Bridge**: Implement a full-featured wrapper for TextMeshPro with animation support, style systems, and live updates.

2. **Interactive Object Framework**: Build a comprehensive framework for interactive objects using the bridge system, supporting gestures, hover effects, and accessibility features.

3. **Remote Debug Console**: Create a debug console that can inspect and modify bridge properties at runtime.

4. **Visual Bridge Editor**: Develop a visual editor for creating and configuring bridge objects without writing JSON manually.

5. **Prefab Instantiation System**: Build a system for dynamic prefab loading and instantiation through JSON configuration.

## Benefits Summary

1. **Non-Invasive**: Works with any Unity object without modifying source
2. **Flexible**: Configure any property exposed by the object
3. **Type-Safe**: JSON.NET handles type conversion for Unity types
4. **Format-Flexible**: Accept multiple input formats but maintain consistent output
5. **Lightweight**: Small runtime footprint with minimal overhead
6. **Schema Integrated**: Works with our content management system
7. **Prefab Compatible**: Maintains prefab workflow without breaking connections
8. **Library Bridges**: Specialized bridges for popular Unity libraries
9. **Input Handling**: Sophisticated input tracking with network optimization
10. **Event System**: JSON-based communication between components 