<template>
  <div class="settings-section">
    <h3>关于</h3>
    <el-descriptions :column="1" border>
      <el-descriptions-item label="应用名称">ZSN AgentBrook</el-descriptions-item>
      <el-descriptions-item label="版本">{{ version }}</el-descriptions-item>
      <el-descriptions-item label="平台">{{ platform.system.platform }}</el-descriptions-item>
    </el-descriptions>
  </div>
</template>

<script setup lang="ts">
import { platform } from '@/platform'

const version = platform.system.getAppVersion()
</script>

<style lang="scss" scoped>
.settings-section {
  max-width: 600px;
  h3 { margin-bottom: 16px; font-size: 18px; }
}
</style>
