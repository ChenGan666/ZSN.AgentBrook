<template>
  <div class="app-layout" :class="{ 'sidebar-collapsed': appStore.sidebarCollapsed }">
    <TitleBar v-if="isTauri()" />

    <div class="app-body">
      <aside
        class="sidebar"
        :style="{ width: appStore.sidebarCollapsed ? '0px' : `${appStore.sidebarWidth}px` }"
      >
        <SideBar @new-chat="handleNewChat" />
      </aside>

      <div
        v-show="!appStore.sidebarCollapsed"
        class="resize-handle"
        @mousedown="onResizeStart"
      />

      <main class="main-content">
        <StatusBar @toggle-sidebar="appStore.sidebarCollapsed = !appStore.sidebarCollapsed" />
        <div class="content-area">
          <router-view />
        </div>
      </main>
    </div>

    <el-dialog v-model="showAppPicker" :title="t('chat.selectApp')" width="400px">
      <div class="app-list">
        <div
          v-for="app in chatStore.apps"
          :key="app.AppID"
          class="app-item"
          @click="selectApp(app)"
        >
          <span class="app-name">{{ app.Name }}</span>
          <span v-if="app.Description" class="app-desc">{{ app.Description }}</span>
        </div>
      </div>
    </el-dialog>

    <FloatingToolbar />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { isTauri, platform } from '@/platform'
import { useAppStore } from '@/stores/app'
import { useChatStore } from '@/stores/chat'
import TitleBar from './TitleBar.vue'
import SideBar from './SideBar.vue'
import StatusBar from './StatusBar.vue'
import FloatingToolbar from '@/components/common/FloatingToolbar.vue'
import { useConnection } from '@/composables/useConnection'
import { useWindowState } from '@/composables/useWindowState'
import { useGlobalShortcut } from '@/composables/useGlobalShortcut'
import type { AppInfo } from '@/types/chat'

const { t } = useI18n()
const appStore = useAppStore()
const chatStore = useChatStore()
const showAppPicker = ref(false)

let resizing = false

function onResizeStart(e: MouseEvent) {
  e.preventDefault()
  resizing = true
  const startX = e.clientX
  const startWidth = appStore.sidebarWidth
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'

  const onMove = (ev: MouseEvent) => {
    if (!resizing) return
    const delta = ev.clientX - startX
    appStore.setSidebarWidth(startWidth + delta)
  }
  const onUp = () => {
    resizing = false
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
  }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

useConnection()
useWindowState()
useGlobalShortcut()

onMounted(() => {
  chatStore.fetchSessions()
  chatStore.fetchApps()

  // 注册通知点击回调，点击通知时切换到对应会话
  platform.notification.onNotificationClick?.((sessionId: string) => {
    if (sessionId) {
      chatStore.selectSession(sessionId)
    }
  })
})

async function handleNewChat() {
  if (chatStore.apps.length === 0) {
    await chatStore.fetchApps()
  }
  showAppPicker.value = true
}

function selectApp(app: AppInfo) {
  showAppPicker.value = false
  // 通过 store action 切换 App：会取消当前会话的活动流并重置选择/消息。
  // 原先这里直接赋值 store 字段，绕过了 action，不清流也不一致。
  chatStore.resetToApp(app.AppID)
}
</script>

<style lang="scss" scoped>
.app-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.app-body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.sidebar {
  flex-shrink: 0;
  overflow: hidden;
  border-right: 1px solid var(--border-color, #e4e7ed);
  background: var(--bg-sidebar, #f8f9fa);

  &:not([style*="width: 0px"]) {
    transition: none;
  }
}

.resize-handle {
  width: 4px;
  cursor: col-resize;
  flex-shrink: 0;
  background: transparent;
  transition: background 0.2s;
  position: relative;
  z-index: 10;

  &:hover {
    background: var(--el-color-primary-light-7);
  }
}

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.content-area {
  flex: 1;
  overflow: hidden;
}

.app-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.app-item {
  padding: 12px;
  border: 1px solid var(--border-color, #e4e7ed);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
  }
}

.app-name {
  font-weight: 500;
  display: block;
}

.app-desc {
  font-size: 12px;
  color: var(--text-secondary, #909399);
  margin-top: 4px;
  display: block;
}
</style>
