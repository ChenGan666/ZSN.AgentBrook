import { defineStore } from 'pinia'
import type { SessionInfo, ChatMessage, AppInfo } from '@/types/chat'
import * as sessionApi from '@/services/session'
import * as chatApi from '@/services/chat'
import { sessionCache, messageCache } from '@/utils/cache'

function parseChatLog(log: any): ChatMessage | null {
  if (!log) return null

  const role = String(log.Role || '').toLowerCase() as ChatMessage['role']
  if (role !== 'user' && role !== 'assistant' && role !== 'system') return null

  let content = ''
  if (log.ContentToGptMsg && typeof log.ContentToGptMsg === 'object') {
    content = log.ContentToGptMsg.content || ''
  }
  if (!content && typeof log.Content === 'string') {
    try {
      const parsed = JSON.parse(log.Content)
      content = parsed.content || ''
    } catch {
      content = log.Content
    }
  }

  return {
    id: log.ChatLogID || `log_${Date.now()}_${Math.random()}`,
    sessionId: log.ChatSessionID || '',
    role,
    content,
    createdAt: log.CreateTime || new Date().toISOString(),
  }
}

interface ChatState {
  sessions: SessionInfo[]
  currentSessionId: string | null
  selectedAppId: string | null
  messages: ChatMessage[]
  apps: AppInfo[]
  loadingSessions: boolean
  loadingMessages: boolean
  sessionsTotal: number
}

export const useChatStore = defineStore('chat', {
  state: (): ChatState => ({
    sessions: [],
    currentSessionId: null,
    selectedAppId: null,
    messages: [],
    apps: [],
    loadingSessions: false,
    loadingMessages: false,
    sessionsTotal: 0,
  }),

  getters: {
    currentSession(): SessionInfo | undefined {
      return this.sessions.find((s) => s.ChatSessionID === this.currentSessionId)
    },
  },

  actions: {
    async fetchSessions(page = 1, size = 50) {
      this.loadingSessions = true
      try {
        const { data } = await sessionApi.getSessionList(page, size)
        if (data.Success && data.Data) {
          const list = data.Data.Data || []
          this.sessionsTotal = data.Data.total || 0
          if (page === 1) {
            this.sessions = list
            await sessionCache.set(list)
          } else {
            this.sessions = [...this.sessions, ...list]
          }
        }
      } finally {
        this.loadingSessions = false
      }
    },

    async selectSession(sessionId: string) {
      this.currentSessionId = sessionId

      this.loadingMessages = true
      try {
        const { data } = await chatApi.getChatList(sessionId)
        if (data.Success && data.Data) {
          const rawList = data.Data as any[]
          this.messages = rawList.map(parseChatLog).filter((m): m is ChatMessage => m !== null)
          await messageCache.set(sessionId, this.messages)
        }
      } finally {
        this.loadingMessages = false
      }
    },

    async deleteSession(id: string) {
      await sessionApi.deleteSession(id)
      this.sessions = this.sessions.filter((s) => s.ChatSessionID !== id)
      this.sessionsTotal = Math.max(0, this.sessionsTotal - 1)
      if (this.currentSessionId === id) {
        this.currentSessionId = this.sessions[0]?.ChatSessionID ?? null
        if (this.currentSessionId) {
          await this.selectSession(this.currentSessionId)
        } else {
          this.messages = []
        }
      }
    },

    async fetchApps() {
      const { data } = await sessionApi.getAppList()
      if (data.Success && data.Data) {
        this.apps = data.Data
        if (!this.selectedAppId && this.apps.length > 0) {
          this.selectedAppId = this.apps[0].AppID
        }
      }
    },

    updateMessage(messageId: string, updated: ChatMessage) {
      const idx = this.messages.findIndex((m) => m.id === messageId)
      if (idx !== -1) {
        this.messages[idx] = updated
      }
    },

    addMessage(message: ChatMessage) {
      this.messages.push(message)
    },
  },
})
