import { onMounted, onUnmounted } from 'vue'
import { isTauri } from '@/platform'

export function useGlobalShortcut() {
  if (!isTauri()) return

  onMounted(async () => {
    try {
      const { register } = await import('@tauri-apps/plugin-global-shortcut')
      await register('Alt+Space', async () => {
        const { getCurrentWindow } = await import('@tauri-apps/api/window')
        const win = getCurrentWindow()
        if (await win.isVisible()) {
          await win.hide()
        } else {
          await win.show()
          await win.setFocus()
        }
      })
    } catch { /* shortcut registration failed */ }
  })

  onUnmounted(async () => {
    try {
      const { unregisterAll } = await import('@tauri-apps/plugin-global-shortcut')
      await unregisterAll()
    } catch { /* ignore */ }
  })
}
