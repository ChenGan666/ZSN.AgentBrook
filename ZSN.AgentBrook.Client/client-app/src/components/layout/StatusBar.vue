<template>
  <div class="status-bar">
    <div class="status-left">
      <el-button text @click="emit('toggleSidebar')">
        <el-icon><Fold /></el-icon>
      </el-button>
      <span class="app-name">{{ sessionTitle }}</span>
    </div>

    <div class="status-right">
      <span class="connection-info">
        <span class="connection-dot" :class="connectionClass" />
        <span class="connection-text">{{ connectionText }}</span>
      </span>
      <el-dropdown trigger="click">
        <el-avatar :size="28" :src="userStore.userInfo?.avatar">
          {{ userStore.userInfo?.name?.charAt(0) || '?' }}
        </el-avatar>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item @click="router.push('/settings')">{{ t('status.settings') }}</el-dropdown-item>
            <el-dropdown-item divided @click="handleLogout">{{ t('status.logout') }}</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Fold } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user'
import { useAppStore } from '@/stores/app'
import { useChatStore } from '@/stores/chat'
import { useAgentStore } from '@/stores/agent'
import { useAuth } from '@/composables/useAuth'

const { t } = useI18n()
const router = useRouter()
const userStore = useUserStore()
const appStore = useAppStore()
const chatStore = useChatStore()
const agentStore = useAgentStore()
const { logout } = useAuth()

const sessionTitle = computed(() => {
  if (appStore.mode === 'agent') {
    const session = agentStore.currentSessionId
      ? agentStore.sessions.find((s) => s.id === agentStore.currentSessionId)
      : null
    return session?.title || t('agent.newSession')
  }
  if (chatStore.currentSessionId && chatStore.currentSession) {
    return chatStore.currentSession.TopicSummary || t('chat.newConversation')
  }
  return t('chat.newConversation')
})

const emit = defineEmits<{ toggleSidebar: [] }>()

const connectionClass = computed(() => ({
  connected: appStore.connectionStatus === 'connected',
  slow: appStore.connectionStatus === 'slow',
  disconnected: appStore.connectionStatus === 'disconnected',
}))

const connectionText = computed(() => {
  switch (appStore.connectionStatus) {
    case 'connected': return t('status.connected')
    case 'slow': return t('status.slowWithLatency', { latency: appStore.apiLatency })
    case 'disconnected': return t('status.disconnected')
    default: return t('status.checking')
  }
})

async function handleLogout() {
  await logout()
}
</script>

<style lang="scss" scoped>
.status-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid var(--border-color, #e4e7ed);
  background: var(--bg-card, #fff);
}

.status-left, .status-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-left {
  flex: 1 1 auto;
  min-width: 0;
}

.status-right {
  flex-shrink: 0;
}

.connection-info {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--text-secondary, #909399);
  flex-shrink: 0;
  white-space: nowrap;
}

.connection-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  &.connected { background: #67c23a; }
  &.slow { background: #e6a23c; }
  &.disconnected { background: #f56c6c; }
}

.app-name {
  font-size: 14px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
