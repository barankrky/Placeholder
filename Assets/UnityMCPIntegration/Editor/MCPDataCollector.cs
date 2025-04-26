using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plugins.GamePilot.Editor.MCP
{
    public enum SceneInfoDetail
    {
        RootObjectsOnly,
        FullHierarchy
    }

    public enum GameObjectInfoDetail
    {
        BasicInfo,
        IncludeComponents,
        IncludeChildren,
        IncludeComponentsAndChildren  // New option to include both components and children
    }

    public class MCPDataCollector : IDisposable
    {
        private readonly Queue<LogEntry> logBuffer = new Queue<LogEntry>();
        private readonly int maxLogBufferSize = 1000;
        private bool isLoggingEnabled = true;
        
        public MCPDataCollector()
        {
            // Start capturing logs
            Application.logMessageReceived += HandleLogMessage;
        }
        
        public void Dispose()
        {
            // Unsubscribe to prevent memory leaks
            Application.logMessageReceived -= HandleLogMessage;
        }
        
        private void HandleLogMessage(string message, string stackTrace, LogType type)
        {
            if (!isLoggingEnabled) return;
            
            var logEntry = new LogEntry
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.UtcNow
            };
            
            lock (logBuffer)
            {
                logBuffer.Enqueue(logEntry);
                while (logBuffer.Count > maxLogBufferSize)
                {
                    logBuffer.Dequeue();
                }
            }
        }
        
        public bool IsLoggingEnabled
        {
            get => isLoggingEnabled;
            set
            {
                if (isLoggingEnabled == value) return;
                
                isLoggingEnabled = value;
                if (value)
                {
                    Application.logMessageReceived += HandleLogMessage;
                }
                else
                {
                    Application.logMessageReceived -= HandleLogMessage;
                }
            }
        }
        
        public LogEntry[] GetRecentLogs(int count = 50)
        {
            lock (logBuffer)
            {
                return logBuffer.Reverse().Take(count).Reverse().ToArray();
            }
        }
        
        public MCPEditorState GetEditorState()
        {
            var state = new MCPEditorState
            {
                ActiveGameObjects = GetActiveGameObjects(),
                SelectedObjects = GetSelectedObjects(),
                PlayModeState = EditorApplication.isPlaying ? "Playing" : "Stopped",
                SceneHierarchy = GetSceneHierarchy(),
                Timestamp = DateTime.UtcNow
            };
            
            return state;
        }
        
        private string[] GetActiveGameObjects()
        {
            try
            {
                var foundObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                return foundObjects.Where(o => o != null).Select(obj => obj.name).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting active GameObjects: {ex.Message}");
                return new string[0];
            }
        }
        
        private string[] GetSelectedObjects()
        {
            try
            {
                return Selection.gameObjects.Where(o => o != null).Select(obj => obj.name).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting selected objects: {ex.Message}");
                return new string[0];
            }
        }
        
        private List<MCPGameObjectInfo> GetSceneHierarchy()
        {
            var hierarchy = new List<MCPGameObjectInfo>();
            
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.IsValid())
                {
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var root in rootObjects.Where(o => o != null))
                    {
                        hierarchy.Add(GetGameObjectHierarchy(root));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting scene hierarchy: {ex.Message}");
            }
            
            return hierarchy;
        }
        
        private MCPGameObjectInfo GetGameObjectHierarchy(GameObject obj)
        {
            if (obj == null) return null;
            
            try
            {
                var info = new MCPGameObjectInfo
                {
                    Name = obj.name,
                    Path = GetGameObjectPath(obj),
                    Components = obj.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name)
                        .ToArray(),
                    Children = new List<MCPGameObjectInfo>(),
                    Active = obj.activeSelf,
                    Layer = obj.layer,
                    Tag = obj.tag
                };
                
                var transform = obj.transform;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTransform = transform.GetChild(i);
                    if (childTransform != null && childTransform.gameObject != null)
                    {
                        var childInfo = GetGameObjectHierarchy(childTransform.gameObject);
                        if (childInfo != null)
                        {
                            info.Children.Add(childInfo);
                        }
                    }
                }
                
                return info;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP] Error processing GameObject {obj.name}: {ex.Message}");
                return new MCPGameObjectInfo { Name = obj.name, Path = GetGameObjectPath(obj) };
            }
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return string.Empty;
            
            try
            {
                string path = obj.name;
                Transform parent = obj.transform.parent;
                
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                
                return path;
            }
            catch (Exception)
            {
                return obj.name;
            }
        }

        // New method to get current scene information based on requested detail level
        public MCPSceneInfo GetCurrentSceneInfo(SceneInfoDetail detailLevel)
        {
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                
                var sceneInfo = new MCPSceneInfo
                {
                    Name = scene.name,
                    Path = scene.path,
                    IsDirty = scene.isDirty,
                    RootCount = scene.rootCount
                };
                
                if (scene.IsValid())
                {
                    var rootObjects = scene.GetRootGameObjects();
                    
                    // Map scene objects based on detail level requested
                    switch (detailLevel)
                    {
                        case SceneInfoDetail.RootObjectsOnly:
                            sceneInfo.RootObjects = rootObjects
                                .Where(o => o != null)
                                .Select(o => new MCPGameObjectReference
                                {
                                    Name = o.name,
                                    InstanceID = o.GetInstanceID(),
                                    Path = GetGameObjectPath(o),
                                    Active = o.activeSelf,
                                    ChildCount = o.transform.childCount
                                })
                                .ToList();
                            break;
                            
                        case SceneInfoDetail.FullHierarchy:
                            sceneInfo.RootObjects = rootObjects
                                .Where(o => o != null)
                                .Select(o => GetGameObjectReferenceWithChildren(o))
                                .ToList();
                            break;
                    }
                }
                
                return sceneInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting scene info: {ex.Message}");
                return new MCPSceneInfo { Name = "Error", ErrorMessage = ex.Message };
            }
        }
        
        // Helper method to create a game object reference with children
        private MCPGameObjectReference GetGameObjectReferenceWithChildren(GameObject obj)
        {
            if (obj == null) return null;
            
            var reference = new MCPGameObjectReference
            {
                Name = obj.name,
                InstanceID = obj.GetInstanceID(),
                Path = GetGameObjectPath(obj),
                Active = obj.activeSelf,
                ChildCount = obj.transform.childCount,
                Children = new List<MCPGameObjectReference>()
            };
            
            // Add all children
            var transform = obj.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                var childTransform = transform.GetChild(i);
                if (childTransform != null && childTransform.gameObject != null)
                {
                    var childRef = GetGameObjectReferenceWithChildren(childTransform.gameObject);
                    if (childRef != null)
                    {
                        reference.Children.Add(childRef);
                    }
                }
            }
            
            return reference;
        }
        
        // Get detailed information about specific game objects
        public List<MCPGameObjectDetail> GetGameObjectsInfo(int[] objectInstanceIDs, GameObjectInfoDetail detailLevel)
        {
            var results = new List<MCPGameObjectDetail>();
            
            try
            {
                foreach (var id in objectInstanceIDs)
                {
                    var obj = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (obj != null)
                    {
                        results.Add(GetGameObjectDetail(obj, detailLevel));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Error getting game object details: {ex.Message}");
            }
            
            return results;
        }
        
        // Helper method to get detailed info about a game object
        private MCPGameObjectDetail GetGameObjectDetail(GameObject obj, GameObjectInfoDetail detailLevel)
        {
            var detail = new MCPGameObjectDetail
            {
                Name = obj.name,
                InstanceID = obj.GetInstanceID(),
                Path = GetGameObjectPath(obj),
                Active = obj.activeSelf,
                ActiveInHierarchy = obj.activeInHierarchy,
                Tag = obj.tag,
                Layer = obj.layer,
                LayerName = LayerMask.LayerToName(obj.layer),
                IsStatic = obj.isStatic,
                Transform = new MCPTransformInfo
                {
                    Position = obj.transform.position,
                    Rotation = obj.transform.rotation.eulerAngles,
                    LocalPosition = obj.transform.localPosition,
                    LocalRotation = obj.transform.localRotation.eulerAngles,
                    LocalScale = obj.transform.localScale
                }
            };
            
            // Include components if requested
            if (detailLevel == GameObjectInfoDetail.IncludeComponents || 
                detailLevel == GameObjectInfoDetail.IncludeComponentsAndChildren)
            {
                detail.Components = obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => new MCPComponentInfo
                    {
                        Type = c.GetType().Name,
                        IsEnabled = GetComponentEnabled(c),
                        InstanceID = c.GetInstanceID()
                    })
                    .ToList();
            }
            
            // Include children if requested
            if (detailLevel == GameObjectInfoDetail.IncludeChildren || 
                detailLevel == GameObjectInfoDetail.IncludeComponentsAndChildren)
            {
                detail.Children = new List<MCPGameObjectDetail>();
                var transform = obj.transform;
                
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTransform = transform.GetChild(i);
                    if (childTransform != null && childTransform.gameObject != null)
                    {
                        // When including both components and children, make sure to include components for children too
                        GameObjectInfoDetail childDetailLevel = detailLevel == GameObjectInfoDetail.IncludeComponentsAndChildren 
                            ? GameObjectInfoDetail.IncludeComponentsAndChildren 
                            : GameObjectInfoDetail.BasicInfo;
                            
                        var childDetail = GetGameObjectDetail(
                            childTransform.gameObject, 
                            childDetailLevel);
                            
                        detail.Children.Add(childDetail);
                    }
                }
            }
            
            return detail;
        }
        
        // Helper for getting component enabled state
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
