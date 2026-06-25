import { defineStore } from 'pinia'
import type {
  SessionInfo,
  ChatMessage,
  AppInfo,
  SessionStatusInfo,
  StreamContext,
} from '@/types/chat'
import * as sessionApi from '@/services/session'
import * as chatApi from '@/services/chat'
import { sessionCache, messageCache } from '@/utils/cache'
import { normalizeRecord, mergeRecords } from '@/utils/process'

/**
 * Simple stable 8-char hash for deterministic fallback IDs. Same input → same
 * output, so the same chat-log entry produces the same fallback id across
 * repeated getChatList calls (which keeps retry/reload's ID-based lookups
 * stable after a refresh).
 */
function stableHash(input: string): string {
  let h1 = 0xdeadbeef ^ 0
  let h2 = 0x41c6ce57 ^ 0
  for (let i = 0; i < input.length; i++) {
    const ch = input.charCodeAt(i)
    h1 = Math.imul(h1 ^ ch, 2654435761)
    h2 = Math.imul(h2 ^ ch, 1597334677)
  }
  h1 = Math.imul(h1 ^ (h1 >>> 16), 2246822507) ^ Math.imul(h2 ^ (h2 >>> 13), 3266489909)
  h2 = Math.imul(h2 ^ (h2 >>> 16), 2246822507) ^ Math.imul(h1 ^ (h1 >>> 13), 3266489909)
  const hash = 4294967296 * (2097151 & h2) + (h1 >>> 0)
  return hash.toString(16).padStart(8, '0').slice(-8)
}

function parseChatLog(log: any): ChatMessage | null {
  if (!log) return null

  const role = String(log.Role || '').toLowerCase() as ChatMessage['role']
  if (role !== 'user' && role !== 'assistant' && role !== 'system') return null

  let content = ''
  if (log.ContentToGptMsg && typeof log.ContentToGptMsg === 'object') {
    content = log.ContentToGptMsg.content || ''
  }
  if (!content && typeof log.Content === 'string') {
    const trimmed = log.Content.trim()
    // Only attempt JSON parse when it actually looks like a JSON object/array.
    // Previously any string was JSON.parsed and on failure used as raw content,
    // which leaked raw JSON into the bubble when Content was JSON without a
    // `content` field.
    if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
      try {
        const parsed = JSON.parse(log.Content)
        if (parsed && typeof parsed === 'object' && 'content' in parsed) {
          content = parsed.content || ''
        } else {
          content = log.Content
        }
      } catch {
        content = log.Content
      }
    } else {
      content = log.Content
    }
  }

  // Deterministic fallback id so the same logical message yields the same id
  // across repeated fetches. This keeps retry/reload's ID-based lookups valid
  // even after a refresh replaces the message list. (Previously a random id
  // broke retry after refresh.)
  const sid = log.ChatSessionID || ''
  const ts = log.CreateTime || ''
  const contentKey = stableHash(typeof content === 'string' ? content.slice(0, 256) : '')
  return {
    id: log.ChatLogID || `log_${sid || 'unknown'}_${role}_${ts}_${contentKey}`,
    sessionId: sid,
    role,
    content,
    createdAt: ts || new Date().toISOString(),
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
  /**
   * Reactive mirror of which sessions currently have an active local SSE
   * stream. UI reads this to show a streaming indicator. The full
   * (non-reactive) StreamContext lives in the module-level `streamContexts`
   * Map because AbortController must stay a plain object.
   */
  streamingSessions: Record<string, boolean>
  /**
   * Authoritative local terminal-status signal, keyed by sessionId → 0 (success)
   * or -1 (failed). Written by the SSE terminal handler the moment a terminal
   * frame arrives, BEFORE the stream's finalizeStream removes the session from
   * runningSessionIds.
   *
   * This exists to fix a status-desync race: previously the heartbeat guard and
   * the fetchSessions re-apply both judged "is this session locally finished?"
   * by checking `!runningSessionIds.includes(id)`. But the terminal handler
   * writes SessionStatus to 0/-1 while the session is STILL in
   * runningSessionIds (it's only removed on finalizeStream, up to ~15s later).
   * During that window the heartbeat overwrote the local terminal status with
   * a lagged server "running" (1), making the status dot flicker back to blue.
   * Reading this map instead closes the race because it is set immediately on
   * the terminal frame, independent of runningSessionIds timing.
   *
   * Cleared by finalizeStream (after the status has been persisted via
   * fetchSessions) and by clearAllSessions.
   */
  locallyTerminalStatus: Record<string, number>
  /**
   * Re-entrancy guard for selectSession. Each call increments this and
   * captures its value; after every await it checks whether it is still the
   * latest call. If a newer selectSession started (user clicked another
   * session), the stale call aborts WITHOUT touching this.messages —
   * preventing the race where currentSessionId and this.messages briefly
   * belong to different sessions, which caused cross-session message
   * contamination on rapid switching.
   */
  selectSessionToken: number
}

