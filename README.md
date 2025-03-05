# CraftSpace

A 3D Internet Archive browser with Unity WebGL frontend and SvelteKit backend.

## Project Structure

```
CraftSpace/
├── SvelteKit/               - Backend server and web hosting
│   └── BackSpace/           - SvelteKit application
│       ├── scripts/         - Data processing scripts
│       ├── src/             - SvelteKit application source
│       └── static/          - Static files including Unity build
│           └── data/        - Collection data (all collections)
│
├── Unity/                   - 3D visualization frontend
│   └── CraftSpace/          - Unity project
│       ├── Assets/          - Unity assets
│       │   └── Resources/   - Embedded collections (subset)
│       └── Build/           - Unity build output
│
├── collections.json         - Master collection configuration
└── Notes/                   - Development notes and documentation
```

## Collection Management

CraftSpace uses a sophisticated system to manage Internet Archive collections:

1. Collections are defined in the master `collections.json` configuration
2. Data processing scripts download and process collections from Internet Archive
3. Collection data is stored in SvelteKit's static directory for serving
4. High-priority collections are also embedded within the Unity WebGL build
5. The system employs multi-level caching for optimal performance

For full details, see:
- [BackSpace README](SvelteKit/BackSpace/README.md) - Server-side implementation
- [CraftSpace README](Unity/CraftSpace/README.md) - Unity client implementation

## Getting Started

1. Install dependencies in the SvelteKit project:
   ```bash
   cd SvelteKit/BackSpace
   npm install
   ```

2. Process collections:
   ```bash
   npm run build:scripts
   npm run download-collections
   ```

3. Build the Unity WebGL application:
   ```bash
   npm run build:unity
   ```

4. Build and start the SvelteKit server:
   ```bash
   npm run build
   npm run start
   ```

5. Visit http://localhost:3000 to access the application

## Architecture Overview

CraftSpace implements a multi-tier architecture for fetching, processing, and displaying Internet Archive content:

1. **Content Pipeline (Node.js)** - Backend TypeScript tools that:
   - Fetch content from Internet Archive
   - Process documents and images
   - Generate texture atlases
   - Produce standardized JSON metadata
   - **Advanced**: Analyze document content and relationships using AI

2. **SvelteKit Server** - Handles:
   - API endpoints for content queries
   - Authentication with Internet Archive
   - Content delivery optimization
   - Metadata caching and search
   - **Advanced**: Orchestrate document classification and clustering pipelines
   - **Multi-screen**: Message brokering between control surfaces and displays
   - **NL Interface**: Processing natural language queries and generating visualization instructions

3. **Svelte Client** - Browser application that:
   - Provides user interface
   - Manages content browsing and search
   - Coordinates with Unity WebGL for display
   - Handles further client-side processing
   - **Advanced**: Interactive visualization of content relationships
   - **Multi-screen**: Functions as control surface on mobile/tablet devices
   - **Chat UI**: Offers natural language interface for intuitive exploration

4. **Unity WebGL Client** - Rendering engine that:
   - Displays document content via texture atlases
   - Handles interactive visualization
   - Receives control instructions via JSON
   - Optimizes rendering for different devices
   - **Advanced**: Renders clustered document collections in meaningful spatial arrangements
   - **Multi-screen**: Coordinates multiple display views across devices/screens
   - **Smart View**: Responds to highlighting and zooming commands from natural language interface

5. **AI Analysis Layer** - Document understanding system that:
   - Extracts key concepts and themes from documents
   - Classifies documents into relevant categories
   - Identifies relationships between documents
   - Generates optimized document clusters
   - Creates metadata for improved search and navigation
   - Guides atlas generation for efficient rendering of related content
   - **Query Understanding**: Processes natural language queries about content
   - **View Orchestration**: Translates queries into visualization instructions
   - **Knowledge Augmentation**: Leverages LLM's intrinsic knowledge about well-known books and documents to:
     - Enhance search with literary and historical context
     - Identify significant passages worthy of highlighting
     - Generate informed suggestions for related materials
     - Provide background information not present in the document itself
     - Connect documents to broader intellectual traditions and movements
     - Recognize and explain references to other works, people, or events
     - Score documents with a "wellKnown" property (0-1) based on LLM familiarity
     - Enable filtering/sorting based on document recognition level
     - Allow users to explore either canonical works or discover obscure content
     - Focus computational resources on items where the LLM can provide rich context

