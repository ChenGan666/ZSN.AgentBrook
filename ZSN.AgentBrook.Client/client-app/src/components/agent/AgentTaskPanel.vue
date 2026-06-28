<template>
  <!-- Agent 任务清单面板：当前编排步骤 + 历史清单 + 导出 -->
  <div class="task-panel">
    <div class="panel-header">
      <span class="panel-title">{{ t('agent.taskPanel', '任务清单') }}</span>
      <div class="panel-actions">
        <el-tooltip :content="t('agent.exportMarkdown', '导出 Markdown')" placement="bottom">
          <el-button text size="small" :icon="Download" :disabled="!currentPlan" @click="exportCurrent('md')" />
        </el-tooltip>
        <el-tooltip :content="t('agent.exportJson', '导出 JSON')" placement="bottom">
          <el-button text size="small" :disabled="!currentPlan" @click="exportCurrent('json')">JSON</el-button>
        </el-tooltip>
        <el-button text size="small" :icon="Refresh" @click="loadHistory" />
      </div>
    </div>

    <!-- 当前编排 -->
    <div v-if="currentPlan" class="current-plan">
      <div class="goal-line">{{ currentPlan.goal }}</div>
      <div class="steps">
        <div v-if="currentPlan.apps.length === 0" class="empty-step">
          {{ currentPlan.status === 'planning' ? t('agent.planning', '正在规划…') : t('agent.noSteps', '暂无步骤') }}
        </div>
        <div
          v-for="(s, i) in currentPlan.apps"
          :key="i"
          class="step-row"
          :class="`status-${s.status}`"
        >
          <span class="step-icon">
            <el-icon v-if="s.status === 'executing'" class="is-loading"><Loading /></el-icon>
            <el-icon v-else-if="s.status === 'completed'" color="#67c23a"><CircleCheck /></el-icon>
            <el-icon v-else-if="s.status === 'failed'" color="#f56c6c"><CircleClose /></el-icon>
            <el-icon v-else color="#e6a23c"><Clock /></el-icon>
          </span>
          <span class="step-text">{{ i + 1 }}. 《{{ s.appName || s.appID }}》</span>
          <span class="step-status">{{ statusLabel(s.status) }}</span>
        </div>
      </div>
    </div>
    <div v-else class="no-current">{{ t('agent.noCurrentTask', '暂无进行中的任务') }}</div>

    <!-- 历史清单 -->
    <div class="history-section">
      <div class="section-title">{{ t('agent.history', '历史任务') }}</div>
      <div v-if="history.length === 0" class="empty-history">{{ t('agent.noHistory', '暂无历史') }}</div>
      <div v-for="p in history" :key="p.planId" class="history-item">
        <div class="history-goal" :title="p.goal">{{ p.goal }}</div>
        <div class="history-meta">
          <el-tag size="small" :type="statusTag(p.status)">{{ statusLabel2(p.status) }}</el-tag>
          <span class="history-time">{{ formatTime(p.updatedAt) }}</span>
        </div>
        <div class="history-actions">
          <el-button text size="small" @click="exportPlan(p, 'md')">MD</el-button>
          <el-button text size="small" @click="exportPlan(p, 'json')">JSON</el-button>
          <el-button text size="small" :icon="Delete" @click="removePlan(p.planId)" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { Download, Refresh, Loading, CircleCheck, CircleClose, Clock, Delete } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { useAgentStore } from '@/stores/agent'
import { platform } from '@/platform'
import {
  createPlanMirror,
  applyStepEvent,
  planToMarkdown,
  planToJson,
  savePlan,
  getAllPlans,
  getPlansBySession,
  deletePlan,
} from '@/utils/agentPlan'
import type { AgentPlanMirror } from '@/utils/db'
import type { StepEvent } from '@/types/agent'

const { t } = useI18n()
const agentStore = useAgentStore()

/** 当前显示的 plan 镜像（随当前会话切换） */
const currentPlan = ref<AgentPlanMirror | null>(null)
/** 当前真实运行中编排的 plan 镜像（不受界面切换影响） */
const activePlan = ref<AgentPlanMirror | null>(null)
const history = ref<AgentPlanMirror[]>([])

// 暴露给父视图：开始新编排 / 推送事件 / 收别
function startPlan(sessionId: string, goal: string) {
  activePlan.value = createPlanMirror(sessionId, goal)
  currentPlan.value = activePlan.value
  void savePlan(activePlan.value)
}

function handleStepEvent(e: StepEvent) {
  // 始终更新真实运行中的计划，即使当前界面不在该会话
  if (activePlan.value) {
    applyStepEvent(activePlan.value, e)
    void savePlan(activePlan.value)
  }
}

function finishPlan() {
  // 收别真实运行中的计划；当前显示若与其一致，自动同步
  if (!activePlan.value) return
  const terminalStatuses = ['completed', 'failed', 'paused']
  if (!terminalStatuses.includes(activePlan.value.status)) {
    activePlan.value.status = 'completed'
    activePlan.value.apps.forEach((s) => {
      if (s.status === 'executing') {
        s.status = 'completed'
        s.endTime = Date.now()
      }
    })
  }
  void savePlan(activePlan.value)
  activePlan.value = null
  loadHistory()
}

defineExpose({ startPlan, handleStepEvent, finishPlan })

async function loadPlanBySession(sessionId: string) {
  try {
    // 若该会话正真实运行中，直接显示内存中的 activePlan
    if (activePlan.value && activePlan.value.sessionId === sessionId) {
      currentPlan.value = activePlan.value
      return
    }
    const plans = await getPlansBySession(sessionId)
    if (plans.length > 0) {
      plans.sort((a: AgentPlanMirror, b: AgentPlanMirror) => b.updatedAt - a.updatedAt)
      currentPlan.value = plans[0]
      // 容错：刷新/重连后，若计划仍处非终态，统一标记为失败
      repairCurrentPlan()
    } else {
      currentPlan.value = null
    }
  } catch {
    /* ignore */
  }
}

