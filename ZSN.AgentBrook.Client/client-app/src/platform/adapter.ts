/**
 * 平台适配层 - 接口定义
 * 业务代码统一调用此接口，无需关心运行在 Tauri 还是浏览器
 */

export interface FilePickOptions {
  multiple?: boolean
  filters?: { name: string; extensions: string[] }[]
}

export interface PlatformStorage {
  get(key: string): Promise<string | null>
  set(key: string, value: string): Promise<void>
  remove(key: string): Promise<void>
}

export interface PlatformFile {
  pick(options?: FilePickOptions): Promise<File[]>
  save(data: Blob, suggestedName: string): Promise<void>
}

export interface PlatformAudio {
  convertToWav(input: Blob): Promise<ArrayBuffer>
}

export interface PlatformSystem {
  platform: 'tauri' | 'web'
  openExternal(url: string): Promise<void>
  getAppVersion(): string
}

export interface NotificationOptions {
  sessionId?: string
}

/**
 * Outcome of a notification attempt. Lets callers (e.g. the test button) tell
 * the user what actually happened, instead of assuming success just because no
 * exception was thrown.
 *
 *  - `sent`              — the underlying OS API was called. NOTE: this does
 *                          NOT guarantee the OS surfaced the notification — if
 *                          the system's global notification setting is off (or
 *                          focus-assist is muting it), the toast is still
 *                          dropped silently.
 *  - `permission_denied` — the user/system denied notification permission.
 *  - `unsupported`       — the platform has no notification API (e.g. an old
 *                          browser without the Notification API).
 *  - `error`             — the API threw; `message` has details.
 */
export type NotificationStatus =
  | 'sent'
  | 'permission_denied'
  | 'unsupported'
  | 'error'

export interface NotificationResult {
  status: NotificationStatus
  /** Human-readable detail (error message, permission state, etc.). */
  message?: string
}

export interface PlatformNotification {
  show(title: string, body: string, options?: NotificationOptions): void | Promise<void> | Promise<NotificationResult>
  onNotificationClick?(callback: (sessionId: string) => void): void
}

/** 本地能力（文件读写/命令执行）—— 仅 Tauri 环境实现，Web 环境降级提示。 */
export interface LocalFileResult {
  path: string
  content: string
}

export interface LocalExecResult {
  code: number
  stdout: string
  stderr: string
}

export interface PlatformLocal {
  /** 平台是否支持本地能力（Tauri=true，Web=false）。 */
  available: boolean
  /** 读取本地文件为文本。 */
  readFile(path: string): Promise<LocalFileResult>
  /** 写入文本到本地文件。 */
  writeFile(path: string, content: string): Promise<void>
  /** 列出目录下的条目。 */
  listDir(path: string): Promise<string[]>
  /** 用用户选择的目录对话框选一个目录（自动获得该目录访问权）。 */
  pickDirectory(): Promise<string | null>
  /**
   * 执行命令（参数数组，非 shell 字符串，防注入）。
   * @param command 命令名（必须在 shell scope 白名单内）
   * @param args 参数数组
   * @param cwd 工作目录
   * @param timeoutSec 超时（秒）
   */
  exec(command: string, args: string[], cwd?: string, timeoutSec?: number): Promise<LocalExecResult>
}

export interface PlatformAdapter {
  storage: PlatformStorage
  file: PlatformFile
  audio: PlatformAudio
  system: PlatformSystem
  notification: PlatformNotification
  local: PlatformLocal
}
