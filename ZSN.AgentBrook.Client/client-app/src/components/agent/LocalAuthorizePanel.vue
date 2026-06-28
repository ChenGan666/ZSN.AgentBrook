<template>
  <!--
    本地能力授权弹窗（L5 HITL）。
    监听 window 'agent-authorize-request' 事件显示，提供：
      仅本次 / 授权整个目录 / 拒绝。
    命中预授权范围的能力不会弹窗（由 useLocalCapability 自动批准）。
  -->
  <el-dialog
    v-model="visible"
    :title="t('agent.localAuthorize', '本地能力授权')"
    width="440px"
    :close-on-click-modal="false"
    :show-close="false"
    align-center
  >
    <div class="authorize-body">
      <el-icon class="warn-icon"><WarningFilled /></el-icon>
      <div class="authorize-text">
        <div class="authorize-title">{{ t('agent.requestLocal', 'Agent 请求执行本地操作') }}</div>
        <code class="authorize-detail">{{ detail }}</code>
        <div class="authorize-tip">{{ t('agent.authorizeTip', '请确认是否允许。仅本次允许不会写入白名单。') }}</div>
      </div>
    </div>
    <template #footer>
      <el-button @click="deny">{{ t('common.reject', '拒绝') }}</el-button>
      <el-button @click="allowOnce">{{ t('agent.allowOnce', '仅本次') }}</el-button>
      <el-button type="primary" @click="allowDirectory">{{ t('agent.allowDir', '授权整个目录') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { WarningFilled } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useLocalCapability } from '@/composables/useLocalCapability'
import { useAgentStore } from '@/stores/agent'

const { t } = useI18n()
const agentStore = useAgentStore()
const { resolveAuthorize, grantDirectoryViaPicker, getPendingDetail } = useLocalCapability()

const visible = ref(false)
const detail = ref('')

function onRequested(e: Event) {
  const ce = e as CustomEvent
  detail.value = getPendingDetail() || (typeof ce.detail === 'string' ? ce.detail : '')
  visible.value = true
}

function allowOnce() {
  visible.value = false
  resolveAuthorize(true)
}

async function allowDirectory() {
  // 用户经目录选择器选的目录自动获访问权，写入白名单
  const ok = await grantDirectoryViaPicker('readWrite')
  if (ok) {
    ElMessage.success(t('agent.dirGranted', '已授权目录，后续该目录内操作将自动批准'))
    visible.value = false
    resolveAuthorize(true)
  } else {
    ElMessage.info(t('agent.dirGrantCancel', '未选择目录'))
  }
}

function deny() {
  visible.value = false
  resolveAuthorize(false)
}

onMounted(() => window.addEventListener('agent-authorize-request', onRequested))
onUnmounted(() => window.removeEventListener('agent-authorize-request', onRequested))
</script>

<style lang="scss" scoped>
.authorize-body {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}
.warn-icon {
  font-size: 22px;
  color: var(--el-color-warning, #e6a23c);
  flex-shrink: 0;
  margin-top: 2px;
}
.authorize-text {
  flex: 1 1 auto;
  min-width: 0;
}
.authorize-title {
  font-weight: 600;
  margin-bottom: 6px;
}
.authorize-detail {
  display: block;
  background: var(--bg-hover, #f7f8fa);
  border-radius: 6px;
  padding: 8px 10px;
  font-family: ui-monospace, monospace;
  font-size: 12px;
  word-break: break-all;
  max-height: 160px;
  overflow-y: auto;
}
.authorize-tip {
  margin-top: 8px;
  font-size: 12px;
  color: var(--text-secondary, #909399);
}
</style>
