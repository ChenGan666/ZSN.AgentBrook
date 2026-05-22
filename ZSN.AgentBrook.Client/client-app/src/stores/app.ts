import { defineStore } from 'pinia'

type ConnectionStatus = 'connected' | 'slow' | 'disconnected' | 'checking'

interface AppState {
  connectionStatus: ConnectionStatus
  apiLatency: number
  sidebarCollapsed: boolean
}

export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    connectionStatus: 'checking',
    apiLatency: 0,
    sidebarCollapsed: false,
  }),
  actions: {
    setConnection(status: ConnectionStatus, latency = 0) {
      this.connectionStatus = status
      this.apiLatency = latency
    },
  },
})
