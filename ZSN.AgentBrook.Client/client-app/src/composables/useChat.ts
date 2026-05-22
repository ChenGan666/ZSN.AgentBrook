import { ref, onUnmounted } from 'vue'
import { useChatStore } from '@/stores/chat'
import { secureStorage } from '@/utils/storage'
import { getChatCompletionsUrl } from '@/services/chat'
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

const TOKEN_CHECK_ERROR = 80001
const MEMBER_TOKEN_CHECK_ERROR = 80002
const STREAM_UI_FLUSH_MS = 200
const STREAM_MAX_CHARS_PER_NODE = 200000
const STREAM_TAIL_CHARS = 1200

export function useChat() {
  const chatStore = useChatStore()
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

  function normalizeRecord(r: ExecutionRecordInfo): NormalizedRecord {
    const outputs = Array.isArray(r.Outputs)
      ? r.Outputs.map((o) => {
          let parsedValue: any = o && typeof o.value === 'string' ? o.value : o?.value || ''
          if (typeof parsedValue === 'string' && parsedValue) {
            try {
              parsedValue = JSON.parse(parsedValue)
            } catch { /* keep as string */ }
          }
          return { varname: o?.varname, type: o?.type, value: parsedValue }
        })
      : []
    return {
      recordId: r.RecordID,
      sessionId: r.SessionID,
      processesId: r.ProcessesID,
      workflowId: r.WorkflowID,
      taskId: r.TaskID,
      fromMainTaskId: r.FromMainTaskID,
      nodeId: r.NodeID,
      nodeName: r.NodeName,
      nextNodeId: r.NextNodeID,
      startTime: r.StartTime,
      endTime: r.EndTime,
      status: r.Status,
      inputs: r.Inputs,
      outputs,
      logs: Array.isArray(r.Logs) ? r.Logs : [],
    }
  }

  function mergeRecords(
    prev: NormalizedRecord[],
    incoming: NormalizedRecord[],
  ): NormalizedRecord[] {
    const map = new Map<string, NormalizedRecord>()
    for (const r of prev) map.set(r.recordId, r)
    for (const r of incoming) map.set(r.recordId, r)
    return Array.from(map.values())
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

    await doSSERequest(content, sessionId, appId, aiMsgId, false, attachments)
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

    // pending state for batched UI flush
    let pendingProcess: any = null
    let pendingIncomingRecords: NormalizedRecord[] = []
    let pendingStreamEnvelope: StreamEnvelopeItem[] = []
    let pendingFinalText = ''
    let pendingStatus = ''
    let pendingTimestamp: number | null = null
    let processFlushTimer: ReturnType<typeof setTimeout> | null = null

    const assistantMessage = () => chatStore.messages.find((m) => m.id === aiMsgId)

    const flushProcessUI = () => {
      processFlushTimer = null
      const msg = assistantMessage()
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
      const msg = assistantMessage()
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

      // Extract SessionID
      if (messageData.SessionID) {
        chatStore.currentSessionId = messageData.SessionID
        msg.sessionId = messageData.SessionID
      } else if (messageData.ProcessInfo?.SessionID) {
        chatStore.currentSessionId = messageData.ProcessInfo.SessionID
        msg.sessionId = messageData.ProcessInfo.SessionID
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

        // Terminal status
        if (
          status &&
          (String(status).toLowerCase() === 'success' ||
            String(status).toLowerCase() === 'failed' ||
            String(status).toLowerCase() === 'error')
        ) {
          msg.loading = false
          flushProcessUI()
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
      const msg = assistantMessage()
      if (msg) {
        msg.loading = false
        chatStore.updateMessage(aiMsgId, msg)
      }
    } catch (error: any) {
      if (error.name === 'AbortError') return
      const msg = assistantMessage()
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
    isStreaming,
    streamError,
  }
}
