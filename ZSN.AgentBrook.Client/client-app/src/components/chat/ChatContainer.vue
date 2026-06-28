<template>
  <div
    class="chat-container"
    ref="containerRef"
    v-loading="chatStore.loadingMessages"
    :element-loading-text="t('chat.loadingMessages')"
    @scroll="onScroll"
  >
    <div class="messages-wrapper">
      <SessionStatusBanner />
      <ChatMessage
        v-for="msg in chatStore.messages"
        :key="msg.id"
        :message="msg"
        @retry-process="(payload) => emit('retryProcess', payload)"
        @retry-node="(payload) => emit('retryNode', payload)"
        @regenerate="(messageId) => emit('regenerate', messageId)"
      />
      <div v-if="!chatStore.loadingMessages && chatStore.messages.length === 0" class="empty-state">
        <p>{{ t('chat.startNewConversation') }}</p>
      </div>
    </div>
    <transition name="fade">
      <div v-if="showNewMessageHint" class="new-message-hint" @click="scrollToBottom">
        {{ t('chat.newMessageHint') }}
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '@/stores/chat'
import ChatMessage from './ChatMessage.vue'
import SessionStatusBanner from './SessionStatusBanner.vue'

const { t } = useI18n()

const emit = defineEmits<{
  retryProcess: [payload: { sessionId: string; processesId: string; messageId: string | null }]
  retryNode: [payload: { nodeId: string; sessionId: string; processesId: string; taskId: string; messageId: string | null }]
  regenerate: [messageId: string]
}>()

const chatStore = useChatStore()
const containerRef = ref<HTMLElement | null>(null)
const isAtBottom = ref(true)
const showNewMessageHint = ref(false)

function scrollToBottom() {
  if (!containerRef.value) return
  containerRef.value.scrollTop = containerRef.value.scrollHeight
  showNewMessageHint.value = false
}

function onScroll() {
  const el = containerRef.value
  if (!el) return
  isAtBottom.value = el.scrollHeight - el.scrollTop - el.clientHeight < 50
}

watch(
  () => chatStore.currentSessionId,
  async () => {
    isAtBottom.value = true
    showNewMessageHint.value = false
    await nextTick()
    scrollToBottom()
  },
)

watch(
  () => chatStore.messages.length,
  async () => {
    if (isAtBottom.value) {
      await nextTick()
      scrollToBottom()
    } else {
      const lastMsg = chatStore.messages[chatStore.messages.length - 1]
      if (lastMsg?.role === 'assistant') {
        showNewMessageHint.value = true
      }
    }
  },
)

watch(
  () => chatStore.messages[chatStore.messages.length - 1]?.content,
  async () => {
    if (isAtBottom.value) {
      await nextTick()
      scrollToBottom()
    }
  },
)
</script>

<style lang="scss" scoped>
.chat-container {
  flex: 1;
  overflow-y: auto;
  position: relative;
}

.messages-wrapper {
  padding: 16px 0 100px;
}

.empty-state {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;
  color: var(--text-secondary, #909399);
  font-size: 16px;
}

.new-message-hint {
  position: absolute;
  bottom: 16px;
  left: 50%;
  transform: translateX(-50%);
  padding: 6px 16px;
  background: var(--el-color-primary);
  color: #fff;
  border-radius: 16px;
  cursor: pointer;
  font-size: 13px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>
