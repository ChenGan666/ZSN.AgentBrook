<template>
  <div class="settings-section">
    <h3>{{ t('server.title') }}</h3>
    <el-form label-position="top">
      <el-form-item :label="t('server.apiUrl')">
        <el-input v-model="settingsStore.apiBaseUrl" placeholder="https://api.example.com" />
      </el-form-item>
      <el-form-item>
        <el-button @click="testConnection" :loading="testing">{{ t('server.testConnection') }}</el-button>
        <el-tag v-if="testResult !== null" :type="testResult ? 'success' : 'danger'" class="test-result">
          {{ testResult ? t('server.connectionSuccess') : t('server.connectionFailed') }}
        </el-tag>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSettingsStore } from '@/stores/settings'
import http from '@/services/http'

const { t } = useI18n()
const settingsStore = useSettingsStore()
const testing = ref(false)
const testResult = ref<boolean | null>(null)

async function testConnection() {
  testing.value = true
  testResult.value = null
  try {
    await http.post('/Base/Get', {}, { timeout: 5000, baseURL: settingsStore.apiBaseUrl })
    testResult.value = true
  } catch {
    testResult.value = false
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
.test-result { margin-left: 12px; }
</style>
