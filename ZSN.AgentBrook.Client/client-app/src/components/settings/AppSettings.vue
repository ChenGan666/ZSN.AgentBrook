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
        <el-button
          size="small"
          style="margin-left: 12px"
          :loading="testing"
          @click="onTestNotification"
        >
          {{ t('settings.notificationTest') }}
        </el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useSettingsStore, type ThemeMode, type Locale } from '@/stores/settings'
import { platform } from '@/platform'

const { t, locale } = useI18n()
const settingsStore = useSettingsStore()
const theme = ref(settingsStore.theme)
const testing = ref(false)

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

/**
 * Fire a test notification directly through the platform adapter (bypassing
 * the document.hasFocus() gate, so it works even while the settings window is
 * focused). The adapter returns a NotificationResult so we can report the real
 * outcome: even when the API call succeeds (`status: 'sent'`), the OS may still
 * suppress the toast if its global notification setting is off — so we show a
 * follow-up hint pointing the user to the system settings in that case.
 */
async function onTestNotification() {
  testing.value = true
  try {
    const result = await platform.notification.show(
      t('settings.notificationTest'),
      t('settings.notificationTestSent'),
      { sessionId: undefined },
    )
    // Adapter always returns a NotificationResult now, but guard for safety.
    const status = result?.status ?? 'sent'
    switch (status) {
      case 'sent':
        // API call succeeded — but the OS can still drop the toast if its
        // notification setting is off (the root cause we hit). Show a
        // warning with the hint so the user knows to check system settings.
        ElMessage.warning(t('settings.notificationTestSentButHidden'))
        break
      case 'permission_denied':
        ElMessage.error(t('settings.notificationTestDenied'))
        break
      case 'unsupported':
        ElMessage.warning(t('settings.notificationTestUnsupported'))
        break
      case 'error':
        ElMessage.error(t('settings.notificationTestError', { message: result?.message ?? '' }))
        break
    }
  } catch (e) {
    ElMessage.error(t('settings.notificationTestError', { message: e instanceof Error ? e.message : String(e) }))
  } finally {
    testing.value = false
  }
}
</script>

<style lang="scss" scoped>
.settings-section {
  max-width: 600px;
  h3 { margin-bottom: 16px; font-size: 18px; }
}
</style>
