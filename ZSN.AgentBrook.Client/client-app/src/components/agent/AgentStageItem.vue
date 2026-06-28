<template>
  <!-- 单个过程阶段：可折叠/展开 + 状态图标 + 流式内容 + 等待动画 -->
  <div class="stage-item" :class="[`status-${stage.status}`]">
    <div class="stage-head" @click="toggleCollapse">
      <span class="status-icon">
        <el-icon v-if="stage.status === 'running'" class="is-loading"><Loading /></el-icon>
        <el-icon v-else-if="stage.status === 'completed'" color="#67c23a"><CircleCheck /></el-icon>
        <el-icon v-else-if="stage.status === 'failed'" color="#f56c6c"><CircleClose /></el-icon>
        <el-icon v-else color="#e6a23c"><WarningFilled /></el-icon>
      </span>
      <span class="stage-title">{{ stage.title }}</span>
      <span v-if="durationText" class="stage-duration">{{ durationText }}</span>
      <span v-if="stage.status === 'running'" class="dots">
        <span class="dot" /><span class="dot" /><span class="dot" />
      </span>
      <el-icon class="collapse-arrow" :class="{ collapsed: stage.collapsed }">
        <ArrowDown />
      </el-icon>
    </div>
    <transition name="collapse">
      <div v-show="!stage.collapsed" class="stage-body">
        <pre v-if="stage.content" class="stage-content">{{ stage.content }}</pre>
        <div v-if="stage.error" class="stage-error">{{ stage.error }}</div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { Loading, CircleCheck, CircleClose, WarningFilled, ArrowDown } from '@element-plus/icons-vue'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import type { AgentStage } from '@/types/agent'

const props = defineProps<{ stage: AgentStage }>()
const emit = defineEmits<{ toggle: [stage: AgentStage] }>()

function toggleCollapse() {
  // collapsed 由父组件统一管理（受控），这里只发事件
  emit('toggle', props.stage)
}

// 阶段执行时长
const elapsedMs = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

function updateElapsed() {
  const s = props.stage
  if (s.status === 'running' && s.startTime) {
    elapsedMs.value = Date.now() - s.startTime
  } else if (s.endTime && s.startTime) {
    elapsedMs.value = s.endTime - s.startTime
  } else {
    elapsedMs.value = 0
  }
}

function startTimer() {
  stopTimer()
  if (props.stage.status === 'running') {
    timer = setInterval(updateElapsed, 1000)
  }
}

function stopTimer() {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

const durationText = computed(() => {
  if (!elapsedMs.value) return ''
  const ms = elapsedMs.value
  const s = Math.floor(ms / 1000)
  const m = Math.floor(s / 60)
  const h = Math.floor(m / 60)
  if (h > 0) return `${h}h ${m % 60}m ${s % 60}s`
  if (m > 0) return `${m}m ${s % 60}s`
  if (s >= 10) return `${s}s`
  return `${(ms / 1000).toFixed(1)}s`
})

watch(() => props.stage.status, () => {
  updateElapsed()
  startTimer()
})

onMounted(() => {
  updateElapsed()
  startTimer()
})

onUnmounted(stopTimer)
</script>

<style lang="scss" scoped>
.stage-item {
  border-left: 2px solid var(--border-color, #ebeef5);
  margin: 4px 0;
  padding-left: 10px;
  &.status-running {
    border-left-color: var(--el-color-primary, #409eff);
  }
  &.status-completed {
    border-left-color: #67c23a;
  }
  &.status-failed {
    border-left-color: #f56c6c;
  }
  &.status-aborted {
    border-left-color: #e6a23c;
  }
}
.stage-head {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 3px 0;
  user-select: none;
}
.status-icon {
  display: inline-flex;
  font-size: 14px;
  .is-loading {
    animation: rotating 1.5s linear infinite;
  }
}
.stage-title {
  flex: 1 1 auto;
  font-size: 13px;
  color: var(--text-primary, #303133);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.stage-duration {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--text-secondary, #909399);
  font-variant-numeric: tabular-nums;
  min-width: 44px;
  text-align: right;
}
.dots {
  display: inline-flex;
  gap: 2px;
  .dot {
    width: 4px;
    height: 4px;
    border-radius: 50%;
    background: var(--el-color-primary, #409eff);
    animation: blink 1.2s infinite ease-in-out both;
    &:nth-child(2) { animation-delay: 0.2s; }
    &:nth-child(3) { animation-delay: 0.4s; }
  }
}
.collapse-arrow {
  font-size: 12px;
  color: var(--text-secondary, #909399);
  transition: transform 0.2s;
  &.collapsed {
    transform: rotate(-90deg);
  }
}
.stage-body {
  padding: 6px 0 8px 4px;
}
.stage-content {
  margin: 0;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  line-height: 1.6;
  color: var(--text-regular, #606266);
  background: var(--bg-hover, #f7f8fa);
  border-radius: 6px;
  padding: 8px 10px;
  white-space: pre-wrap;
  word-break: break-word;
  max-height: 320px;
  overflow-y: auto;
}
.stage-error {
  margin-top: 4px;
  font-size: 12px;
  color: var(--el-color-danger, #f56c6c);
}
.collapse-enter-active,
.collapse-leave-active {
  transition: opacity 0.2s, max-height 0.2s;
  overflow: hidden;
}
@keyframes rotating {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
@keyframes blink {
  0%, 80%, 100% { opacity: 0.2; }
  40% { opacity: 1; }
}
</style>
