<template>
  <div class="settings-section">
    <h3>{{ t('about.title') }}</h3>
    <el-descriptions :column="1" border>
      <el-descriptions-item :label="t('about.appName')">ZSN AgentBrook</el-descriptions-item>
      <el-descriptions-item :label="t('about.version')">{{ version }}</el-descriptions-item>
      <el-descriptions-item :label="t('about.platform')">{{ platform.system.platform }}</el-descriptions-item>
    </el-descriptions>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { platform } from '@/platform'

const { t } = useI18n()
const version = platform.system.getAppVersion()
</script>

<style lang="scss" scoped>
.settings-section {
  max-width: 600px;
  h3 { margin-bottom: 16px; font-size: 18px; }
}
</style>
