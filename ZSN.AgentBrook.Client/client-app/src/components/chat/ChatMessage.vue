<template>
  <div :class="['chat-message', `role-${message.role}`]">
    <div class="message-avatar">
      <div class="avatar-icon">
        {{ message.role === 'user' ? '👤' : '🤖' }}
      </div>
    </div>

    <div class="message-content">
      <div class="message-header">
        <span class="message-role">{{ roleLabel }}</span>
        <span class="message-time">{{ formattedTime }}</span>
      </div>

      <!-- Process status -->
      <ProcessStatus
        v-if="message.process"
        :process-data="message.process"
        :auto-collapse="hasActiveHitl"
      />

      <!-- Markdown content -->
      <div v-if="message.content" class="message-text" v-html="renderedContent" />

      <!-- Images -->
      <div v-if="message.images && message.images.length" class="message-images">
        <div
          v-for="(image, index) in message.images"
          :key="index"
          class="image-item"
        >
          <img :src="image.url || (image as any)" :alt="image.name || '图片'" />
          <div v-if="image.name" class="image-name">{{ image.name }}</div>
        </div>
      </div>

      <!-- Files -->
      <div v-if="message.files && message.files.length" class="message-files">
        <div
          v-for="(file, index) in message.files"
          :key="index"
          class="file-item"
        >
          <div class="file-icon">📄</div>
          <div class="file-info">
            <div class="file-name">{{ file.name }}</div>
            <div class="file-size">{{ formatFileSize(file.size) }}</div>
          </div>
        </div>
      </div>

      <!-- Loading: streaming log panel + dots -->
      <div v-if="message.loading" class="message-loading-wrapper">
        <div v-if="streamText" ref="streamScrollRef" class="stream-scroll-panel">
          <pre class="stream-content">{{ streamText }}<span class="typing-cursor">▌</span></pre>
        </div>
        <div class="message-loading">
          <span class="loading-dot" />
          <span class="loading-dot" />
          <span class="loading-dot" />
        </div>
      </div>

      <!-- HITL panels from process records -->
      <template v-if="message.process && message.process.records">
        <template v-for="(rec, idx) in message.process.records" :key="getNodeKey(rec, idx)">
          <HitlInputPanel
            v-if="showHitl(rec) || showHitlInput(rec)"
            :hitl-data="getHITLData(rec)"
            :node-key="getNodeKey(rec, idx)"
            :is-submitting="submittingSet.has(getNodeKey(rec, idx))"
            :is-submitted="submittedSet.has(getNodeKey(rec, idx))"
            :submit-error-key="submitErrorKey"
            :submit-error-msg="submitErrorMsg"
            @submit="handleHitlSubmit"
          />
        </template>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, nextTick } from 'vue'
import { renderMarkdown } from '@/utils/markdown'
import { execHumanInTheLoop, execHumanInTheLoopByForm } from '@/services/hitl'
import ProcessStatus from './ProcessStatus.vue'
import HitlInputPanel from './HitlInputPanel.vue'
import type { ChatMessage, NormalizedRecord } from '@/types/chat'

const props = defineProps<{
  message: ChatMessage
}>()

const streamScrollRef = ref<HTMLElement | null>(null)

// Stream text from process.streamsByNode
const streamText = computed(() => {
  const streams = props.message?.process?.streamsByNode
  if (!streams || typeof streams !== 'object') return ''
  return Object.values(streams)
    .map((s: any) => s?.text || '')
    .filter(Boolean)
    .join('\n')
})

// Auto-scroll stream panel
watch(streamText, () => {
  nextTick(() => {
    const el = streamScrollRef.value
    if (el) el.scrollTop = el.scrollHeight
  })
})

const roleLabel = computed(() => {
  const labels: Record<string, string> = { user: '用户', assistant: 'AI 助手', system: '系统' }
  return labels[props.message.role] || props.message.role
})

const formattedTime = computed(() => {
  if (!props.message.createdAt) return ''
  const date = new Date(props.message.createdAt)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  if (diff < 60000) return '刚刚'
  if (diff < 3600000) return `${Math.floor(diff / 60000)} 分钟前`
  if (date.toDateString() === now.toDateString()) {
    return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  }
  return date.toLocaleString('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
})

const renderedContent = computed(() => renderMarkdown(props.message.content))

function formatFileSize(bytes: number): string {
  if (!bytes) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  let size = bytes
  let unitIndex = 0
  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024
    unitIndex++
  }
  return `${size.toFixed(1)} ${units[unitIndex]}`
}

// --- HITL logic ---

const hasActiveHitl = computed(() => {
  const records = props.message?.process?.records
  if (!Array.isArray(records)) return false
  return records.some((rec) => showHitl(rec) || showHitlInput(rec))
})

const submittingSet = ref(new Set<string>())
const submittedSet = ref(new Set<string>())
const submitErrorKey = ref<string | null>(null)
const submitErrorMsg = ref('')

