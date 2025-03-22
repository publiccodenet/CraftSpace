# Unity Bridge Architecture

## Overview

The Unity Bridge system enables real-time communication between Unity and web technologies, facilitating seamless integration of web content, JavaScript execution, data exchange, and interactive UI elements. This system allows developers to leverage the power of web technologies within Unity applications and provides a robust framework for creating rich, interactive experiences.

## Key Integration Components

The Unity Bridge system consists of several key components that dovetail together:

1. **Cross Platform Web Browser and JavaScript Engine**
   - Integrates native web browser/JavaScript with Unity
   - Hides platform-specific implementations from the developer
   - Enables live coding and debugging to greatly accelerate 
     iterative development without recompiling the Unity app

2. **JavaScript/Unity3D Bridge**
   - Enables bidirectional communication between Unity and JavaScript
   - Provides access to Unity objects, components, and functionality

3. **JSON <=> C# Conversion Utilities**
   - Automatically converts between JSON and C# objects
   - Handles Unity-specific types like Vector3, Quaternion, 
     Material, ParticleSystem, etc.

4. **Accessor Path Expressions**
   - Path syntax for traversing and modifying Unity objects with JSON

5. **JSON Messaging**
   - Asynchronous messaging protocol between JavaScript and Unity
   - Allows JavaScript to instantiate, configure, control, query, 
     and handle events from Unity objects

## Core Architecture Components

The Bridge architecture consists of several interconnected components:

### Class Hierarchy

```
- Means root object (with name of superclass in parens).
+ Means subclass of parent in outline.
* Means sub-object of parent in outline (with name of superclass in parens).
```

```
- Accessor (none)

- Bridge (BridgeObject)
  - Central manager for bridge communication

- BridgeTransport (MonoBehaviour)
  + BridgeTransportCEF
    * UnityJSWindow (EditorWindow)
    * WebProvider (none)
  + BridgeTransportSocketIO
  + BridgeTransportWebGL
  + BridgeTransportWebServer
  + BridgeTransportWebView
    * BridgePlugin (MonoBehaviour)

- BridgeExtensions (none)
- BridgeJsonConverter (JsonConverter)

- BridgeObject (MonoBehaviour)
  - Base class for objects that can communicate via the bridge
  + Tracker
    * TrackerProxy (MonoBehaviour)
    + PieTracker
    + Cuboid
      * Tile (MonoBehaviour)
    + ParticleSystemHelper
    + KeyboardTracker
    + TextureViewer
    + KineticText
    + ProCamera
  + LeanTweenBridge
  + TextOverlays
  + ToolbarButton
  + ProText
  + OverlayText
  + UnityBridge

- ProxyGroup (MonoBehaviour)

- NamedAssetManager (MonoBehaviour)
  * NamedAsset (ScriptableObject)

- MonoPInvokeCallbackAttribute (System.Attribute)
```

## Core Components

### Bridge (Bridge.cs)

The central manager for bridge communication that:
- Maintains object registrations and identity mappings
- Distributes events between Unity and JavaScript
- Manages global variables and callbacks
- Controls lifecycle (start/stop/restart)

```csharp
public class Bridge : BridgeObject {
    public static Bridge mainBridge;
    public Dictionary<string, object> idToObject;
    public Dictionary<object, string> objectToID;
    
    // Transport management
    public BridgeTransport transport;
    
    // Methods
    public void StartBridge();
    public void StopBridge();
    public void SendEvent(JObject ev);
    public void HandleCreate(JObject ev);
    public void HandleQuery(JObject ev);
}
```

### BridgeObject (BridgeObject.cs)

Base class for objects that can communicate via the bridge:
- Receives events from JavaScript
- Maintains object identity and references
- Supports serialization and updates

```csharp
public class BridgeObject : MonoBehaviour {
    public string id;
    public Bridge bridge;
    public JObject interests;
    
    public virtual void HandleEvent(JObject ev);
    public void SendEventName(string eventName, JObject data = null);
    public virtual void AnimateData(JArray data);
}
```

### BridgeTransport (BridgeTransport.cs)

Abstract base class for transport implementations:
- Manages event queues between Unity and JavaScript
- Provides interface for specific transport implementations
- Handles initialization and cleanup

```csharp
public class BridgeTransport : MonoBehaviour {
    public string driver;
    public Bridge bridge;
    public List<string> bridgeToUnityEventQueue;
    public List<string> unityToBridgeEventQueue;
    
    public virtual void StartTransport();
    public virtual void StopTransport();
    public virtual void EvaluateJS(string js);
}
```

## Transport Implementations

### WebGL Transport (BridgeTransportWebGL.cs)

