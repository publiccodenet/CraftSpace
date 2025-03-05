# CraftSpace + BackSpace Integration

This project combines a Unity WebGL application (CraftSpace) with a SvelteKit web application (BackSpace) for browsing and visualizing Internet Archive content.

## Project Structure

- `Unity/CraftSpace/`: Unity project for visualization
- `SvelteKit/BackSpace/`: SvelteKit web application serving as the backend and hosting the Unity WebGL client

## Development Setup

### Prerequisites

- [Node.js](https://nodejs.org/) (v18+)
- [Unity](https://unity.com/) (2022.3.19f1 or compatible)
- [Unity WebGL Build Support](https://docs.unity3d.com/Manual/webgl-gettingstarted.html)

### First-time Setup

1. Clone the repository
   ```bash
   git clone https://github.com/yourusername/craftspace.git
   cd craftspace
   ```

2. Install SvelteKit dependencies
   ```bash
   cd SvelteKit/BackSpace
   npm install
   ```

3. Open the Unity project in Unity Hub
   - Open Unity Hub
   - Add the `Unity/CraftSpace` project
   - Make sure you have WebGL build support installed

### Development Workflow

#### Running the SvelteKit Server

```bash
cd SvelteKit/BackSpace
npm run dev
```

This will start the SvelteKit development server at http://localhost:5173

#### Building the Unity WebGL Application

1. Open the Unity project in Unity Editor
2. Go to File > Build Settings
3. Select WebGL platform
4. Click "Build" and select the `Build/WebGL` folder within the Unity project

Alternatively, use the automated build script:

```bash
cd SvelteKit/BackSpace
npm run build:unity
```

#### Full Build Process

To build both Unity and SvelteKit:

```bash
cd SvelteKit/BackSpace
npm run build:all
```

## Unity-to-SvelteKit Communication

The Unity WebGL client communicates with the SvelteKit app through JavaScript interop:

### From SvelteKit to Unity

```js
// Send a message to a GameObject in Unity
window.sendMessageToUnity('GameController', 'LoadDocument', '{"id":"document123"}');
```

### From Unity to SvelteKit

In your Unity C# scripts:

```csharp
using UnityEngine;

public class WebGLBridge : MonoBehaviour
{
    public void SendMessageToSvelteKit(string message)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        SendMessageToJS(message);
        #else
        Debug.Log($"Would send to SvelteKit: {message}");
        #endif
    }

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SendMessageToJS(string message);
}
```

Add a JavaScript file in your Unity project's `WebGLTemplates` folder:

```js
// bridge.js
function SendMessageToJS(message) {
  if (window.unityMessageHandler) {
    window.unityMessageHandler(message);
  }
}
```

## API Endpoints

The SvelteKit server provides these API endpoints:

- `GET /api/index`: Returns a basic empty array (placeholder)
- `GET /api/collections`: Search Internet Archive collections
  - Query params: `q` (search term), `page`, `limit`

## Deployment

The project includes a GitHub Actions workflow for CI/CD in `.github/workflows/build-deploy.yml`. To use it:

1. Set up a `UNITY_LICENSE` secret in your GitHub repository
2. Configure the deployment step based on your hosting provider
3. Push to the main branch to trigger the workflow

## Troubleshooting

- **Unity WebGL not loading**: Check browser console for errors. Ensure all Unity files are correctly copied to the `static/unity` folder.
- **CORS issues**: When testing locally, you might encounter CORS errors when Unity tries to load assets. Use a CORS proxy or disable CORS in your browser for local development. 