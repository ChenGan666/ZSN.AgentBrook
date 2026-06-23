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

/**
 * Per-session streaming context.
 *
 * Holds the non-reactive state for a single in-flight SSE stream. The
 * `abortController` cannot live in reactive store state (it must remain a
 * plain object), so these contexts are kept in a module-level Map inside the
 * chat store and addressed by sessionId. The store's reactive
 * `streamingSessions: Record<string, boolean>` mirrors membership for the UI.
 *
 * A single sendMessage may register under a temporary key when the real
 * SessionID is unknown (new chat). When the first SSE frame carries the real
 * SessionID, `migrateStream` moves the context to the real key. All sids that
 * were registered for this logical stream are tracked in `registeredSids` so
 * `unregisterStream` can clean up `runningSessionIds` for every sid that was
 * added (fixing the leak when the SSE sid differs from the requested sid).
 */
export interface StreamContext {
  /** AbortController for the underlying fetch. */
  abortController: AbortController
  /** The persistent AI message reference that survives session switches. */
  aiMessage: ChatMessage | null
  /** The temp id (ai_<ts>) of the assistant placeholder. */
  aiMsgId: string
  /** The original sessionId passed to sendMessage (may be '' for new chats). */
  originSid: string
  /**
   * Every sessionId that has been added to runningSessionIds for this stream.
   * On unregistration each of these is removed to keep runningSessionIds in
   * sync regardless of how many times the SSE-provided sid changed.
   */
  registeredSids: string[]
  /**
   * 0-based index of this stream's assistant message within the session's
   * non-synthetic assistant message sequence, captured at send time. Used to
   * re-bind the SSE `aiMessage` to the correct store message after a
   * switch-away-then-back: when the user returns, `selectSession` replaces the
   * `ai_<ts>` id with the server's real ChatLogID (order-merge by position),
   * so `find(aiMsgId)` fails. Falling back to this index re-locates the right
   * message and re-binds, so the live workflow tree keeps updating.
   */
  assistantIndex: number
}
