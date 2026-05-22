import { onMounted, onUnmounted } from 'vue'
import { isTauri } from '@/platform'

export function useWindowState() {
  if (!isTauri()) return

  let saveTimer: ReturnType<typeof setTimeout> | null = null

  async function savePosition() {
    const { getCurrentWindow } = await import('@tauri-apps/api/window')
    const win = getCurrentWindow()
    const pos = await win.outerPosition()
    const size = await win.outerSize()
    localStorage.setItem('window_state', JSON.stringify({
      x: pos.x, y: pos.y,
      width: size.width, height: size.height,
    }))
  }

  async function restorePosition() {
    const saved = localStorage.getItem('window_state')
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
    restorePosition()
    window.addEventListener('resize', () => {
      if (saveTimer) clearTimeout(saveTimer)
      saveTimer = setTimeout(savePosition, 500)
    })
  })

  onUnmounted(() => {
    if (saveTimer) clearTimeout(saveTimer)
  })
}
