<template>
  <div class="login-page" @mousedown="onDragStart">
    <!-- LEFT PANEL -->
    <div class="left-panel">
      <div class="left-noise"></div>
      <div class="left-grid"></div>

      <div class="left-top">
        <div class="brand">
          <img src="@/assets/images/logo-w.png" alt="AgentBrook" />
          <span>AgentBrook</span>
        </div>

        <div class="hero">
            <h1>
                {{ t('login.heroLine1Pre') }} <span>{{ t('login.heroLine1Highlight') }}</span><br />
                {{ t('login.heroLine2') }}
            </h1>
          <p>{{ t('login.heroDesc') }}</p>
        </div>
      </div>

      <div class="left-image">
        <img src="@/assets/images/Login_001.png" alt="AgentBrook AI" />
      </div>

      <div class="left-features">
        <div class="feature">
          <h3>{{ t('login.feature1Title') }}</h3>
          <p>{{ t('login.feature1Desc') }}</p>
        </div>
        <div class="feature">
          <h3>{{ t('login.feature2Title') }}</h3>
          <p>{{ t('login.feature2Desc') }}</p>
        </div>
        <div class="feature">
          <h3>{{ t('login.feature3Title') }}</h3>
          <p>{{ t('login.feature3Desc') }}</p>
        </div>
        <div class="feature">
          <h3>{{ t('login.feature4Title') }}</h3>
          <p>{{ t('login.feature4Desc') }}</p>
        </div>
      </div>
    </div>

    <!-- RIGHT PANEL -->
    <div class="right-panel">
      <div class="right-glow"></div>

      <el-button class="close-btn" text @click="handleClose">
        <el-icon :size="20"><Close /></el-icon>
      </el-button>

      <div class="login-card">
        <div class="login-logo">
          <img src="@/assets/images/logo-b.png" alt="AgentBrook" />
        </div>

        <h2 class="login-heading">{{ t('login.welcome') }}</h2>
        <p class="login-sub">{{ t('login.subtitle') }}</p>

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          label-position="top"
          @submit.prevent="handleLogin"
        >
          <el-form-item :label="t('login.phone')" prop="phone">
            <el-input
              v-model="form.phone"
              :placeholder="t('login.phonePlaceholder')"
              :prefix-icon="Phone"
              maxlength="11"
              size="large"
            />
          </el-form-item>

          <el-form-item :label="t('login.password')" prop="password">
            <el-input
              v-model="form.password"
              type="password"
              :placeholder="t('login.passwordPlaceholder')"
              :prefix-icon="Lock"
              show-password
              size="large"
              @keyup.enter="handleLogin"
            />
          </el-form-item>

          <el-form-item>
            <el-button
              type="primary"
              :loading="loading"
              class="login-btn"
              size="large"
              @click="handleLogin"
            >
              {{ t('login.submit') }}
            </el-button>
          </el-form-item>
        </el-form>

        <div v-if="error" class="login-error">
          <el-alert :title="error" type="error" show-icon :closable="false" />
        </div>
      </div>

      <div class="bottom-bar">
        <el-button class="settings-btn" text @click="showSettings = true">
          <el-icon :size="18"><Setting /></el-icon>
        </el-button>
        <el-select v-model="currentLocale" size="small" class="lang-select" @change="onLocaleChange">
          <el-option label="中文" value="zh-CN" />
          <el-option label="English" value="en-US" />
        </el-select>
      </div>
    </div>

    <!-- SETTINGS DIALOG -->
    <el-dialog
      v-model="showSettings"
      :title="t('login.serverSettings')"
      width="420px"
      :close-on-click-modal="false"
      align-center
    >
      <el-form label-position="top">
        <el-form-item :label="t('login.serverAddr')">
          <el-input
            v-model="settingsStore.apiBaseUrl"
            placeholder="http://host:port/api"
          />
        </el-form-item>
        <el-form-item>
          <div class="settings-actions">
            <el-button @click="testConnection" :loading="testing">{{ t('login.testConnection') }}</el-button>
            <el-tag v-if="testResult !== null" :type="testResult ? 'success' : 'danger'">
              {{ testResult ? t('login.connectionSuccess') : t('login.connectionFailed') }}
            </el-tag>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showSettings = false">{{ t('login.close') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { Phone, Lock, Close, Setting } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuth } from '@/composables/useAuth'
