<template>
  <div class="login-page">
    <el-button class="close-btn" text @click="handleClose">
      <el-icon :size="20"><Close /></el-icon>
    </el-button>

    <div class="login-card">
      <h1 class="login-title">{{ appTitle }}</h1>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @submit.prevent="handleLogin"
      >
        <el-form-item label="手机号" prop="phone">
          <el-input
            v-model="form.phone"
            placeholder="请输入手机号"
            :prefix-icon="Phone"
            maxlength="11"
          />
        </el-form-item>

        <el-form-item label="密码" prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="请输入密码"
            :prefix-icon="Lock"
            show-password
            @keyup.enter="handleLogin"
          />
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            :loading="loading"
            class="login-btn"
            @click="handleLogin"
          >
            登录
          </el-button>
        </el-form-item>
      </el-form>

      <div v-if="error" class="login-error">
        <el-alert :title="error" type="error" show-icon :closable="false" />
      </div>
    </div>

    <el-button class="settings-btn" text @click="showSettings = true">
      <el-icon :size="20"><Setting /></el-icon>
    </el-button>

    <el-dialog
      v-model="showSettings"
      title="服务端设置"
      width="420px"
      :close-on-click-modal="false"
      align-center
    >
      <el-form label-position="top">
        <el-form-item label="服务端地址">
          <el-input
            v-model="settingsStore.apiBaseUrl"
            placeholder="http://host:port/api"
          />
        </el-form-item>
        <el-form-item>
          <div class="settings-actions">
            <el-button @click="testConnection" :loading="testing">测试连接</el-button>
            <el-tag v-if="testResult !== null" :type="testResult ? 'success' : 'danger'">
              {{ testResult ? '连接成功' : '连接失败' }}
            </el-tag>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showSettings = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { Phone, Lock, Close, Setting } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuth } from '@/composables/useAuth'
import { useSettingsStore } from '@/stores/settings'
import http from '@/services/http'

const settingsStore = useSettingsStore()

async function handleClose() {
  try {
    const { getCurrentWindow } = await import('@tauri-apps/api/window')
    await getCurrentWindow().close()
  } catch {
    window.close()
  }
}

const appTitle = import.meta.env.VITE_APP_TITLE || 'ZSN AgentBrook'
const formRef = ref<FormInstance>()
const { login, loading, error } = useAuth()

const form = reactive({
  phone: '',
  password: '',
})

const rules: FormRules = {
  phone: [
    { required: true, message: '请输入手机号', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少6位', trigger: 'blur' },
  ],
}

async function handleLogin() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  await login(form.phone, form.password)
}

const showSettings = ref(false)
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
.login-page {
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: var(--bg-primary, #f5f7fa);
}

.close-btn {
  position: absolute;
  top: 16px;
  right: 16px;
}

.settings-btn {
  position: absolute;
  bottom: 20px;
  left: 20px;
  color: var(--text-secondary, #909399);
  &:hover {
    color: var(--text-primary, #303133);
  }
}

.login-card {
  width: 400px;
  padding: 40px;
  background: var(--bg-card, #fff);
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.login-title {
  text-align: center;
  margin-bottom: 32px;
  font-size: 24px;
  color: var(--text-primary, #303133);
}

.login-btn {
  width: 100%;
}

.login-error {
  margin-top: 12px;
}

.settings-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
