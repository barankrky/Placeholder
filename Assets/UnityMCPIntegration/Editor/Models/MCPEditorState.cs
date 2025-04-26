using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace Plugins.GamePilot.Editor.MCP
{
    [Serializable]
    public class MCPEditorState
    {
        [JsonProperty("activeGameObjects")]
        public string[] ActiveGameObjects { get; set; } = new string[0];
        
        [JsonProperty("selectedObjects")]
        public string[] SelectedObjects { get; set; } = new string[0];
        
        [JsonProperty("playModeState")]
        public string PlayModeState { get; set; } = "Stopped";
        
        [JsonProperty("sceneHierarchy")]
        public List<MCPGameObjectInfo> SceneHierarchy { get; set; } = new List<MCPGameObjectInfo>();
        
        // Removed ProjectStructure property
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Enhanced project information properties
        [JsonProperty("renderPipeline")]
        public string RenderPipeline { get; set; } = "Unknown";
        
        [JsonProperty("buildTarget")]
        public string BuildTarget { get; set; } = "Unknown";
        
        [JsonProperty("projectName")]
        public string ProjectName { get; set; } = "Unknown";
        
        [JsonProperty("graphicsDeviceType")]
        public string GraphicsDeviceType { get; set; } = "Unknown";
        
        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; } = "Unknown";
        
        [JsonProperty("currentSceneName")]
        public string CurrentSceneName { get; set; } = "Unknown";
        
        [JsonProperty("currentScenePath")]
        public string CurrentScenePath { get; set; } = "Unknown";
        
        [JsonProperty("availableMenuItems")]
        public List<string> AvailableMenuItems { get; set; } = new List<string>();
    }
    
    [Serializable]
    public class MCPGameObjectInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("components")]
        public string[] Components { get; set; } = new string[0];
        
        [JsonProperty("children")]
        public List<MCPGameObjectInfo> Children { get; set; } = new List<MCPGameObjectInfo>();
        
        [JsonProperty("active")]
        public bool Active { get; set; } = true;
        
        [JsonProperty("layer")]
        public int Layer { get; set; }
        
        [JsonProperty("tag")]
        public string Tag { get; set; }
    }
    
    // Removed MCPProjectStructure class
    
    [Serializable]
    public class LogEntry
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("stackTrace")]
        public string StackTrace { get; set; }
        
        [JsonProperty("type")]
        public UnityEngine.LogType Type { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
