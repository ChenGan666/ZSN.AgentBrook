import { onMounted, onUnmounted } from 'vue'
import { useAppStore } from '@/stores/app'
import http from '@/services/http'

export function useConnection() {
  const appStore = useAppStore()
  let heartbeatTimer: ReturnType<typeof setInterval> | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null
  let reconnectDelay = 1000

  async function checkConnection() {
    const start = Date.now()
    try {
      await http.post('/Base/Get', {}, { timeout: 5000 })
      const latency = Date.now() - start
      reconnectDelay = 1000

      if (latency > 2000) {
        appStore.setConnection('slow', latency)
      } else {
        appStore.setConnection('connected', latency)
      }
    } catch {
      appStore.setConnection('disconnected')
      scheduleReconnect()
    }
  }

  function scheduleReconnect() {
    if (reconnectTimer) return
    reconnectTimer = setTimeout(async () => {
      reconnectTimer = null
      await checkConnection()
      reconnectDelay = Math.min(reconnectDelay * 2, 30000)
    }, reconnectDelay)
  }

  function startHeartbeat() {
    checkConnection()
    heartbeatTimer = setInterval(checkConnection, 30000)
  }

  function stopHeartbeat() {
    if (heartbeatTimer) clearInterval(heartbeatTimer)
    if (reconnectTimer) clearTimeout(reconnectTimer)
  }

  onMounted(startHeartbeat)
  onUnmounted(stopHeartbeat)

  return { checkConnection }
}
