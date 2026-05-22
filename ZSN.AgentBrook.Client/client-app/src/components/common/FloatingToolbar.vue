<template>
  <div class="floating-toolbar">
    <template v-if="!isTauriEnv">
      <transition name="fab-fade">
        <div v-if="expanded" class="fab-menu">
          <el-tooltip content="语音输入" placement="left">
            <el-button circle class="fab-item" @click="showVoice = true">
              <el-icon><Microphone /></el-icon>
            </el-button>
          </el-tooltip>
          <el-tooltip content="快速对话" placement="left">
            <el-button circle class="fab-item" @click="showQuickChat = true">
              <el-icon><Edit /></el-icon>
            </el-button>
          </el-tooltip>
        </div>
      </transition>

      <el-button
        type="primary"
        circle
        size="large"
        class="fab-main"
        @click="expanded = !expanded"
      >
        <el-icon :size="24"><ChatDotRound /></el-icon>
      </el-button>

      <el-drawer v-model="showQuickChat" direction="rtl" size="360px" title="快速对话">
        <ChatContainer />
        <ChatInput @send="handleQuickSend" />
      </el-drawer>

      <el-dialog v-model="showVoice" title="语音输入" width="320px">
        <VoiceRecorder @insert="handleVoiceInsert" @send="handleVoiceSend" />
      </el-dialog>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ChatDotRound, Edit, Microphone } from '@element-plus/icons-vue'
import { isTauri } from '@/platform'
import ChatContainer from '@/components/chat/ChatContainer.vue'
import ChatInput from '@/components/chat/ChatInput.vue'
import VoiceRecorder from '@/components/common/VoiceRecorder.vue'

const isTauriEnv = isTauri()
const expanded = ref(false)
const showQuickChat = ref(false)
const showVoice = ref(false)

function handleQuickSend(content: string) {
  // TODO: 对接 useChat
  console.log('quick send:', content)
}

function handleVoiceInsert(_text: string) {
  showVoice.value = false
}

function handleVoiceSend(_text: string) {
  showVoice.value = false
}
</script>

<style lang="scss" scoped>
.floating-toolbar {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 9999;
  display: flex;
  flex-direction: column-reverse;
  align-items: center;
  gap: 12px;
}

.fab-main {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.fab-menu {
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: center;
}

.fab-item {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.fab-fade-enter-active,
.fab-fade-leave-active {
  transition: all 0.2s ease;
}

.fab-fade-enter-from,
.fab-fade-leave-to {
  opacity: 0;
  transform: translateY(10px);
}
</style>
