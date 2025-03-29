# Bridge Setup Guide for CraftSpace

This guide explains how to set up a new Unity scene to use the Bridge system, with specific focus on the WebGL backend.

## Overview

The Bridge system allows seamless communication between Unity and JavaScript. It enables:
- Controlling Unity objects from JavaScript
- Exposing Unity data to JavaScript
- Zero-copy texture and data sharing
- Event-based communication with efficient data transfer
- Remote debugging capabilities

## Setting Up a New Scene

### 1. Required GameObjects

Create a new scene with these components:

```
Scene
 ├── Managers (Empty GameObject)
 │    └── Bridge (Add Bridge.cs component)
 │    └── [Other manager scripts as needed]
 ├── UI (Empty GameObject)
 │    └── [Your UI elements]
 └── Content (Empty GameObject)
      └── [Your scene content]
```

### 2. Configure the Bridge Component

1. Select the `Bridge` GameObject
2. In the Inspector panel, configure the Bridge component:
   - **Target Transform**: Reference to the root object you want to expose to JavaScript (usually the Managers object)
   - **Game ID**: Unique identifier for your application (e.g., "CraftSpace")
   - **Deployment**: Environment (e.g., "development", "production")
   - **Title**: Human-readable name for your application
   - **URL**: Path to the bridge HTML file (usually "bridge.html")

### 3. WebGL Configuration

1. Open Project Settings (Edit > Project Settings)
2. Select the Player tab
3. Configure WebGL settings:
   - Set "WebGL Template" to "Bridge"
   - Enable "Strip Engine Code" for smaller builds
   - In "Publishing Settings", enable "Decompression Fallback"

4. Build Settings (File > Build Settings)
   - Select WebGL platform
   - Set Compression Format to "Disabled" during development (faster loading)

## Understanding Bridge Components

### Core Components

1. **Bridge.cs**: Central manager that handles object registration and event distribution
2. **BridgeObject.cs**: Base class for objects that can communicate with JavaScript
3. **BridgeTransport.cs**: Abstract base class for different communication transports

### WebGL Specific Components

1. **BridgeTransportWebGL.cs**: Implements WebGL-specific communication
2. **Bridge WebGL Template**: HTML template for hosting Unity WebGL content
3. **JavaScript Libraries**: Bridge JavaScript implementation

### Streaming Assets

The `StreamingAssets/Bridge` directory contains essential files:
- **bridge.js**: Core JavaScript API implementation
- **unity.js**: Unity-specific JavaScript helpers
- **bridge-transport-webgl.js**: WebGL transport implementation
- **Other utility libraries**: jQuery, Three.js, etc.

## Interest Query System: The Bridge's Core Innovation

The most powerful feature of the Bridge system is its Interest Query System, which dramatically reduces network traffic and eliminates round-trip delays through a "shopping list" approach to data.

### Key Concepts

1. **Interest Declaration**: The client (JavaScript) tells the server (Unity) which events it cares about.
2. **Query Patterns**: With each interest, the client sends a "shopping list" (query template) of exactly what data it wants to receive when that event occurs.
3. **Path Expressions**: Powerful selectors that can navigate through game objects, components, properties, and methods to extract specific data.
4. **Zero Round-Trip Design**: All required data is gathered and sent with the event, eliminating the need for follow-up requests.

### How It Works

1. **Registration Phase**:
   - The JavaScript client registers interest in specific events on specific objects
   - With each interest, it provides a query pattern that specifies exactly what data it needs
   
2. **Event Processing Phase**:
   - When an event occurs in Unity, the system checks if any clients are interested
   - For interested clients, it processes their query pattern to gather the requested data
   - It packages the event with only the specifically requested data
   - It sends the compact, tailored event back to the client

3. **Client Handling**:
   - The JavaScript client receives the event with precisely the data it requested
   - No follow-up requests are needed, as all required data was included

This approach drastically reduces both the number of messages and the message size compared to traditional approaches that would require separate queries for each piece of data.

### Example of Interest Registration with Query Pattern

