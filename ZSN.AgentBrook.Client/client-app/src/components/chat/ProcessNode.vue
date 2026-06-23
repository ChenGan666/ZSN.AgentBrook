<template>
  <div class="process-node">
    <div class="node-row" @click="handleToggle">
      <span :class="['node-name', 'status-badge', statusClass]">
        {{ node.nodeName || node.nodeId || '未命名节点' }}
      </span>

      <span v-if="isFailed" class="retry-icon" @click.stop="handleRetry" title="重试该节点">
        <el-icon :size="12"><RefreshRight /></el-icon>
      </span>

      <span v-if="hasChildren || hasChildrenGroups" class="toggle-icon" @click.stop="toggleChildren">
        <el-icon :size="10">
          <ArrowDown v-if="childrenExpanded" />
          <ArrowRight v-else />
        </el-icon>
      </span>
      <span v-else-if="hasDetails" class="toggle-icon">
        <el-icon :size="10">
          <ArrowDown v-if="detailsExpanded" />
          <ArrowRight v-else />
        </el-icon>
      </span>
    </div>

    <div
      v-show="detailsExpanded"
      v-if="(node.outputs && node.outputs.length) || (node.logs && node.logs.length)"
      class="node-details"
    >
      <div class="outputs">
        <div v-for="(output, idx) in node.outputs" :key="idx" class="output-item">
          <div class="output-name">
            {{ output.varname }}
            <span class="type">({{ output.type }})</span>
          </div>
          <pre class="output-value">{{ formatValue(output.value) }}</pre>
        </div>
      </div>

      <div v-if="node.inputs" class="inputs">
        <div class="section-title">输入参数</div>
        <textarea class="inputs-textarea" readonly :value="formatInputs(node.inputs)" />
      </div>

      <div v-if="node.logs && node.logs.length" class="logs">
        <div class="section-title">执行日志</div>
        <div v-for="(log, idx) in displayedLogs" :key="idx" class="log-line">{{ log }}</div>
        <div v-if="hasMoreLogs" class="logs-toggle" @click.stop="logsExpanded = !logsExpanded">
          {{ logsExpanded ? '收起' : `展开更多 (${node.logs.length - 3} 条)` }}
        </div>
      </div>

      <div
        v-if="(node.startTime || node.endTime) && node.logs && node.logs.length"
        class="time-info"
      >
        <span v-if="node.startTime">开始: {{ formatTime(node.startTime) }}</span>
        <span v-if="node.endTime">结束: {{ formatTime(node.endTime) }}</span>
        <span v-if="node.startTime && node.endTime">
          耗时: {{ calculateDuration(node.startTime, node.endTime) }}
        </span>
      </div>
    </div>

    <div v-if="childrenExpanded && (hasChildren || hasChildrenGroups)">
      <template v-if="node.childrenGroups && node.childrenGroups.length">
        <div
          v-for="(group, idx) in node.childrenGroups"
          :key="idx"
          class="children-group-wrapper"
        >
          <div v-if="!group.processesId" class="node-children">
            <ProcessNode
              v-for="child in group.nodes"
              :key="child.recordId || child.nodeId"
              :node="child"
              :level="level + 1"
            />
          </div>
          <div v-else class="sub-task-group">
            <ProcessNode
              v-for="child in group.nodes"
              :key="child.recordId || child.nodeId"
              :node="child"
              :level="level + 1"
            />
          </div>
        </div>
      </template>
      <div v-else class="node-children">
        <ProcessNode
          v-for="child in node.children"
          :key="child.recordId || child.nodeId"
          :node="child"
          :level="level + 1"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, inject } from 'vue'
import { ArrowDown, ArrowRight, RefreshRight } from '@element-plus/icons-vue'
import type { NormalizedRecord } from '@/types/chat'

interface TreeNode extends NormalizedRecord {
  children: TreeNode[]
  childrenGroups?: { processesId: string | null; nodes: TreeNode[] }[]
}