WebGL-specific transport for direct browser integration:
- Uses Unity's WebGL interop capabilities
- Manages shared textures and data between Unity and JavaScript
- Provides low-level access to browser DOM and JavaScript

### Web Server Transport (BridgeTransportWebServer.cs)

Web server-based transport using a local web server:
- Hosts a local server within Unity for JavaScript execution
- Communicates via HTTP and WebSockets
- Supports texture sharing and JavaScript evaluation

### WebView Transport (BridgeTransportWebView.cs)

Native platform WebView integration:
- Embeds a WebView within the Unity application
- Renders web content as a texture in the scene
- Provides bi-directional communication with web content

### Socket.IO Transport (BridgeTransportSocketIO.cs)

Socket.IO-based real-time communication:
- Connects to external Socket.IO servers
- Supports distributed communication across multiple clients
- Enables real-time data sharing and events

## Interactive Components

### Tracker (Tracker.cs)

Base class for interactive objects with input tracking:
- Handles mouse/touch events (enter, exit, down, up, drag)
- Manages mouse position tracking and raycasting
- Provides callbacks for derived classes

```csharp
public class Tracker : BridgeObject {
    public bool mouseTracking = true;
    public bool mouseEntered = false;
    public bool mouseDown = false;
    public bool dragTracking = false;
    
    public virtual void OnMouseEnter();
    public virtual void OnMouseExit();
    public virtual void OnMouseDown();
    public virtual void OnMouseUp();
    public virtual void OnMouseDrag();
}
```

### ProCamera (ProCamera.cs)

Advanced camera controller with multiple tracking modes:
- Supports orbit, drag, approach, zoom, and tilt modes
- Handles smooth animations and transitions
- Provides target tracking and snapping

### PieTracker (PieTracker.cs)

Radial menu system with slices and items:
- Tracks mouse position in a radial coordinate system
- Manages menu item selection and events
- Supports customizable slice counts and orientations

### TextOverlays (TextOverlays.cs)

UI system for text and information displays:
- Manages panels for side and center information
- Handles screen-space text positioning
- Dispatches UI interaction events

## Data Conversion and Serialization

### BridgeJsonConverter (BridgeJsonConverter.cs)

JSON conversion for bridge communication:
- Converts between C# objects and JSON
- Handles special types (Vector3, Quaternion, etc.)
- Provides extensible conversion framework

### Accessor (Accessor.cs)

Flexible property access system:
- Provides unified access to fields, properties, and collections
- Supports path-based access (e.g., "transform.position.x")
- Handles type conversion and serialization

## JSON Converters

Unity types are converted to/from JSON using various formats:

### Vector2

```javascript
{ // 2d vector
    x: 0,
    y: 0
}
```

### Vector3

```javascript
{ // 3d vector
    x: 0,
    y: 0,
    z: 0
}
```

### Vector4

```javascript
{ // 4d vector
    x: 0,
    y: 0,
    z: 0,
    w: 0
}
```

### Quaternion

```javascript
{ // 4d quaternion
    x: 0,
    y: 0,
    z: 0,
    w: 0
}

// Alternative: Euler angles in degrees
{
    roll: 0,
    pitch: 0,
    yaw: 0
}
```

### Color

```javascript
"#00000000" // html color string

{ // rgb color (alpha defaults to 1)
    r: 0,
    g: 0,
    b: 0
}

{ // rgba color
    r: 0,
    g: 0,
    b: 0,
    a: 1
}
```

### Matrix4x4

```javascript
[ // 16 element matrix array
    1, 0, 0, 0, 
    0, 1, 0, 0, 
    0, 0, 1, 0, 
    0, 0, 0, 1
]
```

### Particle System Types

Various particle system types with specific JSON representations:

- `ParticleSystem.MinMaxCurve`
- `ParticleSystem.MinMaxGradient`
- `Gradient`
- `AnimationCurve`
- `Keyframe`
- `GradientColorKey`
- `GradientAlphaKey`

### Resource References

```javascript
// Texture
"ResourcePath"

// Material
"ResourcePath"

// Shader
"ShaderName"
```

## Accessor Path Expressions

Path expressions provide a flexible way to access and modify properties of Unity objects.

### Path Syntax

A Path consists of a series of steps separated by slashes, like "foo/bar/baz".

### Step Syntax

A Step consists of an optional type (defaulting to "member") followed by a colon, then a type-specific string. 

```
Step modifiers:
? - Makes a step conditional, returns null instead of raising an error
! - Makes a step excited, evaluates the value when setting instead of treating it as literal
```

### Step Types

