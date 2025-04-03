#!/usr/bin/env node

/**
 * Unity Automation Script
 * 
 * This script provides a command-line interface to automate Unity operations
 * from the BackSpace application.
 * 
 * Usage:
 *   tsx scripts/unity-automation.js regenerate-schemas
 *   tsx scripts/unity-automation.js build-dev
 *   tsx scripts/unity-automation.js build-prod
 *   tsx scripts/unity-automation.js test
 *   tsx scripts/unity-automation.js ci
 *   tsx scripts/unity-automation.js list-versions
 * 
 * Environment Variables:
 *   UNITY_PRECONFIGURED - Set to 'true' if running on a preconfigured runner
 *   UNITY_APP - Path to the Unity project (default: '../../Unity/CraftSpace')
 *   UNITY_VERSION - Version of Unity to use
 *   UNITY_PATH - Direct path to Unity executable
 */

import { execSync, exec } from 'child_process';
import path from 'path';
import fs from 'fs-extra';
import chalk from 'chalk';
import os from 'os';
import { fileURLToPath } from 'url';

// Import the discovery function
import { discoverUnityEnvironment } from './unity-env.js';

// ESM-compatible way to get current directory
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Constants (resolved based on where unity-automation.js is)
const UNITY_APP_CONFIG_KEY = 'UNITY_APP'; // Use a key for clarity
const UNITY_PROJECT_PATH_FROM_ENV = process.env[UNITY_APP_CONFIG_KEY];
const UNITY_AUTOMATION_DIR = __dirname;

// Parse command line arguments
const args = process.argv.slice(2);
const command = args[0];

if (!command) {
  showHelp();
  process.exit(1);
}

// Main Execution block
(async () => {
  // Discover Unity environment
  try {
    const unityEnv = await discoverUnityEnvironment();
    
    // Check if project was found
    if (unityEnv.UNITY_PROJECT_FOUND !== 'true') {
        console.error(chalk.red('Unity project discovery failed. Check paths and unity-env.js logs.'));
        process.exit(1);
    }

    // Extract key paths for convenience
    const UNITY_PROJECT_PATH = unityEnv.UNITY_APP;
    const UNITY_EXECUTABLE_SCRIPT = path.join(UNITY_PROJECT_PATH, 'run-unity.sh');

    // Make run-unity.sh executable if it exists
    if (fs.existsSync(UNITY_EXECUTABLE_SCRIPT)) {
      try {
        fs.chmodSync(UNITY_EXECUTABLE_SCRIPT, 0o755);
      } catch (err) {
        console.error(chalk.red(`Error setting executable permissions for run-unity.sh: ${err.message}`));
      }
    } else {
      console.warn(chalk.yellow(`run-unity.sh not found at ${UNITY_EXECUTABLE_SCRIPT}. Run 'npm run unity:setup' or check path constants.`));
    }

    console.log(chalk.blue(`=== Unity Automation: ${command} ===`));
    console.log(chalk.gray(`Unity Project Path: ${UNITY_PROJECT_PATH}`));

    // Check if UNITY_PATH was found before attempting commands that need it
    const needsUnityPath = !['install', 'list-versions', 'check-logs'].includes(command);
    if (needsUnityPath && !unityEnv.UNITY_PATH) {
        console.error(chalk.red('Failed to determine UNITY_PATH via unity-env.js. Cannot execute Unity command.'));
        console.error(chalk.yellow('Ensure Unity is installed and discoverable, or set UNITY_PATH/UNITY_VERSION manually in your environment.'));
        process.exit(1);
    }

    switch (command) {
      case 'regenerate-schemas':
        await runUnityCommand('-batchmode -projectPath . -ignoreCompilerErrors -executeMethod CraftSpace.Editor.SchemaGenerator.ImportAllSchemasMenuItem -quit -logFile -', unityEnv);
        break;
      case 'build-dev':
        await runUnityCommand('-batchmode -projectPath . -executeMethod Build.BuildDev -quit -logFile -', unityEnv);
        break;
      case 'build-prod':
        await runUnityCommand('-batchmode -projectPath . -executeMethod Build.BuildProd -quit -logFile -', unityEnv);
        break;
      case 'build-webgl-dev':
        await runUnityCommand('-batchmode -projectPath . -executeMethod Build.BuildWebGL_Dev -quit -logFile -', unityEnv);
        break;
      case 'build-webgl-prod':
        await runUnityCommand('-batchmode -projectPath . -executeMethod Build.BuildWebGL_Prod -quit -logFile -', unityEnv);
        break;
      case 'test':
        await runUnityCommand('-batchmode -projectPath . -runTests -testResults ./test-results.xml -quit -logFile -', unityEnv);
        break;
      case 'ci':
        // CI script likely runs unity-env itself, so just execute
        await runShellScript('./ci-build.sh', UNITY_PROJECT_PATH);
        break;
      case 'check-logs':
        await checkLogs(UNITY_PROJECT_PATH);
        break;
      case 'install':
        // Install doesn't need the full env setup, just creates files
        await createUnityFiles(UNITY_PROJECT_PATH);
        console.log(chalk.green('Unity automation files created successfully!'));
        break;
      case 'list-versions':
        // Use unity-env to list versions
        await listUnityVersions();
        break;
      default:
        console.error(chalk.red(`Unknown command: ${command}`));
        showHelp();
        process.exit(1);
    }
  } catch (error) {
    console.error(chalk.red(`Error during command execution: ${error.message}`));
    // Always log stack trace for debugging
    if (error.stack) {
        console.error(error.stack);
    }
    process.exit(1);
  }
})(); 

