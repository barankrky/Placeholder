using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugins.GamePilot.Editor.MCP
{
    /// <summary>
    /// Centralized logging system for MCP components that respects component-level logging settings.
    /// </summary>
    public static class MCPLogger
    {
        // Dictionary to track enabled status for each component
        private static readonly Dictionary<string, bool> componentLoggingEnabled = new Dictionary<string, bool>();
        
        // Default log state (off by default)
        private static bool globalLoggingEnabled = false;
        
        /// <summary>
        /// Enable or disable logging globally for all components
        /// </summary>
        public static bool GlobalLoggingEnabled 
        { 
            get => globalLoggingEnabled;
            set => globalLoggingEnabled = value;
        }
        
        /// <summary>
        /// Initialize a component for logging
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="enabledByDefault">Whether logging should be enabled by default</param>
        public static void InitializeComponent(string componentName, bool enabledByDefault = false)
        {
            if (!componentLoggingEnabled.ContainsKey(componentName))
            {
                componentLoggingEnabled[componentName] = enabledByDefault;
            }
        }
        
        /// <summary>
        /// Set logging state for a specific component
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="enabled">Whether logging should be enabled</param>
        public static void SetComponentLoggingEnabled(string componentName, bool enabled)
        {
            // Make sure component exists before setting state
            if (!componentLoggingEnabled.ContainsKey(componentName))
            {
                InitializeComponent(componentName, false);
            }
            
            componentLoggingEnabled[componentName] = enabled;
        }
        
        /// <summary>
        /// Check if logging is enabled for a component
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <returns>True if logging is enabled</returns>
        public static bool IsLoggingEnabled(string componentName)
        {
            // If global logging is disabled, nothing gets logged
            if (!globalLoggingEnabled) return false;
            
            // If component isn't registered, assume it's disabled
            if (!componentLoggingEnabled.ContainsKey(componentName))
            {
                InitializeComponent(componentName, false);
                return false;
            }
            
            // Return component-specific setting
            return componentLoggingEnabled[componentName];
        }
        
        /// <summary>
        /// Log a message if logging is enabled for the component
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="message">Message to log</param>
        public static void Log(string componentName, string message)
        {
            if (IsLoggingEnabled(componentName))
            {
                Debug.Log($"[MCP] [{componentName}] {message}");
            }
        }
        
        /// <summary>
        /// Log a warning if logging is enabled for the component
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="message">Message to log</param>
        public static void LogWarning(string componentName, string message)
        {
            if (IsLoggingEnabled(componentName))
            {
                Debug.LogWarning($"[MCP] [{componentName}] {message}");
            }
        }
        
        /// <summary>
        /// Log an error if logging is enabled for the component
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="message">Message to log</param>
        public static void LogError(string componentName, string message)
        {
            if (IsLoggingEnabled(componentName))
            {
                Debug.LogError($"[MCP] [{componentName}] {message}");
            }
        }
        
        /// <summary>
        /// Log an exception if logging is enabled for the component
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="ex">Exception to log</param>
        public static void LogException(string componentName, Exception ex)
        {
            if (IsLoggingEnabled(componentName))
            {
                Debug.LogError($"[MCP] [{componentName}] Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Get all registered components
        /// </summary>
        public static IEnumerable<string> GetRegisteredComponents()
        {
            return componentLoggingEnabled.Keys;
        }
        
        /// <summary>
        /// Get logging state for a component
        /// </summary>
        public static bool GetComponentLoggingEnabled(string componentName)
        {
            if (!componentLoggingEnabled.ContainsKey(componentName))
                return false;
                
            return componentLoggingEnabled[componentName];
        }
    }
}
