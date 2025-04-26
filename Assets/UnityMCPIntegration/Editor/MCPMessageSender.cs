using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using System.Linq;

namespace Plugins.GamePilot.Editor.MCP
{
    public class MCPMessageSender
    {
        private readonly MCPConnectionManager connectionManager;
        
        public MCPMessageSender(MCPConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }
        
        public async Task SendEditorStateAsync(MCPEditorState state)
        {
            if (state == null) return;
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "editorState",
                    data = state
                }, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending editor state: {ex.Message}");
            }
        }
        
        public async Task SendLogEntryAsync(LogEntry logEntry)
        {
            if (logEntry == null) return;
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "log",
                    data = new
                    {
                        message = logEntry.Message,
                        stackTrace = logEntry.StackTrace,
                        logType = logEntry.Type.ToString(),
                        timestamp = logEntry.Timestamp
                    }
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending log entry: {ex.Message}");
            }
        }
        
        public async Task SendCommandResultAsync(string commandId, object result, 
            IEnumerable<string> logs, IEnumerable<string> errors, IEnumerable<string> warnings)
        {
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "commandResult",
                    data = new
                    {
                        commandId,
                        result = result,
                        logs = logs ?? Array.Empty<string>(),
                        errors = errors ?? Array.Empty<string>(),
                        warnings = warnings ?? Array.Empty<string>(),
                        executionSuccess = errors == null || !errors.GetEnumerator().MoveNext()
                    }
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending command result: {ex.Message}");
            }
        }
        
        public async Task SendErrorMessageAsync(string errorCode, string errorMessage)
        {
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "error",
                    data = new
                    {
                        code = errorCode,
                        message = errorMessage,
                        timestamp = DateTime.UtcNow
                    }
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending error message: {ex.Message}");
            }
        }

        public async Task SendGetLogsResponseAsync(string requestId, LogEntry[] logs)
        {
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "logsResponse",
                    data = new
                    {
                        requestId,
                        logs,
                        timestamp = DateTime.UtcNow
                    }
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending logs response: {ex.Message}");
            }
        }
        
        public async Task SendSceneInfoAsync(string requestId, MCPSceneInfo sceneInfo)
        {
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "sceneInfo",
                    data = new
                    {
                        requestId,
                        sceneInfo,
                        timestamp = DateTime.UtcNow
                    }
                }, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending scene info: {ex.Message}");
            }
        }
        
        public async Task SendGameObjectsDetailsAsync(string requestId, List<MCPGameObjectDetail> gameObjectDetails)
        {
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "gameObjectsDetails",
                    data = new
                    {
                        requestId,
                        gameObjectDetails,
                        count = gameObjectDetails?.Count ?? 0,
                        timestamp = DateTime.UtcNow
                    }
                }, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending game objects details: {ex.Message}");
            }
        }
        
        // Add new method to send pong message back to the server
        public async Task SendPongAsync()
        {
            if (!connectionManager.IsConnected) return;
            
            try
            {
                var message = JsonConvert.SerializeObject(new
                {
                    type = "pong",
                    data = new
                    {
                        timestamp = DateTime.UtcNow.Ticks
                    }
                });
                
                await connectionManager.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending pong message: {ex.Message}");
            }
        }
        
        // Add new method to handle single GameObject details request
        public async Task SendGameObjectDetailsAsync(string requestId, GameObject gameObject)
        {
            if (!connectionManager.IsConnected || gameObject == null) return;
            
            try
            {
                // Create a list with a single game object detail
                var gameObjectDetail = new MCPGameObjectDetail
                {
                    Name = gameObject.name,
                    InstanceID = gameObject.GetInstanceID(),
                    Path = GetGameObjectPath(gameObject),
                    Active = gameObject.activeSelf,
                    ActiveInHierarchy = gameObject.activeInHierarchy,
                    Tag = gameObject.tag,
                    Layer = gameObject.layer,
                    LayerName = LayerMask.LayerToName(gameObject.layer),
                    IsStatic = gameObject.isStatic,
                    Transform = new MCPTransformInfo
                    {
                        Position = gameObject.transform.position,
                        Rotation = gameObject.transform.rotation.eulerAngles,
                        LocalPosition = gameObject.transform.localPosition,
                        LocalRotation = gameObject.transform.localRotation.eulerAngles,
                        LocalScale = gameObject.transform.localScale
                    },
                    Components = gameObject.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => new MCPComponentInfo
                        {
                            Type = c.GetType().Name,
                            IsEnabled = GetComponentEnabled(c),
                            InstanceID = c.GetInstanceID()
                        })
                        .ToList()
                };
                
                var details = new List<MCPGameObjectDetail> { gameObjectDetail };
                
                // Use the existing method to send the details
                await SendGameObjectsDetailsAsync(requestId, details);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error sending game object details: {ex.Message}");
                await SendErrorMessageAsync("GAME_OBJECT_DETAIL_ERROR", ex.Message);
            }
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return string.Empty;
            
            string path = obj.name;
            var parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private bool GetComponentEnabled(Component component)
        {
            // Try to check if component is enabled (for components that support it)
            try
            {
                if (component is Behaviour behaviour)
                    return behaviour.enabled;
                
                if (component is Renderer renderer)
                    return renderer.enabled;
                
                if (component is Collider collider)
                    return collider.enabled;
            }
            catch
            {
                // Ignore any exceptions
            }
            
            // Default to true for components that don't have an enabled property
            return true;
        }
    }
}
