import { onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '@/stores/chat'
import { useSettingsStore } from '@/stores/settings'
import { secureStorage } from '@/utils/storage'
import { getChatCompletionsUrl, getChatRegenerateUrl, getNodeExecutionRecordUrl, retryNode as retryNodeApi } from '@/services/chat'
import { createApiRequest, APP_SECRET } from '@/utils/crypto'
import { getAccessToken, refreshMemberToken } from '@/services/auth'
import router from '@/router'
import type {
  ChatMessage,
  SSEMessage,
  StreamEnvelopeItem,
  StreamByNode,
  NormalizedRecord,
  AttachmentItem,
} from '@/types/chat'
import { useFileUpload } from '@/composables/useFileUpload'
import { normalizeRecord, mergeRecords } from '@/utils/process'
import { messageCache } from '@/utils/cache'
import { useNotifications } from '@/composables/useNotifications'

const TOKEN_CHECK_ERROR = 80001
const MEMBER_TOKEN_CHECK_ERROR = 80002
const STREAM_UI_FLUSH_MS = 200
const STREAM_MAX_CHARS_PER_NODE = 200000
const STREAM_TAIL_CHARS = 1200
/** Max wall-clock silence on an SSE stream before the client aborts. Guards
 * against a server that sends a terminal-status frame but never closes the
 * connection (which would leave msg.loading=true forever). */
const STREAM_SILENCE_TIMEOUT_MS = 90_000
/** After a terminal-status frame, give the server this long to close the
 * stream before we abort on the client. */
const STREAM_FINAL_CLOSE_TIMEOUT_MS = 15_000

function stripMarkdown(text: string): string {
  return text
    .replace(/```[\s\S]*?```/g, '[代码]')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/\*\*([^*]+)\*\*/g, '$1')
    .replace(/\*([^*]+)\*/g, '$1')
    .replace(/#+\s/g, '')
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
    .replace(/!\[([^\]]*)\]\([^)]+\)/g, '[图片]')
    .replace(/[#*`\[\]()]/g, '')
    .trim()
}

export function useChat() {
  const chatStore = useChatStore()
  const settingsStore = useSettingsStore()
  const { t } = useI18n()
  const streamError = ref<string | null>(null)
  const { uploadFile } = useFileUpload()
  const { notifySessionCompleted } = useNotifications()

  // The global `isStreaming` ref and module-level `abortController` are GONE.
  // Per-session streaming state now lives in chatStore.streamingSessions and
  // the non-reactive streamContexts map (see stores/chat.ts). This composable
  // still exposes an `isStreaming` computed for backward-compat callers, but
  // it now reflects the *current* session's streaming state.
  const isStreaming = computed(() => chatStore.isStreaming(chatStore.currentSessionId))

  // --- SSE stream merge helpers ---

  function mergeStreamsByNode(
    prevStreams: Record<string, StreamByNode>,
    envelope: StreamEnvelopeItem[],
  ): Record<string, StreamByNode> {
    const next = { ...prevStreams }
    if (!Array.isArray(envelope)) return next
    for (const e of envelope) {
      if (!e || !e.nodeId) continue
      const nodeId = String(e.nodeId)
      const type = e.type
      const content = e.content != null ? String(e.content) : ''
      const ts = e.timestamp || 0
      const old = next[nodeId] || { text: '', tailText: '', status: 'running' as const, lastTimestamp: 0 }
      const updated = { ...old }

      if (type === 'delta') {
        updated.text = (updated.text || '') + content
        if (updated.text.length > STREAM_MAX_CHARS_PER_NODE) {
          updated.text = updated.text.slice(-STREAM_MAX_CHARS_PER_NODE)
        }
        if (updated.text.length <= STREAM_TAIL_CHARS) {
          updated.tailText = updated.text
        } else {
          updated.tailText = updated.text.slice(-STREAM_TAIL_CHARS)
        }
      } else if (type === 'done') {
        updated.status = 'done'
      }
      if (!updated.lastTimestamp || ts > updated.lastTimestamp) {
        updated.lastTimestamp = ts
      }
      next[nodeId] = updated
    }
    return next
  }

  function normalizeValue(v: any): string {
    if (v == null) return ''
    if (typeof v === 'string') return v
    if (Array.isArray(v)) return v.map(normalizeValue).filter(Boolean).join('\n\n')
    if (typeof v === 'object') {
      const pick = v.text ?? v.content ?? v.value
      if (typeof pick === 'string') return pick
      try {
        return '```json\n' + JSON.stringify(v, null, 2) + '\n```'
      } catch {
        return String(v)
      }
    }
    return String(v)
  }

  function pickBetterStatus(prev: string, next: string): string {
    const rank = (s: string) => {
      const v = String(s || '').toLowerCase()
      if (v === 'failed' || v === 'error') return 3
      if (v === 'success') return 2
      if (v === 'running') return 1
      return 0
    }
    return rank(next) >= rank(prev) ? next : prev
  }

  function getAttachmentType(fileName: string): string {
    const ext = fileName.split('.').pop()?.toLowerCase() || ''
    const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'bmp']
    const codeExts = ['json', 'xml', 'cs', 'js', 'html', 'css']
    if (imageExts.includes(ext)) return 'Image'
    if (codeExts.includes(ext)) return 'Code'
    return 'Document'
  }

  /**
   * Merge an SSE-accumulated assistant message into a cached message that
   * represents the same logical reply. Used when a background stream finishes
   * and we need to fold its data into the target session's cache WITHOUT
   * duplicating the assistant message. We prefer to update an existing slot
   * (matched by id, then by order) rather than appending.
   *
   * Returns a new array with the message merged in (or appended if no slot
   * matched). Does NOT mutate the input array.
   */
  function mergeSSEMessageIntoCache(
    cached: ChatMessage[],
    sseMsg: ChatMessage,
    sseMsgId: string,
  ): ChatMessage[] {
    // 1. Exact id match (the placeholder was cached previously).
    const exactIdx = cached.findIndex((m) => m.id === sseMsgId)
    if (exactIdx !== -1) {
      const merged: ChatMessage = { ...cached[exactIdx], ...sseMsg, id: cached[exactIdx].id }
      const next = cached.slice()
      next[exactIdx] = merged
      return next
    }

    // 2. Match the last assistant message that is still a placeholder
    //    (loading or no process) — this is the slot the SSE was filling.
    for (let i = cached.length - 1; i >= 0; i--) {
      const m = cached[i]
      if (m.role === 'assistant' && (m.loading || !m.process)) {
        const merged: ChatMessage = {
          ...m,
          content: sseMsg.content || m.content,
          process: sseMsg.process || m.process,
          loading: false,
          sessionId: sseMsg.sessionId || m.sessionId,
        }
        const next = cached.slice()
        next[i] = merged
        return next
      }
    }

    // 3. No matching slot — append. (The cache previously had fewer assistant
    //    messages than the SSE produced.)
    return [...cached, sseMsg]
  }

  // --- Main send logic ---

  async function sendMessage(
    content: string,
    sessionId: string | null,
    appId: string,
    files?: File[],
  ) {
    streamError.value = null

    const userMsg: ChatMessage = {
      id: `user_${Date.now()}`,
      sessionId: sessionId || '',
      role: 'user',
      content,
      files: files?.map((f) => ({
        id: `file_${Date.now()}_${Math.random().toString(36).slice(2)}`,
        name: f.name,
        url: '',
        type: f.type,
        size: f.size,
      })),
      createdAt: new Date().toISOString(),
    }
    chatStore.addMessage(userMsg)

    // Upload attachments
    const attachments: AttachmentItem[] = []
    if (files && files.length > 0) {
      for (const file of files) {
        try {
          const result = await uploadFile(file)
          attachments.push({
            Name: file.name,
            Type: getAttachmentType(file.name),
            FilePath: '',
            FileCode: result.fileCode,
            FileURI: result.url,
            IsUploading: false,
            UploadProgress: 100,
          })
        } catch {
          // Skip failed uploads, continue sending message
        }
      }
    }

    const aiMsgId = `ai_${Date.now()}`
    const aiMsg: ChatMessage = {
      id: aiMsgId,
      sessionId: sessionId || '',
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString(),
      loading: true,
      // Seed an initial process so the workflow area (ProcessStatus) renders
      // immediately on send and — critically — survives every failure path.
      // None of the failure handlers (error frame, catch, abort) ever CLEAR
      // process; they only overwrite content/loading. So a seeded process
      // stays visible even when the server fails before sending any
      // ProcessInfo frame (previously the workflow area vanished on failure).
      // On success, flushProcessUI unconditionally reassigns msg.process with
      // real data, so the seed is cleanly replaced.
      process: { status: 'running', results: '', timestamp: null, records: [], streamsByNode: {} },
    }
    chatStore.addMessage(aiMsg)

    // The stream is registered inside doSSERequest with the (possibly temp)
    // key. For a known session we can register up front; for a new chat
    // (sessionId empty) we defer until the first SSE frame resolves the sid.

    await doSSERequest(content, sessionId, appId, aiMsgId, false, attachments)
  }

  /**
   * 重新生成指定 AI 回复(原地替换)。语义同 ChatGPT "Regenerate":以原始用户输入
   * 重新跑整轮工作流,新结果流式回填到同一个 AI 区块(id 不变)。
   *
   * 实现:
   *  1. 按 assistant 消息在 chatStore.messages 里的位置向前找最近一条 role==='user'
   *     的 content(用户/回复是平级相邻元素,无 parent/child 关系)。
   *  2. 取 assistant 消息的 sessionId、process.records[0].processesId(旧轮)。
   *  3. 原地重置该 assistant 消息为 loading 占位(content 清空、process 重建)。
   *  4. 复用其 id 作为 SSE 的 aiMsgId,调用 doSSERequest 走 /Chat/Regenerate,
   *     携带旧 processesID(后端删旧记录)+ 原始用户 attachments(若需)。
   */
  async function regenerate(assistantMessageId: string) {
    const messages = chatStore.messages
    const idx = messages.findIndex((m) => m.id === assistantMessageId)
    if (idx < 0) {
      console.warn('regenerate: 未找到 assistant 消息', assistantMessageId)
      return
    }
    const assistantMsg = messages[idx]
    if (assistantMsg.role !== 'assistant') {
      console.warn('regenerate: 目标消息不是 assistant', assistantMessageId)
      return
    }
    // 正在生成中,忽略重复点击
    if (chatStore.isStreaming(assistantMsg.sessionId)) {
      return
    }

    // 向前找最近一条 user 消息取原始输入(用户消息存的是原始 content)。
    let userInput = ''
    let userFiles: ChatMessage['files']
    for (let i = idx - 1; i >= 0; i--) {
      if (messages[i].role === 'user') {
        userInput = messages[i].content
        userFiles = messages[i].files
        break
      }
    }
    if (!userInput) {
      console.warn('regenerate: 未找到原始用户输入')
      return
    }

    const sessionId = assistantMsg.sessionId
    const session = chatStore.sessions.find((s) => s.ChatSessionID === sessionId)
    const appId = session?.AppID || chatStore.selectedAppId || chatStore.apps[0]?.AppID || ''
    // 旧轮 ProcessesID(后端据此删旧记录)。取 process.records 里第一个非空的。
    const oldProcessesID =
      assistantMsg.process?.records?.find((r) => r.processesId)?.processesId || ''

    // 原地重置 assistant 消息为 loading 占位(id 保持不变 → 原地替换)。
    chatStore.updateMessage(assistantMessageId, {
      ...assistantMsg,
      content: '',
      loading: true,
      process: { status: 'running', results: '', timestamp: null, records: [], streamsByNode: {} },
    })

    // 还原用户附件为 AttachmentItem(原始 user 消息的 files 仅存元数据,无 FileCode)。
    // 重新生成无法重新上传,故 attachments 为空 —— 后端会从会话历史/原始输入复跑。
    await doSSERequest(
      userInput,
      sessionId,
      appId,
      assistantMessageId,
      false,
      [],
      oldProcessesID ? { processesID: oldProcessesID } : undefined,
    )
  }

  async function doSSERequest(
    content: string,
    sessionId: string | null,
    appId: string,
    aiMsgId: string,
    isRetry = false,
    attachments: AttachmentItem[] = [],
    // When set, the request targets /Chat/Regenerate instead of /Chat/completions.
    // regenerateMeta carries the old ProcessesID (for cleanup) and the original
    // user attachments. The businessData differs accordingly (see below).
    regenerateMeta?: { processesID: string; userAttachments?: AttachmentItem[] },
  ) {
    const abortController = new AbortController()
    // Persistent reference to the AI message that survives session switches.
    // When the user switches sessions, chatStore.messages is replaced and the
    // original message is no longer findable via find(). This reference keeps
    // accumulating SSE data on the correct message object regardless of which
    // session is currently displayed.
    let aiMessage: ChatMessage | null = chatStore.messages.find((m) => m.id === aiMsgId) || null

    // Capture the 0-based index of this assistant message within the session's
    // non-synthetic assistant sequence, BEFORE any session switch. After a
    // switch-away-then-back, selectSession replaces the ai_<ts> id with the
    // server's real ChatLogID (order-merge by position), so find(aiMsgId)
    // fails. This index lets getAssistantMsg re-locate the right store message
    // and re-bind aiMessage to it, so the live workflow tree keeps updating.
    const assistantIndex = chatStore.messages.filter(
      (m) => m.role === 'assistant' && !m.id.startsWith('pending_'),
    ).length - 1

    // Register the stream under a key. If we already know the sid, use it;
    // otherwise use the aiMsgId as a temp key until the first frame resolves
    // the real sid (then migrateStream moves the context).
    const initialKey = sessionId || aiMsgId
    chatStore.registerStream(initialKey, {
      abortController,
      aiMessage,
      aiMsgId,
      originSid: sessionId || '',
      registeredSids: [],
      assistantIndex: Math.max(0, assistantIndex),
    })

    // Track the key the context currently lives under, so we can migrate it.
    let currentKey = initialKey
    let sessionListFetched = false
    let terminalStatusSeen = false
    /** True when the stream ended via abort (user stop / silence timeout) rather
     * than a natural terminal frame. finalizeStream uses this to mark the session
     * locally terminal (success/0) so its status doesn't hang on "running". */
    let streamAborted = false
    /**
     * The message id the SSE stream currently writes to. Starts as the temp
     * ai_<ts> id. After getAssistantMsg re-binds to the real store message
     * (whose id is a server ChatLogID, after a switch-back), this is updated to
     * that real id so subsequent updateMessage(...) calls hit the rendered
     * message instead of silently no-op'ing on the stale temp id.
     */
    let liveMsgId = aiMsgId

    // pending state for batched UI flush
    let pendingProcess: any = null
    let pendingIncomingRecords: NormalizedRecord[] = []
    let pendingStreamEnvelope: StreamEnvelopeItem[] = []
    let pendingFinalText = ''
    let pendingStatus = ''
    let pendingTimestamp: number | null = null
    let processFlushTimer: ReturnType<typeof setTimeout> | null = null

    /**
     * Resolve the assistant message to write to. Prefer the reactive store
     * message when the user is viewing this session; otherwise accumulate on
     * the persistent aiMessage ref (detached from the store, reconciled on
     * switch-back or written to cache on stream end).
     *
     * Re-binding on switch-back: after the user switches away then back,
     * selectSession replaces the ai_<ts> id with the server's real ChatLogID
     * (order-merge), so a plain find(aiMsgId) fails. The first time we detect
     * the user has returned to this stream's session, we re-locate the store
     * message by the captured assistantIndex (falling back to the last
     * loading/placeholder assistant), merge the accumulated SSE data into it,
     * and re-bind aiMessage + liveMsgId so subsequent updates hit the rendered
     * message. Without this, updateMessage(aiMsgId, ...) is a permanent no-op
     * and the live workflow tree freezes (the reported bug).
     */
    const getAssistantMsg = (): ChatMessage | null => {
      // If the message is currently in the store under the live id, use the
      // reactive version.
      const storeMsg = chatStore.messages.find((m) => m.id === liveMsgId)
      if (storeMsg) {
        // Merge any SSE-accumulated data (from the persistent ref) into the
        // store version. Use mergeRecords to avoid duplicating records that
        // already exist in both.
        if (aiMessage && aiMessage !== storeMsg && aiMessage.process) {
          if (!storeMsg.process) {
            storeMsg.process = aiMessage.process
          } else {
            storeMsg.process.records = mergeRecords(
              storeMsg.process.records || [],
              aiMessage.process.records || [],
            )
            storeMsg.process.streamsByNode = {
              ...(storeMsg.process.streamsByNode || {}),
              ...(aiMessage.process.streamsByNode || {}),
            }
            const sseS = aiMessage.process.status
            if (sseS && sseS !== 'running') storeMsg.process.status = sseS
          }
          if (aiMessage.content) storeMsg.content = aiMessage.content
          if (aiMessage.sessionId) storeMsg.sessionId = aiMessage.sessionId
        }
        aiMessage = storeMsg
        const ctx = chatStore.getStreamContext(currentKey)
        if (ctx) ctx.aiMessage = storeMsg
        return storeMsg
      }

      // liveMsgId not found in the store. Two cases:
      //  (a) the user switched to a different session — keep accumulating on
      //      the detached aiMessage ref (background stream);
      //  (b) the user switched back to THIS stream's session, but selectSession
      //      replaced the temp id with a real ChatLogID — re-bind now.
      //
      // 对新会话：sessionId（原始入参）为 null、currentKey 可能仍是临时 aiMsgId
      // （第一帧 SSE 带 SessionID 之前），两条精确匹配都会落空 → 不重绑 → SSE
      // 写到脱离 store 的 aiMessage 引用、updateMessage 因 id 不在 store 而静默
      // no-op，AI 回复区块即使被 selectSession re-inject 进去也不再更新（Mac 复现
      // 根因，依赖 WKWebView 与 WebView2 的微任务时序）。加 activeStreamForThisSession
      // 兜底：当前会话存在活跃流即视为"回到本流会话"，使重绑不再依赖第一帧迁移时序。
      // activeStreamForThisSession 自带归属校验（findStreamContextBySession），不会
      // 跨会话污染。
      const activeStreamForThisSession =
        !!chatStore.findStreamContextBySession(chatStore.currentSessionId)
      const userReturnedToThisSession =
        !!chatStore.currentSessionId &&
        (activeStreamForThisSession ||
          chatStore.currentSessionId === currentKey ||
          chatStore.currentSessionId === sessionId)
      if (userReturnedToThisSession) {
        // Re-locate the store message by the captured assistant index.
        const nonPendingAssistants = chatStore.messages.filter(
          (m) => m.role === 'assistant' && !m.id.startsWith('pending_'),
        )
        let rebound: ChatMessage | undefined
        if (assistantIndex >= 0 && assistantIndex < nonPendingAssistants.length) {
          rebound = nonPendingAssistants[assistantIndex]
        }
        // Defensive fallback: index out of range (e.g. server returned fewer
        // rows than expected) — take the last assistant still loading or
        // lacking process data, which is the slot SSE is filling.
        if (!rebound) {
          for (let i = chatStore.messages.length - 1; i >= 0; i--) {
            const m = chatStore.messages[i]
            if (m.role === 'assistant' && (m.loading || !m.process)) {
              rebound = m
              break
            }
          }
        }
        if (rebound) {
          // Validate the rebound message actually belongs to this stream's
          // session. During a selectSession race, chatStore.messages can
          // momentarily belong to a DIFFERENT session even though
          // currentSessionId matches this stream — rebinding to that foreign
          // message would pollute this stream's aiMessage reference with
          // another session's content (the root cause of cross-session
          // contamination). If sessionId doesn't match, keep accumulating on
          // the detached aiMessage ref instead.
          const belongsToThisStream =
            !rebound.sessionId ||
            rebound.sessionId === currentKey ||
            rebound.sessionId === sessionId ||
            activeStreamForThisSession
          if (!belongsToThisStream) {
            return aiMessage
          }
          // Fold the accumulated detached SSE data into the store message.
          if (aiMessage && aiMessage.process) {
            if (!rebound.process) {
              rebound.process = aiMessage.process
            } else {
              rebound.process.records = mergeRecords(
                rebound.process.records || [],
                aiMessage.process.records || [],
              )
              rebound.process.streamsByNode = {
                ...(rebound.process.streamsByNode || {}),
                ...(aiMessage.process.streamsByNode || {}),
              }
              const sseS = aiMessage.process.status
              if (sseS && sseS !== 'running') rebound.process.status = sseS
            }
            if (aiMessage.content) rebound.content = aiMessage.content
            if (aiMessage.sessionId) rebound.sessionId = aiMessage.sessionId
          }
          // Re-bind: subsequent updates target the real id and render live.
          aiMessage = rebound
          liveMsgId = rebound.id
          const ctx = chatStore.getStreamContext(currentKey)
          if (ctx) ctx.aiMessage = rebound
          return rebound
        }
      }

      // User switched sessions — use the persistent reference to keep accumulating
      return aiMessage
    }

    /**
     * Finalize this stream: reset loading, persist to the correct session's
     * cache (avoiding duplicate assistant messages), and unregister the stream.
     * Centralizing this guarantees msg.loading is ALWAYS reset on every exit
     * path (success, error, abort, token-expired) — previously the
     * token/decrypt early-return paths left loading=true forever, which also
     * permanently blocked refreshCurrentSessionMessages for the session.
     */
    const finalizeStream = async () => {
      if (processFlushTimer) {
        clearTimeout(processFlushTimer)
        processFlushTimer = null
      }
      const msg = getAssistantMsg()
      if (msg) {
        msg.loading = false
        // Write back to the store if the message is currently displayed.
        chatStore.updateMessage(liveMsgId, msg)

        // Persist to cache so the process tree survives session switches.
        // Be careful: if the user switched sessions, chatStore.messages now
        // belongs to a different session — we must not cache it under the
        // wrong sessionId. Update only the target session's cache, and do so
        // via order-merge (mergeSSEMessageIntoCache) to avoid appending a
        // duplicate assistant message when the cache already holds the real
        // ChatLogID entry (which previously caused duplicated AI replies).
        const targetSid = msg.sessionId || sessionId || currentKey || ''
        // 沉淀 sessionId 到消息对象：即使第一帧缺失 SessionID（极端：流被
        // abort 且从未收到带 sid 的帧），也用 currentKey（已迁移到真实 sid，
        // 或仍是临时 key）兜底，确保后续 findStreamContextBySession /
        // mergeSSEMessageIntoCache 能正确归属，避免回复最终丢失归属。
        if (targetSid && !msg.sessionId) msg.sessionId = targetSid
        if (targetSid) {
          // If the stream was aborted (user stop / silence timeout) and we
          // never saw a natural terminal frame, mark the session locally
          // terminal (success) so its status dot doesn't hang on "running"
          // forever. The locallyTerminalStatus signal also makes the next
          // heartbeat/fetchSessions respect this terminal decision.
          if (streamAborted && !terminalStatusSeen) {
            chatStore.markLocallyTerminal(targetSid, 0)
            const session = chatStore.sessions.find((s) => s.ChatSessionID === targetSid)
            if (session && session.SessionStatus === 1) session.SessionStatus = 0
            // Abort without a terminal frame = the workflow didn't complete.
            // Flip the seeded process status to 'error' so the workflow area
            // shows an error badge instead of a stuck 'running' state.
            if (msg.process && String(msg.process.status).toLowerCase() === 'running') {
              msg.process = { ...msg.process, status: 'error' }
            }
          }
          if (chatStore.currentSessionId === targetSid) {
            messageCache.set(targetSid, [...chatStore.messages]).catch(() => {})
          } else {
            try {
              const cached = await messageCache.getBySession(targetSid)
              const merged = mergeSSEMessageIntoCache(cached, msg, liveMsgId)
              await messageCache.set(targetSid, merged)
            } catch { /* ignore cache errors */ }
          }
        }
      }
      chatStore.unregisterStream(currentKey)
    }

    const flushProcessUI = () => {
      processFlushTimer = null
      const msg = getAssistantMsg()
      if (!msg) return
      if (
        !pendingProcess &&
        !pendingStreamEnvelope.length &&
        !pendingIncomingRecords.length &&
        !pendingFinalText &&
        !pendingStatus
      )
        return

      const prevRecords: NormalizedRecord[] = Array.isArray(msg.process?.records)
        ? msg.process.records
        : []
      const mergedRecords = mergeRecords(prevRecords, pendingIncomingRecords)

      const nodeIdsWithOutputs = new Set(
        mergedRecords
          .filter(
            (r) => r && r.nodeId && Array.isArray(r.outputs) && r.outputs.length,
          )
          .map((r) => r.nodeId),
      )

      const prevStreams: Record<string, StreamByNode> = msg.process?.streamsByNode || {}
      const mergedStreams = mergeStreamsByNode(prevStreams, pendingStreamEnvelope)

      for (const nodeId of nodeIdsWithOutputs) {
        if (Object.prototype.hasOwnProperty.call(mergedStreams, nodeId)) {
          delete mergedStreams[nodeId]
        }
      }

      const newStatus = pendingStatus
        ? pendingProcess?.Status
          ? pickBetterStatus(pendingStatus, pendingProcess.Status)
          : pendingStatus
        : msg.process?.status || 'running'

      msg.process = {
        status: newStatus,
        results: pendingFinalText,
        timestamp: pendingTimestamp,
        records: mergedRecords,
        streamsByNode: mergedStreams,
      }

      if (pendingFinalText) {
        msg.content = pendingFinalText
      }

      chatStore.updateMessage(liveMsgId, msg)

      pendingProcess = null
      pendingIncomingRecords = []
      pendingStreamEnvelope = []
      pendingFinalText = ''
      pendingStatus = ''
      pendingTimestamp = null
    }

    const scheduleProcessFlush = () => {
      if (processFlushTimer) return
      processFlushTimer = setTimeout(flushProcessUI, STREAM_UI_FLUSH_MS)
    }

    // --- SSE message handler ---
    function onMessage(messageData: SSEMessage) {
      const msg = getAssistantMsg()
      if (!msg) return

      // Error frame
      if (messageData && messageData.Error) {
        if (
          msg.process?.status &&
          String(msg.process.status).toLowerCase() === 'success'
        ) {
          return // ignore trailing error after success
        }
        msg.content = `错误 (${messageData.ErrorCode}): ${messageData.ErrorDesc || '未知错误'}`
        msg.loading = false
        // Mark the workflow as errored so the workflow area shows an error
        // status badge instead of vanishing (process is seeded by sendMessage,
        // but its status was 'running' — flip it here). Preserve any records
        // already accumulated.
        msg.process = {
          status: 'error',
          results: '',
          timestamp: msg.process?.timestamp ?? null,
          records: msg.process?.records || [],
          streamsByNode: msg.process?.streamsByNode || {},
        }
        chatStore.updateMessage(liveMsgId, msg)
        return
      }

      // Extract SessionID from the SSE message. Migrate the stream context to
      // the real sid when we first learn it (handles new chats where the
      // request was sent with an empty sessionId). Only auto-switch
      // currentSessionId if the user hasn't manually navigated to a different
      // session — otherwise the SSE from a background session would forcibly
      // pull focus back to it.
      const sseSid = messageData.SessionID || messageData.ProcessInfo?.SessionID
      if (sseSid) {
        if (sseSid !== currentKey) {
          chatStore.migrateStream(currentKey, sseSid)
          currentKey = sseSid
        }
        const isCurrentSession =
          !chatStore.currentSessionId ||
          chatStore.currentSessionId === sessionId ||
          chatStore.currentSessionId === sseSid
        if (isCurrentSession) {
          chatStore.currentSessionId = sseSid
        }
        msg.sessionId = sseSid
        if (!sessionListFetched) {
          sessionListFetched = true
          chatStore.fetchSessions(1).catch(() => {})
        }
      }

      // Extract ProcessesID
      if (messageData.ProcessesID) {
        // stored for potential future use
      } else if (messageData.ProcessInfo?.ProcessID) {
        // stored for potential future use
      }

      // ProcessInfo handling
      if (messageData.ProcessInfo) {
        const proc = messageData.ProcessInfo
        const status = proc.Status

        // Collect StreamEnvelope from multiple possible locations
        const envelope: StreamEnvelopeItem[] =
          (Array.isArray((messageData as any).StreamEnvelope) &&
            (messageData as any).StreamEnvelope) ||
          (Array.isArray((messageData as any).streamEnvelope) &&
            (messageData as any).streamEnvelope) ||
          (Array.isArray(proc.StreamEnvelope) && proc.StreamEnvelope) ||
          null

        if (envelope && envelope.length) {
          pendingStreamEnvelope.push(...envelope)
        }

        // Extract final text from Results
        let finalText = ''
        if (Array.isArray(proc.Results) && proc.Results.length > 0) {
          const strItem = proc.Results.find(
            (r) => r && r.type === 'string',
          )
          if (strItem) {
            finalText = normalizeValue(strItem.value)
          }
        }

        // Normalize execution records
        const incomingRecords: NormalizedRecord[] = Array.isArray(
          proc.ExecutionRecordInfos,
        )
          ? proc.ExecutionRecordInfos.map(normalizeRecord)
          : []

        // Check for HITL (HumanInTheLoopInput running)
        const hasHitlRunning = incomingRecords.some((rec) => {
          if (!rec) return false
          const nodeName = String(rec.nodeName || '')
          if (!nodeName.startsWith('HumanInTheLoopInput')) return false
          return String(rec.status || '').toLowerCase() === 'running'
        })

        pendingProcess = proc
        pendingTimestamp = messageData.Timestamp ?? null
        pendingStatus = pendingStatus
          ? pickBetterStatus(pendingStatus, status)
          : status
        if (finalText) pendingFinalText = finalText
        if (incomingRecords.length) {
          pendingIncomingRecords.push(...incomingRecords)
        }

        if (hasHitlRunning) {
          // HITL detected: flush immediately and pause
          flushProcessUI()
          return
        }

        scheduleProcessFlush()

        // Set final text as message content
        if (finalText) {
          const cleaned = typeof finalText === 'string' ? finalText : normalizeValue(finalText)
          if (!/\[object Object\]|undefined/.test(cleaned)) {
            msg.content = cleaned
          }
        }

        // Terminal status — flush UI immediately so the process tree is final,
        // but keep msg.loading=true until the SSE reader loop exits.
        // This prevents the heartbeat from triggering refreshCurrentSessionMessages()
        // (which guards on `messages.some(m => m.loading)`) during the window between
        // terminal status and stream end, avoiding duplicate synthetic workflow blocks.
        const statusLower = String(status || '').toLowerCase()
        if (
          status &&
          (statusLower === 'success' ||
            statusLower === 'failed' ||
            statusLower === 'error')
        ) {
          flushProcessUI()
          terminalStatusSeen = true
          // 更新当前会话的 SessionStatus，并登记本地终态信号。
          // markLocallyTerminal 是关键：它在终态帧到达的瞬间就把本地终态
          // 固化进 store（独立于 runningSessionIds 的时序），从而阻止后续
          // 心跳/ fetchSessions 用服务端滞后的 running(1) 覆盖掉本地终态——
          // 这正是"切换会话后状态圆点闪烁/失常"的根因。
          const terminalStatus = statusLower === 'success' ? 0 : -1
          const sid = msg.sessionId || chatStore.currentSessionId
          if (sid) {
            chatStore.markLocallyTerminal(sid, terminalStatus)
            const session = chatStore.sessions.find((s) => s.ChatSessionID === sid)
            if (session) session.SessionStatus = terminalStatus
          }
        }

        chatStore.updateMessage(liveMsgId, msg)
      }
    }

    // --- Build and send request ---

    let silenceTimer: ReturnType<typeof setTimeout> | null = null

    try {
      const memberToken = await secureStorage.get('member_token')
      const accessToken = await secureStorage.get('access_token')

      const isRegenerate = !!regenerateMeta
      // Regenerate 的 businessData 与 completions 不同:复用已有 session(不新建),
      // 携带旧 ProcessesID(后端据此删旧记录),messages 为原始用户输入。
      const businessData = isRegenerate
        ? {
            status: 0,
            stream: true,
            messages: {
              role: 'User',
              content,
              Attachments: regenerateMeta?.userAttachments || [],
              AdditionalOptions: {},
            },
            sessionID: sessionId || '',
            appid: appId,
            processesID: regenerateMeta?.processesID || '',
            SSE_TimeOut: 30,
          }
        : {
            status: 0,
            stream: true,
            messages: {
              role: 'User',
              content,
              Attachments: attachments,
              AdditionalOptions: {},
            },
            sessionID: sessionId || '',
            appid: appId,
            SSE_TimeOut: 30,
          }

      let encryptKey = APP_SECRET
      let signKey = APP_SECRET
      if (memberToken) {
        encryptKey = memberToken.substring(0, 16)
        signKey = memberToken
      } else if (accessToken) {
        encryptKey = accessToken
        signKey = accessToken
      }
      const apiRequest = createApiRequest(businessData, encryptKey, signKey)

      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      }
      if (accessToken) {
        headers['bearer'] = accessToken
      }
      if (memberToken) {
        headers['memberbearer'] = memberToken
      }

      const response = await fetch(isRegenerate ? getChatRegenerateUrl() : getChatCompletionsUrl(), {
        method: 'POST',
        headers,
        body: JSON.stringify(apiRequest),
        signal: abortController.signal,
      })

      if (response.status === 401) {
        await handleTokenExpired()
        await finalizeStream()
        return
      }

      if (!response.ok) {
        const text = await response.text().catch(() => '')
        let isTokenErr = false
        let isDecryptErr = false
        try {
          const json = JSON.parse(text)
          if (
            json.ErrorCode === TOKEN_CHECK_ERROR ||
            json.ErrorCode === MEMBER_TOKEN_CHECK_ERROR
          ) {
            isTokenErr = true
          }
          if (json.ErrorCode === 60001 && !isRetry) {
            isDecryptErr = true
          }
        } catch { /* not JSON */ }

        if (isTokenErr) {
          await handleTokenExpired()
          await finalizeStream()
          return
        }

        if (isDecryptErr) {
          await retryWithFreshTokens()
          await finalizeStream()
          return
        }

        throw new Error(`HTTP ${response.status}`)
      }

      // Check if response is JSON error instead of SSE stream
      const contentType = response.headers.get('content-type') || ''
      if (contentType.includes('application/json')) {
        const errorResult = await response.json()
        if (
          errorResult.ErrorCode === TOKEN_CHECK_ERROR ||
          errorResult.ErrorCode === MEMBER_TOKEN_CHECK_ERROR
        ) {
          await handleTokenExpired()
          await finalizeStream()
          return
        }
        onMessage({
          Error: true,
          ErrorCode: errorResult.ErrorCode ?? 500,
          ErrorDesc: errorResult.ErrorDesc || `HTTP ${response.status}`,
          Content: `错误: ${errorResult.ErrorDesc || '未知错误'}`,
        })
        await finalizeStream()
        return
      }

      // --- Read SSE stream ---
      const reader = response.body!.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

      // Arm the silence timer. After a terminal-status frame, allow a shorter
      // window for the server to close the stream; otherwise use the longer
      // silence timeout. Guards against a server that never closes the
      // connection (which would leave msg.loading=true forever).
      const armSilenceTimer = () => {
        if (silenceTimer) clearTimeout(silenceTimer)
        silenceTimer = setTimeout(
          () => abortController.abort(),
          terminalStatusSeen ? STREAM_FINAL_CLOSE_TIMEOUT_MS : STREAM_SILENCE_TIMEOUT_MS,
        )
      }
      armSilenceTimer()

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        // Any received data resets the silence timer.
        armSilenceTimer()

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() || ''

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const data = line.slice(6)
            if (data === '[DONE]') continue
            try {
              const parsed = JSON.parse(data)
              onMessage(parsed)
            } catch { /* ignore parse errors */ }
          }
        }
      }

      await finalizeStream()

      // SSE完成后刷新会话列表 + 通知
      const finalMsg = aiMessage
      const resolvedSid = finalMsg?.sessionId || sessionId || currentKey
      chatStore.fetchSessions(1).catch(() => {})
      // The locally-terminal signal has now been reconciled into sessions[]
      // by fetchSessions (and the server has caught up). Drop it so it doesn't
      // accumulate across many sessions over the app lifetime.
      if (resolvedSid) chatStore.clearLocallyTerminal(resolvedSid)

      if (!document.hasFocus() && settingsStore.notificationEnabled && finalMsg) {
        const raw = finalMsg.content || ''
        const isFailed = finalMsg.process?.status === 'failed' || finalMsg.process?.status === 'error'
        const summary = stripMarkdown(raw).slice(0, 120)
        // Route through the coordinator so concurrent completions are deduped
        // (heartbeat may also notify this same session) and aggregated when
        // several sessions finish within a short window.
        notifySessionCompleted(resolvedSid || '', isFailed, summary)
      }
    } catch (error: any) {
      if (error.name === 'AbortError') {
        // Aborted (user cancel, silence timeout, session delete, or unmount).
        // Flag it so finalizeStream marks the session terminal instead of
        // leaving it stuck on "running". Still finalize so loading is cleared
        // and the partial message is persisted.
        streamAborted = true
        await finalizeStream()
        // Persist the terminal status to the server's view of the list, then
        // drop the local terminal signal (it has now been baked into
        // sessions[].SessionStatus and is no longer needed as a guard).
        const finalMsg = aiMessage
        const resolvedSid = finalMsg?.sessionId || sessionId || currentKey
        if (resolvedSid) {
          await chatStore.fetchSessions(1).catch(() => {})
          chatStore.clearLocallyTerminal(resolvedSid)
        }
        return
      }
      const msg = getAssistantMsg()
      if (msg) {
        msg.loading = false
        msg.content = msg.content || `发送失败: ${error.message}`
        // Flip the workflow status to error so the area stays visible with an
        // error badge (process is seeded by sendMessage; preserve its data).
        if (msg.process && String(msg.process.status).toLowerCase() !== 'success') {
          msg.process = { ...msg.process, status: 'error' }
        }
        chatStore.updateMessage(liveMsgId, msg)
      }
      await finalizeStream()
    } finally {
      if (silenceTimer) {
        clearTimeout(silenceTimer)
        silenceTimer = null
      }
    }
  }

  async function retryWithFreshTokens() {
    try {
      await getAccessToken()
      const result = await refreshMemberToken()
      if (!result?.Success) {
        await handleTokenExpired()
        return
      }
      streamError.value = '加密密钥已刷新，请重新发送消息'
    } catch {
      await handleTokenExpired()
    }
  }

  /**
   * Cancel the active SSE stream for a session. Defaults to the current
   * session if none given. This is per-session now — cancelling one session
   * no longer touches another session's stream (previously there was a single
   * global abortController, so the cancel button could only ever reach the
   * most recent stream).
   */
  function cancelStream(sessionId?: string) {
    const sid = sessionId || chatStore.currentSessionId
    if (!sid) return
    const ctx = chatStore.getStreamContext(sid)
    if (ctx) {
      try {
        ctx.abortController.abort()
      } catch { /* ignore */ }
    }
  }

  // --- Retry failed node ---

  interface RetryNodePayload {
    nodeId: string
    sessionId: string
    processesId: string
    taskId: string
    messageId: string | null
  }

  async function retryNode(payload: RetryNodePayload) {
    const { nodeId, sessionId, processesId, taskId, messageId } = payload
    if (!nodeId) {
      console.warn('retryNode: 缺少 nodeId', payload)
      return
    }
    if (!sessionId || !processesId) {
      console.warn('retryNode: 缺少 sessionId 或 processesId', payload)
      return
    }

    // Strip sub-process suffix: "parent_child" → "parent"
    const topProcessesId = processesId.includes('_') ? processesId.split('_')[0] : processesId

    try {
      // Optimistically update node status to 'running'. Use the
      // session-aware updater so this works whether or not the session is
      // currently displayed.
      if (messageId) {
        await chatStore.updateMessageInSession(sessionId, messageId, (msg) => {
          if (msg?.process?.records) {
            const updatedRecords = msg.process.records.map((r: NormalizedRecord) =>
              r.nodeId === nodeId ? { ...r, status: 'running' as const } : r,
            )
            return { ...msg, process: { ...msg.process, records: updatedRecords, status: 'running' } }
          }
          return msg
        })
      }

      const session = chatStore.sessions.find((s) => s.ChatSessionID === sessionId)
      const appID = session?.AppID || chatStore.selectedAppId || ''

      const result = await retryNodeApi({
        NodeID: nodeId,
        SessionID: sessionId,
        ProcessesID: topProcessesId,
        TaskID: taskId || '',
        AppID: appID,
      })
      console.log('retryNode 结果:', result)

      // Refresh execution records after retry
      await reloadNodeExecution(sessionId, topProcessesId, messageId)
    } catch (e) {
      console.error('retryNode 失败:', e)
    }
  }

  /**
   * Registry of in-flight node-reload SSE streams, keyed by processesId. Each
   * has its own AbortController. Tracked so concurrent/overlapping retries
   * don't leak unbounded streams and so they can be cancelled.
   */
  const reloadStreams = new Map<string, AbortController>()

  async function reloadNodeExecution(
    sessionId: string,
    processesId: string,
    messageId: string | null,
  ) {
    if (!sessionId || !processesId) return

    // Resolve the target message id. Prefer the explicit messageId; if absent,
    // fall back to the last assistant message *in the same session* (look it
    // up from the store if it's the current session, otherwise from cache).
    // Previously the fallback grabbed the last assistant message of the
    // currently-displayed session regardless of sessionId, which could update
    // the wrong message.
    let targetMsgId = messageId
    if (!targetMsgId) {
      if (chatStore.currentSessionId === sessionId) {
        targetMsgId = chatStore.messages.filter((m) => m.role === 'assistant').pop()?.id || null
      } else {
        try {
          const cached = await messageCache.getBySession(sessionId)
          targetMsgId = cached.filter((m) => m.role === 'assistant').pop()?.id || null
        } catch { /* ignore */ }
      }
    }
    if (!targetMsgId) return

    // Abort any prior in-flight reload for the same processesId to avoid
    // overlapping streams and last-write-wins status regression.
    const prior = reloadStreams.get(processesId)
    if (prior) {
      try {
        prior.abort()
      } catch { /* ignore */ }
      reloadStreams.delete(processesId)
    }

    const abortCtrl = new AbortController()
    reloadStreams.set(processesId, abortCtrl)

    let pendingIncomingRecords: NormalizedRecord[] = []
    let pendingStatus = ''
    let flushTimer: ReturnType<typeof setTimeout> | null = null

    // flush writes to the target message in the target session (current or
    // background cache). Uses pickBetterStatus so a late 'running' frame can't
    // regress an already-terminal status (previously last-write-wins could).
    const flush = async () => {
      flushTimer = null
      if (!pendingIncomingRecords.length) return

      await chatStore.updateMessageInSession(sessionId, targetMsgId!, (msg) => {
        const prev: NormalizedRecord[] = Array.isArray(msg.process?.records) ? msg.process.records : []
        const baseStatus = msg.process?.status || 'running'
        const nextStatus = pendingStatus ? pickBetterStatus(baseStatus, pendingStatus) : baseStatus
        return {
          ...msg,
          process: {
            status: nextStatus,
            results: '',
            timestamp: Date.now(),
            records: mergeRecords(prev, pendingIncomingRecords),
            streamsByNode: msg.process?.streamsByNode || {},
          },
        }
      })
      pendingIncomingRecords = []
      pendingStatus = ''
    }

    const scheduleFlush = () => {
      if (flushTimer) return
      flushTimer = setTimeout(() => { flush().catch(() => {}) }, 200)
    }

    try {
      const memberToken = await secureStorage.get('member_token')
      const accessToken = await secureStorage.get('access_token')

      const businessData = {
        status: 0,
        stream: true,
        sessionID: sessionId,
        processesID: processesId,
        workflowID: '',
        isAgentNode: false,
        SSE_TimeOut: 30,
      }

      let encryptKey = APP_SECRET
      let signKey = APP_SECRET
      if (memberToken) {
        encryptKey = memberToken.substring(0, 16)
        signKey = memberToken
      } else if (accessToken) {
        encryptKey = accessToken
        signKey = accessToken
      }
      const apiRequest = createApiRequest(businessData, encryptKey, signKey)

      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (accessToken) headers['bearer'] = accessToken
      if (memberToken) headers['memberbearer'] = memberToken

      const response = await fetch(getNodeExecutionRecordUrl(), {
        method: 'POST',
        headers,
        body: JSON.stringify(apiRequest),
        signal: abortCtrl.signal,
      })

      if (!response.ok) throw new Error(`HTTP ${response.status}`)

      const contentType = response.headers.get('content-type') || ''
      if (contentType.includes('application/json')) {
        const err = await response.json().catch(() => ({}))
        console.error('reloadNodeExecution error:', err)
        return
      }

      const reader = response.body!.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() || ''
        for (const line of lines) {
          if (!line.startsWith('data: ')) continue
          const data = line.slice(6)
          if (data === '[DONE]') continue
          try {
            const parsed: SSEMessage = JSON.parse(data)
            if (parsed.ProcessInfo) {
              const proc = parsed.ProcessInfo
              const incoming: NormalizedRecord[] = Array.isArray(proc.ExecutionRecordInfos)
                ? proc.ExecutionRecordInfos.map(normalizeRecord)
                : []
              if (incoming.length) pendingIncomingRecords.push(...incoming)
              pendingStatus = proc.Status ? pickBetterStatus(pendingStatus, proc.Status) : pendingStatus
              scheduleFlush()
            }
          } catch { /* ignore */ }
        }
      }
      await flush()
    } catch (e: any) {
      if (e.name !== 'AbortError') console.error('reloadNodeExecution 失败:', e)
    } finally {
      if (flushTimer) clearTimeout(flushTimer)
      reloadStreams.delete(processesId)
    }
  }

  async function handleTokenExpired() {
    await secureStorage.remove('member_token')
    await secureStorage.remove('access_token')
    const currentPath = router.currentRoute.value.path
    if (currentPath !== '/login') {
      router.push('/login')
    }
  }

  onUnmounted(() => {
    // Cancel all active streams when the chat view unmounts. Per-session state
    // means we must iterate every active stream rather than aborting a single
    // global controller.
    chatStore.cancelAllStreams()
  })

  return {
    sendMessage,
    regenerate,
    cancelStream,
    retryNode,
    reloadNodeExecution,
    isStreaming,
    streamError,
  }
}