/**
 * Run a Unity command using the run-unity.sh script
 * @param {string} args Arguments to pass to Unity
 * @param {object} env Environment variables discovered by discoverUnityEnvironment
 */
async function runUnityCommand(args, env) {
  console.log(chalk.blue(`Running Unity command: ${args}`));
  
  const unityProjectPath = env.UNITY_APP;
  const unityExecutableScript = path.join(unityProjectPath, 'run-unity.sh');

  if (!fs.existsSync(unityExecutableScript)) {
    console.error(chalk.red(`Unity executable script not found at: ${unityExecutableScript}`));
    try {
        // Pass the correct project path to the creation function
        await createUnityExecutableScript(unityProjectPath);
    } catch (createErr) {
        console.error(chalk.red(`Failed to create run-unity.sh: ${createErr.message}`));
        throw new Error('Unity executable script not found and could not be created');
    }
  }
  
  try {
    const cmd = `cd "${unityProjectPath}" && ./run-unity.sh ${args}`;
    console.log(chalk.gray(`Executing: ${cmd}`));
    
    // Execute the command and stream output, passing the discovered environment
    // Merge discovered env with process.env, giving priority to discovered ones
    const executionEnv = { ...process.env, ...env };

    execSync(cmd, { stdio: 'inherit', env: executionEnv });
    
    console.log(chalk.green('Command completed successfully!'));
  } catch (error) {
    console.error(chalk.red(`Command failed with exit code: ${error.status}`));
    throw error;
  }
}

/**
 * Run a shell script in the Unity project directory
 * @param {string} scriptName Relative name of the script (e.g., ./ci-build.sh)
 * @param {string} unityProjectPath Absolute path to the Unity project
 */
async function runShellScript(scriptName, unityProjectPath) {
  console.log(chalk.blue(`Running script: ${scriptName}`));
  
  if (!fs.existsSync(unityProjectPath)) {
     console.error(chalk.red(`Unity project directory not found at: ${unityProjectPath}`));
     throw new Error('Unity project directory not found');
  }
  
  const scriptPath = path.join(unityProjectPath, scriptName);
  if (!fs.existsSync(scriptPath)) {
    console.error(chalk.red(`Script not found at: ${scriptPath}`));
    throw new Error('Script not found');
  }
  
  try {
    const cmd = `cd "${unityProjectPath}" && ${scriptName.startsWith('./') ? scriptName : './' + scriptName}`;
    console.log(chalk.gray(`Executing: ${cmd}`));
    
    execSync(cmd, { stdio: 'inherit' }); // Assuming CI script manages its own environment
    console.log(chalk.green('Script completed successfully!'));
  } catch (error) {
    console.error(chalk.red(`Script failed with exit code: ${error.status}`));
    throw error;
  }
}

/**
 * Check Unity logs for errors
 * @param {string} unityProjectPath Absolute path to the Unity project
 */