function getNodeKey(rec: NormalizedRecord, idx: number): string {
  return rec?.recordId || `${rec?.nodeName || 'node'}-${rec?.startTime || ''}-${idx}`
}

function getOutputByVar(rec: NormalizedRecord, name: string): any {
  if (!rec || !Array.isArray(rec.outputs)) return undefined
  const item = rec.outputs.find((o) => o && o.varname === name)
  return item ? item.value : undefined
}

function getHITLData(rec: NormalizedRecord) {
  const nodeName = String(rec?.nodeName || '')

  const rawAskContent = getOutputByVar(rec, 'askContent') || ''
  let askContent = rawAskContent

  if (nodeName.startsWith('HumanInTheLoopInput')) {
    const newAskContent = getOutputByVar(rec, 'newAskContent') || ''
    if (newAskContent) askContent = rawAskContent ? `${rawAskContent}\n${newAskContent}` : newAskContent
  }

  let options = getOutputByVar(rec, 'options')
  if ((!options || options === 'null') && nodeName.startsWith('HumanInTheLoopInput')) {
    options = getOutputByVar(rec, 'inputOptions')
  }
  const reCallUrl = getOutputByVar(rec, 'reCallUrl')

  if (typeof options === 'string') {
    try { options = JSON.parse(options) } catch { /* keep as-is */ }
  }
  if (!Array.isArray(options)) options = []

  if (nodeName.startsWith('HumanInTheLoopInput')) {
    let parsedParams = getOutputByVar(rec, 'parsedParams')
    if (typeof parsedParams === 'string') {
      try { parsedParams = JSON.parse(parsedParams) } catch { /* keep */ }
    }
    if (Array.isArray(parsedParams) && Array.isArray(options)) {
      const valueMap = new Map<string, any>()
      parsedParams.forEach((p: any) => { if (p?.id != null) valueMap.set(p.id, p.value) })
      options = options.map((opt: any) => {
        if (!opt || opt.id == null) return opt
        return valueMap.has(opt.id) ? { ...opt, value: valueMap.get(opt.id) } : opt
      })
    }
  }

  return { askContent, options, reCallUrl }
}

function showHitl(rec: NormalizedRecord): boolean {
  if (!rec) return false
  const nodeName = String(rec.nodeName || '')
  if (!nodeName.startsWith('HumanInTheLoop') || nodeName.startsWith('HumanInTheLoopInput')) return false
  if (String(rec.status || '').toLowerCase() !== 'running') return false
  const d = getHITLData(rec)
  return !!(d.askContent && d.options && d.options.length && d.reCallUrl)
}

function showHitlInput(rec: NormalizedRecord): boolean {
  if (!rec) return false
  const nodeName = String(rec.nodeName || '')
  if (!nodeName.startsWith('HumanInTheLoopInput')) return false
  if (String(rec.status || '').toLowerCase() !== 'running') return false
  const d = getHITLData(rec)
  return !!(d.askContent && d.options && d.options.length && d.reCallUrl)
}

function parseReCallUrl(url: string) {
  try {
    const u = new URL(url)
    return {
      sessionID: u.searchParams.get('sessionID') || '',
      taskID: u.searchParams.get('taskID') || u.searchParams.get('taskId') || '',
      recordID: u.searchParams.get('recordID') || u.searchParams.get('recordId') || '',
    }
  } catch {
    return { sessionID: '', taskID: '', recordID: '' }
  }
}

async function handleHitlSubmit(payload: { nodeKey: string; reCallUrl: string; inputOptions: any[] }) {
  const { nodeKey, reCallUrl, inputOptions } = payload || {}
  if (!nodeKey || !reCallUrl) return

  submitErrorKey.value = null
  submitErrorMsg.value = ''
  submittingSet.value.add(nodeKey)
  submittingSet.value = new Set(submittingSet.value)

  try {
    const { sessionID, taskID, recordID } = parseReCallUrl(reCallUrl)
    const result = await execHumanInTheLoopByForm({
      sessionID, taskID, recordID,
      data: { inputOptions },
    })
    if (!result?.data?.Success) {
      throw new Error(result?.data?.ErrorDesc || '提交失败')
    }
    submittedSet.value.add(nodeKey)
    submittedSet.value = new Set(submittedSet.value)
  } catch (e: any) {
    submitErrorKey.value = nodeKey
    submitErrorMsg.value = e?.message || '提交失败'
  } finally {
    submittingSet.value.delete(nodeKey)
    submittingSet.value = new Set(submittingSet.value)
  }
}
</script>

