<template>
  <div class="sidebar-container">
    <div class="sidebar-header">
      <el-button type="primary" class="new-chat-btn" @click="emit('newChat')">
        <el-icon><Plus /></el-icon>
        {{ t('chat.newChat') }}
      </el-button>
    </div>

    <div class="sidebar-sessions">
      <SessionList />
    </div>

    <div class="sidebar-footer">
      <div class="user-info" v-if="userStore.userInfo" @click="toggleSettings">
        <el-avatar :size="32" :src="userStore.userInfo.avatar">
          {{ userStore.userInfo.name?.charAt(0) || '?' }}
        </el-avatar>
        <span class="user-name">{{ userStore.userInfo.name }}</span>
      </div>
      <el-button text @click="toggleSettings">
        <el-icon><Setting /></el-icon>
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Plus, Setting } from '@element-plus/icons-vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user'
import SessionList from '@/components/chat/SessionList.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const emit = defineEmits<{ newChat: [] }>()

function toggleSettings() {
  if (route.path === '/settings') {
    router.push('/chat')
  } else {
    router.push('/settings')
  }
}
</script>

<style lang="scss" scoped>
.sidebar-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 12px;
}

.sidebar-header {
  margin-bottom: 12px;
}

.new-chat-btn {
  width: 100%;
}

.sidebar-sessions {
  flex: 1;
  overflow-y: auto;
}

.sidebar-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
  border-top: 1px solid var(--border-color, #e4e7ed);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  border-radius: 6px;
  padding: 4px;
  transition: background 0.2s;

  &:hover {
    background: var(--el-fill-color-light);
  }
}

.user-name {
  font-size: 14px;
  color: var(--text-primary, #303133);
}
</style>
