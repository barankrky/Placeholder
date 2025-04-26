import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { WebSocketHandler } from './websocketHandler.js';
import fs from 'fs/promises';
import path from 'path';
import { z } from 'zod';
import { createTwoFilesPatch } from 'diff';
import { minimatch } from 'minimatch';
import {
  ReadFileArgsSchema,
  ReadMultipleFilesArgsSchema,
  WriteFileArgsSchema,
  EditFileArgsSchema,
  ListDirectoryArgsSchema,
  DirectoryTreeArgsSchema,
  SearchFilesArgsSchema,
  GetFileInfoArgsSchema,
  FindAssetsByTypeArgsSchema
  // Remove ListScriptsArgsSchema since it no longer exists in toolDefinitions.ts
} from './toolDefinitions.js';

// Interface definitions
interface FileInfo {
  size: number;
  created: Date;
  modified: Date;
  accessed: Date;
  isDirectory: boolean;
  isFile: boolean;
  permissions: string;
}

interface TreeEntry {
  name: string;
  type: 'file' | 'directory';
  children?: TreeEntry[];
}

// Helper functions
// Updated validatePath function to properly handle empty paths
async function validatePath(requestedPath: string, assetRootPath: string): Promise<string> {
  // If path is empty or just quotes, use the asset root path directly
  if (!requestedPath || requestedPath.trim() === '' || requestedPath.trim() === '""' || requestedPath.trim() === "''") {
    return assetRootPath;
  }
  
  // Clean the path to remove any unexpected quotes or escape characters
  let cleanPath = requestedPath.replace(/['"\\]/g, '');
  
  // Handle empty path after cleaning
  if (!cleanPath || cleanPath.trim() === '') {
    return assetRootPath;
  }
  
  // Normalize path to handle both Windows and Unix-style paths
  const normalized = path.normalize(cleanPath);
  
  // For relative paths, join with asset root path
  // Only check for absolute paths using path.isAbsolute - all other paths are considered relative
  let absolute;
  if (path.isAbsolute(normalized)) {
    absolute = normalized;
    
    // Additional check: if the absolute path is outside the project and doesn't exist,
    // try treating it as a relative path first
    if (!absolute.startsWith(assetRootPath)) {
      const tryRelative = path.join(assetRootPath, normalized);
      try {
        await fs.access(tryRelative);
        // If we can access it as a relative path, use that instead
        absolute = tryRelative;
      } catch {
        // If we can't access it as a relative path either, keep the original absolute path
        // and let the next check handle the potential error
      }
    }
  } else {
    absolute = path.join(assetRootPath, normalized);
  }
    
  const resolvedPath = path.resolve(absolute);
  
  // Ensure we don't escape out of the Unity project folder
  // Special case for empty path: it should always resolve to the project root
  if (!resolvedPath.startsWith(assetRootPath) && requestedPath.trim() !== '') {
    throw new Error(`Access denied: Path ${requestedPath} is outside the Unity project directory`);
  }
  
  return resolvedPath;
}

async function getFileStats(filePath: string): Promise<FileInfo> {
  const stats = await fs.stat(filePath);
  return {
    size: stats.size,
    created: stats.birthtime,
    modified: stats.mtime,
    accessed: stats.atime,
    isDirectory: stats.isDirectory(),
    isFile: stats.isFile(),
    permissions: stats.mode.toString(8).slice(-3),
  };
}

async function searchFiles(
  rootPath: string,
  pattern: string,
  excludePatterns: string[] = []
): Promise<string[]> {
  const results: string[] = [];

  async function search(currentPath: string) {
    const entries = await fs.readdir(currentPath, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(currentPath, entry.name);

      try {
        // Check if path matches any exclude pattern
        const relativePath = path.relative(rootPath, fullPath);
        const shouldExclude = excludePatterns.some(pattern => {
          const globPattern = pattern.includes('*') ? pattern : `**/${pattern}/**`;
          return minimatch(relativePath, globPattern, { dot: true });
        });

        if (shouldExclude) {
          continue;
        }

        if (entry.name.toLowerCase().includes(pattern.toLowerCase())) {
          results.push(fullPath);
        }

        if (entry.isDirectory()) {
          await search(fullPath);
        }
      } catch (error) {
        // Skip invalid paths during search
        continue;
      }
    }
  }

  await search(rootPath);
  return results;
}

function normalizeLineEndings(text: string): string {
  return text.replace(/\r\n/g, '\n');
}

function createUnifiedDiff(originalContent: string, newContent: string, filepath: string = 'file'): string {
  // Ensure consistent line endings for diff
  const normalizedOriginal = normalizeLineEndings(originalContent);
  const normalizedNew = normalizeLineEndings(newContent);

  return createTwoFilesPatch(
    filepath,
    filepath,
    normalizedOriginal,
    normalizedNew,
    'original',
    'modified'
  );
}

async function applyFileEdits(
  filePath: string,
  edits: Array<{oldText: string, newText: string}>,
  dryRun = false
): Promise<string> {
  // Read file content and normalize line endings
  const content = normalizeLineEndings(await fs.readFile(filePath, 'utf-8'));

  // Apply edits sequentially
  let modifiedContent = content;
  for (const edit of edits) {
    const normalizedOld = normalizeLineEndings(edit.oldText);
    const normalizedNew = normalizeLineEndings(edit.newText);

    // If exact match exists, use it
    if (modifiedContent.includes(normalizedOld)) {
      modifiedContent = modifiedContent.replace(normalizedOld, normalizedNew);
      continue;
    }

    // Otherwise, try line-by-line matching with flexibility for whitespace
    const oldLines = normalizedOld.split('\n');
    const contentLines = modifiedContent.split('\n');
    let matchFound = false;

    for (let i = 0; i <= contentLines.length - oldLines.length; i++) {
      const potentialMatch = contentLines.slice(i, i + oldLines.length);

      // Compare lines with normalized whitespace
      const isMatch = oldLines.every((oldLine, j) => {
        const contentLine = potentialMatch[j];
        return oldLine.trim() === contentLine.trim();
      });

      if (isMatch) {
        // Preserve original indentation of first line
        const originalIndent = contentLines[i].match(/^\s*/)?.[0] || '';
        const newLines = normalizedNew.split('\n').map((line, j) => {
          if (j === 0) return originalIndent + line.trimStart();
          // For subsequent lines, try to preserve relative indentation
          const oldIndent = oldLines[j]?.match(/^\s*/)?.[0] || '';
          const newIndent = line.match(/^\s*/)?.[0] || '';
          if (oldIndent && newIndent) {
            const relativeIndent = newIndent.length - oldIndent.length;
            return originalIndent + ' '.repeat(Math.max(0, relativeIndent)) + line.trimStart();
          }
          return line;
        });

        contentLines.splice(i, oldLines.length, ...newLines);
        modifiedContent = contentLines.join('\n');
        matchFound = true;
        break;
      }
    }

    if (!matchFound) {
      throw new Error(`Could not find exact match for edit:\n${edit.oldText}`);
    }
  }

  // Create unified diff
  const diff = createUnifiedDiff(content, modifiedContent, filePath);

  // Format diff with appropriate number of backticks
  let numBackticks = 3;
  while (diff.includes('`'.repeat(numBackticks))) {
    numBackticks++;
  }
  const formattedDiff = `${'`'.repeat(numBackticks)}diff\n${diff}${'`'.repeat(numBackticks)}\n\n`;

  if (!dryRun) {
    await fs.writeFile(filePath, modifiedContent, 'utf-8');
  }

  return formattedDiff;
}

async function buildDirectoryTree(currentPath: string, assetRootPath: string, maxDepth: number = 5, currentDepth: number = 0): Promise<TreeEntry[]> {
  if (currentDepth >= maxDepth) {
    return [{ name: "...", type: "directory" }];
  }
  
  const validPath = await validatePath(currentPath, assetRootPath);
  const entries = await fs.readdir(validPath, { withFileTypes: true });
  const result: TreeEntry[] = [];

  for (const entry of entries) {
    const entryData: TreeEntry = {
      name: entry.name,
      type: entry.isDirectory() ? 'directory' : 'file'
    };

    if (entry.isDirectory()) {
      const subPath = path.join(currentPath, entry.name);
      entryData.children = await buildDirectoryTree(subPath, assetRootPath, maxDepth, currentDepth + 1);
    }

    result.push(entryData);
  }

  return result;
}

// Function to recognize Unity asset types based on file extension
function getUnityAssetType(filePath: string): string {
  const ext = path.extname(filePath).toLowerCase();
  
  // Common Unity asset types
  const assetTypes: Record<string, string> = {
    '.unity': 'Scene',
    '.prefab': 'Prefab',
    '.mat': 'Material',
    '.fbx': 'Model',
    '.cs': 'Script',
    // Remove .js since Unity doesn't support JavaScript scripts
    '.anim': 'Animation',
    '.controller': 'Animator Controller',
    '.asset': 'ScriptableObject',
    '.png': 'Texture',
    '.jpg': 'Texture',
    '.jpeg': 'Texture',
    '.tga': 'Texture',
    '.wav': 'Audio',
    '.mp3': 'Audio',
    '.ogg': 'Audio',
    '.shader': 'Shader',
    '.compute': 'Compute Shader',
    '.ttf': 'Font',
    '.otf': 'Font',
    '.physicMaterial': 'Physics Material',
    '.mask': 'Avatar Mask',
    '.playable': 'Playable',
    '.mixer': 'Audio Mixer',
    '.renderTexture': 'Render Texture',
    '.lighting': 'Lighting Settings',
    '.shadervariants': 'Shader Variants',
    '.spriteatlas': 'Sprite Atlas',
    '.guiskin': 'GUI Skin',
    '.flare': 'Flare',
    '.brush': 'Brush',
    '.overrideController': 'Animator Override Controller',
    '.preset': 'Preset',
    '.terrainlayer': 'Terrain Layer',
    '.signal': 'Signal',
    '.signalasset': 'Signal Asset',
    '.giparams': 'Global Illumination Parameters',
    '.cubemap': 'Cubemap',
  };
  
  return assetTypes[ext] || 'Other';
}

// Handler function to process filesystem tools
export async function handleFilesystemTool(name: string, args: any, projectPath: string) {
  switch (name) {
    case "read_file": {
      const parsed = ReadFileArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const validPath = await validatePath(parsed.data.path, projectPath);
      const content = await fs.readFile(validPath, "utf-8");
      return {
        content: [{ type: "text", text: content }],
      };
    }

    case "read_multiple_files": {
      const parsed = ReadMultipleFilesArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const results = await Promise.all(
        parsed.data.paths.map(async (filePath: string) => {
          try {
            const validPath = await validatePath(filePath, projectPath);
            const content = await fs.readFile(validPath, "utf-8");
            return `${filePath}:\n${content}\n`;
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : String(error);
            return `${filePath}: Error - ${errorMessage}`;
          }
        }),
      );
      return {
        content: [{ type: "text", text: results.join("\n---\n") }],
      };
    }

    case "write_file": {
      const parsed = WriteFileArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const validPath = await validatePath(parsed.data.path, projectPath);
      
      // Ensure directory exists
      const dirPath = path.dirname(validPath);
      await fs.mkdir(dirPath, { recursive: true });
      
      await fs.writeFile(validPath, parsed.data.content, "utf-8");
      return {
        content: [{ type: "text", text: `Successfully wrote to ${parsed.data.path}` }],
      };
    }

    case "edit_file": {
      const parsed = EditFileArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const validPath = await validatePath(parsed.data.path, projectPath);
      const result = await applyFileEdits(validPath, parsed.data.edits, parsed.data.dryRun);
      return {
        content: [{ type: "text", text: result }],
      };
    }

    case "list_directory": {
      const parsed = ListDirectoryArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const validPath = await validatePath(parsed.data.path, projectPath);
      const entries = await fs.readdir(validPath, { withFileTypes: true });
      const formatted = entries
        .map((entry) => {
          if (entry.isDirectory()) {
            return `[DIR] ${entry.name}`;
          } else {
            // For files, detect Unity asset type
            const filePath = path.join(validPath, entry.name);
            const assetType = getUnityAssetType(filePath);
            return `[${assetType}] ${entry.name}`;
          }
        })
        .join("\n");
      return {
        content: [{ type: "text", text: formatted }],
      };
    }

    case "directory_tree": {
      const parsed = DirectoryTreeArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const treeData = await buildDirectoryTree(parsed.data.path, projectPath, parsed.data.maxDepth);
      return {
        content: [{ type: "text", text: JSON.stringify(treeData, null, 2) }],
      };
    }

    case "search_files": {
      const parsed = SearchFilesArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const validPath = await validatePath(parsed.data.path, projectPath);
      const results = await searchFiles(validPath, parsed.data.pattern, parsed.data.excludePatterns);
      return {
        content: [{ 
          type: "text", 
          text: results.length > 0 
            ? `Found ${results.length} results:\n${results.join("\n")}` 
            : "No matches found" 
        }],
      };
    }

    case "get_file_info": {
      const parsed = GetFileInfoArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      const validPath = await validatePath(parsed.data.path, projectPath);
      const info = await getFileStats(validPath);
      
      // Also get Unity-specific info if it's an asset file
      const additionalInfo: Record<string, string> = {};
      if (info.isFile) {
        additionalInfo.assetType = getUnityAssetType(validPath);
      }
      
      const formattedInfo = Object.entries({ ...info, ...additionalInfo })
        .map(([key, value]) => `${key}: ${value}`)
        .join("\n");
        
      return {
        content: [{ type: "text", text: formattedInfo }],
      };
    }

    case "find_assets_by_type": {
      const parsed = FindAssetsByTypeArgsSchema.safeParse(args);
      if (!parsed.success) {
        return {
          content: [{ type: "text", text: `Invalid arguments: ${parsed.error}` }],
          isError: true
        };
      }
      
      // Clean the inputs by removing quotes
      const assetType = parsed.data.assetType.replace(/['"]/g, '');
      const searchPath = parsed.data.searchPath.replace(/['"]/g, '');
      const maxDepth = parsed.data.maxDepth;
      
      console.error(`[Unity MCP] Finding assets of type "${assetType}" in path "${searchPath}" with maxDepth ${maxDepth}`);
      
      const validPath = await validatePath(searchPath, projectPath);
      
      const results: Array<{path: string, name: string, type: string}> = [];
      
      // Helper function to get the appropriate file extensions for the asset type
      function getFileExtensionsForType(type: string): string[] {
        type = type.toLowerCase();
        const extensionMap: Record<string, string[]> = {
          'scene': ['.unity'],
          'prefab': ['.prefab'],
          'material': ['.mat'],
          'script': ['.cs'], // Remove .js since Unity doesn't support JavaScript scripts
          'model': ['.fbx', '.obj', '.blend', '.max', '.mb', '.ma'],
          'texture': ['.png', '.jpg', '.jpeg', '.tga', '.tif', '.tiff', '.psd', '.exr', '.hdr'],
          'audio': ['.wav', '.mp3', '.ogg', '.aiff', '.aif'],
          'animation': ['.anim'],
          'animator': ['.controller'],
          'shader': ['.shader', '.compute', '.cginc']
        };
        
        return extensionMap[type] || [];
      }
      
      // Get the extensions to search for
      const extensions = getFileExtensionsForType(assetType);
      
      // Recursive function to search for assets with depth tracking
      async function searchAssets(dir: string, currentDepth: number = 1) {
        // Stop recursion if we've reached the maximum depth
        if (maxDepth !== -1 && currentDepth > maxDepth) {
          return;
        }
        
        try {
          const entries = await fs.readdir(dir, { withFileTypes: true });
          
          for (const entry of entries) {
            const fullPath = path.join(dir, entry.name);
            const relativePath = path.relative(projectPath, fullPath);
            
            if (entry.isDirectory()) {
              // Recursively search subdirectories
              await searchAssets(fullPath, currentDepth + 1);
            } else {
              // Check if the file matches the requested asset type
              const ext = path.extname(entry.name).toLowerCase();
              
              if (extensions.length === 0) {
                // If no extensions specified, match by Unity asset type
                const fileAssetType = getUnityAssetType(fullPath);
                if (fileAssetType.toLowerCase() === assetType.toLowerCase()) {
                  results.push({
                    path: relativePath,
                    name: entry.name,
                    type: fileAssetType
                  });
                }
              } else if (extensions.includes(ext)) {
                // Match by extension
                results.push({
                  path: relativePath,
                  name: entry.name,
                  type: assetType
                });
              }
            }
          }
        } catch (error) {
          console.error(`Error accessing directory ${dir}:`, error);
        }
      }
      
      await searchAssets(validPath);
      
      return {
        content: [{ 
          type: "text", 
          text: results.length > 0 
            ? `Found ${results.length} ${assetType} assets:\n${JSON.stringify(results, null, 2)}` 
            : `No "${assetType}" assets found in ${searchPath || "Assets"}` 
        }],
      };
    }

    default:
      return {
        content: [{ type: "text", text: `Unknown tool: ${name}` }],
        isError: true,
      };
  }
}

// Register filesystem tools with the MCP server
// This function is now only a stub that doesn't actually do anything
// since all tools are registered in toolDefinitions.ts
export function registerFilesystemTools(server: Server, wsHandler: WebSocketHandler) {
  // This function is now deprecated as tool registration has moved to toolDefinitions.ts
  console.log("Filesystem tools are now registered in toolDefinitions.ts");
}
