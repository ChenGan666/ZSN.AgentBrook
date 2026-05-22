import { ref, onMounted } from 'vue'
import { isTauri } from '@/platform'

export function useUpdater() {
  const updateAvailable = ref(false)
  const updateProgress = ref(0)
  const updateVersion = ref('')

  async function checkForUpdates() {
    if (!isTauri()) return
    try {
      const { check } = await import('@tauri-apps/plugin-updater')
      const update = await check()
      if (update) {
        updateAvailable.value = true
        updateVersion.value = update.version
      }
    } catch { /* ignore */ }
  }

  async function installUpdate() {
    if (!isTauri()) return
    try {
      const { check } = await import('@tauri-apps/plugin-updater')
      const update = await check()
      if (!update) return

      await update.downloadAndInstall((event) => {
        if (event.event === 'Progress' && event.data.contentLength) {
          updateProgress.value = Math.round(
            (event.data.chunkLength / event.data.contentLength) * 100,
          )
        }
      })

      const { relaunch } = await import('@tauri-apps/plugin-process')
      await relaunch()
    } catch { /* ignore */ }
  }

  onMounted(() => {
    setTimeout(checkForUpdates, 5000)
    setInterval(checkForUpdates, 4 * 60 * 60 * 1000)
  })

  return { updateAvailable, updateVersion, updateProgress, checkForUpdates, installUpdate }
}