6. **Multi-screen Control System** - Coordination layer that:
   - Enables multiple users to connect simultaneously
   - Distributes control interfaces to phones, tablets, and laptops
   - Synchronizes state across all connected clients
   - Routes commands from control surfaces to appropriate display clients
   - Manages different display configurations (multiple WebGL instances or native applications)
   - Handles role-based permissions for collaborative exploration

7. **Natural Language Interface** - Conversational layer that:
   - Provides chat-based interaction with the content collection
   - Interprets user queries in plain language
   - Generates JSON-structured directives for the visualization system
   - Formulates API calls to LLMs with pre-digested content summaries
   - Translates query results into viewport controls (zoom, pan, highlight)
   - Offers contextual explanations about displayed content
   - Maintains conversation history for coherent multi-turn interactions

8. **Blade Runner-esque Document Analyzer** - Advanced visual exploration system that:
   - Provides futuristic voice-controlled image/document navigation
   - Enables seamless zooming into specific regions with "enhance" commands
   - Reveals hidden details through multi-spectral analysis visualization
   - Dynamically describes discovered content through real-time AI analysis
   - Allows for surgical precision in examining document fragments
   - Identifies and visualizes patterns across document collections
   - Reconstructs partial or damaged content through AI inference
   - Provides cinematic transitions between content views
   - Offers dramatic visual highlighting of discovered insights
   - Integrates voice commands with gestural controls for intuitive navigation

Data flows through the system as consistent JSON objects, from initial content extraction to final rendering in Unity. This architecture keeps heavy processing in JavaScript/TypeScript while leveraging Unity's visualization capabilities.

### Implementation Approach

The system will be developed incrementally:

1. **MVP Phase**: Basic functionality for searching, retrieving, and displaying Internet Archive content
   - Limited document sets
   - Simple atlas generation
   - Direct rendering in Unity
   - Single screen/device operation

2. **Enhanced Phase**: Improved processing and organization
   - Expanded document handling
   - Multi-resolution atlases
   - Basic content categorization
   - Initial multi-screen control capabilities

3. **Advanced Phase**: AI-powered document analysis
   - LLM-based content analysis (using Claude/GPT models)
   - Document clustering and classification
   - Intelligent atlas generation optimized for content relationships
   - Spatial arrangement based on semantic connections
   - Full collaborative multi-user experience

### Hierarchical Atlas Generation Strategy

The system employs a sophisticated approach to atlas generation that optimizes performance while enabling exploration of massive document collections:

1. **Tag-based Document Classification**
   - Each document receives multiple tags through AI analysis
   - Tags form a hierarchical taxonomy (general → specific)
   - Documents can belong to multiple categories simultaneously
   - Tags capture both content themes and visual characteristics

2. **Multi-resolution Progressive Atlas System**
   - Base atlas: All documents represented at minimal resolution (1px per document)
   - Category atlases: Documents sharing tags grouped into specialized atlases
   - Resolution tiers: Each category has atlases at various detail levels
   - Documents appear in multiple atlases simultaneously
   
3. **Dynamic Loading Strategy**
   - Initial view uses the universal low-resolution atlas
   - As users zoom into specific regions/categories, higher-resolution atlases load
   - System prioritizes loading atlases based on user focus
   - Smooth transitions between resolution levels
   
4. **Hierarchical Exploration Model**
   - Navigation follows the tag taxonomy structure
   - Zooming in represents moving deeper into the hierarchy
   - Cross-connections between categories allow lateral exploration
   - Resolution increases proportionally with hierarchy depth

This approach allows the system to begin with a bird's-eye view of the entire collection while maintaining the ability to examine individual documents in detail. The hierarchical organization of both tags and atlas resolutions creates a scalable system capable of handling "zillions of documents" by only loading the specific high-resolution atlases needed for the current view context.

## Setup

