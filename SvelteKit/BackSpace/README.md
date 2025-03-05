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
