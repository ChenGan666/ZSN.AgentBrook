<template>
  <div class="session-list">
    <div class="session-toolbar">
      <el-input
        v-model="searchQuery"
        :placeholder="t('chat.searchSession')"
        :prefix-icon="Search"
        clearable
        size="small"
      />
      <el-button text size="small" @click="handleClearAll" :title="t('chat.clearAll')">
        <el-icon><Delete /></el-icon>
      </el-button>
    </div>

    <div
      ref="scrollRef"
      class="session-items"
      v-loading="chatStore.loadingSessions"
      @scroll="onScroll"
    >
      <template v-for="group in groupedSessions" :key="group.label">
        <div class="group-label">{{ group.label }}</div>
        <div
          v-for="session in group.items"
          :key="session.ChatSessionID"
          class="session-item"
          :class="{ active: session.ChatSessionID === chatStore.currentSessionId }"
          @click="chatStore.selectSession(session.ChatSessionID)"
          @contextmenu.prevent="showContextMenu($event, session)"
        >
          <div class="session-title">
            <span
              v-if="session.SessionStatus === 1"
              class="session-status running"
              :title="t('chat.sessionRunning')"
            ></span>
            <span
              v-else-if="session.SessionStatus === -1"
              class="session-status failed"
              :title="t('chat.sessionFailed')"
            ></span>
            {{ session.TopicSummary || t('chat.newConversation') }}
          </div>
          <div class="session-meta">
            <span class="session-app">{{ getAppName(session.AppID) }}</span>
            <span class="session-time">{{ formatTime(session.CreateTime) }}</span>
          </div>
        </div>
      </template>

      <div v-if="hasMore && !chatStore.loadingSessions" class="load-more" @click="loadMore">
        {{ t('chat.loadMore') }}
      </div>
      <div v-if="!hasMore && chatStore.sessions.length > 0" class="load-end">
        {{ t('chat.allLoaded') }}
      </div>
      <div v-if="!chatStore.loadingSessions && chatStore.sessions.length === 0" class="empty-hint">
        {{ t('chat.noSessions') }}
      </div>
    </div>

    <teleport to="body">
      <div
        v-if="contextMenu.visible"
        class="context-menu"
        :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
      >
        <div class="context-menu-item danger" @click="handleDelete">{{ t('chat.deleteSession') }}</div>
      </div>
    </teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, watch, onMounted, onUnmounted } from 'vue'
import { Search, Delete } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '@/stores/chat'
import type { SessionInfo } from '@/types/chat'

const { t, locale } = useI18n()
const chatStore = useChatStore()
const searchQuery = ref('')
const scrollRef = ref<HTMLElement | null>(null)
const pageSize = 50
let currentPage = 1

const contextMenu = reactive({ visible: false, x: 0, y: 0, session: null as SessionInfo | null })

const hasMore = computed(() => chatStore.sessions.length < chatStore.sessionsTotal)

const filteredSessions = computed(() => {
  if (!searchQuery.value) return chatStore.sessions
  const q = searchQuery.value.toLowerCase()
  return chatStore.sessions.filter(
    (s) =>
      s.TopicSummary?.toLowerCase().includes(q) ||
      s.AppID?.toLowerCase().includes(q),
  )
})

interface SessionGroup {
  label: string
  items: SessionInfo[]
}

const groupedSessions = computed((): SessionGroup[] => {
  const sessions = filteredSessions.value
  if (!sessions.length) return []

  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const yesterday = new Date(today.getTime() - 86400000)
  const last7 = new Date(today.getTime() - 7 * 86400000)
  const last30 = new Date(today.getTime() - 30 * 86400000)

  const groups = new Map<string, { label: string; order: number; items: SessionInfo[] }>()

  for (const session of sessions) {
    const date = new Date(session.CreateTime)
    let key: string
    let label: string
    let order: number

    if (date >= today) {
      key = 'today'
      label = t('time.today')
      order = 0
    } else if (date >= yesterday) {
      key = 'yesterday'
      label = t('time.yesterday')
      order = 1
    } else if (date >= last7) {
      key = 'last7'
      label = t('time.last7days')
      order = 2
    } else if (date >= last30) {
      key = 'last30'
      label = t('time.last30days')
      order = 3
    } else if (date.getFullYear() === now.getFullYear()) {
      key = `month_${date.getMonth()}`
      label = locale.value === 'zh-CN' ? `${date.getMonth() + 1}月` : date.toLocaleDateString('en-US', { month: 'short' })
      order = 4 + date.getMonth()
    } else {
      key = `year_${date.getFullYear()}`
      label = locale.value === 'zh-CN' ? `${date.getFullYear()}年` : `${date.getFullYear()}`
      order = 100 + date.getFullYear()
    }

    if (!groups.has(key)) {
      groups.set(key, { label, order, items: [] })
    }
    groups.get(key)!.items.push(session)
  }

  return Array.from(groups.values())
    .sort((a, b) => a.order - b.order)
    .map(({ label, items }) => ({ label, items }))
})

