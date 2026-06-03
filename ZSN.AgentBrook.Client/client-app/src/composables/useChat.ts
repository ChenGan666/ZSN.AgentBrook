import { ref, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '@/stores/chat'
import { useSettingsStore } from '@/stores/settings'
import { secureStorage } from '@/utils/storage'
import { getChatCompletionsUrl, getNodeExecutionRecordUrl, retryNode as retryNodeApi } from '@/services/chat'
import { createApiRequest, APP_SECRET } from '@/utils/crypto'
import { getAccessToken, refreshMemberToken } from '@/services/auth'
import router from '@/router'
import type {
  ChatMessage,
  SSEMessage,
  StreamEnvelopeItem,
  StreamByNode,
  NormalizedRecord,
  MessageProcess,
  ExecutionRecordInfo,
  AttachmentItem,
} from '@/types/chat'
import { useFileUpload } from '@/composables/useFileUpload'
import { normalizeRecord, mergeRecords } from '@/utils/process'
import { messageCache } from '@/utils/cache'
import { platform } from '@/platform'

const TOKEN_CHECK_ERROR = 80001
const MEMBER_TOKEN_CHECK_ERROR = 80002
const STREAM_UI_FLUSH_MS = 200
const STREAM_MAX_CHARS_PER_NODE = 200000
const STREAM_TAIL_CHARS = 1200

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
  const isStreaming = ref(false)
  const streamError = ref<string | null>(null)
  const { uploadFile } = useFileUpload()
  let abortController: AbortController | null = null

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

  // --- Main send logic ---

  async function sendMessage(
    content: string,
    sessionId: string | null,
    appId: string,
    files?: File[],
  ) {
    isStreaming.value = true
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
    }
    chatStore.addMessage(aiMsg)

    // 加入运行中会话列表（SSE可能返回新的sessionId，在doSSERequest中更新）
    if (sessionId) {
      chatStore.addRunningSession(sessionId)
    }

    await doSSERequest(content, sessionId, appId, aiMsgId, false, attachments)

    // SSE完成后从运行列表移除。
    // 使用消息中实际解析到的 sessionId（在 SSE 中可能被更新为新的 SessionID），
    // 避免因用户切换会话导致 chatStore.currentSessionId 指向错误的会话。
    const resolvedSid = aiMsg.sessionId || sessionId
    if (resolvedSid) {
      chatStore.removeRunningSession(resolvedSid)
    }

    chatStore.fetchSessions(1).catch(() => {})

    if (!document.hasFocus() && settingsStore.notificationEnabled) {
      const raw = aiMsg.content || ''
      const isFailed = aiMsg.process?.status === 'failed' || aiMsg.process?.status === 'error'
      const title = isFailed ? t('chat.notifyFailed') : t('chat.notifyCompleted')
      const summary = stripMarkdown(raw).slice(0, 120) || (isFailed ? t('chat.notifyViewDetail') : t('chat.notifyViewReply'))
      platform.notification.show(title, summary, { sessionId: resolvedSid || '' })
    }
  }

  async function doSSERequest(
    content: string,
    sessionId: string | null,
    appId: string,
    aiMsgId: string,
    isRetry = false,
    attachments: AttachmentItem[] = [],
  ) {
    abortController = new AbortController()

    let sessionListFetched = false

    // pending state for batched UI flush
    let pendingProcess: any = null
    let pendingIncomingRecords: NormalizedRecord[] = []
    let pendingStreamEnvelope: StreamEnvelopeItem[] = []
    let pendingFinalText = ''
    let pendingStatus = ''
    let pendingTimestamp: number | null = null
    let processFlushTimer: ReturnType<typeof setTimeout> | null = null

    // Persistent reference to the AI message that survives session switches.
    // When the user switches sessions, chatStore.messages is replaced and the
    // original message is no longer findable via find(). This reference keeps
    // accumulating SSE data on the correct message object regardless of which
    // session is currently displayed.
    let aiMessage: ChatMessage | null = chatStore.messages.find((m) => m.id === aiMsgId) || null

    const getAssistantMsg = (): ChatMessage | null => {
      // If the message is currently in the store, use the reactive version
      const storeMsg = chatStore.messages.find((m) => m.id === aiMsgId)
      if (storeMsg) {
        // Merge any SSE-accumulated data (from the persistent ref) into the
        // store version. Use mergeRecords to avoid duplicating records that
        // already exist in both.
        if (aiMessage && aiMessage !== storeMsg && aiMessage.process) {
          if (!storeMsg.process) {
            storeMsg.process = aiMessage.process
          } else {
            // Merge records: SSE records + store records, deduped by recordId
            storeMsg.process.records = mergeRecords(
              storeMsg.process.records || [],
              aiMessage.process.records || [],
            )
            // Merge streamsByNode
            storeMsg.process.streamsByNode = {
              ...(storeMsg.process.streamsByNode || {}),
              ...(aiMessage.process.streamsByNode || {}),
            }
            // Use the more-terminal status
            const sseS = aiMessage.process.status
            if (sseS && sseS !== 'running') storeMsg.process.status = sseS
          }
          if (aiMessage.content) storeMsg.content = aiMessage.content
          if (aiMessage.sessionId) storeMsg.sessionId = aiMessage.sessionId
        }
        aiMessage = storeMsg
        return storeMsg
      }
      // User switched sessions — use the persistent reference to keep accumulating
      return aiMessage
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

      msg.process = {
        status:
          pendingStatus ||
          pendingProcess?.Status ||
          msg.process?.status ||
          'running',
        results: pendingFinalText,
        timestamp: pendingTimestamp,
        records: mergedRecords,
        streamsByNode: mergedStreams,
      }

      if (pendingFinalText) {
        msg.content = pendingFinalText
      }

      chatStore.updateMessage(aiMsgId, msg)

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
        chatStore.updateMessage(aiMsgId, msg)
        return
      }

      // Extract SessionID from the SSE message.
      // Only auto-switch currentSessionId if the user hasn't manually
      // navigated to a different session. Otherwise the SSE from a
      // background session would forcibly pull focus back to it.
      const sseSid = messageData.SessionID || messageData.ProcessInfo?.SessionID
      if (sseSid) {
        const isCurrentSession =
          !chatStore.currentSessionId ||
          chatStore.currentSessionId === sessionId ||
          chatStore.currentSessionId === sseSid
        if (isCurrentSession) {
          chatStore.currentSessionId = sseSid
        }
        msg.sessionId = sseSid
        chatStore.addRunningSession(sseSid)
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
          // TODO: set active HITL in store if needed
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
        if (
          status &&
          (String(status).toLowerCase() === 'success' ||
            String(status).toLowerCase() === 'failed' ||
            String(status).toLowerCase() === 'error')
        ) {
          flushProcessUI()
          // 更新当前会话的 SessionStatus
          const terminalStatus = String(status).toLowerCase() === 'success' ? 0 : -1
          const sid = msg.sessionId || chatStore.currentSessionId
          if (sid) {
            const session = chatStore.sessions.find(s => s.ChatSessionID === sid)
            if (session) session.SessionStatus = terminalStatus
          }
        }

        chatStore.updateMessage(aiMsgId, msg)
      }
    }

    // --- Build and send request ---

    try {
      const memberToken = await secureStorage.get('member_token')
      const accessToken = await secureStorage.get('access_token')

      const businessData = {
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
        SSE_TimeOut: 5,
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

      const response = await fetch(getChatCompletionsUrl(), {
        method: 'POST',
        headers,
        body: JSON.stringify(apiRequest),
        signal: abortController.signal,
      })

      if (response.status === 401) {
        await handleTokenExpired()
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
          return
        }

        if (isDecryptErr) {
          await retryWithFreshTokens()
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
          return
        }
        onMessage({
          Error: true,
          ErrorCode: errorResult.ErrorCode ?? 500,
          ErrorDesc: errorResult.ErrorDesc || `HTTP ${response.status}`,
          Content: `错误: ${errorResult.ErrorDesc || '未知错误'}`,
        })
        return
      }

      // --- Read SSE stream ---
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

      // Flush any remaining pending data
      flushProcessUI()
      const msg = getAssistantMsg()
      if (msg) {
        msg.loading = false
        chatStore.updateMessage(aiMsgId, msg)

        // Persist to cache so process tree survives session switches.
        // Be careful: if the user switched sessions, chatStore.messages now
        // belongs to a different session — we must not cache it under the
        // wrong sessionId. Instead, update only the target session's cache.
        const targetSid = msg.sessionId || sessionId || ''
        if (targetSid) {
          if (chatStore.currentSessionId === targetSid) {
            // User is still viewing this session — cache the full array
            messageCache.set(targetSid, [...chatStore.messages]).catch(() => {})
          } else {
            // User switched away — read existing cache, update the ai message, write back
            messageCache.getBySession(targetSid).then((cached) => {
              const idx = cached.findIndex((m: ChatMessage) => m.id === aiMsgId)
              if (idx !== -1) {
                cached[idx] = msg
              } else {
                cached.push(msg)
              }
              return messageCache.set(targetSid, cached)
            }).catch(() => {})
          }
        }
      }
    } catch (error: any) {
      if (error.name === 'AbortError') return
      const msg = getAssistantMsg()
      if (msg) {
        msg.loading = false
        msg.content = msg.content || `发送失败: ${error.message}`
        chatStore.updateMessage(aiMsgId, msg)
      }
    } finally {
      isStreaming.value = false
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
      // Re-send with fresh tokens - the caller should re-invoke sendMessage
      // For now, report error to user
      streamError.value = '加密密钥已刷新，请重新发送消息'
    } catch {
      await handleTokenExpired()
    }
  }

  function cancelStream() {
    abortController?.abort()
    isStreaming.value = false
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
      // Optimistically update node status to 'running'
      if (messageId) {
        const msg = chatStore.messages.find(m => m.id === messageId)
        if (msg?.process?.records) {
          const updatedRecords = msg.process.records.map((r: NormalizedRecord) =>
            r.nodeId === nodeId ? { ...r, status: 'running' as const } : r
          )
          msg.process = { ...msg.process, records: updatedRecords, status: 'running' }
          chatStore.updateMessage(messageId, msg)
        }
      }

      const session = chatStore.sessions.find(s => s.ChatSessionID === sessionId)
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

  async function reloadNodeExecution(
    sessionId: string,
    processesId: string,
    messageId: string | null,
  ) {
    if (!sessionId || !processesId) return

    const targetMsgId = messageId || chatStore.messages.filter(m => m.role === 'assistant').pop()?.id
    if (!targetMsgId) return

    const abortCtrl = new AbortController()

    let pendingIncomingRecords: NormalizedRecord[] = []
    let pendingProcess: any = null
    let pendingStatus = ''
    let flushTimer: ReturnType<typeof setTimeout> | null = null

    const flush = () => {
      flushTimer = null
      const msg = chatStore.messages.find(m => m.id === targetMsgId)
      if (!msg || !pendingIncomingRecords.length) return

      const prev: NormalizedRecord[] = Array.isArray(msg.process?.records) ? msg.process.records : []
      msg.process = {
        status: pendingStatus || msg.process?.status || 'running',
        results: '',
        timestamp: Date.now(),
        records: mergeRecords(prev, pendingIncomingRecords),
        streamsByNode: msg.process?.streamsByNode || {},
      }
      chatStore.updateMessage(targetMsgId, msg)
      pendingIncomingRecords = []
      pendingStatus = ''
    }

    const scheduleFlush = () => {
      if (flushTimer) return
      flushTimer = setTimeout(flush, 200)
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
        SSE_TimeOut: 5,
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
              pendingStatus = proc.Status || pendingStatus
              scheduleFlush()
            }
          } catch { /* ignore */ }
        }
      }
      flush()
    } catch (e: any) {
      if (e.name !== 'AbortError') console.error('reloadNodeExecution 失败:', e)
    } finally {
      if (flushTimer) clearTimeout(flushTimer)
    }
  }

  async function handleTokenExpired() {
    await secureStorage.remove('member_token')
    await secureStorage.remove('access_token')
    isStreaming.value = false
    const currentPath = router.currentRoute.value.path
    if (currentPath !== '/login') {
      router.push('/login')
    }
  }

  onUnmounted(() => {
    cancelStream()
  })

  return {
    sendMessage,
    cancelStream,
    retryNode,
    reloadNodeExecution,
    isStreaming,
    streamError,
  }
}