/**
 * Module-level (non-reactive) registry of per-session stream contexts. Keyed
 * by sessionId. When a stream's real SessionID is only learned from the first
 * SSE frame, the context is migrated from a temporary key to the real one.
 */
const streamContexts = new Map<string, StreamContext>()

/**
 * In-flight dedup for fetchSessions(1). Multiple callers (mount, post-SSE,
 * first SSE frame, heartbeat) frequently fire fetchSessions(1) concurrently;
 * without dedup the last-resolving call wins on `this.sessions` and the
 * localCompleted snapshot is computed from inconsistent intermediate states.
 * Concurrent calls for page 1 share the same promise.
 */
let fetchSessionsPromise: Promise<void> | null = null

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
    streamingSessions: {},
    locallyTerminalStatus: {},
    selectSessionToken: 0,
  }),

  getters: {
    currentSession(): SessionInfo | undefined {
      return this.sessions.find((s) => s.ChatSessionID === this.currentSessionId)
    },
    hasRunningSessions(): boolean {
      return this.runningSessionIds.length > 0
    },
    /** True if any local SSE stream is active. */
    anyStreaming(): boolean {
      return Object.keys(this.streamingSessions).length > 0
    },
  },

  actions: {
    // ------------------------------------------------------------------
    // Per-session stream registry
    // ------------------------------------------------------------------

    /** Whether a local SSE stream is active for the given session. */
    isStreaming(sessionId: string | null | undefined): boolean {
      if (!sessionId) return false
      return !!this.streamingSessions[sessionId]
    },

    /**
     * Register a new in-flight stream. The context is keyed by `key` (which
     * may be a temp key for new chats). `addRunningSession` is called for the
     * sid and tracked in ctx.registeredSids so unregistration can remove it
     * later even if the SSE sid changes.
     */
    registerStream(key: string, ctx: StreamContext) {
      streamContexts.set(key, ctx)
      this.streamingSessions = { ...this.streamingSessions, [key]: true }
      if (key && !ctx.registeredSids.includes(key)) {
        ctx.registeredSids.push(key)
      }
      this.addRunningSession(key)
    },

    /**
     * Move a stream context from one key to another (e.g. temp key → real
     * SessionID learned from the first SSE frame). Updates runningSessionIds
     * so both sids stay consistent. After migration the old key's stream
     * registration is cleared; the context is reachable under the new key.
     */
    migrateStream(oldKey: string, newKey: string) {
      if (!newKey || oldKey === newKey) return
      const ctx = streamContexts.get(oldKey)
      if (!ctx) return
      streamContexts.delete(oldKey)
      streamContexts.set(newKey, ctx)
      const next = { ...this.streamingSessions }
      delete next[oldKey]
      next[newKey] = true
      this.streamingSessions = next
      if (!ctx.registeredSids.includes(newKey)) {
        ctx.registeredSids.push(newKey)
      }
      this.addRunningSession(newKey)
      // The old sid is no longer the "real" running sid; remove it from the
      // running list so the heartbeat tracks the correct one.
      this.removeRunningSession(oldKey)
    },

    /** Look up the (non-reactive) stream context for a key. */
    getStreamContext(key: string): StreamContext | undefined {
      return streamContexts.get(key)
    },

    /**
     * Find an active stream context associated with a session, tolerating the
     * temp-key window. For a brand-new chat the stream is registered under the
     * `aiMsgId` temp key until the first SSE frame migrates it to the real sid
     * (see migrateStream). During that window `isStreaming(realSid)` and
     * `getStreamContext(realSid)` return false even though a live stream exists
     * — which makes selectSession's re-inject guard (chat.ts:491) skip the live
     * AI message and the reply block disappears when the user clicks the new
     * session mid-stream (reproduces on Mac, not Windows, due to WKWebView vs
     * WebView2 timing differences). This scans all contexts and matches by:
     *   - exact key == sessionId (already migrated), OR
     *   - ctx.originSid == sessionId (stream sent with a known sid), OR
     *   - ctx.aiMessage?.sessionId == sessionId (first frame already set it).
     * It does NOT match a context whose aiMessage.sessionId is empty on a
     * foreign sid (that would attribute ANY brand-new orphan stream to the
     * clicked session — cross-session contamination).
     */
    findStreamContextBySession(sessionId: string | null | undefined): StreamContext | undefined {
      if (!sessionId) return undefined
      const exact = streamContexts.get(sessionId)
      if (exact) return exact
      for (const [, ctx] of streamContexts) {
        if (ctx.originSid === sessionId) return ctx
        const liveSid = ctx.aiMessage?.sessionId
        if (liveSid && liveSid === sessionId) return ctx
      }
      // 兜底：未迁移的临时 key 流（新会话第一帧 SSE 带 SessionID 之前）。此时流
      // 挂在 aiMsgId 临时 key 下，originSid=''、aiMessage.sessionId=''，上面的
      // 精确匹配都落空——这正是 Mac 上"点击新会话时第一帧还没到"的窗口，若不兜
      // 底则 selectSession 的 re-inject 被跳过、AI 回复区块消失。
      //
      // 不能无条件把任意孤儿临时 key 流匹配给点击的会话（会把 A 会话的流错配给
      // B 会话 → 跨会话污染）。安全判据：该临时流的 aiMsgId 恰好存在于当前
      // this.messages 里（selectSession 已为本次 sessionId 重建好列表，且 live
      // 占位消息就在其中）。用一个明确属于本会话列表的 aiMsgId 作为锚点，才能
      // 确定这个未迁移的临时 key 流属于当前 sessionId。
      const liveMsgIds = new Set(this.messages.map((m) => m.id))
      for (const [, ctx] of streamContexts) {
        if (ctx.aiMsgId && liveMsgIds.has(ctx.aiMsgId) && !ctx.originSid) {
          return ctx
        }
      }
      return undefined
    },

    /**
     * Unregister a stream. Removes every sid that was registered for this
     * stream from runningSessionIds (fixes the leak when the SSE sid differed
     * from the requested sid). The streamingSessions flag is cleared for the
     * key.
     */
    unregisterStream(key: string) {
      const ctx = streamContexts.get(key)
      streamContexts.delete(key)
      const next = { ...this.streamingSessions }
      delete next[key]
      this.streamingSessions = next
      if (ctx) {
        for (const sid of ctx.registeredSids) {
          this.removeRunningSession(sid)
        }
      }
    },

    /** Cancel and unregister every active stream (e.g. on unmount). */
    cancelAllStreams() {
      for (const [, ctx] of streamContexts) {
        try {
          ctx.abortController.abort()
        } catch { /* ignore */ }
      }
      streamContexts.clear()
      this.streamingSessions = {}
    },

    // ------------------------------------------------------------------
    // Local terminal-status signal (authoritative, independent of
    // runningSessionIds timing — see ChatState.locallyTerminalStatus doc)
    // ------------------------------------------------------------------

    /** Record that a session reached a terminal state locally (0=success, -1=failed). */
    markLocallyTerminal(sessionId: string, status: number) {
      if (!sessionId) return
      this.locallyTerminalStatus = { ...this.locallyTerminalStatus, [sessionId]: status }
    },

    /** Drop the local terminal signal for a session (after it's been persisted). */
    clearLocallyTerminal(sessionId: string) {
      if (!sessionId) return
      if (this.locallyTerminalStatus[sessionId] === undefined) return
      const next = { ...this.locallyTerminalStatus }
      delete next[sessionId]
      this.locallyTerminalStatus = next
    },

    clearAllLocallyTerminal() {
      this.locallyTerminalStatus = {}
    },

    // ------------------------------------------------------------------
    // Session list
    // ------------------------------------------------------------------

    async fetchSessions(page = 1, size = 50) {
      // Dedup concurrent page-1 fetches so the localCompleted snapshot is
      // computed from a single consistent state and we don't fire redundant
      // network calls. Page>1 (pagination) is not deduped.
      if (page === 1 && fetchSessionsPromise) {
        return fetchSessionsPromise
      }
      const run = this._doFetchSessions(page, size)
      if (page === 1) {
        fetchSessionsPromise = run.finally(() => {
          fetchSessionsPromise = null
        })
        return fetchSessionsPromise
      }
      return run
    },

    async _doFetchSessions(page = 1, size = 50) {
      this.loadingSessions = true
      try {
        const { data } = await sessionApi.getSessionList(page, size)
        if (data.Success && data.Data) {
          const list = data.Data.Data || []
          this.sessionsTotal = data.Data.total || 0
          if (page === 1) {
            // Before the server data overwrites this.sessions, capture the
            // authoritative local terminal statuses from `locallyTerminalStatus`
            // (set by the SSE terminal handler) plus any session that's already
            // terminal AND no longer running. The server DB often lags behind
            // the local terminal decision, so without re-applying these, a
            // just-finished session flips back to "running".
            //
            // We snapshot from TWO sources:
            //  - locallyTerminalStatus (authoritative, set on terminal frame)
            //  - sessions already at 0/-1 and out of runningSessionIds
            //    (covers sessions that finished in a prior app run / via
            //     heartbeat agreement and were never re-tracked)
            const terminalSnapshot = new Map<string, number>(
              Object.entries(this.locallyTerminalStatus),
            )
            for (const s of this.sessions) {
              if (
                (s.SessionStatus === 0 || s.SessionStatus === -1) &&
                !this.runningSessionIds.includes(s.ChatSessionID) &&
                !terminalSnapshot.has(s.ChatSessionID)
              ) {
                terminalSnapshot.set(s.ChatSessionID, s.SessionStatus)
              }
            }

            this.sessions = list

            // Re-apply running status for sessions still in the running list
            // (server may have reported them as 0 due to lag).
            for (const id of this.runningSessionIds) {
              const s = this.sessions.find((ss) => ss.ChatSessionID === id)
              if (s && s.SessionStatus === 0) {
                s.SessionStatus = 1
              }
            }

            // Re-apply the locally-known terminal status EXACTLY (0 success or
            // -1 failed). Previously this hardcoded 0, which turned a failed
            // session's red dot into a green "completed" dot after a refresh.
            for (const [id, status] of terminalSnapshot) {
              const s = this.sessions.find((ss) => ss.ChatSessionID === id)
              if (s) {
                s.SessionStatus = status
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
      // Re-entrancy guard: capture a token. After each await, if a newer
      // selectSession started, this (stale) call aborts without touching
      // this.messages — preventing the race where currentSessionId and
      // this.messages briefly belong to different sessions (the root cause of
      // cross-session message contamination on rapid switching).
      this.selectSessionToken++
      const myToken = this.selectSessionToken

      // Persist the OUTGOING session's messages before switching — but only if
      // this.messages actually belongs to the outgoing session. Previously
      // there was no invariant tying this.messages to currentSessionId, so a
      // rapid double-switch could write one session's messages under another
      // session's cache key (cache poisoning). If the outgoing session still
      // has an active stream, its authoritative aiMessage lives in the stream
      // context and will be persisted by the stream itself on completion, so
      // we skip persisting here to avoid clobbering live data.
      const oldSid = this.currentSessionId
      if (oldSid && oldSid !== sessionId && this.messages.length > 0) {
        // findStreamContextBySession tolerates the temp-key window (new chat
        // whose stream hasn't migrated to its real sid yet); isStreaming(oldSid)
        // would return false in that window and let us clobber the live stream's
        // authoritative cache here.
        if (!this.findStreamContextBySession(oldSid)) {
          messageCache.set(oldSid, [...this.messages]).catch(() => {})
        }
      }

      this.currentSessionId = sessionId
      this.loadingMessages = true

      // Load cached messages first (may contain process data from a previous live SSE session)
      let cachedMessages: ChatMessage[] = []
      try {
        cachedMessages = await messageCache.getBySession(sessionId)
        // Abort if a newer selectSession superseded this one during the await.
        if (myToken !== this.selectSessionToken) return
        if (cachedMessages.length > 0) {
          this.messages = cachedMessages
        }
      } catch { /* ignore */ }

      try {
        // 1. Load chat messages from API
        const { data } = await chatApi.getChatList(sessionId)
        // Abort if a newer selectSession superseded this one during the await.
        if (myToken !== this.selectSessionToken) return
        if (data.Success && data.Data) {
          const rawList = data.Data as any[]
          const apiMessages = rawList.map(parseChatLog).filter((m): m is ChatMessage => m !== null)

          // Preserve process data from cache by order.
          // SSE-created messages use temporary IDs (ai_xxx) while API uses real
          // ChatLogIDs, so we match by position rather than by ID.
          if (cachedMessages.length > 0) {
            const cachedWithProcess = cachedMessages.filter(
              // 排除所有合成消息（pending_ 占位、err_fallback_ 兜底）——它们不应
              // 参与 order-merge，否则会消耗 assistant 槽位、把 process 错配到错误
              // 消息，并导致流式会话切换回来时正在执行的 AI 回复区域消失。
              (m) => m.process && !m.id.startsWith('pending_') && !m.id.startsWith('err_fallback_'),
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
              const cached = cachedWithProcess[i]
              const processData = cached.process
              const apiIdx = apiAssistantIndices[i]
              if (processData) {
                this.messages[apiIdx] = {
                  ...this.messages[apiIdx],
                  process: processData,
                  // 工作流的最终文本存在执行记录的 Results 里，不在 chat-log
                  // 的 Content/ContentToGptMsg 字段，所以 parseChatLog 解析出的
                  // content 可能为空。此时用缓存（SSE 流式积累的）content 回填，
                  // 避免切换会话回来后最终回复内容丢失。API 有值时优先用 API。
                  content: this.messages[apiIdx].content || cached.content || '',
                }
              }
            }

            // When the workflow hasn't produced a chat-log entry yet (still
            // running), the API may return fewer assistant messages than what
            // the cache holds. Append the extra cached messages so the active
            // SSE stream can continue to update them and the workflow tree
            // remains visible.
            if (cachedWithProcess.length > apiAssistantIndices.length) {
              const existingIds = new Set(this.messages.map((m) => m.id))
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

        // For a session with an ACTIVE SSE stream, the rebuild above (cache +
        // API) cannot contain the in-progress assistant message: the cache
        // write was skipped while streaming (chat.ts:371 guard), and the API
        // likely hasn't persisted the chat-log row yet. Without this message,
        // the AI reply area disappears and SSE's getAssistantMsg can't rebind
        // to a rendered object (so the workflow tree never updates).
        //
        // Re-inject the stream's live aiMessage so the bubble renders and SSE
        // keeps mutating the exact same object reference (identity preserved →
        // no detach, no freeze). loadSessionExecutionRecords is still skipped
        // for this session (its isStreaming guard below), so it won't clobber
        // the live process data. The cache is intentionally NOT re-written
        // here — the stream owns it and persists on completion.
        //
        // We look up the stream context via findStreamContextBySession instead
        // of isStreaming(sessionId)+getStreamContext(sessionId). For a NEW chat
        // the stream is registered under the aiMsgId temp key until the first
        // SSE frame calls migrateStream; during that window the real sid key is
        // absent, so the old `isStreaming(sessionId)` guard skipped the push
        // and the reply block vanished when the user clicked the new session
        // mid-stream (Mac repro). findStreamContextBySession matches the still-
        // temp-keyed context by originSid / aiMessage.sessionId, closing it.
        const streamCtx = this.findStreamContextBySession(sessionId)
        if (streamCtx) {
          const live = streamCtx.aiMessage
          // Validate the injected aiMessage actually belongs to this session
          // (sessionId matches, or is empty for a brand-new chat before the
          // first frame resolves the sid). A poisoned ctx.aiMessage (e.g. a
          // rebind that grabbed a foreign session's message during a race)
          // would otherwise inject another session's content here.
          const liveSid = live?.sessionId
          const sidOk = !liveSid || liveSid === sessionId
          if (live && sidOk) {
            // 定位本流在当前 messages 里的 assistant 槽位（按 streamCtx.
            // assistantIndex，回落到"最后一个 loading/无 process 的 assistant"）。
            // 服务端常在流式中就已持久化 assistant 行（真实 ChatLogID），它和 live
            // 的 aiMsgId 占位 id 不同——若直接 push 会产生两条 AI 回复区块。这里
            // 改为把 live 的 SSE 数据合并进已有槽位并把流重绑到它；仅当没有任何
            // 槽位时才 push 占位。合并写法复用 getAssistantMsg / mergeSSEMessageIntoCache。
            const nonPendingAssistants = this.messages.filter(
              (m) => m.role === 'assistant' && !m.id.startsWith('pending_'),
            )
            let slot: ChatMessage | undefined
            const ai = streamCtx.assistantIndex
            if (ai >= 0 && ai < nonPendingAssistants.length) {
              slot = nonPendingAssistants[ai]
            }
            if (!slot) {
              for (let i = this.messages.length - 1; i >= 0; i--) {
                const m = this.messages[i]
                if (m.role === 'assistant' && (m.loading || !m.process)) {
                  slot = m
                  break
                }
              }
            }
            if (slot && slot.id !== live.id) {
              if (live.process) {
                if (!slot.process) {
                  slot.process = live.process
                } else {
                  slot.process.records = mergeRecords(
                    slot.process.records || [],
                    live.process.records || [],
                  )
                  slot.process.streamsByNode = {
                    ...(slot.process.streamsByNode || {}),
                    ...(live.process.streamsByNode || {}),
                  }
                  const sseS = live.process.status
                  if (sseS && sseS !== 'running') slot.process.status = sseS
                }
                if (live.content) slot.content = slot.content || live.content
                if (live.sessionId) slot.sessionId = live.sessionId
              }
              // 重绑流到该槽位：getAssistantMsg 的重绑分支（已放宽过）会把 slot
              // 重新绑为 aiMessage 并把 liveMsgId 设为 slot.id，后续 SSE 直接写它。
              streamCtx.aiMessage = slot
            } else if (!slot && !this.messages.some((m) => m.id === live.id)) {
              this.messages.push(live)
            }
          }
        }

        // Abort if a newer selectSession superseded this one before the heavy
        // load below — don't run execution records / backfill for a stale session.
        if (myToken !== this.selectSessionToken) return

        // Load execution records so completed/failed sessions show their workflow
        // tree. BUT skip for sessions with an active SSE stream: the stream is the
        // authoritative, live source of process data for those sessions, and
        // loadSessionExecutionRecords would overwrite the live records/outputs
        // with a potentially-lagged server snapshot — clobbering the workflow
        // execution result after a switch-back. When the stream finishes
        // (unregisterStream clears streamingSessions), the next selectSession /
        // refresh will load records normally.
        if (!this.isStreaming(sessionId)) {
          await this.loadSessionExecutionRecords(sessionId)
        }
      } catch {
        // getChatList / loadSessionExecutionRecords failures are non-fatal: the
        // cached messages (loaded above) are still shown. Swallow so the finally
        // block below still runs the backfill — otherwise a network/HTTP error
        // here would skip ensureProcess and leave failed replies process-less.
      } finally {
        // If a newer selectSession superseded this call, do NOT run the
        // backfill or clear loadingMessages here — the newer call owns those.
        if (myToken !== this.selectSessionToken) return
        // Unconditionally guarantee every assistant message has a process so
        // the workflow area ALWAYS renders — even for failed/historical replies
        // whose cache had no process, whose execution-records API returned
        // empty, OR when getChatList threw above (caught). This MUST be in the
        // finally so it runs on EVERY exit path, matching "every AI reply must
        // show its workflow block (so users can retry failed nodes)".
        try {
          this.ensureProcessForAssistantMessages(sessionId)
        } catch { /* ignore */ }
        this.loadingMessages = false
      }
    },

    /**
     * Fetch workflow execution records for a session and attach them
     * to the corresponding assistant messages as `process` data.
     *
     * Matching is done by order: the i-th process corresponds to the
     * i-th assistant message that does not already have process data.
     *
     * Messages that are currently receiving live SSE updates (loading=true)
     * are skipped for the moment — their data is live and richer — but the
     * unmatched process slot is deferred (returned) so the caller / stream
     * can backfill it once the stream finishes, rather than being silently
     * dropped as before.
     */
    async loadSessionExecutionRecords(sessionId: string): Promise<number> {
      try {
        const { data } = await chatApi.getSessionExecutionRecords(sessionId)
        if (!data.Success || !data.Data) return 0

        let processList = data.Data as any[]
        if (!Array.isArray(processList) || processList.length === 0) return 0

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
        let deferredCount = 0
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

          // Skip when this message is receiving live SSE updates — its data is
          // live and more current than the execution-records snapshot.
          // Messages with loading=true are mid-stream (sendMessage sets it);
          // they are updated by the SSE flush path, not here.
          if (msg.process && msg.loading) {
            deferredCount++
            continue
          }

          // Apply the authoritative execution-records data IN PLACE (mutate the
          // existing object's `process` field) rather than creating a new
          // object via `this.messages[msgIndex] = {...}`.
          //
          // This is load-bearing for the switch-back workflow-freeze fix: the
          // SSE stream holds a reference to this exact object (via
          // getAssistantMsg's rebind). Creating a new object would detach that
          // reference, so subsequent SSE flushes would silently no-op against a
          // stale object and the workflow tree would freeze. In-place mutation
          // preserves object identity, so the live stream keeps updating the
          // same object the UI renders.
          msg.process = {
            status: proc.Status || msg.process?.status || 'success',
            results: proc.Results || msg.process?.results || '',
            timestamp: Date.now(),
            records,
            streamsByNode: existingStreams,
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

        // Return how many process slots were deferred (i.e. skipped because a
        // message is mid-stream). Callers / the active stream can use this to
        // know there is authoritative data waiting to be backfilled.
        return deferredCount
      } catch {
        // Silently ignore — execution records are an enhancement
        return 0
      }
    },

    /**
     * Guarantee every non-synthetic assistant message in `this.messages` has a
     * `process` field, so the workflow area (ProcessStatus) ALWAYS renders —
     * even for historical/failed replies whose cache had no process and whose
     * server returned no execution records.
     *
     * This runs UNCONDITIONALLY (independent of loadSessionExecutionRecords's
     * early-returns or the isStreaming skip), after every message load path
     * (selectSession, refreshCurrentSessionMessages). Status is inferred from
     * the session's SessionStatus so the badge is meaningful.
     */
    ensureProcessForAssistantMessages(sessionId: string) {
      const session = this.sessions.find((s) => s.ChatSessionID === sessionId)
      const inferredStatus = session?.SessionStatus === -1 ? 'failed' : 'success'
      let changed = false

      // 兜底：失败会话且完全没有任何 AI 回复消息时，创建一个合成消息
      // 让用户至少能看到红色"出错了"的工作流区域（而非空白对话）。
      // 不缓存此兜底消息，避免污染后续加载时的缓存合并逻辑。
      const fallbackId = `err_fallback_${sessionId}`
      const realAssistantMsg = this.messages.some(
        (m) => m.role === 'assistant' && m.id !== fallbackId,
      )
      if (!realAssistantMsg && inferredStatus === 'failed') {
        // 清理可能残留的旧 fallback
        this.messages = this.messages.filter((m) => m.id !== fallbackId)
        this.messages.push({
          id: fallbackId,
          sessionId,
          role: 'assistant',
          content: '',
          createdAt: new Date().toISOString(),
          loading: false,
          process: {
            status: 'failed',
            results: '',
            timestamp: null,
            records: [],
            streamsByNode: {},
          },
        })
        // 不设置 changed=true —— 纯兜底不写入缓存
      }

      for (const msg of this.messages) {
        if (msg.role === 'assistant' && !msg.id.startsWith('pending_') && !msg.id.startsWith('err_fallback_') && !msg.process) {
          msg.process = {
            status: inferredStatus,
            results: '',
            timestamp: null,
            records: [],
            streamsByNode: {},
          }
          changed = true
        }
      }
      if (changed) {
        messageCache.set(sessionId, [...this.messages]).catch(() => {})
      }
    },

    async deleteSession(id: string) {
      // Cancel any active stream for this session first.
      const ctx = streamContexts.get(id)
      if (ctx) {
        try {
          ctx.abortController.abort()
        } catch { /* ignore */ }
        this.unregisterStream(id)
      }
      await sessionApi.deleteSession(id)
      this.sessions = this.sessions.filter((s) => s.ChatSessionID !== id)
      this.sessionsTotal = Math.max(0, this.sessionsTotal - 1)
      try {
        await messageCache.clearSession(id)
      } catch { /* ignore */ }
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
      // id 未命中：直接 no-op，不去找别的 slot 合并、也不 push。
      // getAssistantMsg 的 Path A 与重绑分支返回的都是 this.messages 数组里的实际
      // 引用，并在返回前已就地写入 process/content；liveMsgId 也会在同一次调用里
      // 被重绑为真实 slot id。因此当 messageId miss（多为 selectSession 把临时
      // ai_<ts> 换成真实 ChatLogID 后的陈旧 id）时，真实渲染对象已是最新——无需
      // 再去找别的 slot 合并（会把本流结果写到错误的行 → 无结果输出），也不需 push
      // （会产生重复 AI 区块）。保持静默即可。
    },

    /**
     * Update a message that lives in a specific session's message list. If the
     * session is the currently-viewed one, this.messages is updated reactively;
     * otherwise the change is persisted to that session's cache so it is
     * visible when the user navigates back. This replaces the old pattern of
     * only being able to update the currently-displayed messages.
     */
    async updateMessageInSession(sessionId: string, messageId: string, updater: (m: ChatMessage) => ChatMessage) {
      if (this.currentSessionId === sessionId) {
        const idx = this.messages.findIndex((m) => m.id === messageId)
        if (idx !== -1) {
          this.messages[idx] = updater(this.messages[idx])
          messageCache.set(sessionId, [...this.messages]).catch(() => {})
        }
        return
      }
      // Background session: read cache, update, write back.
      try {
        const cached = await messageCache.getBySession(sessionId)
        const idx = cached.findIndex((m) => m.id === messageId)
        if (idx !== -1) {
          cached[idx] = updater(cached[idx])
          await messageCache.set(sessionId, cached)
        }
      } catch { /* ignore */ }
    },

    addMessage(message: ChatMessage) {
      this.messages.push(message)
    },

    addRunningSession(sessionId: string) {
      if (!sessionId || this.runningSessionIds.includes(sessionId)) return
      this.runningSessionIds.push(sessionId)
      const session = this.sessions.find((s) => s.ChatSessionID === sessionId)
      if (session) session.SessionStatus = 1
    },

    removeRunningSession(sessionId: string) {
      this.runningSessionIds = this.runningSessionIds.filter((id) => id !== sessionId)
      const session = this.sessions.find((s) => s.ChatSessionID === sessionId)
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

      // 如果有消息正在 SSE 流式写入（loading=true），跳过刷新避免竞态覆盖。
      // 另外只要本会话有活跃 SSE 流就跳过：selectSession 重建期间 this.messages
      // 可能短暂全是 API 消息（无 loading 标志），此时心跳触发本方法会整体
      // this.messages = apiMessages 覆盖，把 re-inject 的 live AI 占位抹掉
      // （Mac 时序下可观测到）。
      if (this.messages.some((m) => m.loading)) return
      if (this.findStreamContextBySession(sid)) return

      try {
        const { data } = await chatApi.getChatList(sid)
        if (!data.Success || !data.Data) return

        const rawList = data.Data as any[]
        const apiMessages = rawList.map(parseChatLog).filter((m): m is ChatMessage => m !== null)

        // Preserve process data from current non-synthetic messages by order.
        // SSE-created messages use temporary IDs (ai_xxx) while API uses real
        // ChatLogIDs, so we match by position rather than by ID.
        const currentWithProcess = this.messages.filter(
          (m) => m.process && !m.id.startsWith('pending_') && !m.id.startsWith('err_fallback_'),
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
          const current = currentWithProcess[i]
          const processData = current.process
          const apiIdx = apiAssistantIndices[i]
          if (processData) {
            this.messages[apiIdx] = {
              ...this.messages[apiIdx],
              process: processData,
              // 工作流的最终文本在执行记录 Results 里，不在 chat-log Content。
              // parseChatLog 解析出的 content 可能为空，此时用当前（SSE 积累
              // 的）content 回填，避免心跳刷新覆盖掉最终回复内容。
              content: this.messages[apiIdx].content || current.content || '',
            }
          }
        }

        await messageCache.set(sid, [...this.messages])

        // Reload execution records to pick up any new data — but not while an
        // active SSE stream owns this session's process data (it would
        // overwrite live records with a lagged server snapshot).
        if (!this.isStreaming(sid)) {
          await this.loadSessionExecutionRecords(sid)
        }
        // Guarantee every assistant message has a process (workflow area
        // always renders), regardless of what the execution-records API
        // returned. Mirrors selectSession's post-load backfill.
        this.ensureProcessForAssistantMessages(sid)
      } catch {
        // Silently ignore
      }
    },

    updateSessionStatusFromHeartbeat(list: SessionStatusInfo[]): SessionStatusInfo[] {
      const completedList: SessionStatusInfo[] = []
      for (const item of list) {
        // 守卫：如果该会话有活跃的 SSE 流，或本地仍认为它在运行（runningSessionIds），
        // 且服务端报告终结状态（非 1），则整体跳过。
        // ClawAI 异步并行模式下，主节点 TaskInfo.State=Completed 但子工作流仍在运行，
        // 服务端会错误地报告 SessionStatus=0，导致会话状态在"运行中"与"已完成"之间
        // 反复闪烁。扩展守卫：除了 SSE 流活跃的会话外，也保护页面刷新后 SSE 流已断开
        // 但本地仍追踪为 runningSessionIds 的会话。
        const isLocallyRunning =
          this.isStreaming(item.ChatSessionID) ||
          this.runningSessionIds.includes(item.ChatSessionID)

        if (isLocallyRunning && item.SessionStatus !== 1) {
          continue
        }

        const session = this.sessions.find((s) => s.ChatSessionID === item.ChatSessionID)
        if (session) {
          // Authority direction: a locally-known terminal state takes
          // precedence over a lagged server "running" report.
          //
          // The authoritative source is `locallyTerminalStatus`, which is set
          // the moment the SSE terminal frame arrives — BEFORE the session is
          // removed from runningSessionIds (that only happens on
          // finalizeStream, up to ~15s later). Reading this map (not
          // runningSessionIds membership) closes the race where the heartbeat
          // overwrote a locally-set terminal status back to 1 during the
          // terminal→finalize window — the visible symptom was the status dot
          // flickering from terminal back to blue for a switched-away session.
          const localTerminal = this.locallyTerminalStatus[item.ChatSessionID]
          if (localTerminal !== undefined && item.SessionStatus === 1) {
            // Server lags behind our local terminal decision; keep local status.
            // (If localTerminal is 0/-1 it stays; we do NOT overwrite with 1.)
          } else if (localTerminal !== undefined) {
            // Server agrees the session is terminal (0 or -1). Prefer the
            // server's value only when it's also terminal, so a server-side
            // retry that flips failed→success still propagates. But never let
            // it regress to running.
            session.SessionStatus = item.SessionStatus
          } else {
            session.SessionStatus = item.SessionStatus
          }
        }
        // 已完成或失败的会话，从运行列表移除
        if (item.SessionStatus !== 1) {
          this.removeRunningSession(item.ChatSessionID)
          completedList.push(item)
        }
      }
      return completedList
    },

    // ------------------------------------------------------------------
    // Bulk actions (used by UI components instead of direct state mutation)
    // ------------------------------------------------------------------

    /**
     * Clear all sessions, messages, and current selection. Cancels any active
     * streams and clears caches. Replaces the direct state mutation that used
     * to live in SessionList.vue (which bypassed actions, skipped cache
     * cleanup, and left streams running).
     */
    async clearAllSessions() {
      this.cancelAllStreams()
      this.sessions = []
      this.currentSessionId = null
      this.messages = []
      this.sessionsTotal = 0
      this.runningSessionIds = []
      this.streamingSessions = {}
      this.clearAllLocallyTerminal()
      try {
        await sessionCache.clear()
        await messageCache.clear()
      } catch { /* ignore */ }
    },

    /**
     * Reset state when the user switches the active App (starts a fresh chat
     * context). Cancels streams for the outgoing session. Replaces the direct
     * state mutation that used to live in AppLayout.vue.
     */
    resetToApp(appId: string) {
      // Cancel the outgoing session's stream if any.
      if (this.currentSessionId) {
        const ctx = streamContexts.get(this.currentSessionId)
        if (ctx) {
          try {
            ctx.abortController.abort()
          } catch { /* ignore */ }
          this.unregisterStream(this.currentSessionId)
        }
      }
      this.selectedAppId = appId
      this.currentSessionId = null
      this.messages = []
    },
  },
})
