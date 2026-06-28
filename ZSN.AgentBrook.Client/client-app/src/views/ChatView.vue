<template>
  <div class="chat-view">
    <ChatContainer
      @retry-process="handleRetryProcess"
      @retry-node="handleRetryNode"
      @regenerate="handleRegenerate"
    />
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
const { sendMessage, regenerate, cancelStream, retryNode, isStreaming, streamError } = useChat()

const currentSessionStatus = computed(() => chatStore.currentSession?.SessionStatus ?? 0)

const effectiveRunning = computed(() => {
  // 服务端状态为运行中
  if (currentSessionStatus.value === 1) return true
  // 本地 SSE 流式传输：isStreaming 现在已经是"当前会话是否有活动流"的
  // computed（见 useChat.ts），无需再在这里校验 currentSessionId 归属。
  return isStreaming.value
})

function handleSend(content: string, files?: File[]) {
  const currentSession = chatStore.currentSession
  const appId = currentSession?.AppID || chatStore.selectedAppId || chatStore.apps[0]?.AppID || ''
  sendMessage(content, chatStore.currentSessionId, appId, files)
}

function handleRetryProcess(payload: { sessionId: string; processesId: string; messageId: string | null }) {
  // 直接重载整个会话的执行记录（而非单个 processesId），
  // 确保 ClawAI 子工作流（图片/视频生成等）的最新状态也被拉取。
  // 单 processesId 的 reloadNodeExecution 在 ClawAI 场景下会拿到
  // 主 ClawAI 节点的记录，遗漏子工作流的实际完成状态。
  chatStore.loadSessionExecutionRecords(payload.sessionId)
}

function handleRetryNode(payload: { nodeId: string; sessionId: string; processesId: string; taskId: string; messageId: string | null }) {
  retryNode(payload)
}

function handleRegenerate(messageId: string) {
  regenerate(messageId)
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
