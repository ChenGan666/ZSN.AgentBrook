<template>
  <div class="meeting-transcribe-view">
    <div class="transcribe-titlebar" data-tauri-drag-region>
      <span>会议转写</span>
      <el-button text size="small" @click="closeWindow">
        <el-icon><Close /></el-icon>
      </el-button>
    </div>
    <MeetingTranscribe />
  </div>
</template>

<script setup lang="ts">
import { Close } from '@element-plus/icons-vue'
import { isTauri } from '@/platform'
import MeetingTranscribe from '@/components/common/MeetingTranscribe.vue'

async function closeWindow() {
  if (!isTauri()) return
  const { getCurrentWindow } = await import('@tauri-apps/api/window')
  await getCurrentWindow().close()
}
</script>

<style lang="scss" scoped>
.meeting-transcribe-view {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.transcribe-titlebar {
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
