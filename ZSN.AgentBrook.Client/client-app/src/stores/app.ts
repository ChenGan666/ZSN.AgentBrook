import { defineStore } from 'pinia'

type ConnectionStatus = 'connected' | 'slow' | 'disconnected' | 'checking'

interface AppState {
  connectionStatus: ConnectionStatus
  apiLatency: number
  sidebarCollapsed: boolean
  sidebarWidth: number
}

export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    connectionStatus: 'checking',
    apiLatency: 0,
    sidebarCollapsed: false,
    sidebarWidth: 280,
  }),
  actions: {
    setConnection(status: ConnectionStatus, latency = 0) {
      this.connectionStatus = status
      this.apiLatency = latency
    },
    setSidebarWidth(width: number) {
      this.sidebarWidth = Math.max(200, Math.min(560, width))
    },
  },
  persist: {
    key: 'agentbrook-app',
    storage: localStorage,
  },
})