import { useSettingsStore, type Locale } from '@/stores/settings'
import { isTauri } from '@/platform'
import http from '@/services/http'

const { t, locale } = useI18n()
const settingsStore = useSettingsStore()

const currentLocale = computed({
  get: () => locale.value as Locale,
  set: (v: Locale) => { locale.value = v },
})

function onLocaleChange(val: Locale) {
  settingsStore.setLocale(val)
}

async function handleClose() {
  try {
    const { getCurrentWindow } = await import('@tauri-apps/api/window')
    await getCurrentWindow().close()
  } catch {
    window.close()
  }
}

const formRef = ref<FormInstance>()
const { login, loading, error } = useAuth()

const form = reactive({
  phone: '',
  password: '',
})

const rules = computed<FormRules>(() => ({
  phone: [
    { required: true, message: t('login.phoneRequired'), trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: t('login.phoneInvalid'), trigger: 'blur' },
  ],
  password: [
    { required: true, message: t('login.passwordRequired'), trigger: 'blur' },
    { min: 6, message: t('login.passwordMin'), trigger: 'blur' },
  ],
}))

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

async function onDragStart(e: MouseEvent) {
  if (!isTauri()) return
  const target = e.target as HTMLElement
  if (target.closest('input, textarea, button, a, select, [role="button"], .login-card, .bottom-bar, .close-btn, .el-dialog')) return
  e.preventDefault()
  try {
    const { getCurrentWindow } = await import('@tauri-apps/api/window')
    await getCurrentWindow().startDragging()
  } catch { /* ignore */ }
}

let unlockResize: (() => Promise<void>) | null = null

onMounted(async () => {
  if (!isTauri()) return
  try {
    const { getCurrentWindow } = await import('@tauri-apps/api/window')
    const appWindow = getCurrentWindow()
    await appWindow.setResizable(false)
    await appWindow.setAlwaysOnTop(true)
    unlockResize = async () => {
      await appWindow.setAlwaysOnTop(false)
      await appWindow.setResizable(true)
    }
  } catch { /* ignore */ }
})

onUnmounted(() => {
  if (unlockResize) unlockResize()
})
</script>

<style lang="scss" scoped>
/* ============ PAGE LAYOUT ============ */
.login-page {
  display: flex;
  width: 100vw;
  height: 100vh;
  overflow: hidden;
  font-family: 'Inter', 'PingFang SC', 'Microsoft YaHei', sans-serif;
}

/* ============ LEFT PANEL (60%) ============ */
.left-panel {
  position: relative;
  flex: 0 0 60%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 50px;
  overflow: hidden;
  color: #fff;
  background:
    radial-gradient(circle at top, rgba(54, 102, 255, 0.25), transparent 40%),
    radial-gradient(circle at bottom, rgba(0, 212, 255, 0.12), transparent 30%),
    linear-gradient(135deg, #040816 0%, #071127 45%, #08192f 100%);
}

.left-noise {
  position: absolute;
  inset: 0;
  opacity: 0.03;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='140' height='140' viewBox='0 0 140 140'%3E%3Cg fill='white'%3E%3Ccircle cx='2' cy='2' r='2'/%3E%3C/g%3E%3C/svg%3E");
  pointer-events: none;
}

.left-grid {
  position: absolute;
  bottom: -20%;
  left: -10%;
  width: 140%;
  height: 60%;
  background:
    linear-gradient(rgba(92, 146, 255, 0.12) 1px, transparent 1px),
    linear-gradient(90deg, rgba(92, 146, 255, 0.12) 1px, transparent 1px);
  background-size: 50px 50px;
  transform: perspective(800px) rotateX(75deg);
  opacity: 0.4;
}

/* Brand */
.left-top {
  position: relative;
  z-index: 2;
}

.brand {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 70px;

  img {
    width: 38px;
    height: 38px;
    object-fit: contain;
  }

  span {
    font-size: 30px;
    font-weight: 700;
    letter-spacing: -1px;
  }
}

    .hero {
        h1 {
            font-size: 45px;
            line-height: 1.1;
            letter-spacing: -2px;
            font-weight: 700;
            max-width: 650px;
            padding-right:50px;

            span {
                color: #6ea8ff;
            }
        }

        p {
            margin-top: 24px;
            font-size: 20px;
            color: rgba(255, 255, 255, 0.65);
            line-height: 1.7;
            max-width: 450px;
            padding-right: 60px;
        }
    }

/* Background Image */
.left-image {
  position: absolute;
  inset: 0;
  z-index: 0;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    object-position: center;
  }
}

