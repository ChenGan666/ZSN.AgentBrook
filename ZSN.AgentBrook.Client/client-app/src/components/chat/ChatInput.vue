<template>
  <div class="chat-input-wrapper" :class="wrapperClass">
    <div v-if="pendingFiles.length" class="pending-files">
      <span v-for="(f, i) in pendingFiles" :key="i" class="file-tag">
        <el-icon><Document /></el-icon>
        {{ f.name }}
        <span class="file-size">{{ formatSize(f.size) }}</span>
        <el-button text size="small" @click="pendingFiles.splice(i, 1)">
          <el-icon><Close /></el-icon>
        </el-button>
      </span>
    </div>

    <div class="chat-input">
      <div class="app-selector" title="切换应用" @click="showAppDialog = true">
        <span class="app-selector-name">{{ currentAppName }}</span>
        <el-icon :size="12"><ArrowUp /></el-icon>
      </div>

      <el-dialog v-model="showAppDialog" :title="t('chat.selectApp')" width="400px" append-to-body>
        <div class="app-dialog-list">
          <div
            v-for="app in chatStore.apps"
            :key="app.AppID"
            class="app-dialog-item"
            :class="{ active: app.AppID === chatStore.selectedAppId }"
            @click="onSelectApp(app)"
          >
            <span class="app-dialog-name">{{ app.Name }}</span>
            <span v-if="app.Description" class="app-dialog-desc">{{ app.Description }}</span>
          </div>
        </div>
      </el-dialog>

      <div class="input-area">
        <el-input
          ref="inputRef"
          v-model="inputText"
          type="textarea"
          :autosize="{ minRows: 1, maxRows: 6 }"
          :placeholder="placeholderText"
          :disabled="isRunning"
          resize="none"
          @keydown="handleKeydown"
          @paste="handlePaste"
        />
      </div>
      <div class="input-actions">
        <el-button
          text
          :class="{ 'mic-recording': voiceState === 'recording', 'mic-connecting': voiceState === 'connecting' }"
          @click="toggleVoice"
        >
          <el-icon><Microphone /></el-icon>
        </el-button>
        <el-button text @click="triggerFilePick">
          <el-icon><Paperclip /></el-icon>
        </el-button>
        <!-- 对话处理中：发送按钮切换为强制停止按钮，点击中断当前会话的 SSE 流。
             之前这里只有 :disabled 的发送按钮，emit('cancel') 从无触发点，因此
             缺少强制停止功能。 -->
        <el-button
          v-if="isRunning"
          type="danger"
          :icon="VideoPause"
          circle
          :title="t('chat.stop')"
          @click="emit('cancel')"
        />
        <el-button
          v-else
          type="primary"
          :icon="Promotion"
          circle
          :disabled="!canSend"
          @click="handleSend"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Paperclip, Promotion, Document, Close, Microphone, ArrowUp, VideoPause } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { useSettingsStore } from '@/stores/settings'
import { useVoiceStore } from '@/stores/voice'
import { useChatStore } from '@/stores/chat'
import type { AppInfo } from '@/types/chat'
import { useFileUpload } from '@/composables/useFileUpload'
import { useVoice } from '@/composables/useVoice'

const props = defineProps<{
  isStreaming?: boolean
  sessionStatus?: number
}>()

const emit = defineEmits<{
  send: [content: string, files?: File[]]
  cancel: []
}>()

const { t } = useI18n()
const settingsStore = useSettingsStore()
const voiceStore = useVoiceStore()
const chatStore = useChatStore()
const { pickFiles, validateFile } = useFileUpload()
const { startRecording, stopRecording } = useVoice()
const inputRef = ref()
const showAppDialog = ref(false)
const inputText = ref('')
const pendingFiles = ref<File[]>([])

const currentAppName = computed(() => {
  const appId = chatStore.selectedAppId
  if (!appId) return ''
  const app = chatStore.apps.find((a) => a.AppID === appId)
  return app?.Name || ''
})

const isRunning = computed(() => props.isStreaming || props.sessionStatus === 1)
const isFailed = computed(() => !props.isStreaming && props.sessionStatus === -1)

const wrapperClass = computed(() => ({
  'is-waiting': isRunning.value,
  'is-failed': isFailed.value,
}))

const placeholderText = computed(() => {
  if (isRunning.value) return t('chat.waitingForReply')
  if (voiceState.value === 'recording') return t('chat.listening')
  return t('chat.inputPlaceholder')
})

function onSelectApp(app: AppInfo) {
  chatStore.selectedAppId = app.AppID
  showAppDialog.value = false
}
// 语音录音期间：在光标位置插入文本
let textBeforeCursor = ''
let textAfterCursor = ''
let lastRecognized = ''

const voiceState = computed(() => voiceStore.state)

const canSend = computed(() => {
  return (inputText.value.trim() || pendingFiles.value.length > 0) && !isRunning.value
})

// 实时将语音识别结果插入到光标位置
watch(() => voiceStore.recognizedText + '|' + voiceStore.interimText, () => {
  const recognized = voiceStore.recognizedText
  const interim = voiceStore.interimText
  if (recognized.length > lastRecognized.length) {
    const newPart = recognized.slice(lastRecognized.length)
    lastRecognized = recognized
    textBeforeCursor += newPart
  }
  inputText.value = textBeforeCursor + interim + textAfterCursor
})

