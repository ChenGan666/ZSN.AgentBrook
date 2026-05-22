<template>
  <div class="settings-section">
    <h3>{{ t('cache.title') }}</h3>
    <el-form label-position="top">
      <el-form-item :label="t('cache.cacheUsage')">
        <span>{{ formattedSize }}</span>
        <el-button text size="small" @click="refreshSize">{{ t('cache.refresh') }}</el-button>
      </el-form-item>
      <el-form-item>
        <el-button @click="clearChatCache" :loading="clearingChat">{{ t('cache.clearChat') }}</el-button>
        <el-button type="danger" @click="clearAll" :loading="clearingAll">{{ t('cache.clearAll') }}</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getCacheSize, clearAllCache, messageCache } from '@/utils/cache'

const { t } = useI18n()
const cacheSize = ref(0)
const clearingChat = ref(false)
const clearingAll = ref(false)

const formattedSize = ref('')

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

async function refreshSize() {
  formattedSize.value = t('cache.calculating')
  cacheSize.value = await getCacheSize()
  formattedSize.value = formatBytes(cacheSize.value)
}

async function clearChatCache() {
  clearingChat.value = true
  try {
    await messageCache.clear()
    ElMessage.success(t('cache.chatCleared'))
    await refreshSize()
  } finally {
    clearingChat.value = false
  }
}

async function clearAll() {
  try {
    await ElMessageBox.confirm(t('cache.clearConfirm'), t('cache.confirm'))
  } catch {
    return
  }
  clearingAll.value = true
  try {
    await clearAllCache()
    ElMessage.success(t('cache.allCleared'))
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
