<template>
  <div class="chat-container" ref="containerRef" @scroll="onScroll">
    <div class="messages-wrapper">
      <ChatMessage
        v-for="msg in chatStore.messages"
        :key="msg.id"
        :message="msg"
      />
      <div v-if="chatStore.messages.length === 0" class="empty-state">
        <p>开始新的对话</p>
      </div>
    </div>
    <transition name="fade">
      <div v-if="showNewMessageHint" class="new-message-hint" @click="scrollToBottom">
        有新消息，点击查看
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useChatStore } from '@/stores/chat'
import ChatMessage from './ChatMessage.vue'

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
  padding: 16px 0;
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
