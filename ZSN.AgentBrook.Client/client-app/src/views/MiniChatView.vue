<template>
  <div class="mini-chat-view">
    <div class="mini-titlebar" data-tauri-drag-region>
      <span>快速对话</span>
      <el-button text size="small" @click="closeWindow">
        <el-icon><Close /></el-icon>
      </el-button>
    </div>

    <ChatContainer />
    <ChatInput @send="handleSend" />
  </div>
</template>

<script setup lang="ts">
import { Close } from '@element-plus/icons-vue'
import { isTauri } from '@/platform'
import ChatContainer from '@/components/chat/ChatContainer.vue'
import ChatInput from '@/components/chat/ChatInput.vue'

async function closeWindow() {
  if (!isTauri()) return
  const { getCurrentWindow } = await import('@tauri-apps/api/window')
  await getCurrentWindow().close()
}

function handleSend(content: string, files?: File[]) {
  // TODO: 与 useChat 对接
  console.log('mini send:', content, files)
}
</script>

<style lang="scss" scoped>
.mini-chat-view {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.mini-titlebar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 8px;
  background: var(--bg-card, #fff);
  border-bottom: 1px solid var(--border-color, #e4e7ed);
  font-size: 13px;
  user-select: none;
  -webkit-user-select: none;
}
</style>