const props = defineProps<{
  node: TreeNode
  level: number
}>()

const childrenExpanded = ref(false)
const detailsExpanded = ref(false)
const logsExpanded = ref(false)

const hasChildren = computed(() => props.node.children && props.node.children.length > 0)
const hasChildrenGroups = computed(
  () => props.node.childrenGroups && props.node.childrenGroups.length > 0,
)
const hasDetails = computed(
  () =>
    (props.node.outputs && props.node.outputs.length) ||
    (props.node.inputs && props.node.inputs.length) ||
    (props.node.logs && props.node.logs.length),
)

const isFailed = computed(() => {
  const status = String(props.node.status || '').toLowerCase()
  return status === 'failed' || status === 'error' || status === 'fail'
})

const onRetryNode = inject<((node: TreeNode) => void) | null>('onRetryNode', null)

const statusClass = computed(() => {
  const status = String(props.node.status || '').toLowerCase()
  if (status === 'running') return 'status-running'
  if (status === 'success') return 'status-success'
  if (status === 'failed' || status === 'error' || status === 'fail') return 'status-failed'
  return 'status-unknown'
})

const displayedLogs = computed(() => {
  if (!props.node.logs?.length) return []
  return logsExpanded.value ? props.node.logs : props.node.logs.slice(0, 3)
})

const hasMoreLogs = computed(() => props.node.logs && props.node.logs.length > 3)

watch(
  () => props.node.status,
  (newStatus) => {
    const s = String(newStatus || '').toLowerCase()
    if (s === 'success' || s === 'failed' || s === 'error' || s === 'fail') {
      detailsExpanded.value = false
    }
  },
  { immediate: true },
)

function toggleChildren() {
  childrenExpanded.value = !childrenExpanded.value
}
function handleToggle() {
  if (hasDetails.value) detailsExpanded.value = !detailsExpanded.value
}
function handleRetry() {
  if (onRetryNode) {
    onRetryNode(props.node)
  }
}

function formatValue(val: any): string {
  try {
    if (typeof val === 'string') return val.length > 500 ? val.substring(0, 500) + '...' : val
    const str = JSON.stringify(val, null, 2)
    return str.length > 500 ? str.substring(0, 500) + '...' : str
  } catch {
    return String(val)
  }
}

function formatInputs(inputs: any): string {
  try {
    return typeof inputs === 'string' ? inputs : JSON.stringify(inputs, null, 2)
  } catch {
    return String(inputs)
  }
}

