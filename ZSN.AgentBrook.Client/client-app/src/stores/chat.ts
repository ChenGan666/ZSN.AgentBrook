import { defineStore } from 'pinia'
import type { SessionInfo, ChatMessage, AppInfo, SessionStatusInfo } from '@/types/chat'
import * as sessionApi from '@/services/session'
import * as chatApi from '@/services/chat'
import { sessionCache, messageCache } from '@/utils/cache'
import { normalizeRecord } from '@/utils/process'

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
  runningSessionIds: string[]
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
    runningSessionIds: [],
  }),

  getters: {
    currentSession(): SessionInfo | undefined {
      return this.sessions.find((s) => s.ChatSessionID === this.currentSessionId)
    },
    hasRunningSessions(): boolean {
      return this.runningSessionIds.length > 0
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
            // Snapshot locally-completed statuses before the server data
            // overwrites them. removeRunningSession sets SessionStatus=0
            // but the server DB may not have the update yet.
            const localCompleted = new Set<string>()
            for (const s of this.sessions) {
              if (s.SessionStatus === 0 && !this.runningSessionIds.includes(s.ChatSessionID)) {
                localCompleted.add(s.ChatSessionID)
              }
            }

            this.sessions = list

            // Re-apply running status for sessions still in the running list
            for (const id of this.runningSessionIds) {
              const s = this.sessions.find(ss => ss.ChatSessionID === id)
              if (s && s.SessionStatus === 0) {
                s.SessionStatus = 1
              }
            }

            // Re-apply locally-completed status so sessions that just
            // finished are not shown as "running" due to server lag
            for (const id of localCompleted) {
              const s = this.sessions.find(ss => ss.ChatSessionID === id)
              if (s && s.SessionStatus === 1) {
                s.SessionStatus = 0
              }
            }

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
      // Persist current session's messages before switching away.
      if (this.currentSessionId && this.currentSessionId !== sessionId && this.messages.length > 0) {
        messageCache.set(this.currentSessionId, [...this.messages]).catch(() => {})
      }

      this.currentSessionId = sessionId
      this.loadingMessages = true

      // Load cached messages first (may contain process data from a previous live SSE session)
      let cachedMessages: ChatMessage[] = []
      try {
        cachedMessages = await messageCache.getBySession(sessionId)
        if (cachedMessages.length > 0) {
          this.messages = cachedMessages
        }
      } catch { /* ignore */ }

      try {
        // 1. Load chat messages from API
        const { data } = await chatApi.getChatList(sessionId)
        if (data.Success && data.Data) {
          const rawList = data.Data as any[]
          const apiMessages = rawList.map(parseChatLog).filter((m): m is ChatMessage => m !== null)

          // Preserve process data from cache by order.
          // SSE-created messages use temporary IDs (ai_xxx) while API uses real
          // ChatLogIDs, so we match by position rather than by ID.
          if (cachedMessages.length > 0) {
            const cachedWithProcess = cachedMessages.filter(
              m => m.process && !m.id.startsWith('pending_'),
            )

            const apiAssistantIndices: number[] = []
            this.messages = apiMessages.map((apiMsg, apiIdx) => {
              if (apiMsg.role === 'assistant') {
                apiAssistantIndices.push(apiIdx)
              }
              return apiMsg
            })

            const orderMatchCount = Math.min(cachedWithProcess.length, apiAssistantIndices.length)
            for (let i = 0; i < orderMatchCount; i++) {
              const processData = cachedWithProcess[i].process
              const apiIdx = apiAssistantIndices[i]
              if (processData) {
                this.messages[apiIdx] = {
                  ...this.messages[apiIdx],
                  process: processData,
                }
              }
            }

            // When the workflow hasn't produced a chat-log entry yet (still
            // running), the API may return fewer assistant messages than what
            // the cache holds. Append the extra cached messages so the active
            // SSE stream can continue to update them and the workflow tree
            // remains visible.
            if (cachedWithProcess.length > apiAssistantIndices.length) {
              const existingIds = new Set(this.messages.map(m => m.id))
              for (let i = apiAssistantIndices.length; i < cachedWithProcess.length; i++) {
                const extra = cachedWithProcess[i]
                if (!existingIds.has(extra.id)) {
                  this.messages.push(extra)
                }
              }
            }
          } else {
            this.messages = apiMessages
          }

          await messageCache.set(sessionId, this.messages)
        } else if (cachedMessages.length > 0) {
          this.messages = cachedMessages
        }
      } finally {
        // Keep loading=true until execution records are also loaded below.
        // This prevents the UI from flashing through partial states.
      }

      // Always load execution records so every session — running, completed,
      // or failed — shows its workflow tree. The execution-records API is the
      // authoritative source and loadSessionExecutionRecords safely handles all
      // states (messages with/without process, missing chat-log entries, etc.).
      try {
        await this.loadSessionExecutionRecords(sessionId)
      } finally {
        this.loadingMessages = false
      }
    },

    /**
     * Fetch workflow execution records for a session and attach them
     * to the corresponding assistant messages as `process` data.
     *
     * Matching is done by order: the i-th process corresponds to the
     * i-th assistant message that does not already have process data.
     */
    async loadSessionExecutionRecords(sessionId: string) {
      try {
        const { data } = await chatApi.getSessionExecutionRecords(sessionId)
        if (!data.Success || !data.Data) return

        let processList = data.Data as any[]
        if (!Array.isArray(processList) || processList.length === 0) return

        // Merge sub-processes into their parent ProcessInfo.
        // The server groups execution records by ProcessesID, so a workflow
        // with sub-tasks (ProcessesID = "parentId_childId") produces multiple
        // ProcessInfo entries. We merge children into the parent so the tree
        // builder in ProcessStatus sees all records at once.
        const childPids = new Set<string>()
        for (const proc of processList) {
          const pid: string = proc.ProcessID || ''
          if (pid.includes('_')) {
            const parentPid = pid.substring(0, pid.indexOf('_'))
            const parent = processList.find((p: any) => (p.ProcessID || '') === parentPid)
            if (parent) {
              const childRecords = Array.isArray(proc.ExecutionRecordInfos)
                ? proc.ExecutionRecordInfos
                : []
              if (!Array.isArray(parent.ExecutionRecordInfos)) {
                parent.ExecutionRecordInfos = []
              }
              parent.ExecutionRecordInfos.push(...childRecords)
              // Update parent status if child is still running / failed
              const childStatus = String(proc.Status || '').toLowerCase()
              const parentStatus = String(parent.Status || '').toLowerCase()
              if (childStatus === 'running' && parentStatus !== 'running') {
                parent.Status = 'running'
              } else if ((childStatus === 'failed' || childStatus === 'error') && parentStatus === 'success') {
                parent.Status = proc.Status
              }
              childPids.add(pid)
            }
          }
        }
        // Remove child processes — their records are now in the parent
        if (childPids.size > 0) {
          processList = processList.filter((p: any) => !childPids.has(p.ProcessID || ''))
        }

        // Collect ALL non-synthetic assistant message indices (with or without process).
        // Each such message corresponds to one process slot regardless of whether
        // its process data was already filled by SSE / cache.
        const allAssistantIndices: number[] = []
        this.messages.forEach((msg, idx) => {
          if (msg.role === 'assistant' && !msg.id.startsWith('pending_')) {
            allAssistantIndices.push(idx)
          }
        })

        // --- 1. Match processes to existing assistant messages by order ---
        // Apply execution records (authoritative) to messages that don't have
        // active SSE data. Messages with loading=true are receiving live SSE
        // updates — keep their richer real-time data.
        // Messages with loading=false (from cache) may be stale — overwrite them.
        let filledCount = 0
        const matchCount = Math.min(processList.length, allAssistantIndices.length)
        for (let i = 0; i < matchCount; i++) {
          const msgIndex = allAssistantIndices[i]
          const msg = this.messages[msgIndex]
          const proc = processList[i]
          const rawRecords = Array.isArray(proc.ExecutionRecordInfos)
            ? proc.ExecutionRecordInfos
            : []
          const records = rawRecords.map((r: any) => normalizeRecord(r))

          // Preserve streamsByNode from existing process data (SSE provides this)
          const existingStreams = msg.process?.streamsByNode || {}

          // Skip only when SSE is actively streaming to this message —
          // its data is live and more current than the execution-records snapshot.
          if (msg.process && msg.loading) continue

          this.messages[msgIndex] = {
            ...msg,
            process: {
              status: proc.Status || msg.process?.status || 'success',
              results: proc.Results || msg.process?.results || '',
              timestamp: Date.now(),
              records,
              streamsByNode: existingStreams,
            },
          }
          filledCount++
        }

        // --- 2. Create synthetic assistant messages for truly unrepresented processes ---
        // Only needed when the server returns more processes than we have
        // non-synthetic assistant messages.
        if (processList.length > allAssistantIndices.length) {
          // Remove old synthetic messages for this session
          this.messages = this.messages.filter(
            (m) => !(m.role === 'assistant' && m.id.startsWith('pending_')),
          )

          for (let i = allAssistantIndices.length; i < processList.length; i++) {
            const proc = processList[i]
            const rawRecords = Array.isArray(proc.ExecutionRecordInfos)
              ? proc.ExecutionRecordInfos
              : []
            const records = rawRecords.map((r: any) => normalizeRecord(r))
            const status = proc.Status || 'running'

            const syntheticMsg: ChatMessage = {
              id: `pending_${proc.ProcessID || i}`,
              sessionId,
              role: 'assistant',
              content: '',
              createdAt: new Date().toISOString(),
              loading: status === 'running',
              process: {
                status,
                results: proc.Results || '',
                timestamp: Date.now(),
                records,
                streamsByNode: {},
              },
            }

            this.messages.push(syntheticMsg)
          }
        }

        // Persist updated messages back to cache
        const hasChanges = filledCount > 0 || processList.length > allAssistantIndices.length
        if (hasChanges) {
          await messageCache.set(sessionId, [...this.messages])
        }
      } catch {
        // Silently ignore — execution records are an enhancement
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

    addRunningSession(sessionId: string) {
      if (!sessionId || this.runningSessionIds.includes(sessionId)) return
      this.runningSessionIds.push(sessionId)
      const session = this.sessions.find(s => s.ChatSessionID === sessionId)
      if (session) session.SessionStatus = 1
    },

    removeRunningSession(sessionId: string) {
      this.runningSessionIds = this.runningSessionIds.filter(id => id !== sessionId)
      const session = this.sessions.find(s => s.ChatSessionID === sessionId)
      if (session && session.SessionStatus === 1) {
        session.SessionStatus = 0
      }
    },

    /**
     * Refresh messages for the current session silently (no loading indicator).
     * Used when heartbeat detects a session has completed — replaces synthetic
     * pending messages with real chat log entries.
     */
    async refreshCurrentSessionMessages() {
      const sid = this.currentSessionId
      if (!sid) return

      // 如果有消息正在 SSE 流式写入（loading=true），跳过刷新避免竞态覆盖
      if (this.messages.some(m => m.loading)) return

      try {
        const { data } = await chatApi.getChatList(sid)
        if (!data.Success || !data.Data) return

        const rawList = data.Data as any[]
        const apiMessages = rawList.map(parseChatLog).filter((m): m is ChatMessage => m !== null)

        // Preserve process data from current non-synthetic messages by order.
        // SSE-created messages use temporary IDs (ai_xxx) while API uses real
        // ChatLogIDs, so we match by position rather than by ID.
        const currentWithProcess = this.messages.filter(
          m => m.process && !m.id.startsWith('pending_'),
        )

        // Build the new message list, then re-attach process data by order
        const apiAssistantIndices: number[] = []
        this.messages = apiMessages.map((apiMsg, apiIdx) => {
          if (apiMsg.role === 'assistant') {
            apiAssistantIndices.push(apiIdx)
          }
          return apiMsg
        })

        // Match by order: i-th current process → i-th API assistant message
        const orderMatchCount = Math.min(currentWithProcess.length, apiAssistantIndices.length)
        for (let i = 0; i < orderMatchCount; i++) {
          const processData = currentWithProcess[i].process
          const apiIdx = apiAssistantIndices[i]
          if (processData) {
            this.messages[apiIdx] = {
              ...this.messages[apiIdx],
              process: processData,
            }
          }
        }

        await messageCache.set(sid, [...this.messages])

        // Reload execution records to pick up any new data
        await this.loadSessionExecutionRecords(sid)
      } catch {
        // Silently ignore
      }
    },

    updateSessionStatusFromHeartbeat(list: SessionStatusInfo[]): SessionStatusInfo[] {
      const completedList: SessionStatusInfo[] = []
      for (const item of list) {
        const session = this.sessions.find(s => s.ChatSessionID === item.ChatSessionID)
        if (session) {
          session.SessionStatus = item.SessionStatus
        }
        // 已完成或失败的会话，从运行列表移除
        if (item.SessionStatus !== 1) {
          this.removeRunningSession(item.ChatSessionID)
          completedList.push(item)
        }
      }
      return completedList
    },
  },
})