function getAppName(appId: string): string {
  if (!appId) return ''
  const app = chatStore.apps.find((a) => a.AppID === appId)
  return app?.Name || appId
}

function formatTime(iso: string): string {
  if (!iso) return ''
  const date = new Date(iso)
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const yesterday = new Date(today.getTime() - 86400000)
  const loc = locale.value as string

  if (date >= today) {
    return date.toLocaleTimeString(loc, { hour: '2-digit', minute: '2-digit' })
  }
  if (date >= yesterday) {
    return t('time.yesterday')
  }
  return date.toLocaleDateString(loc, { month: '2-digit', day: '2-digit' })
}

async function loadMore() {
  currentPage++
  await chatStore.fetchSessions(currentPage, pageSize)
}

function onScroll() {
  const el = scrollRef.value
  if (!el || !hasMore.value || chatStore.loadingSessions) return
  if (el.scrollHeight - el.scrollTop - el.clientHeight < 50) {
    loadMore()
  }
}

function showContextMenu(event: MouseEvent, session: SessionInfo) {
  contextMenu.visible = true
  contextMenu.x = event.clientX
  contextMenu.y = event.clientY
  contextMenu.session = session
}

async function handleDelete() {
  if (contextMenu.session) {
    try {
      await ElMessageBox.confirm(t('chat.deleteConfirm'), t('chat.deleteTitle'), {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning',
      })
      await chatStore.deleteSession(contextMenu.session.ChatSessionID)
      ElMessage.success(t('chat.deleted'))
    } catch { /* cancelled */ }
  }
  contextMenu.visible = false
}

async function handleClearAll() {
  try {
    await ElMessageBox.confirm(t('chat.clearConfirm'), t('chat.clearTitle'), {
      confirmButtonText: t('common.clear'),
      cancelButtonText: t('common.cancel'),
      type: 'warning',
    })
    await import('@/services/session').then((m) => m.cleanUpSessions())
    // 通过 store action 清理本地状态：取消所有活动 SSE 流、清空会话列表/
    // 消息/选择、并清掉缓存。原先这里直接赋值 store 字段，绕过了 action，
    // 既不清缓存也不取消流，导致孤儿流和缓存残留。
    await chatStore.clearAllSessions()
    ElMessage.success(t('chat.cleared'))
  } catch { /* cancelled */ }
}

function hideContextMenu() {
  contextMenu.visible = false
}

watch(searchQuery, () => {
  currentPage = 1
})

onMounted(() => {
  document.addEventListener('click', hideContextMenu)
})

onUnmounted(() => {
  document.removeEventListener('click', hideContextMenu)
})
</script>

<style lang="scss" scoped>
.session-list {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.session-toolbar {
  display: flex;
  gap: 4px;
  margin-bottom: 8px;
  align-items: center;

  .el-input {
    flex: 1;
  }
}

.session-items {
  flex: 1;
  overflow-y: auto;

  &::-webkit-scrollbar {
    width: 4px;
  }
  &::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 2px;
  }
}

.group-label {
  padding: 8px 12px 4px;
  font-size: 11px;
  color: #9ca3af;
  font-weight: 500;
  user-select: none;
}

.session-item {
  padding: 10px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.2s;

  &:hover {
    background: #f3f4f6;
  }

  &.active {
    background: #e0e7ff;
  }
}

.session-title {
  font-size: 14px;
  color: #1f2937;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.4;
  display: flex;
  align-items: center;
  gap: 6px;
}

.session-status {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &.running {
    background: #3b82f6;
    animation: pulse 1.5s ease-in-out infinite;
  }

  &.failed {
    background: #ef4444;
  }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.session-meta {
  display: flex;
  justify-content: space-between;
  margin-top: 4px;
  font-size: 12px;
  color: #9ca3af;
}

.session-app {
  max-width: 60%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.load-more,
.load-end,
.empty-hint {
  padding: 12px;
  text-align: center;
  font-size: 12px;
  color: #9ca3af;
}

.load-more {
  cursor: pointer;
  &:hover { color: #6b7280; }
}

.context-menu {
  position: fixed;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  z-index: 9999;
  padding: 4px 0;
  min-width: 100px;
}

.context-menu-item {
  padding: 8px 16px;
  cursor: pointer;
  font-size: 13px;
  color: #374151;

  &:hover {
    background: #f3f4f6;
  }

  &.danger {
    color: #ef4444;
    &:hover {
      background: #fef2f2;
    }
  }
}
</style>
