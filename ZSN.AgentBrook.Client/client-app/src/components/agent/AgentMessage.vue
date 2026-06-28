<template>
  <!-- Agent 消息气泡：用户目标(用户消息) / 过程阶段+最终结果(助手消息) -->
  <div class="agent-message" :class="[`role-${msg.role}`]">
    <!-- 用户消息：目标 + 选中 App -->
    <template v-if="msg.role === 'user'">
      <div class="user-goal">
        <div class="goal-text">{{ msg.goal }}</div>
        <div v-if="msg.selectedApps && msg.selectedApps.length" class="goal-chips">
          <span v-for="app in msg.selectedApps" :key="app.appID" class="goal-chip">
            <img v-if="app.icon" :src="app.icon" class="chip-icon" alt="" />
            @{{ app.name }}
          </span>
        </div>
      </div>
    </template>

    <!-- 助手消息：过程阶段（默认折叠）+ 最终结果（默认展开） -->
    <template v-else>
      <div v-if="msg.stages.length" class="stages-wrap">
        <div class="stages-toolbar">
          <span class="stages-label">{{ t('agent.process', '编排过程') }}</span>
          <el-button
            v-if="msg.stages.length > 1"
            link
            size="small"
            @click="toggleAll(msg)"
          >
            {{ allCollapsed(msg) ? t('common.expandAll', '全部展开') : t('common.collapseAll', '全部折叠') }}
          </el-button>
        </div>
        <AgentStageItem
          v-for="stage in msg.stages"
          :key="stage.id"
          :stage="stage"
          @toggle="onToggleStage(msg, stage)"
        />
      </div>

      <!-- 最终结果：默认展开 -->
      <div v-if="msg.finalAnswer != null" class="final-answer">
        <div class="final-head">
          <el-icon color="#67c23a"><CircleCheck /></el-icon>
          <span>{{ t('agent.finalResult', '最终结果') }}</span>
        </div>
        <div class="final-body" v-html="renderedAnswer"></div>
      </div>

      <!-- 进行中且无最终结果：显示当前运行阶段的流式内容（服务端返回的实时增量） -->
      <div v-else-if="msg.running" class="final-answer running thinking-stream">
        <el-icon class="is-loading"><Loading /></el-icon>
        <div class="thinking-body">
          <div class="thinking-title">{{ currentStageLabel || t('agent.thinking', '正在思考…') }}</div>
          <div v-if="currentRunningStage?.content" ref="thinkingContentRef" class="thinking-content" v-html="renderedThinking"></div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onUnmounted } from 'vue'
import type { AgentMessage, AgentStage } from '@/types/agent'
import { CircleCheck, Loading } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import AgentStageItem from './AgentStageItem.vue'

const props = defineProps<{ msg: AgentMessage }>()
const { t } = useI18n()
const thinkingContentRef = ref<HTMLElement | null>(null)

// 当前时间戳，用于实时刷新运行中阶段的时长显示
const now = ref(Date.now())
let timer: ReturnType<typeof setInterval> | null = null

function startTimer() {
  if (props.msg.running && !timer) {
    timer = setInterval(() => {
      now.value = Date.now()
    }, 1000)
  }
}

