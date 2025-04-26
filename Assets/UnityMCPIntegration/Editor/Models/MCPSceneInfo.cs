using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace Plugins.GamePilot.Editor.MCP
{
    [Serializable]
    public class MCPSceneInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("isDirty")]
        public bool IsDirty { get; set; }
        
        [JsonProperty("rootCount")]
        public int RootCount { get; set; }
        
        [JsonProperty("rootObjects")]
        public List<MCPGameObjectReference> RootObjects { get; set; } = new List<MCPGameObjectReference>();
        
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
    
    [Serializable]
    public class MCPGameObjectReference
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("instanceID")]
        public int InstanceID { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("active")]
        public bool Active { get; set; }
        
        [JsonProperty("childCount")]
        public int ChildCount { get; set; }
        
        [JsonProperty("children")]
        public List<MCPGameObjectReference> Children { get; set; }
    }
    
    [Serializable]
    public class MCPGameObjectDetail
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("instanceID")]
        public int InstanceID { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("active")]
        public bool Active { get; set; }
        
        [JsonProperty("activeInHierarchy")]
        public bool ActiveInHierarchy { get; set; }
        
        [JsonProperty("tag")]
        public string Tag { get; set; }
        
        [JsonProperty("layer")]
        public int Layer { get; set; }
        
        [JsonProperty("layerName")]
        public string LayerName { get; set; }
        
        [JsonProperty("isStatic")]
        public bool IsStatic { get; set; }
        
        [JsonProperty("transform")]
        public MCPTransformInfo Transform { get; set; }
        
        [JsonProperty("components")]
        public List<MCPComponentInfo> Components { get; set; }
        
        [JsonProperty("children")]
        public List<MCPGameObjectDetail> Children { get; set; }
    }
    
    [Serializable]
    public class MCPTransformInfo
    {
        [JsonProperty("position")]
        public Vector3 Position { get; set; }
        
        [JsonProperty("rotation")]
        public Vector3 Rotation { get; set; }
        
        [JsonProperty("localPosition")]
        public Vector3 LocalPosition { get; set; }
        
        [JsonProperty("localRotation")]
        public Vector3 LocalRotation { get; set; }
        
        [JsonProperty("localScale")]
        public Vector3 LocalScale { get; set; }
    }
    
    [Serializable]
    public class MCPComponentInfo
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }
        
        [JsonProperty("instanceID")]
        public int InstanceID { get; set; }
    }
}
