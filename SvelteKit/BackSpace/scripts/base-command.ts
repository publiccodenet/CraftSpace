#!/usr/bin/env node
/**
 * Base command class for CLI scripts 🧰
 * 
 * Provides common functionality for all CLI commands including:
 * - Command registration and parsing
 * - Standardized logging with emoji support
 * - Error handling
 * - Common option patterns
 */
import { Command } from 'commander';
import chalk from 'chalk';
import { EMOJI, CLI_FORMATTING } from '../src/lib/constants/index.js';

export abstract class BaseCommand {
  protected program: Command;
  protected commandName: string;
  
  constructor(name: string, description: string) {
    this.commandName = name;
    this.program = new Command();
    
    // Set up base command
    this.program
      .name(name)
      .description(description)
      .version('0.1.0');
      
    // Add common options that apply to all/most commands
    this.program
      .option('-v, --verbose', 'Show verbose output')
      .option('-j, --json', 'Output as JSON');
  }
  
  // Parse command line arguments
  public parse(): Command {
    return this.program.parse();
  }
  
  // Logging helpers with emoji support
  public log(message: string, ...args: unknown[]): void {
    if (args.length === 0) {
      console.log(message);
    } else {
      console.log(message, ...args);
    }
  }
  
  public info(message: string, ...args: unknown[]): void {
    console.log(`${CLI_FORMATTING.BLUE}${EMOJI.INFO} ${message}${CLI_FORMATTING.RESET}`, ...args);
  }
  
  public success(message: string, ...args: unknown[]): void {
    console.log(`${CLI_FORMATTING.GREEN}${EMOJI.SUCCESS} ${message}${CLI_FORMATTING.RESET}`, ...args);
  }
  
  public warn(message: string, ...args: unknown[]): void {
    console.log(`${CLI_FORMATTING.YELLOW}${EMOJI.WARNING} ${message}${CLI_FORMATTING.RESET}`, ...args);
  }
  
  public error(message: string, ...args: unknown[]): void {
    console.error(`${CLI_FORMATTING.RED}${EMOJI.ERROR} ${message}${CLI_FORMATTING.RESET}`, ...args);
  }
  
  public debug(message: string, ...args: unknown[]): void {
    // Only show debug in verbose mode
    if (this.program.opts().verbose) {
      console.log(`${CLI_FORMATTING.DIM}${EMOJI.DEBUG} ${message}${CLI_FORMATTING.RESET}`, ...args);
    }
  }
  
  // Create a banner for important messages
  public banner(message: string): void {
    const padding = '═'.repeat(Math.max(0, (80 - message.length - 2) / 2));
    console.log(`\n${padding} ${message} ${padding}\n`);
  }
  
  // Show a progress message with status
  public progress(current: number, total: number, message: string): void {
    const percent = Math.round((current / total) * 100);
    const bar = '▰'.repeat(Math.round(percent / 10)) + '▱'.repeat(10 - Math.round(percent / 10));
    process.stdout.write(`\r${EMOJI.RUNNING} [${bar}] ${percent}% ${message}`);
    if (current === total) {
      process.stdout.write('\n');
    }
  }
  
  // Handle common errors
  protected handleError(error: Error, exitCode = 1): never {
    this.error(`${this.commandName} failed:`, error);
    process.exit(exitCode);
  }
}

/**
 * Create a command factory for a specific entity type
 */
export function createEntityCommandFactory<T>(
  entityName: string,
  validateFn: (data: unknown) => { success: boolean; data?: T; errors?: any }
) {
  return {
    createListCommand: (listFn: () => Promise<T[]>) => {
      return new Command('list')
        .description(`List all ${entityName}s`)
        .option('-j, --json', 'Output as JSON')
        .option('-v, --verbose', 'Show verbose output')
        .action(async (options) => {
          try {
            const entities = await listFn();
            
            if (options.json) {
              console.log(JSON.stringify(entities, null, 2));
              return;
            }
            
            console.log(chalk.blue(`Found ${entities.length} ${entityName}s:`));
            // Entity-specific formatting would go here
          } catch (error) {
            console.error(chalk.red(`Error listing ${entityName}s:`), error);
            process.exit(1);
          }
        });
    },
    
    createCreateCommand: (createFn: (data: T) => Promise<T>) => {
      // Entity-specific implementation
      return new Command('create')
        .description(`Create a new ${entityName}`)
        // Options would be added specific to the entity
        .action(async (options) => {
          try {
            // Entity-specific data construction
            const data = {} as unknown as T;
            
            // Validate with the provided function
            const validationResult = validateFn(data);
            
            if (!validationResult.success) {
              console.error(chalk.red(`Invalid ${entityName} data:`));
              console.error(validationResult.errors);
              process.exit(1);
            }
            
            const result = await createFn(validationResult.data!);
            console.log(chalk.green(`${entityName} created successfully`));
          } catch (error) {
            console.error(chalk.red(`Error creating ${entityName}:`), error);
            process.exit(1);
          }
        });
    }
    
    // Additional common command factories would go here
  };
} 