function repairCurrentPlan() {
  if (!currentPlan.value) return
  // 当前会话正在真实运行中：计划非终态是正常现象，不修复
  const session = agentStore.currentSession
  if (session?.running) return
  const terminalStatuses = ['completed', 'failed', 'paused']
  if (!terminalStatuses.includes(currentPlan.value.status)) {
    currentPlan.value.status = 'failed'
    currentPlan.value.apps.forEach((s) => {
      if (s.status === 'executing') {
        s.status = 'failed'
        s.endTime = Date.now()
      }
    })
    void savePlan(currentPlan.value)
    loadHistory()
  }
}

async function loadHistory() {
  try {
    history.value = await getAllPlans()
    history.value.sort((a: AgentPlanMirror, b: AgentPlanMirror) => b.updatedAt - a.updatedAt)
  } catch {
    /* ignore */
  }
}

async function exportCurrent(fmt: 'md' | 'json') {
  if (currentPlan.value) await exportPlan(currentPlan.value, fmt)
}

async function exportPlan(plan: AgentPlanMirror, fmt: 'md' | 'json') {
  try {
    const content = fmt === 'md' ? planToMarkdown(plan) : planToJson(plan)
    const blob = new Blob([content], {
      type: fmt === 'md' ? 'text/markdown;charset=utf-8' : 'application/json;charset=utf-8',
    })
    const name = `agent-task-${plan.planId}.${fmt === 'md' ? 'md' : 'json'}`
    await platform.file.save(blob, name)
    ElMessage.success(t('agent.exportDone', '已导出'))
  } catch (err: any) {
    // 文件写入失败不阻塞编排，仅提示（D10/降级友好）
    ElMessage.warning(t('agent.exportFailed', '导出失败') + (err?.message ? `：${err.message}` : ''))
  }
}

async function removePlan(planId: string) {
  await deletePlan(planId)
  await loadHistory()
}

function statusLabel(s: string): string {
  return (
    {
      pending: '⏸',
      executing: t('agent.executing', '执行中'),
      completed: '✅',
      failed: '❌',
      skipped: '⏭',
      paused: t('agent.paused', '已中断'),
    } as Record<string, string>
  )[s] || s
}
function statusLabel2(s: string): string {
  return (
    {
      planning: t('agent.planning', '规划中'),
      executing: t('agent.executing', '执行中'),
      completed: t('agent.completed', '已完成'),
      failed: t('agent.failed', '失败'),
      paused: t('agent.paused', '已暂停'),
    } as Record<string, string>
  )[s] || s
}
function statusTag(s: string): 'success' | 'warning' | 'info' | 'danger' {
  if (s === 'completed') return 'success'
  if (s === 'failed') return 'danger'
  if (s === 'paused') return 'warning'
  return 'info'
}
function formatTime(ts: number): string {
  try {
    return new Date(ts).toLocaleString()
  } catch {
    return ''
  }
}

// 编排状态变化时同步任务面板
watch(
  () => agentStore.activeSessionId,
  (activeId) => {
    if (!activeId) finishPlan()
  },
)

// 切换会话时恢复当前会话的任务清单
watch(
  () => agentStore.currentSessionId,
  (sessionId) => {
    if (sessionId) void loadPlanBySession(sessionId)
  },
  { immediate: true },
)

onMounted(() => {
  loadHistory()
  if (agentStore.currentSessionId) {
    void loadPlanBySession(agentStore.currentSessionId)
  }
})
</script>

<style lang="scss" scoped>
.task-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  font-size: 13px;
}
.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 10px;
  border-bottom: 1px solid var(--border-color, #ebeef5);
  flex-shrink: 0;
}
.panel-title {
  font-weight: 600;
}
.panel-actions {
  display: flex;
  align-items: center;
  gap: 2px;
}
.current-plan {
  padding: 10px;
  border-bottom: 1px solid var(--border-color, #ebeef5);
}
.goal-line {
  font-weight: 500;
  margin-bottom: 8px;
  color: var(--text-primary, #303133);
}
.steps {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.empty-step {
  color: var(--text-secondary, #909399);
  padding: 4px 0;
}
.step-row {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 3px 0;
  &.status-executing .step-text {
    color: var(--el-color-primary, #409eff);
  }
}
.step-icon {
  display: inline-flex;
  font-size: 13px;
  .is-loading {
    animation: spin 1.2s linear infinite;
  }
}
.step-text {
  flex: 1 1 auto;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.step-status {
  font-size: 11px;
  color: var(--text-secondary, #909399);
}
.no-current {
  padding: 16px 10px;
  color: var(--text-secondary, #909399);
  text-align: center;
  border-bottom: 1px solid var(--border-color, #ebeef5);
}
.history-section {
  flex: 1 1 auto;
  overflow-y: auto;
  padding: 8px 10px;
}
.section-title {
  font-size: 12px;
  color: var(--text-secondary, #909399);
  margin-bottom: 6px;
}
.empty-history {
  color: var(--text-secondary, #909399);
  font-size: 12px;
  padding: 8px 0;
}
.history-item {
  padding: 6px 8px;
  border-radius: 6px;
  margin-bottom: 6px;
  background: var(--bg-hover, #f7f8fa);
}
.history-goal {
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-bottom: 4px;
}
.history-meta {
  display: flex;
  align-items: center;
  gap: 6px;
}
.history-time {
  font-size: 11px;
  color: var(--text-secondary, #909399);
}
.history-actions {
  display: flex;
  gap: 2px;
  margin-top: 4px;
}
@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
