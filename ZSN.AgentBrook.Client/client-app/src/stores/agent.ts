/**
 * Agent 模式 UI 状态（stores/agent.ts）。
 *
 * 与 Chat 平行、零侵入：管理编排模式下的"壳"状态——选定编排模型、@ 选中的 App、
 * 过程化消息列表、编排运行标志。编排循环本身（Plan-Act-Reflect）在
 * composables/useAgentOrchestrator.ts；本 store 只持有 UI 与跨组件共享状态。
 *
 * 持久化：仅 modelID / mode 偏好持久化（避免运行态/消息被错误恢复）。
 */
import { defineStore } from 'pinia'
import type { AgentMessage, AgentModelDTO } from '@/types/agent'
import i18n from '@/locales'

interface SelectedApp {
  appID: string
  name: string
  icon?: string
}

/** Agent 会话（问题 2：支持多会话管理）。 */
export interface AgentSession {
  id: string
  title: string
  goal?: string
  modelID: number | null
  selectedApps: SelectedApp[]
  messages: AgentMessage[]
  running: boolean
  createdAt: number
  updatedAt: number
}

/** 目录授权（L5）：白名单 read / readWrite。 */
export interface DirectoryGrant {
  id: string
  path: string
  scope: 'read' | 'readWrite'
  recursive: boolean
  grantedAt: number
}

/** 命令授权（L5）：白名单 + 参数约束。 */
export interface CommandGrant {
  id: string
  command: string
  argWhitelist?: string[]
  argBlacklist?: string[]
  workingDirGrant?: string
  networkAllowed?: boolean
  timeoutSec?: number
  grantedAt: number
}

/** 本地能力调用审计日志（L5）。 */
export interface LocalAuditEntry {
  id: string
  type: 'readFile' | 'writeFile' | 'listDir' | 'exec'
  detail: string
  status: 'approved' | 'denied' | 'blocked' | 'error'
  at: number
}

interface AgentState {
  /** 选定的编排大脑模型 ID（持久化） */
  modelID: number | null
  /** 最近一次拉取的模型列表（缓存） */
  models: AgentModelDTO[]
  /** @ 选中的可编排 App */
  selectedAppIDs: string[]
  selectedApps: SelectedApp[]
  /** 编排进行中（由 L3 orchestrator 写） */
  running: boolean
  /** 过程化消息列表 */
  messages: AgentMessage[]
  /** 全部展开/折叠开关（可选） */
  expandAllStages: boolean
  /** L5：本地能力授权（目录/命令白名单，持久化） */
  directoryGrants: DirectoryGrant[]
  commandGrants: CommandGrant[]
  /** L5：本地能力调用审计日志（仅内存，不持久化） */
  auditLogs: LocalAuditEntry[]
  /** Agent 会话列表（问题 2）。 */
  sessions: AgentSession[]
  /** 当前激活会话 ID。 */
  currentSessionId: string | null
  /** 当前真正在跑编排的会话 ID（内存状态，不持久化）。 */
  activeSessionId: string | null
}