<style lang="scss" scoped>
.chat-message {
  display: flex;
  gap: 12px;
  padding: 16px;
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.role-user {
  background-color: #f7f7f8;
}

.role-assistant {
  background-color: #ffffff;
}

.message-avatar {
  flex-shrink: 0;
}

.avatar-icon {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.role-user .avatar-icon {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.message-content {
  flex: 1;
  min-width: 0;
}

.message-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.message-role {
  font-weight: 600;
  font-size: 14px;
  color: #374151;
}

.message-time {
  font-size: 12px;
  color: #9ca3af;
}

.message-text {
  color: #1f2937;
  line-height: 1.6;
  word-wrap: break-word;
}

// Markdown deep styles
.message-text {
  :deep(p) { margin: 0 0 12px 0; &:last-child { margin-bottom: 0; } }
  :deep(h1), :deep(h2), :deep(h3), :deep(h4), :deep(h5), :deep(h6) {
    margin: 16px 0 8px 0; font-weight: 600; line-height: 1.3;
  }
  :deep(h1) { font-size: 1.5em; }
  :deep(h2) { font-size: 1.3em; }
  :deep(h3) { font-size: 1.1em; }
  :deep(code) {
    background-color: #f3f4f6; padding: 2px 6px; border-radius: 4px;
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace; font-size: 0.9em; color: #e11d48;
  }
  :deep(.code-block) {
    margin: 12px 0; border-radius: 8px; overflow: hidden; background-color: #1e293b;
  }
  :deep(.code-header) {
    display: flex; justify-content: space-between; align-items: center;
    padding: 8px 12px; background-color: #0f172a; border-bottom: 1px solid #334155;
  }
  :deep(.code-language) {
    font-size: 12px; color: #94a3b8; text-transform: uppercase; font-weight: 600;
  }
  :deep(.code-copy) {
    background: none; border: none; color: #94a3b8; cursor: pointer;
    padding: 4px; display: flex; align-items: center; transition: color 0.2s;
    &:hover { color: #e2e8f0; }
  }
  :deep(pre) { margin: 0; padding: 12px; overflow-x: auto; }
  :deep(pre code) {
    background: none; padding: 0; color: #e2e8f0; font-size: 13px; line-height: 1.5;
  }
  :deep(ul), :deep(ol) { margin: 8px 0; padding-left: 24px; }
  :deep(li) { margin: 4px 0; }
  :deep(blockquote) {
    margin: 12px 0; padding: 8px 16px; border-left: 4px solid #e5e7eb;
    background-color: #f9fafb; color: #6b7280;
  }
  :deep(a) { color: #3b82f6; text-decoration: none; &:hover { text-decoration: underline; } }
  :deep(table) { width: 100%; border-collapse: collapse; margin: 12px 0; }
  :deep(th), :deep(td) { border: 1px solid #e5e7eb; padding: 8px 12px; text-align: left; }
  :deep(th) { background-color: #f3f4f6; font-weight: 600; }
  :deep(.table-wrapper) { overflow-x: auto; margin: 12px 0; }
  :deep(hr) { border: none; border-top: 1px solid #e5e7eb; margin: 16px 0; }
  :deep(strong) { font-weight: 600; }
  :deep(em) { font-style: italic; }
}

// Images
.message-images {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
}

.image-item {
  position: relative;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
  transition: transform 0.2s;
  max-width: 200px;

  &:hover { transform: scale(1.02); }
  img { width: 100%; height: auto; display: block; border-radius: 8px; }
}

.image-name {
  position: absolute;
  bottom: 0; left: 0; right: 0;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  padding: 4px 8px;
  font-size: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

// Files
.message-files {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.file-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background-color: #f3f4f6;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.file-icon {
  font-size: 24px;
  flex-shrink: 0;
}

.file-info {
  flex: 1;
  min-width: 0;
}

.file-name {
  font-size: 14px;
  font-weight: 500;
  color: #374151;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.file-size {
  font-size: 12px;
  color: #9ca3af;
  margin-top: 2px;
}

// Loading / streaming
.message-loading-wrapper {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.stream-scroll-panel {
  height: 100px;
  overflow-y: auto;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  padding: 8px 12px;
  font-size: 12px;
  line-height: 1.5;
  color: #64748b;
  scroll-behavior: smooth;

  &::-webkit-scrollbar { width: 4px; }
  &::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 2px; }
}

.stream-content {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
}

.typing-cursor {
  animation: cursorBlink 1s step-end infinite;
  color: #3b82f6;
  font-weight: bold;
}

@keyframes cursorBlink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0; }
}

.message-loading {
  display: flex;
  gap: 6px;
  align-items: center;
}

.loading-dot {
  width: 6px;
  height: 6px;
  background: #9ca3af;
  border-radius: 50%;
  animation: blink 1.4s infinite both;

  &:nth-child(1) { animation-delay: -0.32s; }
  &:nth-child(2) { animation-delay: -0.16s; }
}

@keyframes blink {
  0%, 80%, 100% { opacity: 0; }
  40% { opacity: 1; }
}
</style>
