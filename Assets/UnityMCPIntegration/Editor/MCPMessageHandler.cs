using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using System.Linq;

namespace Plugins.GamePilot.Editor.MCP
{
    public class MCPMessageHandler
    {
        private readonly MCPDataCollector dataCollector;
        private readonly MCPCodeExecutor codeExecutor;
        private readonly MCPMessageSender messageSender;
        
        public MCPMessageHandler(MCPDataCollector dataCollector, MCPMessageSender messageSender)
        {
            this.dataCollector = dataCollector ?? throw new ArgumentNullException(nameof(dataCollector));
            this.messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
            this.codeExecutor = new MCPCodeExecutor();
        }
        
        public async void HandleMessage(string messageJson)
        {
            if (string.IsNullOrEmpty(messageJson)) return;
            
            try
            {
                Debug.Log($"[MCP] Received message: {messageJson}");
                var message = JsonConvert.DeserializeObject<MCPMessage>(messageJson);
                if (message == null) return;
                
                switch (message.Type)
                {
                    case "selectGameObject":
                        await HandleSelectGameObjectAsync(message.Data);
                        break;
                    
                    case "togglePlayMode":
                        await HandleTogglePlayModeAsync();
                        break;
                    
                    case "executeEditorCommand":
                        await HandleExecuteCommandAsync(message.Data);
                        break;
                    
                    case "requestEditorState": // Consolidated to a single message type
                        await HandleRequestEditorStateAsync(message.Data);
                        break;
                        
                    case "getLogs":
                        await HandleGetLogsAsync(message.Data);
                        break;
                        
                    case "handshake":
                        await HandleHandshakeAsync(message.Data);
                        break;
                        
                    case "getSceneInfo":
                        await HandleGetSceneInfoAsync(message.Data);
                        break;
                        
                    case "getGameObjectsInfo":
                        await HandleGetGameObjectsInfoAsync(message.Data);
                        break;
                        
                    case "ping": // Renamed from 'heartbeat' to 'ping' to match protocol
                        await HandlePingAsync(message.Data);
                        break;
                        
                    default:
                        Debug.LogWarning($"[MCP] Unknown message type: {message.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error handling message: {ex.Message}\nMessage: {messageJson}");
            }
        }
        
        private async Task HandleHandshakeAsync(JToken data)
        {
            try
            {
                string message = data["message"]?.ToString() ?? "Server connected";
                Debug.Log($"[MCP] Handshake received: {message}");
                
                // Send a simple acknowledgment, but don't send full editor state until requested
                await messageSender.SendPongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error handling handshake: {ex.Message}");
            }
        }
        
        // Add a ping handler to respond to server heartbeats
        private async Task HandlePingAsync(JToken data)
        {
            try
            {
                // Simply respond with a pong message
                await messageSender.SendPongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error handling ping: {ex.Message}");
            }
        }
        
        private async Task HandleSelectGameObjectAsync(JToken data)
        {
            try
            {
                string objectPath = data["path"]?.ToString();
                string requestId = data["requestId"]?.ToString();
                
                if (string.IsNullOrEmpty(objectPath)) return;
                
                var obj = GameObject.Find(objectPath);
                if (obj != null)
                {
                    Selection.activeGameObject = obj;
                    Debug.Log($"[MCP] Selected GameObject: {objectPath}");
                    
                    // If requestId was provided, send back object details
                    if (!string.IsNullOrEmpty(requestId))
                    {
                        // Use the new method to send details for a single GameObject
                        await messageSender.SendGameObjectDetailsAsync(requestId, obj);
                    }
                }
                else
                {
                    Debug.LogWarning($"[MCP] GameObject not found: {objectPath}");
                    
                    if (!string.IsNullOrEmpty(requestId))
                    {
                        await messageSender.SendErrorMessageAsync("OBJECT_NOT_FOUND", $"GameObject not found: {objectPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error selecting GameObject: {ex.Message}");
            }
        }
        
        private async Task HandleTogglePlayModeAsync()
        {
            try
            {
                EditorApplication.isPlaying = !EditorApplication.isPlaying;
                Debug.Log($"[MCP] Toggled play mode to: {EditorApplication.isPlaying}");
                
                // Send updated editor state after toggling play mode
                var editorState = dataCollector.GetEditorState();
                await messageSender.SendEditorStateAsync(editorState);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error toggling play mode: {ex.Message}");
            }
        }
        
        private async Task HandleExecuteCommandAsync(JToken data)
        {
            try
            {
                // Support both old and new parameter naming
                string commandId = data["commandId"]?.ToString() ?? data["id"]?.ToString() ?? Guid.NewGuid().ToString();
                string code = data["code"]?.ToString();
                
                if (string.IsNullOrEmpty(code))
                {
                    Debug.LogWarning("[MCP] Received empty code to execute");
                    await messageSender.SendErrorMessageAsync("EMPTY_CODE", "Received empty code to execute");
                    return;
                }
                
                Debug.Log($"[MCP] Executing command: {commandId}\n{code}");
                
                var result = codeExecutor.ExecuteCode(code);
                
                // Send back the results
                await messageSender.SendCommandResultAsync(
                    commandId,
                    result,
                    codeExecutor.GetLogs(),
                    codeExecutor.GetErrors(),
                    codeExecutor.GetWarnings()
                );
                
                Debug.Log($"[MCP] Command execution completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error executing command: {ex.Message}");
            }
        }
        
        // Renamed from HandleGetEditorStateAsync to HandleRequestEditorStateAsync for clarity
        private async Task HandleRequestEditorStateAsync(JToken data)
        {
            try
            {
                // Get current editor state with enhanced project info
                var editorState = GetEnhancedEditorState();
                
                // Send it to the server
                await messageSender.SendEditorStateAsync(editorState);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting editor state: {ex.Message}");
            }
        }
        
        // New method to get enhanced editor state with more project information
        private MCPEditorState GetEnhancedEditorState()
        {
            // Get base editor state from data collector
            var state = dataCollector.GetEditorState();
            
            // Add additional project information
            EnhanceEditorStateWithProjectInfo(state);
            
            return state;
        }
        
        // Add additional project information to the editor state
        private void EnhanceEditorStateWithProjectInfo(MCPEditorState state)
        {
            try
            {
                // Add information about current rendering pipeline
                state.RenderPipeline = GetCurrentRenderPipeline();
                
                // Add current build target platform
                state.BuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                
                // Add project name
                state.ProjectName = Application.productName;
                
                // Add graphics API info
                state.GraphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
                
                // Add Unity version
                state.UnityVersion = Application.unityVersion;
                
                // Add current scene name
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                state.CurrentSceneName = currentScene.name;
                state.CurrentScenePath = currentScene.path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error enhancing editor state: {ex.Message}");
            }
        }
        
        // Helper to determine current render pipeline
        private string GetCurrentRenderPipeline()
        {
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null)
                return "Built-in Render Pipeline";
                
            var pipelineType = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType().Name;
            
            // Try to make the name more user-friendly
            if (pipelineType.Contains("Universal"))
                return "Universal Render Pipeline (URP)";
            else if (pipelineType.Contains("HD"))
                return "High Definition Render Pipeline (HDRP)";
            else if (pipelineType.Contains("Lightweight"))
                return "Lightweight Render Pipeline (LWRP)";
            else
                return pipelineType;
        }
        
        private async Task HandleGetLogsAsync(JToken data)
        {
            try
            {
                string requestId = data["requestId"]?.ToString() ?? Guid.NewGuid().ToString();
                int count = data["count"]?.Value<int>() ?? 50;
                
                // Get logs from collector
                var logs = dataCollector.GetRecentLogs(count);
                
                // Send logs back to server
                await messageSender.SendGetLogsResponseAsync(requestId, logs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting logs: {ex.Message}");
            }
        }
        
        private async Task HandleGetSceneInfoAsync(JToken data)
        {
            try
            {
                string requestId = data["requestId"]?.ToString() ?? Guid.NewGuid().ToString();
                string detailLevelStr = data["detailLevel"]?.ToString() ?? "RootObjectsOnly";
                
                // Parse the detail level
                SceneInfoDetail detailLevel;
                if (!Enum.TryParse(detailLevelStr, true, out detailLevel))
                {
                    detailLevel = SceneInfoDetail.RootObjectsOnly;
                }
                
                // Get scene info
                var sceneInfo = dataCollector.GetCurrentSceneInfo(detailLevel);
                
                // Send it to the server
                await messageSender.SendSceneInfoAsync(requestId, sceneInfo);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error handling getSceneInfo: {ex.Message}");
                await messageSender.SendErrorMessageAsync("SCENE_INFO_ERROR", ex.Message);
            }
        }
        
        private async Task HandleGetGameObjectsInfoAsync(JToken data)
        {
            try
            {
                string requestId = data["requestId"]?.ToString() ?? Guid.NewGuid().ToString();
                string detailLevelStr = data["detailLevel"]?.ToString() ?? "BasicInfo";
                
                // Get the list of instance IDs
                int[] instanceIDs;
                if (data["instanceIDs"] != null && data["instanceIDs"].Type == JTokenType.Array)
                {
                    instanceIDs = data["instanceIDs"].ToObject<int[]>();
                }
                else
                {
                    await messageSender.SendErrorMessageAsync("INVALID_PARAMS", "instanceIDs array is required");
                    return;
                }
                
                // Parse the detail level
                GameObjectInfoDetail detailLevel;
                if (!Enum.TryParse(detailLevelStr, true, out detailLevel))
                {
                    detailLevel = GameObjectInfoDetail.BasicInfo;
                }
                
                // Get game object details
                var gameObjectDetails = dataCollector.GetGameObjectsInfo(instanceIDs, detailLevel);
                
                // Send to server
                await messageSender.SendGameObjectsDetailsAsync(requestId, gameObjectDetails);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error handling getGameObjectsInfo: {ex.Message}");
                await messageSender.SendErrorMessageAsync("GAME_OBJECT_INFO_ERROR", ex.Message);
            }
        }
    }
    
    internal class MCPMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("data")]
        public JToken Data { get; set; }
    }
}
