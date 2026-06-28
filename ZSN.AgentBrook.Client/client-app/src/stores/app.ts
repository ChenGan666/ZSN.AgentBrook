import { defineStore } from 'pinia'

type ConnectionStatus = 'connected' | 'slow' | 'disconnected' | 'checking'

/** 应用模式：Chat（原有对话）/ Agent（编排大脑，凌驾于 Chat 之上） */
export type AppMode = 'chat' | 'agent'

interface AppState {
  connectionStatus: ConnectionStatus
  apiLatency: number
  sidebarCollapsed: boolean
  sidebarWidth: number
  /** 当前应用模式（持久化，刷新后保持） */
  mode: AppMode
}

export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    connectionStatus: 'checking',
    apiLatency: 0,
    sidebarCollapsed: false,
    sidebarWidth: 280,
    mode: 'chat',
  }),
  actions: {
    setConnection(status: ConnectionStatus, latency = 0) {
      this.connectionStatus = status
      this.apiLatency = latency
    },
    setSidebarWidth(width: number) {
      this.sidebarWidth = Math.max(200, Math.min(560, width))
    },
    setMode(mode: AppMode) {
      this.mode = mode
    },
  },
  persist: {
    key: 'agentbrook-app',
    storage: localStorage,
  },
})
