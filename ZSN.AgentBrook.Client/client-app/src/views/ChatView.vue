<template>
  <div class="chat-view">
    <ChatContainer />
    <ChatInput
      class="floating-input"
      :is-streaming="effectiveRunning"
      :session-status="currentSessionStatus"
      @send="handleSend"
      @cancel="cancelStream"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import ChatContainer from '@/components/chat/ChatContainer.vue'
import ChatInput from '@/components/chat/ChatInput.vue'
import { useChat } from '@/composables/useChat'
import { useChatStore } from '@/stores/chat'

const chatStore = useChatStore()
const { sendMessage, cancelStream, isStreaming, streamError } = useChat()

const currentSessionStatus = computed(() => chatStore.currentSession?.SessionStatus ?? 0)

const effectiveRunning = computed(() => {
  // 服务端状态为运行中
  if (currentSessionStatus.value === 1) return true
  // 本地 SSE 流式传输：仅当属于当前会话时生效
  if (isStreaming.value) {
    if (!chatStore.currentSessionId) return true
    return chatStore.runningSessionIds.includes(chatStore.currentSessionId)
  }
  return false
})

function handleSend(content: string, files?: File[]) {
  const currentSession = chatStore.currentSession
  const appId = currentSession?.AppID || chatStore.selectedAppId || chatStore.apps[0]?.AppID || ''
  sendMessage(content, chatStore.currentSessionId, appId, files)
}
</script>

<style lang="scss" scoped>
.chat-view {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--bg-primary, #f5f7fa);
}

.floating-input {
  position: absolute;
  bottom: 20px;
  left: 20px;
  right: 20px;
  z-index: 10;
}
</style>
