<template>
  <div class="settings-page">
    <div class="settings-header">
      <h2 class="settings-title">设置</h2>
      <el-button text @click="router.push('/chat')">
        <el-icon :size="20"><Close /></el-icon>
      </el-button>
    </div>
    <el-tabs v-model="activeTab" tab-position="left">
      <el-tab-pane label="个人信息" name="profile">
        <ProfileSettings />
      </el-tab-pane>
      <el-tab-pane label="服务端配置" name="server">
        <ServerSettings />
      </el-tab-pane>
      <el-tab-pane label="应用设置" name="app">
        <AppSettings />
      </el-tab-pane>
      <el-tab-pane label="缓存管理" name="cache">
        <CacheSettings />
      </el-tab-pane>
      <el-tab-pane label="关于" name="about">
        <AboutSettings />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Close } from '@element-plus/icons-vue'
import ProfileSettings from '@/components/settings/ProfileSettings.vue'
import ServerSettings from '@/components/settings/ServerSettings.vue'
import AppSettings from '@/components/settings/AppSettings.vue'
import CacheSettings from '@/components/settings/CacheSettings.vue'
import AboutSettings from '@/components/settings/AboutSettings.vue'

const router = useRouter()
const activeTab = ref('profile')
</script>

<style lang="scss" scoped>
.settings-page {
  height: 100%;
  padding: 24px;
  background: var(--bg-card, #fff);
  overflow-y: auto;
}

.settings-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.settings-title {
  font-size: 18px;
  font-weight: 600;
  margin: 0;
  color: var(--text-primary, #303133);
}
</style>
