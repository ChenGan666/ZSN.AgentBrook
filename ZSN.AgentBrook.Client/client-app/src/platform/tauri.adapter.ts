import type { PlatformAdapter, FilePickOptions, NotificationResult } from './adapter'
// Static import so the named exports resolve with full types (vue-tsc
// sometimes drops non-default exports from dynamic import() type inference).
// The module loads fine on all Tauri targets; it's only invoked on Windows.
import {
  isPermissionGranted as notifIsGranted,
  requestPermission as notifRequestPermission,
  sendNotification as notifSend,
} from '@tauri-apps/plugin-notification'

export class TauriAdapter implements PlatformAdapter {
  storage = {
    async get(key: string): Promise<string | null> {
      const { load } = await import('@tauri-apps/plugin-store')
      const store = await load('settings.json', { autoSave: true })
      return (await store.get(key) as string) ?? null
    },
    async set(key: string, value: string): Promise<void> {
      const { load } = await import('@tauri-apps/plugin-store')
      const store = await load('settings.json', { autoSave: true })
      await store.set(key, value)
    },
    async remove(key: string): Promise<void> {
      const { load } = await import('@tauri-apps/plugin-store')
      const store = await load('settings.json', { autoSave: true })
      await store.delete(key)
    },
  }

  file = {
    async pick(options?: FilePickOptions): Promise<File[]> {
      const { open } = await import('@tauri-apps/plugin-dialog')
      const result = await open({
        multiple: options?.multiple ?? false,
        filters: options?.filters,
      })
      if (!result) return []
      const paths = Array.isArray(result) ? result : [result]
      const { readFile } = await import('@tauri-apps/plugin-fs')
      const files: File[] = []
      for (const path of paths) {
        const data = await readFile(path)
        const name = path.split(/[\\/]/).pop() || 'file'
        files.push(new File([data.buffer as ArrayBuffer], name))
      }
      return files
    },
    async save(data: Blob, suggestedName: string): Promise<void> {
      const { save } = await import('@tauri-apps/plugin-dialog')
      const path = await save({ defaultPath: suggestedName })
      if (!path) return
      const { writeFile } = await import('@tauri-apps/plugin-fs')
      const buffer = await data.arrayBuffer()
      await writeFile(path, new Uint8Array(buffer))
    },
  }

  audio = {
    async convertToWav(input: Blob): Promise<ArrayBuffer> {
      return input.arrayBuffer()
    },
  }

  system = {
    platform: 'tauri' as const,
    async openExternal(url: string): Promise<void> {
      const { open } = await import('@tauri-apps/plugin-shell')
      await open(url)
    },
    getAppVersion(): string {
      return '0.1.0'
    },
  }

  private _pendingSessionId: string | null = null
  private _notificationClickCallback: ((sessionId: string) => void) | null = null
  /** Cached platform string (only resolved once at construction). Used to pick
   * the notification backend: Windows uses the official plugin (which sets the
   * AppUMID via Tauri's identifier, required for WinRT to surface the toast);
   * macOS/Linux keep the custom notify-rust command (the macOS branch works
   * around a dev-mode bug where tauri-plugin-notification shows the source as
   * "com.apple.Terminal"). */
  private _platform: string = ''

  notification = {
    show: async (title: string, body: string, options?: { sessionId?: string }): Promise<NotificationResult> => {
      if (options?.sessionId) {
        this._pendingSessionId = options.sessionId
      }

      const isWin = this._isWindows()

      // Windows: use the official notification plugin. notify-rust's WinRT
      // backend silently drops toasts when no Application User Model ID is
      // registered. The official plugin registers the AppUMID from Tauri's
      // bundle identifier automatically.
      if (isWin) {
        try {
          let granted = await notifIsGranted()
          if (!granted) {
            const perm = await notifRequestPermission()
            granted = perm === 'granted'
          }
          if (!granted) {
            return { status: 'permission_denied', message: '通知权限未授予' }
          }
          notifSend({ title, body })
          return { status: 'sent' }
        } catch {
          // Fall back to the custom Rust command if the plugin is unavailable.
          return this._sendViaRust(title, body, options?.sessionId)
        }
      }

      // macOS / Linux: custom notify-rust command.
      return this._sendViaRust(title, body, options?.sessionId)
    },
    onNotificationClick: (callback: (sessionId: string) => void): void => {
      this._notificationClickCallback = callback
      // macOS/Windows: 点击通知会激活应用窗口，触发 focus 事件
      window.addEventListener('focus', () => {
        if (this._pendingSessionId && this._notificationClickCallback) {
          const sid = this._pendingSessionId
          this._pendingSessionId = null
          this._notificationClickCallback(sid)
        }
      })
    },
  }

  /** Detect Windows via the WebView user agent. Tauri uses WebView2 (Chromium-
   * based) on Windows, whose UA always contains "Windows NT"; macOS uses
   * WKWebView (UA contains "Macintosh"). This avoids a dependency on
   * tauri-plugin-os. Cached after first resolution. */
  private _isWindows(): boolean {
    if (this._platform) return this._platform === 'windows'
    const isWin = typeof navigator !== 'undefined' && /Windows NT/i.test(navigator.userAgent)
    this._platform = isWin ? 'windows' : 'other'
    return isWin
  }

  /** Invoke the custom Rust notify-rust command (macOS/Linux path, and Windows
   * fallback if the official plugin throws). Returns a NotificationResult so
   * callers (test button) can report what happened. */
  private async _sendViaRust(title: string, body: string, sessionId?: string): Promise<NotificationResult> {
    try {
      const { invoke } = await import('@tauri-apps/api/core')
      await invoke('send_system_notification', {
        title,
        body,
        sessionId: sessionId || null,
      })
      return { status: 'sent' }
    } catch (e) {
      return { status: 'error', message: e instanceof Error ? e.message : String(e) }
    }
  }
}
