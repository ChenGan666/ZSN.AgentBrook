<template>
  <!-- Agent 模式会话列表：新建/切换/删除会话（问题 2）。 -->
  <div class="agent-session-list">
    <div class="session-toolbar">
      <el-button
        type="primary"
        size="small"
        class="new-session-btn"
        @click="createNewSession"
      >
        <el-icon><Plus /></el-icon>
        <span>{{ t('agent.newSession') }}</span>
      </el-button>
    </div>

    <div v-if="agentStore.sessions.length === 0" class="empty-sessions">
      {{ t('agent.noSessions') }}
    </div>
    <div v-else ref="listRef" class="session-items">
      <div
        v-for="session in sortedSessions"
        :key="session.id"
        class="session-item"
        :class="{ active: session.id === agentStore.currentSessionId, running: session.running }"
        @click="switchSession(session.id)"
      >
        <div class="session-main">
          <div class="session-title" :title="session.title">
            <el-icon v-if="session.running" class="is-loading"><Loading /></el-icon>
            {{ session.title }}
          </div>
          <div class="session-meta">
            <span class="session-count">{{ session.messages.length }} {{ t('agent.messages') }}</span>
            <span class="session-time">{{ formatTime(session.updatedAt) }}</span>
          </div>
        </div>
        <el-button
          text
          size="small"
          class="delete-btn"
          :disabled="session.running"
          @click.stop="confirmDelete(session.id)"
        >
          <el-icon><Delete /></el-icon>
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, nextTick } from 'vue'
import { Plus, Delete, Loading } from '@element-plus/icons-vue'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { useAgentStore } from '@/stores/agent'

const { t } = useI18n()
const agentStore = useAgentStore()
const listRef = ref<HTMLElement | null>(null)

const sortedSessions = computed(() => {
  // 仅按 updatedAt 降序；切换会话不改变位置，只有新对话才会通过 syncSession 更新时间并置顶
  return [...agentStore.sessions].sort((a, b) => b.updatedAt - a.updatedAt)
})

function createNewSession() {
  agentStore.createSession(t('agent.newSession'))
}

function switchSession(id: string) {
  agentStore.switchSession(id)
}

// 切换会话后，若激活项不在可视区域则滚动到可见，不改变列表位置
watch(() => agentStore.currentSessionId, () => {
  nextTick(() => {
    const el = listRef.value?.querySelector('.session-item.active') as HTMLElement | null
    if (!el) return
    const container = listRef.value
    if (!container) return
    const top = el.offsetTop - container.scrollTop
    const bottom = top + el.offsetHeight
    if (top < 0 || bottom > container.clientHeight) {
      el.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
    }
  })
})

async function confirmDelete(id: string) {
  const session = agentStore.sessions.find((s) => s.id === id)
  if (session?.running) return
  try {
    await ElMessageBox.confirm(t('agent.deleteSessionConfirm'), t('common.delete'), {
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel'),
      type: 'warning',
    })
    agentStore.deleteSession(id)
    ElMessage.success(t('agent.sessionDeleted'))
  } catch {
    // 用户取消删除
  }
}

function formatTime(ts: number): string {
  try {
    return new Date(ts).toLocaleString()
  } catch {
    return ''
  }
}
</script>

<style lang="scss" scoped>
.agent-session-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 0 12px;
}
.session-toolbar {
  display: flex;
  padding: 10px 0;
  border-bottom: 1px solid var(--border-color, #e4e7ed);
  flex-shrink: 0;
}
.new-session-btn {
  width: 100%;
}
.empty-sessions {
  padding: 16px 0;
  text-align: center;
  color: var(--text-secondary, #909399);
  font-size: 13px;
}
.session-items {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}
.session-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 10px;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.2s;
  margin-bottom: 6px;
  &:hover {
    background: var(--bg-hover, #f5f7fa);
  }
  &.active {
    background: var(--el-color-primary-light-9, #ecf5ff);
  }
  &.running {
    border: 1px solid var(--el-color-primary, #409eff);
  }
  .session-title .el-icon {
    margin-right: 4px;
    color: var(--el-color-primary, #409eff);
  }
}
.session-main {
  flex: 1 1 auto;
  min-width: 0;
}
.session-title {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-primary, #303133);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-bottom: 4px;
}
.session-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  color: var(--text-secondary, #909399);
}
.session-count {
  flex-shrink: 0;
}
.session-time {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.delete-btn {
  flex-shrink: 0;
  opacity: 0.6;
  &:hover {
    opacity: 1;
  }
}
</style>