### Unity 6 6000.0.36f1

https://unity.com/releases/editor/archive

### Cursor IDE

https://www.cursor.com/

### Internet Archive CLI

https://archive.org/developers/internetarchive/cli.html

```
$ curl -LOs https://archive.org/download/ia-pex/ia
$ chmod +x ia
$ ./ia help
A command line interface to archive.org.

usage:
    ia [--help | --version]
    ia [--config-file FILE] [--log | --debug] [--insecure] <command> [<args>]...

options:
    -h, --help
    -v, --version
    -c, --config-file FILE  Use FILE as config file.
    -l, --log               Turn on logging [default: False].
    -d, --debug             Turn on verbose logging [default: False].
    -i, --insecure          Use HTTP for all requests instead of HTTPS [default: false]

commands:
    help      Retrieve help for subcommands.
    configure Configure `ia`.
    metadata  Retrieve and modify metadata for items on archive.org.
    upload    Upload items to archive.org.
    download  Download files from archive.org.
    delete    Delete files from archive.org.
    search    Search archive.org.
    tasks     Retrieve information about your archive.org catalog tasks.
    list      List files in a given item.

See 'ia help <command>' for more information on a specific command.
```

### Internet Archive SDK JavaScript Module

```
npm install internetarchive-sdk-js
```

https://github.com/internetarchive/internetarchive-sdk-js

https://www.npmjs.com/package/internetarchive-sdk-js

https://github.com/mxwllstn/internetarchive-sdk-js

# Notes on useful npm modules.

## Recommended npm Packages for Internet Archive Integration

