<template>
  <div class="voice-recorder">
    <el-button
      :type="isRecording ? 'danger' : 'primary'"
      circle
      :size="size"
      @click="toggleRecording"
    >
      <el-icon :size="20">
        <Microphone v-if="!isRecording" />
        <VideoPause v-else />
      </el-icon>
    </el-button>

    <div v-if="voiceStore.state !== 'idle'" class="voice-status">
      <span v-if="voiceStore.state === 'connecting'" class="status-connecting">
        连接中...
      </span>
      <span v-else-if="voiceStore.state === 'recording'" class="status-recording">
        <span class="pulse-dot" /> 录音中
      </span>
      <span v-else-if="voiceStore.state === 'error'" class="status-error">
        识别失败
      </span>
    </div>

    <div v-if="displayText" class="voice-result">
      <el-input
        v-model="editableText"
        type="textarea"
        :rows="3"
        placeholder="识别结果"
      />
      <div class="voice-actions">
        <el-button size="small" @click="emit('insert', editableText)">
          插入到输入框
        </el-button>
        <el-button size="small" type="primary" @click="emit('send', editableText)">
          直接发送
        </el-button>
      </div>
    </div>

    <el-select v-model="voiceStore.language" size="small" class="lang-select">
      <el-option label="中文" value="zh" />
      <el-option label="英文" value="en" />
      <el-option label="中英混合" value="mixed" />
    </el-select>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Microphone, VideoPause } from '@element-plus/icons-vue'
import { useVoiceStore } from '@/stores/voice'
import { useVoice } from '@/composables/useVoice'

defineProps<{
  size?: 'small' | 'default' | 'large'
}>()

const emit = defineEmits<{
  insert: [text: string]
  send: [text: string]
}>()

const voiceStore = useVoiceStore()
const { startRecording, stopRecording, getFullText } = useVoice()
const editableText = ref('')

const isRecording = computed(() =>
  voiceStore.state === 'recording' || voiceStore.state === 'connecting',
)

const displayText = computed(() => getFullText())

watch(displayText, (text) => {
  editableText.value = text
})

async function toggleRecording() {
  if (isRecording.value) {
    await stopRecording()
  } else {
    await startRecording()
  }
}
</script>

<style lang="scss" scoped>
.voice-recorder {
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: center;
}

.voice-status {
  font-size: 12px;
}

.status-connecting { color: var(--el-color-warning); }
.status-recording { color: var(--el-color-danger); }
.status-error { color: var(--el-color-danger); }

.pulse-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--el-color-danger);
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}

.voice-result {
  width: 100%;
}

.voice-actions {
  display: flex;
  gap: 8px;
  margin-top: 8px;
  justify-content: flex-end;
}

.lang-select {
  width: 100px;
}
</style>
