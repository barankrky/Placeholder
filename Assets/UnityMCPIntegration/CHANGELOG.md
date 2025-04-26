## [0.0.1] - 2023-11-11

### First Release
- Initial release of Unity MCP Integration
- WebSocket-based communication between Unity Editor and MCP server
- MCP tools implementation:
    - `get_editor_state` for retrieving Unity project information
    - `get_current_scene_info` for scene hierarchy details
    - `get_game_objects_info` for specific GameObject information
    - `execute_editor_command` for executing C# code in Unity Editor
    - `get_logs` for accessing Unity console logs
- Debug window accessible from Window > MCP Debug menu
- Connection status monitoring
- Support for Unity 2021.3+
- Node.js MCP server (TypeScript implementation)
- Comprehensive documentation
- MIT License