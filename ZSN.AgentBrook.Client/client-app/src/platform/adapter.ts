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

export interface PlatformNotification {
  show(title: string, body: string, options?: NotificationOptions): void
  onNotificationClick?(callback: (sessionId: string) => void): void
}

export interface PlatformAdapter {
  storage: PlatformStorage
  file: PlatformFile
  audio: PlatformAudio
  system: PlatformSystem
  notification: PlatformNotification
}
