<template>
  <div class="app-layout" :class="{ 'sidebar-collapsed': appStore.sidebarCollapsed }">
    <TitleBar v-if="isTauri()" />

    <div class="app-body">
      <aside class="sidebar" :style="{ width: appStore.sidebarCollapsed ? '0px' : '280px' }">
        <SideBar @new-chat="handleNewChat" />
      </aside>

      <main class="main-content">
        <StatusBar @toggle-sidebar="appStore.sidebarCollapsed = !appStore.sidebarCollapsed" />
        <div class="content-area">
          <router-view />
        </div>
      </main>
    </div>

    <el-dialog v-model="showAppPicker" title="选择应用" width="400px">
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
import { isTauri } from '@/platform'
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

const appStore = useAppStore()
const chatStore = useChatStore()
const showAppPicker = ref(false)

useConnection()
useWindowState()
useGlobalShortcut()

onMounted(() => {
  chatStore.fetchSessions()
  chatStore.fetchApps()
})

async function handleNewChat() {
  if (chatStore.apps.length === 0) {
    await chatStore.fetchApps()
  }
  showAppPicker.value = true
}

function selectApp(app: AppInfo) {
  showAppPicker.value = false
  chatStore.selectedAppId = app.AppID
  chatStore.currentSessionId = null
  chatStore.messages = []
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
  transition: width 0.3s ease;
  border-right: 1px solid var(--border-color, #e4e7ed);
  background: var(--bg-sidebar, #f8f9fa);
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