- `string`
- `float`
- `integer`, `int`
- `boolean`, `bool`
- `null`
- `json`
- `index`, `jarray`, `array`, `list`
- `map`, `dict`, `dictionary`, `jobject`
- `transform`
- `component`
- `resource`
- `member`, `field`, `property`
- `object`
- `method`

## Extension Methods

### Material.UpdateMaterial

```csharp
Material.UpdateMaterial(this Material material, JToken materialData)
```

This method modifies the material based on a JSON object that specifies various material properties:

- `copyPropertiesFromMaterial`: Copy properties from another material
- `enableInstancing`: Enable GPU instancing
- `mainTexture`: Set the main texture
- `shader`: Set the shader
- Texture properties: `texture_PropertyName`
- Color properties: `color_PropertyName`
- Float properties: `float_PropertyName`
- Vector properties: `vector_PropertyName`

## JavaScript API

The JavaScript bridge API provides methods for controlling Unity objects:

### Object Creation and Management

```javascript
// Create a new object from a prefab
bridge.createObject({
    prefab: "Prefabs/MyObject",
    id: "myUniqueId",
    parent: "parentObjectId",  // Optional parent object
    update: {
        // Properties to set on the new object
        position: { x: 0, y: 1, z: 0 },
        color: "#FF0000"
    },
    interests: {
        // Event subscriptions
        "Click": true,
        "Hover": true
    }
});

// Delete an object
bridge.deleteObject(objOrId);

// Update an object's properties
bridge.updateObject(objOrId, {
    position: { x: 1, y: 2, z: 3 },
    rotation: { x: 0, y: 90, z: 0 }
});

// Query an object's properties
bridge.queryObject(objOrId, {
    position: "transform/position",
    rotation: "transform/rotation"
}, function(result) {
    console.log(result);
});

// Animate object properties
bridge.animateObject(objOrId, [
    // Animation data
]);
```

### Drawing to Canvas

```javascript
bridge.drawToCanvas({
    width: 512,
    height: 512,
    type: "pie",
    // Other parameters specific to the drawing type
}, function(canvas) {
    // Success callback
}, function(error) {
    // Error callback
});
```

## Cross-Platform Support

The Unity Bridge system is designed to work across multiple platforms:

### WebGL
- Uses the same web browser running the Unity application
- Direct integration with the browser's JavaScript engine

### Android
- Uses WebView (Java object, separate process)
- Communicates through native bridge

### iOS
- Uses WKWebView (Objective C object, separate process)
- Communicates through native bridge

### macOS
- Uses WKWebView (Objective C object, separate process)
- Alternative: Socket.IO (through node server, to any browser)

### Windows
- TODO

## Usage Examples

### Creating Bridge Objects

```csharp
// In C#
public class MyBridgeObject : BridgeObject {
    public override void HandleEvent(JObject ev) {
        if (ev["event"].ToString() == "UpdateValue") {
            // Process event from JavaScript
        }
    }
}
```

```javascript
// In JavaScript
bridge.createObject({
    prefab: "Prefabs/MyBridgeObject",
    id: "myObject1",
    initialData: { value: 42 }
});
```

### Sending Events

```csharp
// From C# to JavaScript
JObject data = new JObject();
data["value"] = 42;
SendEventName("ValueChanged", data);
```

```javascript
// From JavaScript to C#
bridge.sendEvent({
    event: "UpdateValue",
    id: "myObject1",
    data: { value: 42 }
});
```

### Using the Tracker System

```csharp
public class CustomTracker : Tracker {
    public override void HandleMouseDown() {
        base.HandleMouseDown();
        // Custom mouse down logic
    }
    
    public override void HandleMouseDrag() {
        base.HandleMouseDrag();
        // Custom mouse drag logic
    }
}
```

## Test Events

Example event sequence for testing the bridge:

```json
[
    {
        "event": "StartedBridge"
    },
    {
        "event": "Log",
        "data": {
            "line": "Hello, world!"
        }
    },
    {
        "event": "Create",
        "data": {
            "id": "1",
            "prefab": "Prefabs/BridgeObject"
        }
    },
    {
        "event": "Query",
        "id": "3",
        "data": {
            "callbackID": "1",
            "query": {
                "time": "time"
            }
        }
    },
    {
        "event": "Update",
        "id": "3",
        "data": {
            "timeScale": 0.5
        }
    }
]
```

## Implementation Notes

1. **Threading**: Most bridge operations happen on the main thread for Unity compatibility
2. **Performance**: Use event batching for optimal performance with many events
3. **Security**: Consider security implications when evaluating JavaScript from external sources
4. **Cleanup**: Properly dispose bridge objects and resources when no longer needed
5. **Debugging**: Enable console logging to diagnose communication issues

