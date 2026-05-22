import type { PlatformAdapter, FilePickOptions } from './adapter'

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

  notification = {
    show(title: string, body: string): void {
      import('@tauri-apps/api/core').then(({ invoke }) => {
        invoke('send_system_notification', { title, body })
      })
    },
  }
}
