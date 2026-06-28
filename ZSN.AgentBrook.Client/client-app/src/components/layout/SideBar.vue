<template>
  <div class="sidebar-container">
    <div class="sidebar-header">
      <!-- Chat / Agent 模式切换（主界面左上区域，切换下方完整界面） -->
      <div class="mode-switch-wrap">
        <button
          class="mode-tab"
          :class="{ active: appStore.mode === 'chat' }"
          @click="switchMode('chat')"
        >
          <el-icon><ChatDotRound /></el-icon>
          <span>Chat</span>
        </button>
        <button
          class="mode-tab"
          :class="{ active: appStore.mode === 'agent' }"
          @click="switchMode('agent')"
        >
          <el-icon><MagicStick /></el-icon>
          <span>Agent</span>
        </button>
      </div>
      <el-button v-if="appStore.mode === 'chat'" type="primary" class="new-chat-btn" @click="emit('newChat')">
        <el-icon><Plus /></el-icon>
        {{ t('chat.newChat') }}
      </el-button>
    </div>

    <!-- Chat 模式：会话列表；Agent 模式：会话列表 + 简短提示 -->
    <div class="sidebar-sessions" v-if="appStore.mode === 'chat'">
      <SessionList />
    </div>
    <div class="sidebar-sessions agent-session-wrap" v-else>
      <AgentSessionList />
      <div class="agent-tip-mini">
        <el-icon><InfoFilled /></el-icon>
        <span>{{ t('agent.agentTip') }}</span>
      </div>
    </div>

    <div class="sidebar-footer">
      <div class="user-info" v-if="userStore.userInfo" @click="toggleSettings">
        <el-avatar :size="32" :src="userStore.userInfo.avatar">
          {{ userStore.userInfo.name?.charAt(0) || '?' }}
        </el-avatar>
        <span class="user-name">{{ userStore.userInfo.name }}</span>
      </div>
      <el-button text @click="toggleSettings">
        <el-icon><Setting /></el-icon>
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Plus, Setting, ChatDotRound, MagicStick, InfoFilled } from '@element-plus/icons-vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user'
import { useAppStore, type AppMode } from '@/stores/app'
import SessionList from '@/components/chat/SessionList.vue'
import AgentSessionList from '@/components/agent/AgentSessionList.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const appStore = useAppStore()
const emit = defineEmits<{ newChat: [] }>()

/** 切换 Chat / Agent 模式：更新 store mode + 跳转对应路由（下方完整界面切换）。 */
function switchMode(mode: AppMode) {
  if (appStore.mode === mode) return
  appStore.setMode(mode)
  if (mode === 'agent') {
    router.push('/agent')
  } else {
    router.push('/chat')
  }
}

function toggleSettings() {
  if (route.path === '/settings') {
    router.push('/chat')
  } else {
    router.push('/settings')
  }
}
</script>

<style lang="scss" scoped>
.sidebar-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 12px;
}

.sidebar-header {
  margin-bottom: 12px;
}

/* Chat / Agent 模式切换器（左上区域） */
.mode-switch-wrap {
  display: flex;
  gap: 4px;
  padding: 3px;
  margin-bottom: 10px;
  background: var(--el-fill-color-light, #f5f7fa);
  border-radius: 8px;
}

.mode-tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  padding: 7px 0;
  border: none;
  background: transparent;
  border-radius: 6px;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  color: var(--text-secondary, #909399);
  transition: all 0.2s;

  &:hover {
    color: var(--text-primary, #303133);
  }

  &.active {
    background: var(--bg-card, #fff);
    color: var(--el-color-primary, #409eff);
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  }

  .el-icon {
    font-size: 15px;
  }
}

.new-chat-btn {
  width: 100%;
}

.sidebar-sessions {
  flex: 1;
  overflow-y: auto;
}

.sidebar-sessions.agent-session-wrap {
  display: flex;
  flex-direction: column;
  padding: 0;
  overflow: hidden;
}

.agent-tip-mini {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  padding: 10px 14px;
  margin: 0 12px 12px;
  border-radius: 8px;
  background: var(--el-color-primary-light-9, #ecf5ff);
  border: 1px solid var(--el-color-primary-light-7, #d9ecff);
  font-size: 12px;
  line-height: 1.6;
  color: var(--text-regular, #606266);
  flex-shrink: 0;

  .el-icon {
    flex-shrink: 0;
    font-size: 14px;
    color: var(--el-color-primary, #409eff);
    margin-top: 2px;
  }
}

.sidebar-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
  border-top: 1px solid var(--border-color, #e4e7ed);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  border-radius: 6px;
  padding: 4px;
  transition: background 0.2s;

  &:hover {
    background: var(--el-fill-color-light);
  }
}

.user-name {
  font-size: 14px;
  color: var(--text-primary, #303133);
}
</style>
