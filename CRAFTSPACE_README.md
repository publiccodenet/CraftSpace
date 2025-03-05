# CraftSpace Monorepo

This repository contains all components of the CraftSpace project, an immersive 3D visualization platform for Internet Archive collections.

## Repository Structure

```
CraftSpace/
├── .github/                  # GitHub Actions workflows and scripts
├── Unity/                    # Unity WebGL application
│   └── CraftSpace/           # Main Unity project
├── SvelteKit/                # Web application components
│   └── BackSpace/            # SvelteKit web application
├── collections.json          # Collection configuration
└── Notes/                    # Project documentation
```

## Components

### 1. Unity Application (Unity/CraftSpace)

The 3D visualization client built with Unity WebGL that provides:
- Immersive 3D browsing of digital collections
- Multi-resolution rendering of book covers
- Intelligent caching and progressive loading

### 2. SvelteKit Application (SvelteKit/BackSpace)

The web application that:
- Hosts the Unity WebGL build
- Serves collection data
- Provides API endpoints for dynamic queries
- Handles collection processing

### 3. Collections System

A data pipeline that:
- Fetches data from Internet Archive
- Processes book covers at multiple resolutions
- Generates optimized texture atlases
- Implements multi-tiered caching

## Development Setup

1. **Clone Repository**:
   ```bash
   git clone https://github.com/SimHacker/CraftSpace.git
   cd CraftSpace
   ```

2. **Unity Setup**:
   - Open Unity Hub
   - Add project from `Unity/CraftSpace`
   - Use Unity version 2021.3.11f1 or later

3. **SvelteKit Setup**:
   ```bash
   cd SvelteKit/BackSpace
   npm install
   npm run dev
   ```

4. **Collection Processing**:
   ```bash
   cd SvelteKit/BackSpace
   npm run build:scripts
   npm run pipeline-full
   ```

## Building and Deployment

The project uses GitHub Actions for CI/CD. See [GITHUB-README.md](GITHUB-README.md) for details.

Manual build process:

1. **Process Collections**:
   ```bash
   cd SvelteKit/BackSpace
   npm run build:scripts
   npm run pipeline-full
   ```

2. **Build Unity WebGL**:
   - Open Unity project
   - File > Build Settings > WebGL > Build
   - Output to `SvelteKit/BackSpace/static/unity`

3. **Build SvelteKit App**:
   ```bash
   cd SvelteKit/BackSpace
   npm run build
   ```

4. **Deploy**:
   - Deploy `SvelteKit/BackSpace/build` to web server
   - Deploy collection data to CDN (optional)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.

## License

See [LICENSE](LICENSE) for details. 