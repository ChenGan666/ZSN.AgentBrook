import type { PlatformAdapter, FilePickOptions } from './adapter'

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

  system = {
    platform: 'web' as const,
    async openExternal(url: string): Promise<void> {
      window.open(url, '_blank')
    },
    getAppVersion(): string {
      return '0.1.0'
    },
  }

  notification = {
    show(title: string, body: string): void {
      if (!('Notification' in window)) return
      const showNotify = () => {
        const n = new Notification(title, { body })
        n.onclick = () => { window.focus(); n.close() }
      }
      if (Notification.permission === 'granted') {
        showNotify()
      } else if (Notification.permission !== 'denied') {
        Notification.requestPermission().then((perm) => {
          if (perm === 'granted') showNotify()
        })
      }
    },
  }
}