```javascript
// Register interest in MouseDown events with a custom query pattern
bridge.updateInterests("myObjectId", {
  "MouseDown": {
    // Query pattern - this is our "shopping list" of data to receive when the event occurs
    mousePosition: "mousePosition",              // Global mouse position
    localPoint: "raycastHit/point",              // Exact hit point in local space
    distance: "raycastHit/distance",             // Distance from camera
    materialName: "raycastHit/material/name",    // Name of hit material
    worldUp: "transform/up",                     // Object's up vector
    inFrustum: "method:IsInCameraFrustum",       // Result of calling a method
    inventoryCount: "component:Player/itemCount" // Property on another component
  },
  
  // Handler to process event with exactly the data we requested
  handler: (obj, results) => {
    console.log("Mouse down at:", results.mousePosition);
    console.log("Hit point:", results.localPoint, "Distance:", results.distance);
    console.log("Material:", results.materialName, "Up vector:", results.worldUp);
    console.log("In frustum:", results.inFrustum, "Items:", results.inventoryCount);
    
    // Further processing with no need for additional queries
  }
});
```

### Future Extensions

The Interest Query System is designed to be extended in the future with:
- Event filtering that happens server-side to further reduce messages
- Data transformations to pre-process data before sending
- Conditional interest expressions for more dynamic behavior
- Aggregation functions to combine or process multiple values

## Query API: Immediate Access to Data

The same powerful path expression system used for interests is also available for immediate (asynchronous) data queries:

```javascript
// Query multiple data points in a single request
bridge.queryObject("myObjectId", {
  // Query pattern mapping result keys to path expressions
  position: "transform/position",
  rotation: "transform/rotation",
  velocity: "component:Rigidbody/velocity",
  health: "component:PlayerHealth/currentHealth",
  nearbyEnemies: "method:GetNearbyEnemyCount(10)",  // Method call with parameter
  inventory: "component:Inventory/items",           // Array of items
  equippedWeapon: "component:Inventory/equipped/0"  // First equipped item
}, function(result) {
  // All data arrives in a single callback
  console.log("Position:", result.position);
  console.log("Health:", result.health, "Enemies nearby:", result.nearbyEnemies);
  console.log("Inventory contains", result.inventory.length, "items");
  console.log("Equipped weapon:", result.equippedWeapon.name);
});
```

This uses the same underlying mechanism as the interest system, but for immediate one-time queries instead of event subscriptions.

## Enabling Zero-Copy Texture Sharing

The Bridge system supports efficient texture sharing between Unity and JavaScript:

```csharp
// In your Unity script
public Texture2D myTexture;

// Make the texture available to JavaScript
Bridge.mainBridge.SetGlobal("sharedTexture", myTexture);

// Update a texture from JavaScript
public void UpdateTextureFromJS(Texture2D texture, byte[] data)
{
    texture.LoadRawTextureData(data);
    texture.Apply();
}
```

```javascript
// In JavaScript
bridge.queryObject('bridge', { 
    textureInfo: 'sharedTexture' 
}, function(result) {
    // Use the texture information
    console.log("Texture dimensions:", result.textureInfo.width, result.textureInfo.height);
});
```

## JavaScript Bridge API

The JavaScript API includes several key features for object management and event handling:

### 1. Object Management

```javascript
// Create a Unity object
bridge.createObject({
  prefab: "Prefabs/MyPrefab",
  id: "myUniqueId",
  update: { /* initial property values */ },
  interests: { /* event subscriptions with query patterns */ }
});

// Update properties
bridge.updateObject("myUniqueId", {
  position: { x: 0, y: 1, z: 0 },
  rotation: { x: 0, y: 90, z: 0 }
});

// Query properties
bridge.queryObject("myUniqueId", {
  position: "transform/position",
  rotation: "transform/rotation"
}, function(result) {
  console.log("Position:", result.position);
});
```

### 2. Interest-Based Event System

The Bridge uses an efficient "interest"-based event system that allows you to:
1. Declare which events you want to receive
2. Specify exactly what data to include with each event using path expressions
3. Define a handler function to process events with the requested data

This approach eliminates the need for additional round-trip queries to get data when events occur:

```javascript
// Create an object with interest in mouse events
bridge.createObject({
  prefab: "Prefabs/MyPrefab",
  
  // Event interests with query templates for data to receive with each event
  interests: {
    // Each event type gets a query template and handler
    "MouseDown": {
      // Query template: path expressions for data to be included with the event
      mousePosition: 'mousePosition',
      shiftKey: 'shiftKey',
      controlKey: 'controlKey',
      altKey: 'altKey',
      
      // Handler function receives the event data specified in the query,
      // but is stripped out of the interest template sent to Unity,
      // only existing on the JavaScript side.
      handler: (obj, results) => {
        console.log("Mouse Down at:", results.mousePosition.x, results.mousePosition.y);
        console.log("Shift key pressed:", results.shiftKey);
        
        // You can store data on the object for later use
        obj.mouseDownPosition = results.mousePosition;
        
        // Or update Unity-side properties
        bridge.updateObject(obj, {
          dragging: true,
          tracking: results.shiftKey ? "Menu" : "Drag"
        });
      }
    },
    
    // Add handlers for other events
    "MouseDrag": {
      mousePosition: 'mousePosition',
      handler: (obj, results) => {
        console.log("Mouse Drag at:", results.mousePosition);
        obj.mousePosition = results.mousePosition;
      }
    },
    
    "MouseUp": {
      mousePosition: 'mousePosition',
      handler: (obj, results) => {
        bridge.updateObject(obj, { dragging: false });
      }
    }
  }
});
```

### 3. Updating Event Interests

You can update an object's event interests at any time:

```javascript
// Add or modify interest in specific events
bridge.updateInterests("myUniqueId", {
  "Click": {
    // Query specific properties using path expressions
    position: "transform/position",
    rotation: "transform/rotation",
    
    // Handler function receives the data specified in the query
    handler: (obj, results) => {
      console.log("Object clicked at position:", results.position);
      console.log("Object rotation:", results.rotation);
    }
  }
});
```

### 4. Path Expressions

Path expressions are powerful selectors that can:
- Navigate object hierarchies: `transform/position/x`
- Access component properties: `component:Light/intensity`
- Call methods: `method:GetWorldPosition`
- Access array elements: `children/index:2/name`
- Access dictionary entries: `properties/key:color`

```javascript
// Examples of advanced path expressions
bridge.queryObject("myUniqueId", {
  position: "transform/position",
  lightIntensity: "component:Light/intensity", 
  texture: "component:MeshRenderer/material/mainTexture",
  materialColor: "component:Renderer/material/color",
  firstChildName: "transform/method:GetChild(0)/name"
}, callback);
```

### 5. Sending Custom Events

```javascript
bridge.sendEvent({
  event: "CustomEvent",
  id: "myUniqueId",
  data: { 
    value: 42,
    message: "Hello from JavaScript" 
  }
});
```

## P/Invoke and External JavaScript Integration

The Bridge uses P/Invoke to interface with JavaScript in WebGL:

```csharp
[DllImport("__Internal")]
private static extern void _Bridge_EvaluateJS(string js);

// Call JavaScript from C#
public void CallJavaScript(string code)
{
    _Bridge_EvaluateJS(code);
}
```

## Troubleshooting

1. **Communication Issues**:
   - Check the browser console for JavaScript errors
   - Verify that all required Bridge files are in StreamingAssets
   - Ensure the WebGL template is correctly configured

2. **Missing Objects**:
   - Verify object IDs match between Unity and JavaScript
   - Check that objects are properly registered with the Bridge

3. **Texture Sharing Problems**:
   - Ensure textures use a compatible format (RGBA32 recommended)
   - Check for correct texture dimensions

## Best Practices

1. **Interest Query System**:
   - Design query patterns to include exactly what you need - no more, no less
   - Use specific path expressions rather than requesting entire objects
   - Group related interests to reduce the number of handlers
   - Consider the scope and lifetime of interests - don't subscribe to events you no longer need

2. **Performance**:
   - Use batch updates instead of individual property changes
   - Minimize event frequency for better performance
   - Keep path expressions as specific as possible

3. **Security**:
   - Be careful with `EvaluateJS` - it can execute arbitrary code
   - Validate inputs from external sources
   - Limit what objects and properties are exposed

4. **Organization**:
   - Keep Bridge-related code in dedicated components
   - Use consistent naming for bridge-exposed objects
   - Implement proper error handling on both sides

## Upgrading from Legacy Bridge

If you're migrating from an older version of Bridge:

1. Update the WebGL template to use the latest version
2. Ensure StreamingAssets contains the latest JavaScript files
3. Update any custom transport implementations
4. Check for API changes in both C# and JavaScript 