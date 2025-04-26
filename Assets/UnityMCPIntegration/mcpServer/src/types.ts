// MCP Server Types for Unity Integration

// Unity Editor State representation
export interface UnityEditorState {
  activeGameObjects: any[];
  selectedObjects: any[];
  playModeState: string;
  sceneHierarchy: any;
  projectName?: string;
  unityVersion?: string;
  renderPipeline?: string;
  buildTarget?: string;
  graphicsDeviceType?: string;
  currentSceneName?: string;
  currentScenePath?: string;
  timestamp?: string;
  availableMenuItems?: string[];
}

// Log entry from Unity
export interface LogEntry {
  message: string;
  stackTrace: string;
  logType: string;
  timestamp: string;
}

// Scene info from Unity
export interface SceneInfoMessage {
  type: 'sceneInfo';
  data: {
    requestId: string;
    sceneInfo: any;
    timestamp: string;
  };
}

// Game objects details from Unity
export interface GameObjectsDetailsMessage {
  type: 'gameObjectsDetails';
  data: {
    requestId: string;
    gameObjectDetails: any[];
    count: number;
    timestamp: string;
  };
}

// Message types from Unity to Server
export interface EditorStateMessage {
  type: 'editorState';
  data: UnityEditorState;
}

export interface CommandResultMessage {
  type: 'commandResult';
  data: any;
}

export interface LogMessage {
  type: 'log';
  data: LogEntry;
}

export interface PongMessage {
  type: 'pong';
  data: { timestamp: number };
}

// Message types from Server to Unity
export interface ExecuteEditorCommandMessage {
  type: 'executeEditorCommand';
  data: {
    code: string;
  };
}

export interface HandshakeMessage {
  type: 'handshake';
  data: { message: string };
}

export interface PingMessage {
  type: 'ping';
  data: { timestamp: number };
}

export interface RequestEditorStateMessage {
  type: 'requestEditorState';
  data: Record<string, never>;
}

export interface GetSceneInfoMessage {
  type: 'getSceneInfo';
  data: {
    requestId: string;
    detailLevel: string;
  };
}

export interface GetGameObjectsInfoMessage {
  type: 'getGameObjectsInfo';
  data: {
    requestId: string;
    instanceIDs: number[];
    detailLevel: string;
  };
}

// Union type for all Unity messages
export type UnityMessage = 
  | EditorStateMessage 
  | CommandResultMessage 
  | LogMessage
  | PongMessage
  | SceneInfoMessage
  | GameObjectsDetailsMessage;

// Union type for all Server messages
export type ServerMessage =
  | ExecuteEditorCommandMessage
  | HandshakeMessage
  | PingMessage
  | RequestEditorStateMessage
  | GetSceneInfoMessage
  | GetGameObjectsInfoMessage;

// Command result handling
export interface CommandPromise {
  resolve: (data?: any) => void;
  reject: (reason?: any) => void;
}

export interface TreeEntry {
  name: string;
  type: 'file' | 'directory';
  children?: TreeEntry[];
}

export interface MCPSceneInfo {
  name: string;
  path: string;
  rootGameObjects: any[];
  buildIndex: number;
  isDirty: boolean;
  isLoaded: boolean;
}

export interface MCPTransformInfo {
  position: { x: number, y: number, z: number };
  rotation: { x: number, y: number, z: number };
  localPosition: { x: number, y: number, z: number };
  localRotation: { x: number, y: number, z: number };
  localScale: { x: number, y: number, z: number };
}

export interface MCPComponentInfo {
  type: string;
  isEnabled: boolean;
  instanceID: number;
}

export interface MCPGameObjectDetail {
  name: string;
  instanceID: number;
  path: string;
  active: boolean;
  activeInHierarchy: boolean;
  tag: string;
  layer: number;
  layerName: string;
  isStatic: boolean;
  transform: MCPTransformInfo;
  components: MCPComponentInfo[];
}

export enum SceneInfoDetail {
  RootObjectsOnly = 'RootObjectsOnly',
  FullHierarchy = 'FullHierarchy'
}

export enum GameObjectInfoDetail {
  BasicInfo = 'BasicInfo',
  IncludeComponents = 'IncludeComponents',
  IncludeChildren = 'IncludeChildren',
  IncludeComponentsAndChildren = 'IncludeComponentsAndChildren'
}