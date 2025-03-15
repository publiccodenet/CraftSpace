import { Command } from 'commander';
import { contentManager } from '../src/lib/content/index.js';
import { CollectionCreateSchema } from '../src/lib/schemas/collection.js';
import type { CollectionCreate } from '../src/lib/schemas/collection.js';
import { validate } from '../src/lib/utils/validators.js';
import chalk from 'chalk';
import path from 'path';
import fs from 'fs-extra';
import { createLogger } from '../src/lib/utils/logger.js'; 