async function checkLogs(unityProjectPath) {
  console.log(chalk.blue('Checking Unity logs for errors...'));
  
  if (!fs.existsSync(unityProjectPath)) {
     console.error(chalk.red(`Unity project directory not found at: ${unityProjectPath}`));
     throw new Error('Unity project directory not found');
  }
  
  try {
    // Check if any log files exist
    const logsPattern = path.join(unityProjectPath, 'unity-*.log');
    // Use platform-independent globbing or find if needed
    const findCmd = process.platform === 'win32' 
        ? `dir /b "${logsPattern}"` 
        : `find "${unityProjectPath}" -maxdepth 1 -name "unity-*.log"`;
    let logFiles = '';
    try { logFiles = execSync(findCmd, { encoding: 'utf8' }); } catch(e) { /* ignore find errors */ }
    
    if (!logFiles.trim()) {
      console.log(chalk.yellow('No Unity log files found.'));
      return;
    }
    
    // Use platform-independent grep
    const grepCmd = process.platform === 'win32'
        ? `findstr /i /c:"error" "${logsPattern}"`
        : `grep -i error ${logsPattern.replace(/\//g, '/')}`;
        
    try {
        const result = execSync(grepCmd, { encoding: 'utf8' });
        if (result.trim()) {
            console.log(chalk.yellow('Errors found in Unity logs:'));
            console.log(result);
        } else {
             console.log(chalk.green('No errors found in Unity logs!'));
        }
    } catch (grepError) {
        // Grep returns non-zero if no match
        if (grepError.status === 1 || (process.platform === 'win32' && grepError.status !== 0)) {
            console.log(chalk.green('No errors found in Unity logs!'));
        } else {
            throw grepError; // Rethrow actual errors
        }
    }
  } catch (error) {
    console.error(chalk.red(`Error checking logs: ${error.message}`));
    throw error;
  }
}

/**
 * Create the run-unity.sh script in the Unity project
 * @param {string} unityProjectPath Absolute path to the Unity project
 */
async function createUnityExecutableScript(unityProjectPath) {
  const unityExecutableScript = path.join(unityProjectPath, 'run-unity.sh');
  // Create directory if it doesn't exist
  if (!fs.existsSync(unityProjectPath)) {
    console.log(chalk.yellow(`Unity project directory doesn't exist at ${unityProjectPath}. Creating it...`));
    fs.mkdirpSync(unityProjectPath);
  }

  // Use the simplified run-unity.sh content
  const scriptContent = `#!/bin/bash

# run-unity.sh - A simple wrapper script for running Unity commands
# Relies on UNITY_PATH environment variable being set correctly before execution.
# Usage: ./run-unity.sh [arguments]
# Example: ./run-unity.sh -batchmode -projectPath . -executeMethod Build.BuildProd -quit

# Check if UNITY_PATH is set
if [ -z "$UNITY_PATH" ]; then
    echo "Error: UNITY_PATH environment variable is not set."
    echo "Please ensure the environment is configured correctly (e.g., by running unity-env.js)."
    exit 1
fi

# Check if Unity executable exists at the provided path
if [ ! -f "$UNITY_PATH" ]; then
    echo "Error: Unity executable not found at specified UNITY_PATH: $UNITY_PATH"
    exit 1
fi

echo "Using Unity at: $UNITY_PATH"

# Prepare arguments
if [ "$#" -eq 0 ]; then
    echo "No arguments provided. Using defaults."
    ARGS="-batchmode -projectPath . -quit"
else
    ARGS="$@"
fi

# Check if -logFile - is specified (stream to stdout)
if [[ "$ARGS" == *"-logFile -"* ]]; then
    echo "Streaming Unity logs directly to stdout"
    # Run Unity and pass stdout/stderr through directly
    "$UNITY_PATH" $ARGS
    EXIT_CODE=$?
else
    # Add log file argument if not already specified
    # Assumes execution within the project directory context set by the caller (unity-automation.js)
    if [[ "$ARGS" != *"-logFile"* ]]; then
        LOGFILE="unity-$(date +%Y%m%d-%H%M%S).log"
        ARGS="$ARGS -logFile $LOGFILE"
        echo "Log file will be saved to: $LOGFILE"
    fi

    echo "Running Unity command: $UNITY_PATH $ARGS"

    # Run Unity and tee the output to both the log file and stdout
    # Use a temp file for the command output
    TEMP_LOG=$(mktemp)
    "$UNITY_PATH" $ARGS > "$TEMP_LOG" 2>&1 & 
    PID=$!

    # Tail the log file in real-time while Unity is running
    if [[ "$ARGS" == *"-logFile"* ]]; then
        # Extract the logfile name from arguments
        LOG_PATTERN=".*-logFile[= ]([^ ]+).*"
        if [[ $ARGS =~ $LOG_PATTERN ]]; then
            UNITY_LOGFILE="${BASH_REMATCH[1]}"
            echo "Streaming Unity log file: $UNITY_LOGFILE"
            # Wait for log file to be created
            while [ ! -f "$UNITY_LOGFILE" ] && kill -0 $PID 2>/dev/null; do
                sleep 0.5
            done
            # Tail the log if it exists
            if [ -f "$UNITY_LOGFILE" ]; then
                tail -f "$UNITY_LOGFILE" &
                TAIL_PID=$!
            fi
        fi
    fi

    # Wait for Unity to exit
    wait $PID
    EXIT_CODE=$?

    # Stop the tail process if it's running
    if [ ! -z \${TAIL_PID+x} ]; then
        kill $TAIL_PID 2>/dev/null || true
    fi

    # Output the temp log
    cat "$TEMP_LOG"
    rm "$TEMP_LOG"
fi

echo "Unity process finished with exit code: $EXIT_CODE"
exit $EXIT_CODE`;

  // Write the script file
  try {
    fs.writeFileSync(unityExecutableScript, scriptContent, { mode: 0o755 });
    console.log(chalk.green(`Created Unity executable script at: ${unityExecutableScript}`));
  } catch (error) {
    console.error(chalk.red(`Error creating script: ${error.message}`));
    throw error;
  }
}

