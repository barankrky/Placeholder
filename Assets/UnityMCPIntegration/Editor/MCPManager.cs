using System;
using UnityEditor;
using UnityEngine;

namespace Plugins.GamePilot.Editor.MCP
{
    [InitializeOnLoad]
    public class MCPManager
    {
        private static readonly string ComponentName = "MCPManager";
        private static MCPConnectionManager connectionManager;
        private static MCPMessageHandler messageHandler;
        private static MCPDataCollector dataCollector;
        private static MCPMessageSender messageSender;
        
        private static bool isInitialized = false;
        public static bool IsInitialized => isInitialized;
        private static bool autoReconnect = false;
        
        // Constructor called on editor startup due to [InitializeOnLoad]
        static MCPManager()
        {
            // Initialize logger for this component
            MCPLogger.InitializeComponent(ComponentName);
            
            EditorApplication.delayCall += Initialize;
        }
        
        public static void Initialize()
        {
            if (isInitialized)
                return;
                
            MCPLogger.Log(ComponentName, "Initializing Model Context Protocol system...");
            
            try
            {
                // Create components
                dataCollector = new MCPDataCollector();
                connectionManager = new MCPConnectionManager();
                messageSender = new MCPMessageSender(connectionManager);
                messageHandler = new MCPMessageHandler(dataCollector, messageSender);
                
                // Hook up events
                connectionManager.OnMessageReceived += messageHandler.HandleMessage;
                connectionManager.OnConnected += OnConnected;
                connectionManager.OnDisconnected += OnDisconnected;
                connectionManager.OnError += OnError;
                
                // Start connection
                connectionManager.Connect();
                
                // Register update for connection checking only
                EditorApplication.update += Update;
                
                isInitialized = true;
                MCPLogger.Log(ComponentName, "Model Context Protocol system initialized successfully");
            }
            catch (Exception ex)
            {
                MCPLogger.LogException(ComponentName, ex);
                Debug.LogError($"[MCP] Failed to initialize MCP system: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void OnConnected()
        {
            try
            {
                MCPLogger.Log(ComponentName, "Connected to MCP server");
            }
            catch (Exception ex)
            {
                MCPLogger.LogException(ComponentName, ex);
            }
        }

        private static void OnDisconnected()
        {
            try
            {
                MCPLogger.Log(ComponentName, "Disconnected from MCP server");
            }
            catch (Exception ex)
            {
                MCPLogger.LogException(ComponentName, ex);
            }
        }

        private static void OnError(string errorMessage)
        {
            MCPLogger.LogError(ComponentName, $"Connection error: {errorMessage}");
        }
        
        private static void Update()
        {
            try
            {
                // Check if connection manager needs to reconnect
                if (autoReconnect)
                {
                    connectionManager?.CheckConnection();
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogException(ComponentName, ex);
            }
        }
        
        // Public methods for manual control
        public static void RetryConnection()
        {
            connectionManager?.Reconnect();
        }
        
        public static void EnableAutoReconnect(bool enable)
        {
            autoReconnect = enable;
            MCPLogger.Log(ComponentName, $"Auto reconnect set to: {enable}");
        }
        
        public static bool IsConnected => connectionManager?.IsConnected ?? false;
        
        public static void Shutdown()
        {
            if (!isInitialized) return;
            
            try
            {
                MCPLogger.Log(ComponentName, "Shutting down MCP system");
                
                // Unregister update callbacks
                EditorApplication.update -= Update;
                
                // Disconnect
                connectionManager?.Disconnect();
                
                // Cleanup
                dataCollector?.Dispose();
                
                isInitialized = false;
                MCPLogger.Log(ComponentName, "System shutdown completed");
            }
            catch (Exception ex)
            {
                MCPLogger.LogException(ComponentName, ex);
            }
        }
    }
}
