<template>
  <div class="settings-section">
    <h3>{{ t('settings.app') }}</h3>
    <el-form label-position="top">
      <el-form-item :label="t('settings.language')">
        <el-select v-model="currentLocale" @change="onLocaleChange">
          <el-option label="中文" value="zh-CN" />
          <el-option label="English" value="en-US" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('settings.theme')">
        <el-radio-group v-model="theme" @change="onThemeChange">
          <el-radio value="light">{{ t('settings.themeLight') }}</el-radio>
          <el-radio value="dark">{{ t('settings.themeDark') }}</el-radio>
          <el-radio value="system">{{ t('settings.themeSystem') }}</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item :label="t('settings.fontSize')">
        <el-slider v-model="settingsStore.fontSize" :min="12" :max="22" :step="1" show-input />
      </el-form-item>
      <el-form-item :label="t('settings.sendKey')">
        <el-radio-group v-model="settingsStore.sendKey">
          <el-radio value="enter">{{ t('settings.sendKeyEnter') }}</el-radio>
          <el-radio value="ctrl-enter">{{ t('settings.sendKeyCtrlEnter') }}</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item :label="t('settings.notification')">
        <el-switch v-model="settingsStore.notificationEnabled" />
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSettingsStore, type ThemeMode, type Locale } from '@/stores/settings'

const { t, locale } = useI18n()
const settingsStore = useSettingsStore()
const theme = ref(settingsStore.theme)

const currentLocale = computed({
  get: () => locale.value as Locale,
  set: (v: Locale) => { locale.value = v },
})

function onLocaleChange(val: Locale) {
  settingsStore.setLocale(val)
}

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
