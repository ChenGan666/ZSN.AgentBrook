import { onMounted, onUnmounted } from 'vue'
import { isTauri } from '@/platform'

const STORAGE_KEY = 'window_state'

export function useWindowState() {
  if (!isTauri()) return

  let saveTimer: ReturnType<typeof setTimeout> | null = null

  async function saveState() {
    try {
      const { getCurrentWindow } = await import('@tauri-apps/api/window')
      const win = getCurrentWindow()
      const pos = await win.outerPosition()
      const size = await win.outerSize()
      localStorage.setItem(STORAGE_KEY, JSON.stringify({
        x: pos.x, y: pos.y,
        width: size.width, height: size.height,
      }))
    } catch { /* ignore */ }
  }

  function scheduleSave() {
    if (saveTimer) clearTimeout(saveTimer)
    saveTimer = setTimeout(saveState, 500)
  }

  async function restoreState() {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (!saved) return
    try {
      const { x, y, width, height } = JSON.parse(saved)
      const { getCurrentWindow } = await import('@tauri-apps/api/window')
      const win = getCurrentWindow()
      const { LogicalPosition } = await import('@tauri-apps/api/dpi')
      const { LogicalSize } = await import('@tauri-apps/api/dpi')
      await win.setPosition(new LogicalPosition(x, y))
      await win.setSize(new LogicalSize(width, height))
    } catch { /* ignore */ }
  }

  onMounted(() => {
    restoreState()
    window.addEventListener('resize', scheduleSave)
    window.addEventListener('beforeunload', saveState)
  })

  onUnmounted(() => {
    if (saveTimer) clearTimeout(saveTimer)
    saveState()
    window.removeEventListener('resize', scheduleSave)
    window.removeEventListener('beforeunload', saveState)
  })
}
