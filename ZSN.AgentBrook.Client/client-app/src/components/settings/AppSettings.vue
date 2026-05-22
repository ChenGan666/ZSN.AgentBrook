<template>
  <div class="settings-section">
    <h3>应用设置</h3>
    <el-form label-position="top">
      <el-form-item label="主题">
        <el-radio-group v-model="theme" @change="onThemeChange">
          <el-radio value="light">亮色</el-radio>
          <el-radio value="dark">暗色</el-radio>
          <el-radio value="system">跟随系统</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="字体大小">
        <el-slider v-model="settingsStore.fontSize" :min="12" :max="22" :step="1" show-input />
      </el-form-item>
      <el-form-item label="发送快捷键">
        <el-radio-group v-model="settingsStore.sendKey">
          <el-radio value="enter">Enter 发送</el-radio>
          <el-radio value="ctrl-enter">Ctrl+Enter 发送</el-radio>
        </el-radio-group>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useSettingsStore, type ThemeMode } from '@/stores/settings'

const settingsStore = useSettingsStore()
const theme = ref(settingsStore.theme)

watch(() => settingsStore.theme, (v) => { theme.value = v })

function onThemeChange(v: string | number | boolean | undefined) {
  settingsStore.setTheme(v as ThemeMode)
}
</script>

<style lang="scss" scoped>
.settings-section {
  max-width: 600px;
  h3 { margin-bottom: 16px; font-size: 18px; }
}
</style>
