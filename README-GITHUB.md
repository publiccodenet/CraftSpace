# SpaceShip Project GitHub README

A modern Unity-based 3D environment for exploring digital archives, integrated with a SvelteKit web application.

## Project Overview

SpaceShip is an interactive 3D environment called "CraftSpace" built in Unity, embedded in a modern SvelteKit web application. The application is designed to explore digital archives, particularly content from the Internet Archive.

The project consists of:
- **SvelteKit frontend**: Modern web interface using Svelte 5 with runes
- **Unity WebGL application**: 3D environment embedded in the web interface
- **Docker-based server**: For API endpoints and backend functionality 

## Repository Structure

```
/
├── SvelteKit/BackSpace/          # SvelteKit web application
│   ├── src/                      # Source code
│   │   ├── routes/               # SvelteKit routes
│   │   ├── lib/                  # Shared components and utilities
│   │   │   └── components/       # Reusable components
│   │   ├── static/                   # Static assets
│   │   │   └── Build/WebGL/          # Unity WebGL build output
│   │   ├── scripts/                  # Utility scripts
│   │   ├── .github/workflows/        # GitHub Actions workflows
│   │   └── Dockerfile                # Docker configuration for server
│   │
│   ├── Unity/CraftSpace/             # Unity project
│   │   └── Assets/                   # Unity assets
│   │
│   └── Notes/                        # Documentation and notes
```

## Automated Workflows

This project uses GitHub Actions to automate building and deployment:

1. **SvelteKit Build and Deploy**: Builds the SvelteKit app and deploys to GitHub Pages
2. **Docker Build and Push**: Builds and pushes the server Docker image to DockerHub
3. **Unity WebGL Build**: Uses a self-hosted runner to build the Unity WebGL application

## Setup Instructions

### 1. Setting Up GitHub Repository

1. Create a new GitHub repository
2. Add required secrets:
   - `DOCKERHUB_USERNAME`: Your DockerHub username
   - `DOCKERHUB_TOKEN`: DockerHub access token

### 2. Setting Up a Self-Hosted GitHub Runner on MacBook Pro

#### Prerequisites

- macOS 12 (Monterey) or later
- Administrator access on your MacBook Pro
- At least 16GB RAM recommended
- At least 40GB of free disk space

#### Install Developer Tools

1. Install Xcode Command Line Tools:
   ```bash
   xcode-select --install
   ```

2. Install Homebrew:
   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

3. Install Node.js and npm:
   ```bash
   brew install node
   ```

