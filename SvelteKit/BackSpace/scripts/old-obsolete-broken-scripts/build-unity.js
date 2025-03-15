#!/usr/bin/env node

const { execSync } = require('child_process');
const fs = require('fs-extra');
const path = require('path');

// Paths
const UNITY_PROJECT_PATH = path.resolve('../../Unity/CraftSpace');
const UNITY_BUILD_OUTPUT = path.join(UNITY_PROJECT_PATH, 'Build/WebGL');
const SVELTEKIT_TARGET = path.resolve('../static/Build/WebGL');

// Unity executable path - update this for your specific Unity installation
// Default paths by platform:
let UNITY_PATH;

if (process.platform === 'darwin') { // macOS
  UNITY_PATH = '/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity';
} else if (process.platform === 'win32') { // Windows
  UNITY_PATH = 'C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.20f1\\Editor\\Unity.exe';
} else { // Linux
  UNITY_PATH = '/opt/unity/Editor/Unity';
}

async function buildUnity() {
  console.log('Starting Unity WebGL build...');
  
  try {
    // Ensure target directory exists
    fs.ensureDirSync(SVELTEKIT_TARGET);
    
    // Build Unity project
    const buildCommand = `"${UNITY_PATH}" -batchmode -nographics -quit -projectPath "${UNITY_PROJECT_PATH}" -executeMethod BuildScript.BuildWebGL -logFile unity_build.log`;
    
    console.log(`Executing: ${buildCommand}`);
    execSync(buildCommand, { stdio: 'inherit' });
    
    // Check for build log errors
    const logPath = path.join(UNITY_PROJECT_PATH, 'unity_build.log');
    if (fs.existsSync(logPath)) {
      const log = fs.readFileSync(logPath, 'utf8');
      if (log.includes('Error') || log.includes('Exception')) {
        console.error('Unity build failed. Check the log file for details.');
        console.log(log);
        process.exit(1);
      }
    }
    
    // Copy build outputs to SvelteKit static directory
    console.log(`Copying Unity build from ${UNITY_BUILD_OUTPUT} to ${SVELTEKIT_TARGET}`);
    fs.copySync(UNITY_BUILD_OUTPUT, SVELTEKIT_TARGET);
    
    console.log('Unity build and copy completed successfully!');
  } catch (error) {
    console.error('Unity build failed:', error);
    process.exit(1);
  }
}

buildUnity().catch(error => {
  console.error('Build script failed:', error);
  process.exit(1);
}); 