### Internet Archive Access
- **internetarchive-sdk-js** - NodeJS/TypeScript SDK for Internet Archive APIs
  - [npm](https://www.npmjs.com/package/internetarchive-sdk-js)
  - [GitHub](https://github.com/mxwllstn/internetarchive-sdk-js)
  - `npm install internetarchive-sdk-js`

### Image Processing

- **Sharp** - High-performance image processing library with native bindings
  - [npm](https://www.npmjs.com/package/sharp)
  - [GitHub](https://github.com/lovell/sharp)
  - `npm install sharp`

### Texture Packing & Atlas Generation

- **free-tex-packer-core** - Texture packing library for creating texture atlases
  - [npm](https://www.npmjs.com/package/free-tex-packer-core)
  - [GitHub](https://github.com/odrick/free-tex-packer-core)
  - `npm install free-tex-packer-core`
  - **Why this option**: Active development, supports multiple packing algorithms, generates JSON metadata compatible with custom Unity importers, and has a pure JavaScript implementation for both server and client use
  - **Metadata format**: Outputs JSON that describes texture coordinates, which can be converted to Unity's sprite atlas format

**Notes on Texture Packing for Unity**:
- Unity has a built-in Sprite Atlas system (2017.1+), but it operates inside Unity
- For our pipeline, we use JavaScript-based packing to:
  1. Pre-process atlases before Unity consumption
  2. Create consistent atlases across different resolution tiers
  3. Maintain control over the entire pipeline
  4. Generate custom metadata for efficient loading
- The JSON metadata from free-tex-packer-core can be parsed by Unity C# scripts to create sprite references

### Document Processing

- **pdf-lib** - Create and modify PDF documents
  - [npm](https://www.npmjs.com/package/pdf-lib)
  - [GitHub](https://github.com/Hopding/pdf-lib)
  - `npm install pdf-lib`

- **pdfjs-dist** - Mozilla's PDF.js for rendering PDFs (distributed version)
  - [npm](https://www.npmjs.com/package/pdfjs-dist)
  - [GitHub](https://github.com/mozilla/pdf.js)
  - `npm install pdfjs-dist`

### File Format Handling

- **archiver** - Create ZIP and TAR archives
  - [npm](https://www.npmjs.com/package/archiver)
  - [GitHub](https://github.com/archiverjs/node-archiver)
  - `npm install archiver`

- **extract-zip** - Extract ZIP archives
  - [npm](https://www.npmjs.com/package/extract-zip)
  - [GitHub](https://github.com/maxogden/extract-zip)
  - `npm install extract-zip`

- **tar-fs** - TAR file system implementation
  - [npm](https://www.npmjs.com/package/tar-fs)
  - [GitHub](https://github.com/mafintosh/tar-fs)
  - `npm install tar-fs`

- **fluent-ffmpeg** - FFmpeg wrapper for video processing
  - [npm](https://www.npmjs.com/package/fluent-ffmpeg)
  - [GitHub](https://github.com/fluent-ffmpeg/node-fluent-ffmpeg)
  - `npm install fluent-ffmpeg` (requires FFmpeg installed on system)

### AI & Text Analysis

- **openai** - Official OpenAI API client
  - [npm](https://www.npmjs.com/package/openai)
  - [GitHub](https://github.com/openai/openai-node)
  - `npm install openai`

- **@anthropic-ai/sdk** - Official Anthropic Claude API client
  - [npm](https://www.npmjs.com/package/@anthropic-ai/sdk)
  - [GitHub](https://github.com/anthropics/anthropic-sdk-typescript)
  - `npm install @anthropic-ai/sdk`

- **natural** - Natural language processing library
  - [npm](https://www.npmjs.com/package/natural)
  - [GitHub](https://github.com/NaturalNode/natural)
  - `npm install natural`

- **ml-kmeans** - K-means clustering implementation
  - [npm](https://www.npmjs.com/package/ml-kmeans)
  - [GitHub](https://github.com/mljs/kmeans)
  - `npm install ml-kmeans`

### Utility Libraries

- **fs-extra** - Enhanced file system operations
  - [npm](https://www.npmjs.com/package/fs-extra)
  - [GitHub](https://github.com/jprichardson/node-fs-extra)
  - `npm install fs-extra`

- **p-queue** - Promise-based queue for limiting concurrent operations
  - [npm](https://www.npmjs.com/package/p-queue)
  - [GitHub](https://github.com/sindresorhus/p-queue)
  - `npm install p-queue`

- **chokidar** - Efficient file watching
  - [npm](https://www.npmjs.com/package/chokidar)
  - [GitHub](https://github.com/paulmillr/chokidar)
  - `npm install chokidar`

### Real-time Messaging for Multi-screen Control

- **Socket.IO** - Bidirectional real-time event-based communication
  - [npm](https://www.npmjs.com/package/socket.io)
  - [GitHub](https://github.com/socketio/socket.io)
  - `npm install socket.io`
  - **Pros**: Robust fallback mechanisms, broad browser support, widely adopted
  - **Cons**: Can be overengineered for simple use cases, larger bundle size

- **svelte-websocket-store** - Svelte-specific WebSocket store
  - [npm](https://www.npmjs.com/package/svelte-websocket-store)
  - [GitHub](https://github.com/arlac77/svelte-websocket-store)
  - `npm install svelte-websocket-store`
  - **Pros**: Svelte-native reactive approach, lightweight
  - **Cons**: Basic functionality, requires separate WebSocket server implementation

**Recommendation**: Start with Socket.IO for development due to its flexibility and ease of implementation. 

## Features

### Intelligent Low-Resolution Book Cover Visualization

CraftSpace uses an innovative approach to display large collections of books at various distances:

- **Adaptive Resolution Thumbnails**: Books appear with appropriate detail based on viewing distance
- **Error Diffusion Color Representation**: Even at just 2x3 pixels (6 total), book covers remain recognizable
- **Spatial Color Distribution**: The system extracts the 6 most distinct colors from each cover and places them to minimize color error:
  ```
  +-------+-------+
  |   1   |   2   |  Colors are placed to maintain
  +-------+-------+  spatial relationship with the
  |   3   |   4   |  original cover's color layout
  +-------+-------+
  |   5   |   6   |
  +-------+-------+
  ```
- **Maximum Visual Distinction**: This approach preserves gross color patterns and overall visual impression even at extreme distances
- **Seamless Detail Transitions**: As you approach books, they gradually reveal more detail through multiple LOD levels

The visualization pipeline processes book covers to extract distinct representative colors, optimize their placement using error diffusion principles, and create multiresolution texture atlases for efficient rendering of thousands of books simultaneously. 