export const useAgentStore = defineStore('agent', {
  state: (): AgentState => ({
    modelID: null,
    models: [],
    selectedAppIDs: [],
    selectedApps: [],
    running: false,
    messages: [],
    expandAllStages: false,
    directoryGrants: [],
    commandGrants: [],
    auditLogs: [],
    sessions: [],
    currentSessionId: null,
    activeSessionId: null,
  }),
  getters: {
    hasSelectedApps: (s) => s.selectedAppIDs.length > 0,
    hasSelectedModel: (s) => s.modelID != null,
    currentSession: (s) => s.sessions.find((x) => x.id === s.currentSessionId),
    anySessionRunning: (s) => s.sessions.some((x) => x.running),
  },
  actions: {
    setModels(models: AgentModelDTO[]) {
      this.models = models
      // 默认选第一个或保留上次选择
      if (this.modelID == null && models.length > 0) {
        this.modelID = models[0].LargeModelID
      }
    },
    setModel(id: number) {
      this.modelID = id
      this.syncSession()
    },
    /** 批量设置选中 App（来自 @ 浮层） */
    setSelectedApps(apps: SelectedApp[]) {
      this.selectedApps = apps
      this.selectedAppIDs = apps.map((a) => a.appID)
      this.syncSession()
    },
    addSelectedApp(app: SelectedApp) {
      if (!this.selectedAppIDs.includes(app.appID)) {
        this.selectedApps.push(app)
        this.selectedAppIDs.push(app.appID)
        this.syncSession()
      }
    },
    removeSelectedApp(appID: string) {
      this.selectedApps = this.selectedApps.filter((a) => a.appID !== appID)
      this.selectedAppIDs = this.selectedAppIDs.filter((id) => id !== appID)
      this.syncSession()
    },
    clearSelectedApps() {
      this.selectedApps = []
      this.selectedAppIDs = []
      this.syncSession()
    },
    setRunning(running: boolean) {
      this.running = running
      const session = this.sessions.find((s) => s.id === this.currentSessionId)
      if (session) session.running = running
      this.syncSession()
    },
    setActiveSessionId(id: string | null) {
      this.activeSessionId = id
    },
    addMessage(msg: AgentMessage) {
      this.messages.push(msg)
      this.syncSession()
    },
    updateMessage(id: string, patch: Partial<AgentMessage>) {
      const m = this.messages.find((x) => x.id === id)
      if (m) {
        Object.assign(m, patch)
        this.syncSession()
      }
    },
    getMessage(id: string) {
      return this.messages.find((x) => x.id === id)
    },
    clearMessages() {
      this.messages = []
      this.syncSession()
    },
    // ── Agent 会话管理（问题 2） ──
    /** 把当前运行态同步回当前会话，但不刷新 updatedAt（用于切会话等纯 UI 切换） */
    saveCurrentSession() {
      if (!this.currentSessionId) return
      const session = this.sessions.find((s) => s.id === this.currentSessionId)
      if (!session) return
      // 保留引用：运行中消息对象（如 reactive 的 assistantMsg）需要持续更新到会话，
      // 深克隆会切断响应式连接，导致切换会话后丢失后续编排事件。
      // 同时避免初始状态（messages 尚未加载）把空数组覆盖到已有会话。
      if (this.messages.length > 0 || session.messages.length === 0) {
        session.messages = this.messages
      }
      session.selectedApps = JSON.parse(JSON.stringify(this.selectedApps))
      session.modelID = this.modelID
    },
    /** 刷新/重连后修复：把仍标记为 running 的 assistant 消息收别为失败 */
    repairSession() {
      // 若该会话正由当前编排实例真实运行，不修复（避免中断活跃编排）
      if (this.activeSessionId === this.currentSessionId) return
      const { t } = i18n.global
      let repaired = false
      for (const msg of this.messages) {
        if (msg.role === 'assistant' && msg.running) {
          msg.running = false
          const hasFailed = msg.stages.some((s) => s.status === 'failed')
          if (!hasFailed) {
            msg.stages.push({
              id: `s_repair_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
              kind: 'failed',
              title: t('agent.interrupted', '执行中断'),
              status: 'failed',
              content: '',
              collapsed: false,
              error: t('agent.refreshInterrupted', '页面刷新或网络异常导致执行中断'),
            })
          }
          repaired = true
        }
      }
      // 修复时只保存消息，不刷新 updatedAt，避免切换会话导致列表重排
      if (repaired) {
        this.running = false
        this.syncSession(false)
      }
    },
    createSession(title?: string, goal?: string): AgentSession {
      this.saveCurrentSession() // 新建前先保存当前会话，不刷新旧会话时间戳
      const session: AgentSession = {
        id: `agent_session_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
        title: title || '新会话',
        goal,
        modelID: this.modelID,
        selectedApps: [],
        messages: [],
        running: false,
        createdAt: Date.now(),
        updatedAt: Date.now(),
      }
      this.sessions.unshift(session)
      this.currentSessionId = session.id
      this.messages = []
      this.selectedApps = []
      this.selectedAppIDs = []
      this.running = false
      return session
    },
    switchSession(id: string) {
      const session = this.sessions.find((s) => s.id === id)
      if (!session) return
      this.saveCurrentSession() // 切走前先把当前会话内容落盘，但不更新时间戳
      this.currentSessionId = id
      this.messages = session.messages
      this.selectedApps = [...session.selectedApps]
      this.selectedAppIDs = session.selectedApps.map((a) => a.appID)
      this.modelID = session.modelID
      // 恢复目标会话的运行状态，使其与 UI 呈现一致
      this.running = session.running
      // 容错：刷新/重连后原运行中消息可能仍带 running，统一收别为失败
      this.repairSession()
    },
    deleteSession(id: string) {
      const idx = this.sessions.findIndex((s) => s.id === id)
      if (idx === -1) return
      this.sessions.splice(idx, 1)
      if (this.currentSessionId === id) {
        if (this.sessions.length > 0) {
          this.switchSession(this.sessions[0].id)
        } else {
          this.createSession()
        }
      }
    },
    /** 把当前运行态（messages / selectedApps / modelID / title）同步回当前会话。 */
    syncSession(refreshTimestamp = true) {
      if (!this.currentSessionId) return
      const session = this.sessions.find((s) => s.id === this.currentSessionId)
      if (!session) return
      // 保留引用：运行中消息对象（如 reactive 的 assistantMsg）需要持续更新到会话，
      // 深克隆会切断响应式连接，导致切换会话后丢失后续编排事件。
      session.messages = this.messages
      session.selectedApps = JSON.parse(JSON.stringify(this.selectedApps))
      session.modelID = this.modelID
      session.running = this.running
      if (refreshTimestamp) session.updatedAt = Date.now()
      // 用最近一次用户目标自动刷新会话标题
      const lastUser = [...this.messages].reverse().find((m) => m.role === 'user' && m.goal)
      if (lastUser?.goal) {
        session.goal = lastUser.goal
        const short = lastUser.goal.trim().replace(/\s+/g, ' ')
        session.title = short.length > 30 ? `${short.slice(0, 30)}…` : short
      }
    },
    // ── L5：授权管理 ──
    addDirectoryGrant(grant: DirectoryGrant) {
      // 同路径覆盖（去重）
      this.directoryGrants = this.directoryGrants.filter((g) => g.path !== grant.path)
      this.directoryGrants.push(grant)
    },
    removeDirectoryGrant(id: string) {
      this.directoryGrants = this.directoryGrants.filter((g) => g.id !== id)
    },
    addCommandGrant(grant: CommandGrant) {
      this.commandGrants = this.commandGrants.filter((g) => g.command !== grant.command)
      this.commandGrants.push(grant)
    },
    removeCommandGrant(id: string) {
      this.commandGrants = this.commandGrants.filter((g) => g.id !== id)
    },
    // ── L5：审计日志 ──
    addAudit(entry: LocalAuditEntry) {
      this.auditLogs.unshift(entry)
      // 限制审计日志条数，避免无限增长
      if (this.auditLogs.length > 200) this.auditLogs = this.auditLogs.slice(0, 200)
    },
    clearAudit() {
      this.auditLogs = []
    },
  },
  // 持久化偏好类字段 + Agent 会话列表（含消息）；运行态(running/auditLogs)不持久化。
  persist: {
    key: 'agentbrook-agent',
    storage: localStorage,
    // pinia-plugin-persistedstate v3 使用 paths（点路径）
    paths: ['modelID', 'directoryGrants', 'commandGrants', 'sessions', 'currentSessionId'],
  } as any,
})
