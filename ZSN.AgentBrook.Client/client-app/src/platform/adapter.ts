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

export interface PlatformAdapter {
  storage: PlatformStorage
  file: PlatformFile
  audio: PlatformAudio
  system: PlatformSystem
  notification: PlatformNotification
}
