#!/usr/bin/env node
/**
 * Initialize the BackSpace content system directory structure
 */
import fs from 'fs-extra';
import path from 'path';
import chalk from 'chalk';

// Update the content path here
const contentBasePath = path.resolve(
  process.cwd(),
  '../../Content'  // Change from './Content' to '../../Content'
);

// Use correct capitalization
const BASE_DIRS = [
  'Content',
  'Content/collections',
  'Content/config',
  'Content/cache',
  'Content/exports',
  'Content/profiles'
];

console.log(chalk.blue('Initializing BackSpace content system...'));

// Create base directories
for (const dir of BASE_DIRS) {
  const dirPath = path.resolve(dir);
  if (!fs.existsSync(dirPath)) {
    console.log(chalk.yellow(`Creating directory: ${dir}`));
    fs.mkdirSync(dirPath, { recursive: true });
  } else {
    console.log(chalk.green(`Directory exists: ${dir}`));
  }
}

// Create sample collection
const sampleCollection = {
  id: 'sample',
  name: 'Sample Collection',
  query: 'sample',
  lastUpdated: new Date().toISOString(),
  totalItems: 0,
  sort: 'relevance',
  limit: 100,
  exportProfiles: []
};

const sampleCollectionDir = path.resolve('Content/collections/sample');
fs.ensureDirSync(sampleCollectionDir);

fs.writeJsonSync(
  path.join(sampleCollectionDir, 'collection.json'),
  sampleCollection,
  { spaces: 2 }
);

// Create sample indices
const sampleItemsIndex = ["item1", "item2", "item3"];
fs.writeJsonSync(
  path.join(sampleCollectionDir, 'items-index.json'),
  sampleItemsIndex,
  { spaces: 2 }
);

const collectionsIndex = ["sample"];
fs.writeJsonSync(
  path.resolve('Content/collections-index.json'),
  collectionsIndex,
  { spaces: 2 }
);

// Create basic config
const configFile = path.resolve('Content/config/app.json');
fs.writeJsonSync(configFile, {
  basePath: path.resolve('Content'),
  enableLogging: true,
  logLevel: 'debug',
  initialized: true
}, { spaces: 2 });

console.log(chalk.green('✅ Content system initialized successfully!'));
console.log(chalk.blue('You can now use the content management commands:'));
console.log('  npm run collections:list');
console.log('  npm run collections:create -- --name "My Collection" --id my-collection --query "keyword"');

function createCollectionStructure(collectionId, options = {}) {
  // Function body update
}
