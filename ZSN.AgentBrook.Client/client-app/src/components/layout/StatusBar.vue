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
            <el-dropdown-item @click="router.push('/settings')">设置</el-dropdown-item>
            <el-dropdown-item divided @click="handleLogout">退出登录</el-dropdown-item>
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
import { useUserStore } from '@/stores/user'
import { useAppStore } from '@/stores/app'
import { useChatStore } from '@/stores/chat'
import { useAuth } from '@/composables/useAuth'

const router = useRouter()
const userStore = useUserStore()
const appStore = useAppStore()
const chatStore = useChatStore()
const { logout } = useAuth()

const sessionTitle = computed(() => {
  if (chatStore.currentSessionId && chatStore.currentSession) {
    return chatStore.currentSession.TopicSummary || '新对话'
  }
  return '新对话'
})

const emit = defineEmits<{ toggleSidebar: [] }>()

const connectionClass = computed(() => ({
  connected: appStore.connectionStatus === 'connected',
  slow: appStore.connectionStatus === 'slow',
  disconnected: appStore.connectionStatus === 'disconnected',
}))

const connectionText = computed(() => {
  switch (appStore.connectionStatus) {
    case 'connected': return '已连接'
    case 'slow': return `连接缓慢 (${appStore.apiLatency}ms)`
    case 'disconnected': return '连接断开'
    default: return '检测中...'
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

.connection-info {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--text-secondary, #909399);
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
}
</style>
