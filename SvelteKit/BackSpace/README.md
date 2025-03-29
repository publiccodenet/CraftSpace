# BackSpace - SvelteKit Application for CraftSpace

This directory contains the SvelteKit web application component of the CraftSpace project.

## Overview

BackSpace serves as:

1. The web host for the Unity WebGL client
2. The data processing pipeline for Internet Archive collections
3. The API server for dynamic queries and collection access

## Quick Start

```bash
# Install dependencies
npm install

# Start development server (runs on http://localhost:5173)
npm run dev

# or start the server and open the app in a new browser tab
npm run dev -- --open

# Build for production
npm run build
```

## NPM Commands

### Development
- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run check` - Type check the project
- `npm run check:watch` - Type check in watch mode

### Schema Management
- `npm run schema:export` - Export schemas from the database
- `npm run schema:copy` - Copy schemas to Unity
- `npm run schema:copy-to-content` - Copy schemas to Unity content directory
- `npm run schema:build-njsonschema` - Build and install NJsonSchema package
- `npm run schema:debug` - Debug schema-related issues
- `npm run schema:generate-all` - Run schema export and copy in sequence

### Collection Management
- `npm run collection:list` - List all collections
- `npm run collection:create` - Create a new collection
- `npm run collection:process` - Process a collection
- `npm run collection:manage` - Manage collections
- `npm run collection:debug` - Debug collection issues
- `npm run collection:validate` - Validate collection data
- `npm run collection:excluded` - List excluded items

### Item Management
- `npm run item:list` - List items in a collection
- `npm run item:get` - Get details of a specific item
- `npm run item:create` - Create a new item
- `npm run item:fetch` - Fetch item data

### Content Management
- `npm run content:init` - Initialize content directory
- `npm run content:info` - Show content directory info

### System Management
- `npm run unity:install` - Install Unity package
- `npm run path:debug` - Debug path resolution
- `npm run connector:manage` - Manage data connectors
- `npm run export:manage` - Manage data exports
- `npm run processor:manage` - Manage data processors
- `npm run import:debug` - Debug import issues
- `npm run copy-items-to-unity` - Copy items to Unity project

## Documentation

For complete documentation, see [README-BACKSPACE.md](../../README-BACKSPACE.md) in the repository root.

## Project Structure

- `scripts/`: Collection processing scripts and data pipeline
- `src/`: SvelteKit application source code
- `static/`: Static assets including collection data and Unity build

## Basic SvelteKit Usage

Everything you need to build a Svelte project, powered by [`create-svelte`](https://github.com/sveltejs/kit/tree/master/packages/create-svelte).

### Developing

Once you've installed dependencies with `npm install`, start a development server:

```bash
npm run dev

# or start the server and open the app in a new browser tab
npm run dev -- --open
```

### Building

To create a production version of your app:

```bash
npm run build
```

You can preview the production build with `npm run preview`.

> To deploy your app, you may need to install an [adapter](https://svelte.dev/docs/kit/adapters) for your target environment.
