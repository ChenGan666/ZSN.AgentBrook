<template>
  <router-view />
</template>

<script setup lang="ts">
import { useSettingsStore } from '@/stores/settings'

const settingsStore = useSettingsStore()
settingsStore.setTheme(settingsStore.theme)

if (settingsStore.theme === 'system') {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    settingsStore.setTheme('system')
  })
}
</script>