async function toggleVoice() {
  if (voiceState.value === 'recording' || voiceState.value === 'connecting') {
    await stopRecording()
    inputText.value = textBeforeCursor + textAfterCursor
    textBeforeCursor = ''
    textAfterCursor = ''
    lastRecognized = ''
  } else {
    // 获取光标位置，分割文本
    const textarea = inputRef.value?.textarea || inputRef.value?.$el?.querySelector('textarea')
    const cursorPos = textarea?.selectionStart ?? inputText.value.length
    textBeforeCursor = inputText.value.slice(0, cursorPos)
    textAfterCursor = inputText.value.slice(cursorPos)
    lastRecognized = ''
    try {
      await startRecording()
    } catch (e: any) {
      ElMessage.error(e.message || t('chat.recordingFailed'))
    }
  }
}

function handleKeydown(e: Event | KeyboardEvent) {
  if (!(e instanceof KeyboardEvent)) return
  if (voiceState.value === 'recording') return
  const sendKey = settingsStore.sendKey
  const shouldSend =
    (sendKey === 'enter' && e.key === 'Enter' && !e.ctrlKey && !e.shiftKey) ||
    (sendKey === 'ctrl-enter' && e.key === 'Enter' && e.ctrlKey)

  if (shouldSend) {
    e.preventDefault()
    handleSend()
  }
}

function handlePaste(e: ClipboardEvent) {
  const items = e.clipboardData?.items
  if (!items) return
  for (const item of items) {
    if (item.type.startsWith('image/')) {
      const file = item.getAsFile()
      if (file) {
        pendingFiles.value.push(file)
      }
    }
  }
}

async function triggerFilePick() {
  const files = await pickFiles({ multiple: true })
  for (const file of files) {
    if (pendingFiles.value.length >= 5) {
      ElMessage.warning(t('chat.maxAttachments'))
      break
    }
    const error = validateFile(file)
    if (error) {
      ElMessage.warning(`${file.name}: ${error}`)
      continue
    }
    pendingFiles.value.push(file)
  }
}

function handleSend() {
  if (!canSend.value) return
  emit('send', inputText.value.trim(), pendingFiles.value.length ? pendingFiles.value : undefined)
  inputText.value = ''
  pendingFiles.value = []
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes}B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)}KB`
  return `${(bytes / 1024 / 1024).toFixed(1)}MB`
}
</script>

<style lang="scss" scoped>
.chat-input-wrapper {
  background: var(--bg-card, #fff);
  border-radius: 24px;
  border: 1px solid var(--border-color, #e4e7ed);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  overflow: hidden;
}

.pending-files {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 10px 16px 0;
}

.file-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: var(--el-color-primary-light-9);
  border-radius: 12px;
  font-size: 12px;
}

.file-size {
  color: var(--text-secondary);
}

.chat-input {
  display: flex;
  align-items: flex-end;
  gap: 4px;
  padding: 8px 12px 8px 16px;
}

.input-area {
  flex: 1;

  :deep(.el-textarea__inner) {
    border: none;
    box-shadow: none !important;
    background: transparent;
    padding: 4px 0;
    resize: none;
  }
}

.input-actions {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

.app-selector {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 12px;
  border-radius: 16px;
  background: var(--el-color-primary-light-9);
  cursor: pointer;
  white-space: nowrap;
  flex-shrink: 0;
  transition: background 0.2s;

  &:hover {
    background: var(--el-color-primary-light-8);
  }
}

.app-selector-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--el-color-primary);
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.app-dialog-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.app-dialog-item {
  padding: 12px;
  border: 1px solid var(--border-color, #e4e7ed);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
  }

  &.active {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-8);
  }
}

.app-dialog-name {
  font-size: 14px;
  font-weight: 500;
  display: block;
}

.app-dialog-desc {
  font-size: 12px;
  color: var(--text-secondary, #909399);
  margin-top: 4px;
  display: block;
}

.mic-connecting {
  color: var(--el-color-warning);
}

.mic-recording {
  position: relative;
  color: var(--el-color-danger) !important;
  animation: mic-pulse 1.5s ease-in-out infinite;
}

@keyframes mic-pulse {
  0%, 100% {
    background-color: transparent;
    transform: scale(1);
  }
  50% {
    background-color: rgba(245, 108, 108, 0.15);
    transform: scale(1.1);
  }
}

.chat-input-wrapper.is-waiting {
  animation: border-breathe 2.4s ease-in-out infinite;

  .input-area :deep(.el-textarea__inner) {
    cursor: not-allowed;
  }
}

@keyframes border-breathe {
  0%, 100% {
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08), 0 0 0 0 rgba(64, 158, 255, 0);
  }
  50% {
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08), 0 0 12px 4px rgba(64, 158, 255, 0.25);
  }
}

.chat-input-wrapper.is-failed {
  border-color: var(--el-color-danger, #f56c6c);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08), 0 0 8px 2px rgba(245, 108, 108, 0.15);
}
</style>
