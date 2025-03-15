/**
 * Standard export profiles for collections
 * These templates can be referenced by name in collection configurations
 */

export const EXPORT_PROFILES = {
  // Unity WebGL build optimized profile
  unity: {
    name: "unity",
    enabled: true,
    description: "Optimized for Unity WebGL build",
    assets: ["cover", "tile", "pixel"],
    coverOptions: {
      maxWidth: 512,
      maxHeight: 512,
      format: "jpg",
      quality: 85
    },
    atlasOptions: {
      enabled: true,
      size: 2048,
      itemsPerAtlas: 64
    },
    includePatterns: ["id", "title", "creator", "description", "cover.*", "favorite_count"],
    excludePatterns: ["*_cached", "cache_metadata.*", "epub_original_metadata"]
  },
  
  // Standard web view profile
  web: {
    name: "web",
    enabled: true,
    description: "Standard web view with optimized assets",
    assets: ["cover", "tile", "content"],
    coverOptions: {
      maxWidth: 800,
      maxHeight: 1200,
      format: "webp",
      quality: 85
    },
    contentOptions: {
      maxSizeMB: 10,
      formats: ["pdf", "epub"]
    },
    includePatterns: ["*"],
    excludePatterns: ["*_etag", "*_size", "cache_metadata.*"]
  },
  
  // Mobile-optimized profile
  mobile: {
    name: "mobile",
    enabled: true,
    description: "Mobile-optimized with minimal assets",
    assets: ["cover", "pixel"],
    coverOptions: {
      maxWidth: 300,
      maxHeight: 450,
      format: "webp",
      quality: 80
    },
    includePatterns: ["id", "title", "creator", "{description,summary}"],
    excludePatterns: ["*_full", "*_hires"]
  },
  
  // High-resolution exhibition profile
  installation: {
    name: "installation",
    enabled: true,
    description: "High resolution assets for installations and exhibitions",
    assets: ["cover", "pyramid", "content"],
    coverOptions: {
      maxWidth: 2400,
      maxHeight: 3600,
      format: "png"
    },
    pyramidOptions: {
      maxZoomLevel: 5,
      tileSize: 256
    },
    includePatterns: ["*"],
    excludePatterns: []
  },
  
  // Full data archive profile
  archive: {
    name: "archive",
    enabled: true,
    description: "Complete archive with all metadata and assets",
    assets: ["cover", "content", "original"],
    contentOptions: {
      preserveAll: true,
      includeOriginals: true
    },
    includePatterns: ["*"],
    excludePatterns: []
  },
  
  // Minimal metadata only profile
  minimal: {
    name: "minimal",
    enabled: true,
    description: "Minimal metadata only, no assets",
    assets: [],
    includePatterns: ["id", "title", "creator", "date"],
    excludePatterns: ["*_cached", "*_full", "cache_metadata"]
  }
};

/**
 * Get an export profile by name
 * @param {string} profileName - Name of the profile to retrieve
 * @returns {Object|null} - The profile or null if not found
 */
export function getExportProfile(profileName) {
  return EXPORT_PROFILES[profileName] || null;
}

/**
 * Get a list of available export profile names
 * @returns {Array<string>} - List of profile names
 */
export function listExportProfiles() {
  return Object.keys(EXPORT_PROFILES);
}

/**
 * Create an export target from a profile with optional overrides
 * @param {string|Object} profile - Profile name or profile object
 * @param {Object} overrides - Optional overrides to merge with the profile
 * @returns {Object} - The configured export target
 */
export function createExportTarget(profile, overrides = {}) {
  // If profile is a string, look it up by name
  const baseProfile = typeof profile === 'string' 
    ? getExportProfile(profile) 
    : profile;
    
  if (!baseProfile) {
    throw new Error(`Export profile not found: ${profile}`);
  }
  
  // Deep clone the profile to avoid modifying the original
  const clonedProfile = JSON.parse(JSON.stringify(baseProfile));
  
  // Merge overrides into the cloned profile
  return deepMerge(clonedProfile, overrides);
}

/**
 * Deep merge two objects
 * @param {Object} target - Target object
 * @param {Object} source - Source object to merge in
 * @returns {Object} - Merged object
 */
function deepMerge(target, source) {
  // Handle null or undefined inputs
  if (!source) return target;
  if (!target) return source;

  // Create a new output object
  const output = { ...target };
  
  // Iterate over source properties
  Object.keys(source).forEach(key => {
    // If property is an object and not an array, recursively merge
    if (
      source[key] && 
      typeof source[key] === 'object' && 
      !Array.isArray(source[key]) &&
      target[key] && 
      typeof target[key] === 'object' && 
      !Array.isArray(target[key])
    ) {
      output[key] = deepMerge(target[key], source[key]);
    } else {
      // Otherwise, just use the source value
      output[key] = source[key];
    }
  });
  
  return output;
} 