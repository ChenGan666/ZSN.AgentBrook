<template>
  <div class="title-bar" data-tauri-drag-region>
    <div class="title-bar-text">{{ appTitle }}</div>
    <div class="title-bar-controls">
      <button class="tb-btn" @click="minimize">&#8212;</button>
      <button class="tb-btn" @click="toggleMaximize">&#9633;</button>
      <button class="tb-btn tb-close" @click="close">&#10005;</button>
    </div>
  </div>
</template>

<script setup lang="ts">
const appTitle = import.meta.env.VITE_APP_TITLE || 'ZSN AgentBrook'

async function minimize() {
  const { getCurrentWindow } = await import('@tauri-apps/api/window')
  getCurrentWindow().minimize()
}

async function toggleMaximize() {
  const { getCurrentWindow } = await import('@tauri-apps/api/window')
  getCurrentWindow().toggleMaximize()
}

async function close() {
  const { getCurrentWindow } = await import('@tauri-apps/api/window')
  getCurrentWindow().close()
}
</script>

<style lang="scss" scoped>
.title-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 32px;
  padding: 0 8px;
  background: var(--bg-titlebar, #f0f0f0);
  user-select: none;
  -webkit-app-region: drag;
}

.title-bar-text {
  font-size: 12px;
  color: var(--text-secondary, #606266);
}

.title-bar-controls {
  display: flex;
  -webkit-app-region: no-drag;
}

.tb-btn {
  width: 32px;
  height: 32px;
  border: none;
  background: transparent;
  cursor: pointer;
  font-size: 12px;
  &:hover { background: rgba(0,0,0,0.06); }
  &.tb-close:hover { background: #e81123; color: #fff; }
}
</style>
