<template>
  <div class="settings-section">
    <h3>缓存管理</h3>
    <el-form label-position="top">
      <el-form-item label="缓存占用">
        <span>{{ formattedSize }}</span>
        <el-button text size="small" @click="refreshSize">刷新</el-button>
      </el-form-item>
      <el-form-item>
        <el-button @click="clearChatCache" :loading="clearingChat">清除对话缓存</el-button>
        <el-button type="danger" @click="clearAll" :loading="clearingAll">清除全部缓存</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getCacheSize, clearAllCache, messageCache } from '@/utils/cache'

const cacheSize = ref(0)
const clearingChat = ref(false)
const clearingAll = ref(false)

const formattedSize = ref('计算中...')

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

async function refreshSize() {
  cacheSize.value = await getCacheSize()
  formattedSize.value = formatBytes(cacheSize.value)
}

async function clearChatCache() {
  clearingChat.value = true
  try {
    await messageCache.clear()
    ElMessage.success('对话缓存已清除')
    await refreshSize()
  } finally {
    clearingChat.value = false
  }
}

async function clearAll() {
  try {
    await ElMessageBox.confirm('将清除所有本地缓存（保留登录状态），确定继续？', '确认')
  } catch {
    return
  }
  clearingAll.value = true
  try {
    await clearAllCache()
    ElMessage.success('全部缓存已清除')
    await refreshSize()
  } finally {
    clearingAll.value = false
  }
}

onMounted(refreshSize)
</script>

<style lang="scss" scoped>
.settings-section {
  max-width: 600px;
  h3 { margin-bottom: 16px; font-size: 18px; }
}
</style>
