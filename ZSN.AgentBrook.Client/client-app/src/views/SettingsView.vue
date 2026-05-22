<template>
  <div class="settings-page">
    <div class="settings-header">
      <h2 class="settings-title">{{ t('settings.title') }}</h2>
      <el-button text @click="router.push('/chat')">
        <el-icon :size="20"><Close /></el-icon>
      </el-button>
    </div>
    <el-tabs v-model="activeTab" tab-position="left">
      <el-tab-pane :label="t('settings.profile')" name="profile">
        <ProfileSettings />
      </el-tab-pane>
      <el-tab-pane :label="t('settings.server')" name="server">
        <ServerSettings />
      </el-tab-pane>
      <el-tab-pane :label="t('settings.app')" name="app">
        <AppSettings />
      </el-tab-pane>
      <el-tab-pane :label="t('settings.cache')" name="cache">
        <CacheSettings />
      </el-tab-pane>
      <el-tab-pane :label="t('settings.about')" name="about">
        <AboutSettings />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Close } from '@element-plus/icons-vue'
import ProfileSettings from '@/components/settings/ProfileSettings.vue'
import ServerSettings from '@/components/settings/ServerSettings.vue'
import AppSettings from '@/components/settings/AppSettings.vue'
import CacheSettings from '@/components/settings/CacheSettings.vue'
import AboutSettings from '@/components/settings/AboutSettings.vue'

const { t } = useI18n()
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
