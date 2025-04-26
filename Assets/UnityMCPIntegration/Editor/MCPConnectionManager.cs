using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Plugins.GamePilot.Editor.MCP
{
    public class MCPConnectionManager
    {
        private static readonly string ComponentName = "MCPConnectionManager";
        private ClientWebSocket webSocket;
        private Uri serverUri = new Uri("ws://localhost:5010"); // Changed to allow changing
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private bool isConnected = false;
        private float reconnectTimer = 0f;
        private readonly float reconnectInterval = 5f;
        private string lastErrorMessage = string.Empty;
        
        // Statistics
        private int messagesSent = 0;
        private int messagesReceived = 0;
        private int reconnectAttempts = 0;
        
        // Events
        public event Action<string> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        // Properly track the connection state using the WebSocket state and our own flag
        public bool IsConnected => isConnected && webSocket?.State == WebSocketState.Open;
        public string LastErrorMessage => lastErrorMessage;
        public int MessagesSent => messagesSent;
        public int MessagesReceived => messagesReceived;
        public int ReconnectAttempts => reconnectAttempts;
        public Uri ServerUri 
        { 
            get => serverUri;
            set => serverUri = value;
        }
        
        public MCPConnectionManager()
        {
            MCPLogger.InitializeComponent(ComponentName);
            webSocket = new ClientWebSocket();
            webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        }
        
        public async void Connect()
        {
            // Double check connections that look open but may be stale
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    // Try to send a ping to verify connection is truly active
                    bool connectionIsActive = await TestConnection();
                    if (connectionIsActive)
                    {
                        MCPLogger.Log(ComponentName, "WebSocket already connected and active");
                        return;
                    }
                    else
                    {
                        MCPLogger.Log(ComponentName, "WebSocket appears open but is stale, reconnecting...");
                        // Fall through to reconnection logic
                    }
                }
                catch (Exception)
                {
                    MCPLogger.Log(ComponentName, "WebSocket appears open but failed ping test, reconnecting...");
                    // Fall through to reconnection logic
                }
            }
            else if (webSocket != null && webSocket.State == WebSocketState.Connecting)
            {
                MCPLogger.Log(ComponentName, "WebSocket is already connecting");
                return;
            }
            
            // Clean up any existing socket
            if (webSocket != null)
            {
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                            "Reconnecting", CancellationToken.None);
                    }
                    webSocket.Dispose();
                }
                catch (Exception ex)
                {
                    MCPLogger.LogWarning(ComponentName, $"Error cleaning up WebSocket: {ex.Message}");
                }
            }
            
            webSocket = new ClientWebSocket();
            webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            
            try
            {
                MCPLogger.Log(ComponentName, $"Connecting to MCP Server at {serverUri}");
                
                var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeout.Token);
                
                await webSocket.ConnectAsync(serverUri, linkedCts.Token);
                isConnected = true;
                
                MCPLogger.Log(ComponentName, "Successfully connected to MCP Server");
                OnConnected?.Invoke();
                StartReceiving();
            }
            catch (OperationCanceledException)
            {
                lastErrorMessage = "Connection attempt timed out";
                MCPLogger.LogError(ComponentName, lastErrorMessage);
                OnError?.Invoke(lastErrorMessage);
                isConnected = false;
                OnDisconnected?.Invoke();
            }
            catch (WebSocketException we)
            {
                lastErrorMessage = $"WebSocket error: {we.Message}";
                MCPLogger.LogError(ComponentName, lastErrorMessage);
                OnError?.Invoke(lastErrorMessage);
                isConnected = false;
                OnDisconnected?.Invoke();
            }
            catch (Exception e)
            {
                lastErrorMessage = $"Failed to connect: {e.Message}";
                MCPLogger.LogError(ComponentName, lastErrorMessage);
                OnError?.Invoke(lastErrorMessage);
                isConnected = false;
                OnDisconnected?.Invoke();
            }
        }
        
        // Test if connection is still valid with a simple ping
        private async Task<bool> TestConnection()
        {
            try
            {
                // Simple ping test - send a 1-byte message
                byte[] pingData = new byte[1] { 0 };
                await webSocket.SendAsync(
                    new ArraySegment<byte>(pingData),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public void Reconnect()
        {
            reconnectAttempts++;
            MCPLogger.Log(ComponentName, "Manually reconnecting...");
            reconnectTimer = 0;
            Connect();
        }
        
        public void CheckConnection()
        {
            if (!isConnected || webSocket?.State != WebSocketState.Open)
            {
                reconnectTimer += UnityEngine.Time.deltaTime;
                
                if (reconnectTimer >= reconnectInterval)
                {
                    reconnectAttempts++;
                    MCPLogger.Log(ComponentName, "Attempting reconnection...");
                    reconnectTimer = 0f;
                    Connect();
                }
            }
        }
        
        private async void StartReceiving()
        {
            var buffer = new byte[8192]; // 8KB buffer
            
            try
            {
                while (webSocket.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messagesReceived++;
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        MCPLogger.Log(ComponentName, $"Received message: {message.Substring(0, Math.Min(100, message.Length))}...");
                        OnMessageReceived?.Invoke(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        MCPLogger.Log(ComponentName, "Server requested connection close");
                        isConnected = false;
                        OnDisconnected?.Invoke();
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, don't log as error
                MCPLogger.Log(ComponentName, "WebSocket receiving was canceled");
            }
            catch (WebSocketException wsEx)
            {
                if (!cts.IsCancellationRequested)
                {
                    lastErrorMessage = $"WebSocket error: {wsEx.Message}";
                    MCPLogger.LogError(ComponentName, lastErrorMessage);
                    OnError?.Invoke(lastErrorMessage);
                }
            }
            catch (Exception e)
            {
                if (!cts.IsCancellationRequested)
                {
                    lastErrorMessage = $"Connection error: {e.Message}";
                    MCPLogger.LogError(ComponentName, lastErrorMessage);
                    OnError?.Invoke(lastErrorMessage);
                }
            }
            finally
            {
                if (isConnected)
                {
                    isConnected = false;
                    OnDisconnected?.Invoke();
                }
            }
        }
        
        public async Task SendMessageAsync(string message)
        {
            if (!isConnected || webSocket?.State != WebSocketState.Open)
            {
                MCPLogger.LogWarning(ComponentName, "Cannot send message: not connected");
                return;
            }
            
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
                
                messagesSent++;
                MCPLogger.Log(ComponentName, $"Sent message: {message.Substring(0, Math.Min(100, message.Length))}...");
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, don't log as error
                MCPLogger.Log(ComponentName, "Send operation was canceled");
            }
            catch (WebSocketException wsEx)
            {
                lastErrorMessage = $"WebSocket send error: {wsEx.Message}";
                MCPLogger.LogError(ComponentName, lastErrorMessage);
                OnError?.Invoke(lastErrorMessage);
                
                // Connection might be broken, mark as disconnected
                isConnected = false;
                OnDisconnected?.Invoke();
            }
            catch (Exception e)
            {
                lastErrorMessage = $"Failed to send message: {e.Message}";
                MCPLogger.LogError(ComponentName, lastErrorMessage);
                OnError?.Invoke(lastErrorMessage);
                
                // Connection might be broken, mark as disconnected
                isConnected = false;
                OnDisconnected?.Invoke();
            }
        }
        
        public void Disconnect()
        {
            try
            {
                MCPLogger.Log(ComponentName, "Disconnecting from server");
                
                // Cancel any pending operations
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }
                
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    // Begin graceful close
                    var closeTask = webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disconnecting",
                        CancellationToken.None);
                    
                    // Give it a moment to close gracefully
                    Task.WaitAny(new[] { closeTask }, 1000);
                }
                
                // Dispose resources
                if (webSocket != null)
                {
                    webSocket.Dispose();
                    webSocket = null;
                }
            }
            catch (Exception e)
            {
                MCPLogger.LogWarning(ComponentName, $"Error during disconnect: {e.Message}");
            }
            finally
            {
                isConnected = false;
                OnDisconnected?.Invoke();
            }
        }
        
        ~MCPConnectionManager()
        {
            // Ensure resources are cleaned up
            Disconnect();
        }
    }
}
