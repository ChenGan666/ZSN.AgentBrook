import type { PlatformAdapter, FilePickOptions, NotificationResult } from './adapter'

export class WebAdapter implements PlatformAdapter {
  storage = {
    async get(key: string): Promise<string | null> {
      return localStorage.getItem(key)
    },
    async set(key: string, value: string): Promise<void> {
      localStorage.setItem(key, value)
    },
    async remove(key: string): Promise<void> {
      localStorage.removeItem(key)
    },
  }

  file = {
    async pick(options?: FilePickOptions): Promise<File[]> {
      return new Promise((resolve) => {
        const input = document.createElement('input')
        input.type = 'file'
        input.multiple = options?.multiple ?? false
        if (options?.filters?.length) {
          const exts = options.filters.flatMap((f) => f.extensions.map((e) => `.${e}`))
          input.accept = exts.join(',')
        }
        input.onchange = () => {
          resolve(input.files ? Array.from(input.files) : [])
        }
        input.click()
      })
    },
    async save(data: Blob, suggestedName: string): Promise<void> {
      const url = URL.createObjectURL(data)
      const a = document.createElement('a')
      a.href = url
      a.download = suggestedName
      a.click()
      URL.revokeObjectURL(url)
    },
  }

  audio = {
    async convertToWav(input: Blob): Promise<ArrayBuffer> {
      return input.arrayBuffer()
    },
  }

  /**
   * 本地能力（L5）—— Web 环境降级：不支持本地文件/命令。
   * 所有方法抛"不支持"提示，业务层据此给用户降级说明而非崩溃。
   */
  local = {
    available: false,
    async readFile() {
      throw new Error('当前环境（浏览器）不支持本地文件读取')
    },
    async writeFile() {
      throw new Error('当前环境（浏览器）不支持本地文件写入')
    },
    async listDir() {
      throw new Error('当前环境（浏览器）不支持本地目录读取')
    },
    async pickDirectory(): Promise<string | null> {
      return null
    },
    async exec() {
      throw new Error('当前环境（浏览器）不支持命令执行')
    },
  }

  system = {
    platform: 'web' as const,
    async openExternal(url: string): Promise<void> {
      window.open(url, '_blank')
    },
    getAppVersion(): string {
      return '0.1.0'
    },
  }

  private _notificationClickCallback: ((sessionId: string) => void) | null = null

  notification = {
    show: async (title: string, body: string, options?: { sessionId?: string }): Promise<NotificationResult> => {
      if (!('Notification' in window)) {
        return { status: 'unsupported', message: '浏览器不支持通知 API' }
      }
      const showNotify = (): NotificationResult => {
        const n = new Notification(title, { body })
        n.onclick = () => {
          window.focus()
          n.close()
          if (options?.sessionId && this._notificationClickCallback) {
            this._notificationClickCallback(options.sessionId)
          }
        }
        return { status: 'sent' }
      }
      if (Notification.permission === 'granted') {
        return showNotify()
      }
      if (Notification.permission !== 'denied') {
        const perm = await Notification.requestPermission()
        if (perm === 'granted') return showNotify()
        return { status: 'permission_denied', message: '通知权限未授予' }
      }
      return { status: 'permission_denied', message: '通知权限被拒绝' }
    },
    onNotificationClick: (callback: (sessionId: string) => void): void => {
      this._notificationClickCallback = callback
    },
  }
}
