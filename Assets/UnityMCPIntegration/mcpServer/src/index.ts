#!/usr/bin/env node
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { WebSocketHandler } from './websocketHandler.js';
import { registerTools } from './toolDefinitions.js';
import { registerFilesystemTools } from './filesystemTools.js';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

class UnityMCPServer {
  private server: Server;
  private wsHandler: WebSocketHandler;

  constructor() {
    // Initialize MCP Server
    this.server = new Server(
      {
        name: 'unity-mcp-server',
        version: '0.2.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    // Get port from environment variable or use default
    let wsPort = parseInt(process.env.MCP_WEBSOCKET_PORT || '5010');
    
    // Determine Unity project path based on this file's location
    const __filename = fileURLToPath(import.meta.url);
    const __dirname = path.dirname(__filename);
    
    // Get the project root path (parent of Assets)
    let projectRootPath = process.env.UNITY_PROJECT_PATH || this.determineUnityProjectPath(__dirname);
    
    // Sanitize the path to remove any unexpected characters
    projectRootPath = projectRootPath.replace(/["']/g, '');
    
    // Fix potential issue with backslashes being removed
    projectRootPath = path.normalize(projectRootPath);
    
    // Make sure path ends with a directory separator if it's missing
    if (!projectRootPath.endsWith(path.sep)) {
      projectRootPath += path.sep;
    }
    
    console.error(`[Unity MCP] Project root detected as: ${projectRootPath}`);
    
    // Create the full path to the Assets folder for filesystem operations
    const projectPath = path.join(projectRootPath, 'Assets') + path.sep;
    
    // Verify the Assets folder exists
    if (!fs.existsSync(projectPath)) {
      console.error(`[Unity MCP] WARNING: Assets folder not found at ${projectPath}`);
      console.error(`[Unity MCP] Using project root instead: ${projectRootPath}`);
      // If Assets folder doesn't exist, fall back to project root
      process.env.UNITY_PROJECT_PATH = projectRootPath;
    } else {
      console.error(`[Unity MCP] Using project path: ${projectPath}`);
      // Set the environment variable to include the Assets folder
      process.env.UNITY_PROJECT_PATH = projectPath;
    }
    
    // Initialize WebSocket Handler for Unity communication
    this.wsHandler = new WebSocketHandler(wsPort);

    // Register MCP tools
    registerTools(this.server, this.wsHandler);
    
    // Register filesystem tools to access Unity project files
    registerFilesystemTools(this.server, this.wsHandler);

    // Error handling
    this.server.onerror = (error) => console.error('[MCP Error]', error);
    process.on('SIGINT', async () => {
      await this.cleanup();
      process.exit(0);
    });
    
    // Also handle SIGTERM for clean Docker container shutdown
    process.on('SIGTERM', async () => {
      await this.cleanup();
      process.exit(0);
    });
  }

  /**
   * Determine the Unity project path based on the script location
   * Handles both direct installation in Assets and Package Manager installation
   */
  private determineUnityProjectPath(scriptDir: string): string {
    // Normalize the path to ensure consistent separators
    scriptDir = path.normalize(scriptDir);
    console.error(`[Unity MCP] Script directory: ${scriptDir}`);
    
    // Case 1: Installed in Assets folder
    // Example: F:/UnityProjects/UnityMCP/Assets/UnityMCPIntegration/mcpServer/src
    const assetsMatch = /^(.+?[\/\\]Assets)[\/\\].*$/i.exec(scriptDir);
    if (assetsMatch) {
      console.error('[Unity MCP] Detected installation in Assets folder');
      // Return path up to the project root (parent of Assets folder)
      const projectRoot = path.dirname(assetsMatch[1]); // Get parent of Assets folder
      console.error(`[Unity MCP] Project root detected as: ${projectRoot}`);
      return projectRoot;
    }
    
    // Case 2: Installed via Package Manager
    // Example: F:/UnityProjects/UnityGenAIPlugin/Library/PackageCache/com.quaza.unitymcp@d2b8f1260bca/mcpServer/src
    const libraryMatch = /^(.+?[\/\\]Library)[\/\\]PackageCache[\/\\].*$/i.exec(scriptDir);
    if (libraryMatch) {
      console.error('[Unity MCP] Detected installation via Package Manager');
      // Extract the path up to Library and replace Library with Assets
      const projectRoot = path.dirname(libraryMatch[1]); // Get parent of Library folder
      console.error(`[Unity MCP] Project root detected as: ${projectRoot}`);
      
      // Verify that this is really a Unity project by checking for Assets folder
      const assetsPath = path.join(projectRoot, 'Assets');
      if (fs.existsSync(assetsPath)) {
        return projectRoot;
      } else {
        console.error(`[Unity MCP] Warning: Assets folder not found at ${assetsPath}`);
      }
    }
    
    // Try to find Assets folder by walking up directories
    let currentDir = scriptDir;
    while (currentDir && path.dirname(currentDir) !== currentDir) {
      // Check if this directory contains an Assets folder
      const assetsDir = path.join(currentDir, 'Assets');
      if (fs.existsSync(assetsDir) && fs.statSync(assetsDir).isDirectory()) {
        console.error(`[Unity MCP] Found Unity project by locating Assets folder at: ${assetsDir}`);
        return currentDir;
      }
      currentDir = path.dirname(currentDir);
    }
    
    // If we get here, we couldn't determine the project path
    console.error('[Unity MCP] ERROR: Could not detect Unity project directory.');
    console.error('[Unity MCP] Please ensure this server is running within a Unity project structure.');
    console.error('[Unity MCP] Using current directory as fallback, but functionality may be limited.');
    return process.cwd();
  }

  private async cleanup() {
    console.error('Cleaning up resources...');
    await this.wsHandler.close();
    await this.server.close();
  }

  async run() {
    // Connect to stdio for MCP communication
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('[Unity MCP] Server running and ready to accept connections');
    console.error('[Unity MCP] WebSocket server listening on port', this.wsHandler.port);
  }
}

// Start the server
const server = new UnityMCPServer();
server.run().catch(err => {
  console.error('Fatal error in MCP server:', err);
  process.exit(1);
});