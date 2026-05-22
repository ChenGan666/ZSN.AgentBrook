<template>
  <div class="chat-input-wrapper">
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
      <div class="input-area">
        <el-input
          ref="inputRef"
          v-model="inputText"
          type="textarea"
          :autosize="{ minRows: 1, maxRows: 6 }"
          :placeholder="voiceState === 'recording' ? '正在聆听...' : '输入消息...'"
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
        <el-button
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
import { Paperclip, Promotion, Document, Close, Microphone } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useSettingsStore } from '@/stores/settings'
import { useVoiceStore } from '@/stores/voice'
import { useFileUpload } from '@/composables/useFileUpload'
import { useVoice } from '@/composables/useVoice'

const props = defineProps<{
  isStreaming?: boolean
}>()

const emit = defineEmits<{
  send: [content: string, files?: File[]]
  cancel: []
}>()

const settingsStore = useSettingsStore()
const voiceStore = useVoiceStore()
const { pickFiles, validateFile } = useFileUpload()
const { startRecording, stopRecording } = useVoice()
const inputRef = ref()
const inputText = ref('')
const pendingFiles = ref<File[]>([])
// 语音录音期间：在光标位置插入文本
let textBeforeCursor = ''
let textAfterCursor = ''
let lastRecognized = ''

const voiceState = computed(() => voiceStore.state)

const canSend = computed(() => {
  return (inputText.value.trim() || pendingFiles.value.length > 0) && !props.isStreaming
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
      ElMessage.error(e.message || '录音启动失败')
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
      ElMessage.warning('最多 5 个附件')
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
  border-top: 1px solid var(--border-color, #e4e7ed);
  background: var(--bg-card, #fff);
}

.pending-files {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 8px 20px 0;
}

.file-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: var(--el-color-primary-light-9);
  border-radius: 4px;
  font-size: 12px;
}

.file-size {
  color: var(--text-secondary);
}

.chat-input {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  padding: 12px 20px;
}

.input-area {
  flex: 1;
}

.input-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

// 麦克风录音动画
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
</style>