4. Install Docker Desktop:
   Download from [docker.com](https://www.docker.com/products/docker-desktop)

#### Install Unity

1. Download Unity Hub from [unity3d.com](https://unity3d.com/get-unity/download)
2. Install Unity Hub and sign in with your Unity account
3. Install Unity version 2022.3.20f1 (or your preferred version)
   - In Unity Hub, go to "Installs" → "Add" → select version 2022.3.20f1
   - Include WebGL Build Support module

#### Set Up GitHub Runner

1. Go to your GitHub repository → Settings → Actions → Runners
2. Click "New self-hosted runner"
3. Select macOS
4. Follow the provided instructions to download and configure the runner

5. Add labels to your runner:
   ```bash
   ./config.sh --labels unity,configured
   ```

6. Start the runner as a service:
   ```bash
   sudo ./svc.sh install
   sudo ./svc.sh start
   ```

7. Verify the runner is connected in your GitHub repository settings

### 3. Unity WebGL Build Setup

1. Create a build script in your Unity project:

```csharp
// Assets/Editor/BuildScript.cs
using UnityEditor;
using System.IO;

public class BuildScript
{
    public static void BuildWebGL()
    {
        string outputDir = "Build/WebGL";
        
        // Make sure the output directory exists
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        
        // Define build settings
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes,
            targetGroup = BuildTargetGroup.WebGL,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
            locationPathName = outputDir
        };
        
        // Build the project
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
```

2. Ensure your Unity path in the workflow file is correct:
   - Edit `.github/workflows/build-unity-webgl.yml`
   - Update the `UNITY_PATH` variable to match your Unity installation path

### 4. SvelteKit Configuration

1. Navigate to the SvelteKit project:
   ```bash
   cd SvelteKit/BackSpace
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

4. For production builds:
   ```bash
   npm run build
   ```

### 5. Docker Configuration

The Docker setup is already configured in the `Dockerfile`. The GitHub workflow will build and push the Docker image automatically when changes are detected.

## Deployment

### GitHub Pages

The SvelteKit application is automatically deployed to GitHub Pages with the custom domain `SpaceShip.DonHopkins.com`. 

To configure the custom domain:
1. Go to repository Settings → Pages
2. Enter `SpaceShip.DonHopkins.com` in the "Custom domain" field
3. Ensure your DNS provider has a CNAME record pointing to `<username>.github.io`

### Server Deployment

The server is deployed as a Docker container. You can deploy it to any platform that supports Docker containers:

- Digital Ocean App Platform
- AWS ECS/Fargate
- Google Cloud Run
- Self-hosted server

## Development Workflow

### SvelteKit Development

1. Make changes to SvelteKit files
2. Test locally with `npm run dev`
3. Commit and push to trigger automatic deployment

### Unity Development

1. Make changes in the Unity project
2. Test locally in Unity Editor
3. Commit and push to trigger the Unity build workflow
4. The build is automatically copied to the SvelteKit static directory and committed

### Workflow Integration

The integration between Unity and SvelteKit is handled through the `CraftSpace.svelte` component, which loads the Unity WebGL build at runtime.

## Internet Archive Integration

The project includes scripts for downloading content from the Internet Archive:

```bash
# Build TypeScript scripts
npm run build:scripts

# Download items
npm run download-items output_directory
```

## Troubleshooting

### Unity Build Issues

- Check the Unity build log for errors: `Unity/CraftSpace/unity_build.log`
- Ensure the build method name matches in the workflow file and Unity script
- Verify Unity version compatibility

### GitHub Runner Issues

- Check runner status: `sudo ./svc.sh status`
- View runner logs: `tail -f ~/.runner/logs/Worker_*`
- Restart runner if needed: `sudo ./svc.sh restart`

### SvelteKit Build Issues

- Check for JS/TS errors in the console
- Ensure all dependencies are installed: `npm ci`
- Clear the SvelteKit build cache: `rm -rf .svelte-kit`

# GitHub Actions Configuration for CraftSpace

This document explains the GitHub Actions workflows set up for the CraftSpace monorepo.

> **Note:** All workflow files currently have a `.disabled` suffix to prevent them from running automatically. 
> This is a temporary measure during development. To activate a workflow, rename it to remove the `.disabled` suffix.

## Monorepo Structure

The CraftSpace repository is organized as a monorepo containing:

- **Unity/CraftSpace**: Unity WebGL application
- **SvelteKit/BackSpace**: SvelteKit web application
- **Collections**: Internet Archive collection data

## Workflow Overview

### 1. Main Build and Deploy Workflow

**File**: `.github/workflows/build-deploy.yml.disabled`

This comprehensive workflow handles the complete build and deployment process:

1. Builds the Unity WebGL application
2. Processes collections data
3. Builds the SvelteKit application
4. Deploys everything to Digital Ocean

**Trigger**: Manual workflow dispatch (with enable parameter)

### 2. Collection Update Workflow

**File**: `.github/workflows/update-collections.yml.disabled`

This workflow updates the collection data without rebuilding the entire application:

1. Incrementally processes collection data
2. Deploys updated collections to CDN

**Trigger**: 
- Weekly schedule (Monday at 1 AM)
- Manual workflow dispatch

### 3. SvelteKit-Only Workflow

**File**: `.github/workflows/build-deploy-sveltekit.yml.disabled`

For faster iterations on the web application without rebuilding Unity:

1. Builds only the SvelteKit application
2. Deploys to Digital Ocean App Platform

**Trigger**: 
- Push to main branch affecting SvelteKit files
- Manual workflow dispatch

### 4. Unity-Only Workflow

**File**: `.github/workflows/build-unity-webgl.yml.disabled`

For Unity-focused development:

1. Builds only the Unity WebGL application
2. Updates the SvelteKit static directory with the new build
3. Commits the changes back to the repository

**Trigger**:
- Push to main branch affecting Unity files
- Manual workflow dispatch

### 5. Docker Build and Push

**File**: `.github/workflows/build-push-docker.yml.disabled`

For containerized deployment:

1. Builds a Docker image for the SvelteKit application
2. Pushes to DockerHub registry

**Trigger**:
- Changes to Dockerfile or package.json
- Manual workflow dispatch

## Shared Scripts

Scripts in `.github/scripts/` are shared across workflows:

- `process-collections.sh`: Handles collection processing (full or incremental)
- `deploy-collections.sh`: Deploys collections to CDN

## Required Secrets

The following secrets need to be set in the GitHub repository:

### Unity Build
- `UNITY_LICENSE`: Unity license for building WebGL

### Digital Ocean Deployment
- `DIGITALOCEAN_ACCESS_TOKEN`: API token
- `DIGITALOCEAN_APP_ID`: App Platform application ID
- `DIGITALOCEAN_HOST`: Host for SSH connection
- `SSH_PRIVATE_KEY`: SSH private key

### Docker Registry
- `DOCKERHUB_USERNAME`: Docker Hub username
- `DOCKERHUB_TOKEN`: Docker Hub access token

### CDN/Storage
- `DO_SPACES_KEY`: Digital Ocean Spaces access key
- `DO_SPACES_SECRET`: Digital Ocean Spaces secret
- `DO_SPACES_ENDPOINT`: Digital Ocean Spaces endpoint
- `DO_SPACES_BUCKET`: Digital Ocean Spaces bucket name

## Running Workflows

### Manual Trigger

1. First, rename the workflow by removing the `.disabled` suffix
2. Go to "Actions" tab in the GitHub repository
3. Select the workflow you want to run
4. Click "Run workflow"
5. Set "enable" to "true"
6. Click "Run workflow" button

### Adding New Components

When adding new components to the monorepo:

1. Create a new directory at the root level
2. Add a specific workflow file in `.github/workflows/`
3. Share scripts when possible using `.github/scripts/`
4. Update this documentation

## Workflow Integration

The workflows are designed to work together in a complementary way:

```
                       ┌───────────────────┐
                       │ Manual Workflow   │
                       │    Dispatch       │
                       └─────────┬─────────┘
                                 │
                 ┌───────────────┴───────────────┐
                 │                               │
    ┌────────────▼─────────────┐   ┌─────────────▼────────────┐
    │ Component-Specific       │   │ Comprehensive            │
    │ Workflows                │   │ Build and Deploy         │
    └────────────┬─────────────┘   └─────────────┬────────────┘
                 │                               │
┌────────────────┼───────────────────────────────┼────────────────┐
│                │                               │                │
│ ┌──────────────▼───────────────┐ ┌─────────────▼──────────────┐ │
│ │ SvelteKit App               │ │ Unity WebGL                │ │
│ └──────────────┬───────────────┘ └─────────────┬──────────────┘ │
│                │                               │                │
│ ┌──────────────▼───────────────┐ ┌─────────────▼──────────────┐ │
│ │ Docker Build                │ │ Collection Processing      │ │
│ └──────────────────────────────┘ └────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

This multi-tiered approach allows for:

1. **Fast iterative development** on specific components
2. **Automated deployment** of the full application
3. **Scheduled updates** of collection data
4. **Flexible deployment options** (container, direct, CDN)

## Incremental Updates

The workflows support different levels of incremental updates to optimize CI/CD time:

1. **Full Build**: Builds everything from scratch (slowest but most comprehensive)
2. **Unity-Only**: Updates just the Unity WebGL build
3. **SvelteKit-Only**: Updates just the web application
4. **Collections-Only**: Updates just the collection data
5. **Docker-Only**: Rebuilds just the container image

This allows for efficient development cycles based on what component has changed. 