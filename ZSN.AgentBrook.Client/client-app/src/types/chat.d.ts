export interface SessionInfo {
  ChatSessionID: string
  AppID: string
  MemberID: string
  TopicSummary: string
  IsCoCreate: number
  SystemStatus: number
  SessionStatus: number
  CreateTime: string
}

export interface SessionStatusInfo {
  ChatSessionID: string
  SessionStatus: number
  Summary: string
  TopicSummary: string
  AppID: string
}

export interface ChatFile {
  id: string
  name: string
  url: string
  type: string
  size: number
  thumbnail?: string
}

export interface AttachmentItem {
  Name: string
  Type: string
  FilePath: string
  FileCode: string
  FileURI: string
  IsUploading: boolean
  UploadProgress: number
}

export interface StreamEnvelopeItem {
  nodeId: string
  type: 'delta' | 'done'
  content: string
  timestamp: number
}

export interface StreamByNode {
  text: string
  tailText: string
  status: 'running' | 'done'
  lastTimestamp: number
}

export interface ExecutionRecordInfo {
  RecordID: string
  SessionID: string
  ProcessesID: string
  WorkflowID: string
  TaskID: string
  FromMainTaskID: string
  NodeID: string
  NodeName: string
  NextNodeID: string
  StartTime: string
  EndTime: string
  Status: 'running' | 'success' | 'failed' | 'error'
  Inputs: any
  Outputs: { varname: string; type: string; value: any }[]
  Logs: string[]
}

export interface ProcessInfo {
  Status: 'running' | 'success' | 'failed' | 'error'
  SessionID: string
  ProcessID: string
  StreamEnvelope: StreamEnvelopeItem[]
  ExecutionRecordInfos: ExecutionRecordInfo[]
  Results: { type: string; value: any }[]
}

export interface MessageProcess {
  status: string
  results: string
  timestamp: number | null
  records: NormalizedRecord[]
  streamsByNode: Record<string, StreamByNode>
}

export interface NormalizedRecord {
  recordId: string
  sessionId: string
  processesId: string
  workflowId: string
  taskId: string
  fromMainTaskId: string
  nodeId: string
  nodeName: string
  nextNodeId: string
  startTime: string
  endTime: string
  status: string
  inputs: any
  outputs: { varname: string; type: string; value: any }[]
  logs: string[]
}

export interface ChatMessage {
  id: string
  sessionId: string
  role: 'user' | 'assistant' | 'system'
  content: string
  files?: ChatFile[]
  images?: ChatFile[]
  createdAt: string
  loading?: boolean
  process?: MessageProcess
}

export interface AppInfo {
  AppID: string
  Name: string
  AICON: string
  AICONList: string[]
  Description: string
  SessionModelID: number
  SessionModelName: string
  WorkFlowID: string
  SystemStatus: number
}

export interface SSEMessage {
  SessionID?: string
  ProcessesID?: string
  ProcessInfo?: ProcessInfo
  Error?: boolean
  ErrorCode?: number
  ErrorDesc?: string
  Content?: string
  Timestamp?: number
}

export interface HitlField {
  name: string
  label: string
  type: 'text' | 'textarea' | 'select' | 'radio' | 'checkbox' | 'number' | 'date'
  required?: boolean
  placeholder?: string
  options?: { label: string; value: string }[]
  defaultValue?: any
  rules?: any[]
}

export interface HitlRequest {
  formId: string
  title: string
  description?: string
  fields: HitlField[]
  sessionId: string
  messageId: string
}
