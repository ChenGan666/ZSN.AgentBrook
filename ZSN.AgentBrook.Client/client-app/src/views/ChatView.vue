<template>
  <div class="chat-view">
    <ChatContainer />
    <ChatInput
      :is-streaming="isStreaming"
      @send="handleSend"
      @cancel="cancelStream"
    />
  </div>
</template>

<script setup lang="ts">
import ChatContainer from '@/components/chat/ChatContainer.vue'
import ChatInput from '@/components/chat/ChatInput.vue'
import { useChat } from '@/composables/useChat'
import { useChatStore } from '@/stores/chat'

const chatStore = useChatStore()
const { sendMessage, cancelStream, isStreaming, streamError } = useChat()

function handleSend(content: string, files?: File[]) {
  const currentSession = chatStore.currentSession
  const appId = currentSession?.AppID || chatStore.selectedAppId || chatStore.apps[0]?.AppID || ''
  sendMessage(content, chatStore.currentSessionId, appId, files)
}
</script>

<style lang="scss" scoped>
.chat-view {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--bg-primary, #f5f7fa);
}

.stream-error {
  padding: 0 20px 12px;
}
</style>