/* Features */
    .left-features {
        position: absolute;
        z-index: 2;
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 20px;
        bottom:10px;
    }

.feature {
  padding: 20px;
  border-radius: 20px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  transition: all 0.3s ease;

  &:hover {
    transform: translateY(-4px);
    background: rgba(255, 255, 255, 0.06);
  }

  .feature-icon {
    width: 38px;
    height: 38px;
    border-radius: 12px;
    margin-bottom: 16px;
    background: linear-gradient(135deg, #7bc6ff, #5a72ff);
    box-shadow: 0 0 20px rgba(88, 139, 255, 0.4);
  }

  h3 {
    font-size: 16px;
    margin-bottom: 8px;
    font-weight: 600;
  }

  p {
    font-size: 13px;
    line-height: 1.65;
    color: rgba(255, 255, 255, 0.55);
  }
}

/* ============ RIGHT PANEL (40%) ============ */
.right-panel {
  position: relative;
  flex: 0 0 40%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fff;
  overflow: hidden;
}

.right-glow {
  position: absolute;
  top: -200px;
  right: -200px;
  width: 400px;
  height: 400px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(91, 122, 255, 0.1), transparent 70%);
  pointer-events: none;
}

.close-btn {
  position: absolute;
  top: 16px;
  right: 16px;
  color: #6b7280;

  &:hover {
    color: #111827;
  }
}

/* Login Card */
.login-card {
  width: 100%;
  max-width: 360px;
  position: relative;
  z-index: 2;
}

.login-logo {
  display: flex;
  justify-content: center;
  margin-bottom: 24px;

  img {
    width: 64px;
    opacity: 0.92;
  }
}

.login-heading {
  text-align: center;
  font-size: 36px;
  letter-spacing: -0.5px;
  margin-bottom: 10px;
  color: #111827;
}

.login-sub {
  text-align: center;
  color: #6b7280;
  margin-bottom: 36px;
  font-size: 15px;
}

/* Form Overrides */
.login-card {
  :deep(.el-form-item__label) {
    font-weight: 600;
    color: #111827;
    font-size: 14px;
  }

  :deep(.el-input__wrapper) {
    border-radius: 14px;
    padding: 4px 16px;
    box-shadow: 0 0 0 1px #e5e7eb inset;
    transition: box-shadow 0.25s;

    &:hover {
      box-shadow: 0 0 0 1px #c0c4cc inset;
    }

    &.is-focus {
      box-shadow: 0 0 0 1px #5a72ff inset, 0 0 0 4px rgba(90, 114, 255, 0.1);
    }
  }

  :deep(.el-input__inner) {
    height: 42px;
  }
}

.login-btn {
  width: 100%;
  height: 50px;
  border-radius: 14px;
  font-size: 16px;
  font-weight: 600;
  background: linear-gradient(135deg, #08142e, #0d1d44, #2447ff);
  border: none;
  box-shadow: 0 8px 24px rgba(56, 90, 255, 0.22);
  transition: all 0.3s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 32px rgba(56, 90, 255, 0.32);
  }
}

.login-error {
  margin-top: 12px;
}

.settings-btn {
  color: #9ca3af;

  &:hover {
    color: #374151;
  }
}

.bottom-bar {
  position: absolute;
  bottom: 20px;
  left: 20px;
  right: 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.lang-select {
  width: 100px;

  :deep(.el-input__wrapper) {
    border-radius: 8px;
    box-shadow: 0 0 0 1px #e5e7eb inset;
  }
}

.settings-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* ============ RESPONSIVE ============ */
@media (max-width: 1024px) {
  .left-panel {
    display: none;
  }

  .right-panel {
    flex: 1;
  }
}
</style>