/**
 * Create Unity automation files in the project
 * @param {string} unityProjectPath Absolute path to the Unity project
 */
async function createUnityFiles(unityProjectPath) {
  // Create run-unity.sh first
  await createUnityExecutableScript(unityProjectPath);
  
  // Create ci-build.sh for CI pipeline if needed
  const ciBuildScript = path.join(unityProjectPath, 'ci-build.sh');
  if (!fs.existsSync(ciBuildScript)) {
    const ciBuildContent = `#!/bin/bash
# CI Build Script for Unity Project
# This script is used by the CI pipeline to build the Unity project

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Source unity environment variables if available
if [ -f "$SCRIPT_DIR/../SvelteKit/BackSpace/.unity-env" ]; then
  source "$SCRIPT_DIR/../SvelteKit/BackSpace/.unity-env"
else
  # Set up environment variables
  cd "$SCRIPT_DIR/../SvelteKit/BackSpace"
  source <(node scripts/unity-env.js --shell)
  cd "$SCRIPT_DIR"
fi

# Run Unity build
echo "Running Unity build..."
"$SCRIPT_DIR/run-unity.sh" -batchmode -projectPath . -executeMethod Build.BuildProd -quit

exit $?`;

    try {
      fs.writeFileSync(ciBuildScript, ciBuildContent, { mode: 0o755 });
      console.log(chalk.green(`Created CI build script at: ${ciBuildScript}`));
    } catch (error) {
      console.error(chalk.red(`Error creating CI script: ${error.message}`));
      // Not a fatal error, just continue
    }
  }
}

/**
 * List installed Unity versions
 */
async function listUnityVersions() {
  console.log(chalk.blue('Discovering installed Unity versions...'));
  
  try {
    // Call unity-env directly to list versions
    const env = await discoverUnityEnvironment();
    
    if (env.UNITY_VERSIONS_FOUND) {
      const versions = env.UNITY_VERSIONS_FOUND.split(',').filter(Boolean);
      if (versions.length > 0) {
        console.log(chalk.green(`Found ${versions.length} Unity versions:`));
        versions.forEach((version, index) => {
          const isActive = version === env.UNITY_VERSION ? ' (active)' : '';
          console.log(chalk.white(`  ${index + 1}. ${version}${isActive}`));
        });
        return;
      }
    }
    
    console.log(chalk.yellow('No Unity versions found.'));
  } catch (error) {
    console.error(chalk.red(`Error listing Unity versions: ${error.message}`));
    throw error;
  }
}

/**
 * Show help information
 */
function showHelp() {
  console.log(chalk.blue('Unity Automation Tool'));
  console.log('');
  console.log('Commands:');
  console.log('  regenerate-schemas   Regenerate Unity C# schemas from TypeScript/JSON Schema');
  console.log('  build-dev            Build development version of the Unity project');
  console.log('  build-prod           Build production version of the Unity project');
  console.log('  test                 Run Unity tests');
  console.log('  ci                   Run CI build script');
  console.log('  check-logs           Check Unity logs for errors');
  console.log('  install              Install Unity automation files');
  console.log('  list-versions        List installed Unity versions');
  console.log('');
  console.log('Environment Variables:');
  console.log('  UNITY_PRECONFIGURED  Set to true for preconfigured environments');
  console.log('  UNITY_APP            Path to Unity project');
  console.log('  UNITY_VERSION        Unity version to use');
  console.log('  UNITY_PATH           Direct path to Unity executable');
}