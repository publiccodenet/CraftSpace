#!/usr/bin/env node
/**
 * Install Unity WebGL build into the SvelteKit static directory
 * 
 * Usage:
 * npm run install-unity -- --app craftspace --source /path/to/unity/build --clean
 */
import { Command } from 'commander';
import fs from 'fs-extra';
import path from 'path';
import chalk from 'chalk';

const program = new Command();

program
  .name('install-unity')
  .description('Install Unity WebGL build into SvelteKit static directory')
  .requiredOption('--app <name>', 'App name (e.g., craftspace)')
  .requiredOption('--source <path>', 'Source build directory')
  .option('--clean', 'Clean target directory before copying', false)
  .action(async (options) => {
    const sourcePath = path.resolve(options.source);
    const targetPath = path.resolve('static/unity', options.app);
    
    console.log(chalk.blue(`Installing Unity build for ${options.app}`));
    console.log(`Source: ${sourcePath}`);
    console.log(`Target: ${targetPath}`);
    
    // Check if source exists
    if (!await fs.pathExists(sourcePath)) {
      console.error(chalk.red(`Source directory not found: ${sourcePath}`));
      process.exit(1);
    }
    
    // Clean if requested
    if (options.clean && await fs.pathExists(targetPath)) {
      console.log(chalk.yellow(`Cleaning target directory: ${targetPath}`));
      await fs.remove(targetPath);
    }
    
    // Create target directory
    await fs.ensureDir(targetPath);
    
    // Copy build files
    console.log(chalk.blue('Copying build files...'));
    try {
      await fs.copy(sourcePath, targetPath);
      console.log(chalk.green('✅ Unity build installed successfully!'));
    } catch (error) {
      console.error(chalk.red('Error installing Unity build:'), error);
      process.exit(1);
    }
  });

program.parse(); 