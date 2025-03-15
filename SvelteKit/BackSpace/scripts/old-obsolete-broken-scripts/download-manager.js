import fs from 'fs-extra';
import path from 'path';
import fetch from 'node-fetch';
import { pipeline } from 'stream';
import { promisify } from 'util';
import { Transform } from 'stream';

/**
 * Manages concurrent downloads with throttling and retries
 */
export class DownloadManager {
  constructor(maxConcurrent = 5) {
    this.maxConcurrent = maxConcurrent;
    this.currentDownloads = 0;
    this.queue = [];
    this.activeDownloads = new Map(); // Track downloads by URL
    this.completedCount = 0;
    this.failedCount = 0;
    this.totalQueued = 0;
  }
  
  /**
   * Queue a download
   * @param {string} url - URL to download
   * @param {string} destPath - Destination path
   * @param {Object} options - Download options
   * @returns {Promise} Promise that resolves when download completes
   */
  async queueDownload(url, destPath, options = {}) {
    this.totalQueued++;
    
    // Create a new promise that will resolve when download completes
    return new Promise((resolve, reject) => {
      // Create download task
      const task = {
        url,
        destPath,
        options,
        resolve,
        reject,
        retry: 0,
        maxRetries: options.maxRetries || 3
      };
      
      // Queue the task
      this.queue.push(task);
      
      // Try to process queue
      this._processQueue();
    });
  }
  
  /**
   * Process the download queue
   * @private
   */
  async _processQueue() {
    // If we have capacity and tasks in queue, process them
    while (this.currentDownloads < this.maxConcurrent && this.queue.length > 0) {
      const task = this.queue.shift();
      this.currentDownloads++;
      
      // Track active download
      this.activeDownloads.set(task.url, task);
      
      try {
        const stats = await this._downloadFile(task.url, task.destPath, task.options);
        this.completedCount++;
        task.resolve(stats);
      } catch (error) {
        // Retry logic if needed
        if (task.retry < task.maxRetries) {
          task.retry++;
          console.log(`Retrying download (${task.retry}/${task.maxRetries}): ${task.url}`);
          this.queue.unshift(task); // Put back at front of queue
        } else {
          this.failedCount++;
          task.reject(error);
        }
      } finally {
        this.currentDownloads--;
        this.activeDownloads.delete(task.url);
        this._processQueue(); // Process next in queue
      }
    }
    
    // Log status occasionally
    if ((this.completedCount + this.failedCount) % 10 === 0) {
      this._logStatus();
    }
  }
  