function stopTimer() {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

watch(() => props.msg.running, (running) => {
  if (running) startTimer()
  else stopTimer()
}, { immediate: true })

onUnmounted(stopTimer)

// 最终结果做轻量 Markdown 渲染（不引入额外依赖：仅换行 + 基础转义）。
// 复杂渲染可后续接入现有 markdown 渲染工具；本期保持最小依赖。
const renderedAnswer = computed(() => {
  const raw = props.msg.finalAnswer || ''
  const esc = raw
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
  return esc.replace(/\n/g, '<br/>')
})

// 当前正在运行的阶段：用于在 thinking 区域展示服务端实时流式内容
const currentRunningStage = computed<AgentStage | undefined>(() => {
  return [...props.msg.stages].reverse().find((s) => s.status === 'running')
})

// 当前节点名称：只显示当前运行阶段的短名称
const currentStageLabel = computed(() => {
  const stage = currentRunningStage.value
  if (!stage) return ''
  const dur = stage.startTime ? now.value - stage.startTime : 0
  const durText = dur > 0 ? ` (${formatDuration(dur)})` : ''
  switch (stage.kind) {
    case 'planning': return t('agent.stagePlanningShort', '规划') + durText
    case 'callApp': return t('agent.stageCallAppShort', '调用') + (stage.appName ? `《${stage.appName}》` : '') + durText
    case 'reflecting': return t('agent.stageReflectingShort', '反思') + durText
    case 'final': return t('agent.finalResult', '最终结果')
    default: return stage.title
  }
})

function formatDuration(ms: number): string {
  const s = Math.floor(ms / 1000)
  const m = Math.floor(s / 60)
  const h = Math.floor(m / 60)
  if (h > 0) return `${h}h ${m % 60}m ${s % 60}s`
  if (m > 0) return `${m}m ${s % 60}s`
  if (s >= 10) return `${s}s`
  return `${(ms / 1000).toFixed(1)}s`
}

const renderedThinking = computed(() => {
  const raw = currentRunningStage.value?.content || ''
  const esc = raw
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
  return esc.replace(/\n/g, '<br/>')
})

// 流式内容更新时自动滚动到底部，展示最新输出
watch(
  () => currentRunningStage.value?.content,
  () => {
    const el = thinkingContentRef.value
    if (el && props.msg.running) {
      el.scrollTop = el.scrollHeight
    }
  },
  { flush: 'post' },
)

function allCollapsed(msg: AgentMessage) {
  return msg.stages.every((s) => s.collapsed)
}

function toggleAll(msg: AgentMessage) {
  const collapseTo = !allCollapsed(msg)
  msg.stages.forEach((s) => (s.collapsed = collapseTo))
}

function onToggleStage(msg: AgentMessage, stage: AgentStage) {
  // final 阶段不折叠
  if (stage.kind === 'final') return
  stage.collapsed = !stage.collapsed
}
</script>

<style lang="scss" scoped>
.agent-message {
  margin: 12px 0;
}
.role-user {
  display: flex;
  justify-content: flex-end;
  .user-goal {
    max-width: 78%;
    background: var(--el-color-primary, #409eff);
    color: #fff;
    border-radius: 12px 12px 4px 12px;
    padding: 10px 14px;
  }
  .goal-text {
    font-size: 14px;
    line-height: 1.6;
    white-space: pre-wrap;
    word-break: break-word;
  }
  .goal-chips {
    margin-top: 8px;
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }
  .goal-chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: rgba(255, 255, 255, 0.22);
    border-radius: 10px;
    padding: 2px 8px;
    font-size: 12px;
  }
  .chip-icon {
    width: 14px;
    height: 14px;
    border-radius: 3px;
    object-fit: cover;
  }
}
.role-assistant {
  .stages-wrap {
    background: var(--bg-card, #fff);
    border: 1px solid var(--border-color, #ebeef5);
    border-radius: 8px;
    padding: 8px 12px;
    margin-bottom: 8px;
  }
  .stages-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 4px;
  }
  .stages-label {
    font-size: 12px;
    color: var(--text-secondary, #909399);
  }
}
.final-answer {
  background: var(--bg-card, #fff);
  border: 1px solid var(--border-color, #ebeef5);
  border-radius: 8px;
  padding: 10px 14px;
  .final-head {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 14px;
    font-weight: 600;
    color: var(--text-primary, #303133);
    margin-bottom: 6px;
  }
  .final-body {
    font-size: 14px;
    line-height: 1.7;
    color: var(--text-regular, #606266);
    :deep(br) {
      content: '';
    }
  }
  &.running {
    display: flex;
    align-items: flex-start;
    gap: 8px;
    color: var(--text-secondary, #909399);
    font-size: 13px;
    .is-loading {
      animation: spin 1.2s linear infinite;
      flex-shrink: 0;
      margin-top: 2px;
    }
  }
}
.thinking-stream {
  .thinking-body {
    flex: 1;
    min-width: 0;
  }
  .thinking-title {
    font-weight: 500;
    color: var(--text-primary, #303133);
    margin-bottom: 4px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .thinking-content {
    max-height: 240px;
    overflow-y: auto;
    font-size: 13px;
    line-height: 1.6;
    color: var(--text-regular, #606266);
    white-space: pre-wrap;
    word-break: break-word;
    :deep(br) {
      content: '';
    }
  }
}
@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