function formatTime(timeStr: string): string {
  if (!timeStr) return ''
  try {
    return new Date(timeStr).toLocaleTimeString('zh-CN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    })
  } catch {
    return timeStr
  }
}

function calculateDuration(start: string, end: string): string {
  try {
    const diff = new Date(end).getTime() - new Date(start).getTime()
    if (diff < 1000) return `${diff}ms`
    if (diff < 60000) return `${(diff / 1000).toFixed(2)}s`
    return `${(diff / 60000).toFixed(2)}min`
  } catch {
    return ''
  }
}
</script>

<style lang="scss" scoped>
.process-node {
  position: relative;
}

.node-children {
  margin-left: 20px;
}

.children-group-wrapper {
  margin-left: 20px;
}

.sub-task-group {
  display: flex;
  flex-direction: row;
  gap: 12px;
  margin-bottom: 1px;
  align-items: flex-start;

  .process-node {
    border-radius: 6px;
    border: 1px solid #e5e7eb;
    background: #fafafa;
    max-width: 300px;
  }
}

.node-row {
  display: flex;
  align-items: center;
  padding: 1px 0;
  cursor: pointer;
  transition: background 0.2s;

  &:hover {
    background: #f9fafb;
  }
}

.toggle-icon {
  color: #9ca3af;
  cursor: pointer;
  user-select: none;
  flex-shrink: 0;
  margin-left: auto;
  display: flex;
  align-items: center;
}

.retry-icon {
  cursor: pointer;
  color: #3b82f6;
  margin-left: 4px;
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;

  &:hover {
    color: #2563eb;
  }
}

.node-name {
  flex: 1;
  font-size: 9px;
  font-weight: 400;
  color: #6b7280;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 9px;
  font-weight: 400;
}

.status-success {
  background: #f0fdf4;
  color: #10b981;
}

.status-running {
  position: relative;
  background: linear-gradient(white, white) padding-box,
    conic-gradient(from 0deg, #3b82f6, #60a5fa, #2563eb, #1d4ed8, #3b82f6) border-box;
  border: 2px solid transparent;
  animation: borderRotate 3s linear infinite;
  color: #3b82f6;
}

@keyframes borderRotate {
  0% {
    background: linear-gradient(white, white) padding-box,
      conic-gradient(from 0deg, #3b82f6, #60a5fa, #2563eb, #1d4ed8, #3b82f6) border-box;
  }
  25% {
    background: linear-gradient(white, white) padding-box,
      conic-gradient(from 90deg, #3b82f6, #60a5fa, #2563eb, #1d4ed8, #3b82f6) border-box;
  }
  50% {
    background: linear-gradient(white, white) padding-box,
      conic-gradient(from 180deg, #3b82f6, #60a5fa, #2563eb, #1d4ed8, #3b82f6) border-box;
  }
  75% {
    background: linear-gradient(white, white) padding-box,
      conic-gradient(from 270deg, #3b82f6, #60a5fa, #2563eb, #1d4ed8, #3b82f6) border-box;
  }
  100% {
    background: linear-gradient(white, white) padding-box,
      conic-gradient(from 360deg, #3b82f6, #60a5fa, #2563eb, #1d4ed8, #3b82f6) border-box;
  }
}

.status-failed {
  background: #fef2f2;
  color: #ef4444;
}

.status-unknown {
  background: #f3f4f6;
  color: #9ca3af;
}

.node-details {
  margin: 4px 0;
  padding: 2px;
  background: #fafafa;
  border-left: 2px solid #e5e7eb;
}

.section-title {
  font-size: 11px;
  font-weight: 500;
  color: #9ca3af;
  margin-bottom: 6px;
}

.outputs,
.inputs,
.logs {
  margin-bottom: 12px;
}

.output-item {
  margin-bottom: 8px;
}

.output-name {
  font-size: 11px;
  font-weight: 400;
  color: #6b7280;
  margin-bottom: 3px;
}

.type {
  font-size: 10px;
  color: #9ca3af;
}

.output-value {
  margin: 0;
  padding: 6px;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 3px;
  font-size: 11px;
  font-family: 'Monaco', 'Menlo', 'Consolas', monospace;
  color: #4b5563;
  overflow-x: auto;
  max-height: 150px;
  overflow-y: auto;
  white-space: pre-wrap;
  word-break: break-all;
}

.inputs-textarea {
  width: 100%;
  min-height: 80px;
  padding: 8px;
  font-size: 10px;
  font-family: 'Courier New', monospace;
  color: #374151;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 4px;
  resize: vertical;
  line-height: 1.4;
}

.log-line {
  font-size: 11px;
  font-family: 'Monaco', 'Menlo', 'Consolas', monospace;
  color: #6b7280;
  padding: 3px 6px;
  background: #fff;
  border-left: 2px solid #e5e7eb;
  margin-bottom: 3px;
  white-space: pre-wrap;
  word-break: break-all;
}

.logs-toggle {
  font-size: 11px;
  color: #9ca3af;
  cursor: pointer;
  padding: 3px 6px;
  text-align: center;
  user-select: none;

  &:hover {
    color: #6b7280;
  }
}

.time-info {
  display: flex;
  gap: 12px;
  font-size: 10px;
  color: #9ca3af;
  padding-top: 6px;
  border-top: 1px solid #e5e7eb;
  margin-top: 6px;
}
</style>