  /**
   * Downloads a file with retry and backoff logic
   * @param {string} url - The URL to download from
   * @param {string} outputPath - Where to save the downloaded file
   * @param {Object} options - Download options
   * @returns {Object} - Download results
   */
  async _downloadFile(url, outputPath, options = {}) {
    const {
      maxRetries = 5,
      initialBackoffMs = 1000,  // Start with 1 second delay
      maxBackoffMs = 60000,     // Maximum 1 minute delay
      backoffFactor = 2,        // Double the delay each time
      timeout = 30000           // 30 seconds timeout
    } = options;
    
    let attempt = 0;
    let backoffMs = initialBackoffMs;
    
    while (attempt < maxRetries) {
      try {
        if (attempt > 0) {
          console.log(`Retry attempt ${attempt}/${maxRetries} for ${url} (waiting ${backoffMs/1000}s)`);
          // Wait for the backoff period
          await new Promise(resolve => setTimeout(resolve, backoffMs));
          
          // Increase backoff for next potential retry (with a maximum limit)
          backoffMs = Math.min(backoffMs * backoffFactor, maxBackoffMs);
        }
        
        attempt++;
        
        // Create directory if it doesn't exist
        fs.ensureDirSync(path.dirname(outputPath));
        
        // Start timing the download
        const startTime = Date.now();
        
        // Fetch the file with timeout
        const response = await fetch(url, { 
          timeout,
          headers: {
            'User-Agent': 'Mozilla/5.0 (compatible; BackSpaceArchiveBot/1.0; +https://backspace.ai)'
          }
        });
        
        if (!response.ok) {
          if (response.status === 403) {
            return {
              success: false,
              error: `Download forbidden: ${response.statusText}`,
              status: response.status,
              statusText: response.statusText,
              permanent: true
            };
          } else if (response.status === 429) {
            console.log(`Rate limited downloading ${url}, will retry with backoff`);
            continue;
          } else {
            throw new Error(`HTTP error: ${response.status} ${response.statusText}`);
          }
        }
        
        // Get content length for progress reporting
        const contentLength = parseInt(response.headers.get('content-length') || '0', 10);
        
        // Create write stream
        const fileStream = fs.createWriteStream(outputPath);
        
        // Setup pipeline with progress reporting
        let downloadedBytes = 0;
        const progressStream = new Transform({
          transform(chunk, encoding, callback) {
            downloadedBytes += chunk.length;
            if (contentLength > 0) {
              const progress = Math.round((downloadedBytes / contentLength) * 100);
              // Update progress but don't spam the console
              if (progress % 10 === 0) {
                process.stdout.write(`\rDownloading: ${progress}% complete`);
              }
            }
            callback(null, chunk);
          }
        });
        
        await pipeline(response.body, progressStream, fileStream);
        
        if (contentLength > 0) {
          process.stdout.write('\r'); // Clear progress line
          console.log(`Download complete: ${url} (${this.formatFileSize(contentLength)})`);
        } else {
          console.log(`Download complete: ${url}`);
        }
        
        // Calculate download stats
        const endTime = Date.now();
        const durationSec = (endTime - startTime) / 1000;
        const fileSizeBytes = fs.statSync(outputPath).size;
        const speedBps = fileSizeBytes / durationSec;
        const speedMBps = speedBps / (1024 * 1024);
        
        return {
          success: true,
          path: outputPath,
          size: fileSizeBytes,
          duration: durationSec,
          speedBps,
          speedMBps,
          speedFormatted: `${speedMBps.toFixed(2)} MB/s`
        };
      } catch (error) {
        console.error(`Error during download attempt ${attempt}/${maxRetries}:`, error.message);
        
        // If this was our last attempt, give up and report failure
        if (attempt >= maxRetries) {
          return {
            success: false,
            error: `Download failed after ${maxRetries} attempts: ${error.message}`
          };
        }
        
        // Otherwise, continue to next retry attempt
      }
    }
    
    // We should never reach here, but just in case
    return {
      success: false,
      error: `Download failed after ${maxRetries} attempts`
    };
  }
  
  /**
   * Format file size for human-readable output
   * @param {number} bytes - File size in bytes
   * @returns {string} - Formatted file size
   */
  formatFileSize(bytes) {
    if (bytes === 0) return '0 B';
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + sizes[i];
  }
  
  /**
   * Format duration in seconds to a readable format
   * @param {number} seconds - Duration in seconds
   * @returns {string} - Formatted duration
   */
  formatDuration(seconds) {
    if (seconds < 1) {
      return `${(seconds * 1000).toFixed(0)}ms`;
    } else if (seconds < 60) {
      return `${seconds.toFixed(1)}s`;
    } else {
      const minutes = Math.floor(seconds / 60);
      const remainingSeconds = seconds % 60;
      return `${minutes}m ${remainingSeconds.toFixed(0)}s`;
    }
  }
  
  /**
   * Log current status
   * @private
   */
  _logStatus() {
    console.log(
      `📊 Download status: ${this.completedCount} completed, ` +
      `${this.failedCount} failed, ` +
      `${this.queue.length} queued, ` +
      `${this.currentDownloads} active ` +
      `(${this.totalQueued} total)`
    );
  }
  
  /**
   * Wait for all downloads to complete
   * @returns {Promise} Promise that resolves when all downloads are done
   */
  async waitForAll() {
    // If no active downloads and no queue, resolve immediately
    if (this.currentDownloads === 0 && this.queue.length === 0) {
      return;
    }
    
    // Create a promise that resolves when queue empties and active downloads finish
    return new Promise((resolve) => {
      const checkInterval = setInterval(() => {
        if (this.currentDownloads === 0 && this.queue.length === 0) {
          clearInterval(checkInterval);
          this._logStatus(); // Final status
          resolve();
        }
      }, 100);
    });
  }
  
  /**
   * Get download statistics
   * @returns {Object} Download statistics
   */
  getStats() {
    return {
      completed: this.completedCount,
      failed: this.failedCount,
      queued: this.queue.length,
      active: this.currentDownloads,
      total: this.totalQueued
    };
  }
} 