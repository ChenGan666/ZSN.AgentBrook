<template>
  <div class="process-status">
    <div class="process-header" @click="togglePanel">
      <span class="process-title">
        <span :class="['status-badge', statusClass]">{{ statusText }}</span>
        <div class="process-actions">
          <span class="action-btn" @click.stop="handleRefreshClick" title="刷新">
            <el-icon :size="12"><RefreshRight /></el-icon>
          </span>
          <span class="action-btn" @click.stop="togglePanel" title="展开/折叠">
            <el-icon :size="12">
              <ArrowDown v-if="!panelCollapsed" />
              <ArrowRight v-else />
            </el-icon>
          </span>
        </div>
      </span>
    </div>

    <div v-show="!panelCollapsed" class="process-body">
      <ProcessNode
        v-for="node in processTreeWithSubTasks"
        :key="node.recordId || node.nodeId"
        :node="node"
        :level="0"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, provide } from 'vue'
import { ArrowDown, ArrowRight, RefreshRight } from '@element-plus/icons-vue'
import ProcessNode from './ProcessNode.vue'
import type { NormalizedRecord, MessageProcess } from '@/types/chat'

interface TreeNode extends NormalizedRecord {
  children: TreeNode[]
  childrenGroups?: { processesId: string | null; nodes: TreeNode[] }[]
}

const props = defineProps<{
  processData: MessageProcess
  autoCollapse?: boolean
}>()

const emit = defineEmits<{
  retry: []
  retryNode: [node: NormalizedRecord]
}>()

const panelCollapsed = ref(false)

watch(
  () => props.autoCollapse,
  (shouldCollapse) => {
    if (shouldCollapse) panelCollapsed.value = true
  },
  { immediate: true },
)

watch(
  () => props.processData.status,
  (newStatus) => {
    if (String(newStatus || '').toLowerCase() === 'success') {
      panelCollapsed.value = true
    }
  },
  { immediate: true },
)

const statusClass = computed(() => {
  const status = String(props.processData.status || '').toLowerCase()
  if (status === 'running') return 'status-running'
  if (status === 'success') return 'status-success'
  if (status === 'failed' || status === 'error' || status === 'fail') return 'status-failed'
  return 'status-unknown'
})

const statusText = computed(() => {
  const status = String(props.processData.status || '').toLowerCase()
  if (status === 'running') return '正在思考'
  if (status === 'success') return '思考完成'
  if (status === 'failed' || status === 'error' || status === 'fail') return '出错了'
  return 'Unknown'
})

const processTreeWithSubTasks = computed(() => {
  const records = props.processData.records || []
  if (!records.length) return []
  return buildProcessTree(records)
})

function buildProcessTree(records: NormalizedRecord[]): TreeNode[] {
  const sortedRecords = [...records].sort((a, b) => {
    const timeA = new Date(a.startTime || 0).getTime()
    const timeB = new Date(b.startTime || 0).getTime()
    return timeA - timeB
  })

  const nodeMap = new Map<string, TreeNode>()
  const roots: TreeNode[] = []

  sortedRecords.forEach((rec) => {
    const taskId = rec.taskId || ''
    const fromMainTaskId = rec.fromMainTaskId || ''

    const node: TreeNode = {
      ...rec,
      children: [],
    }

    nodeMap.set(taskId, node)

    if (!fromMainTaskId) {
      roots.push(node)
    }
  })

  sortedRecords.forEach((rec) => {
    const taskId = rec.taskId || ''
    const fromMainTaskId = rec.fromMainTaskId || ''

    if (fromMainTaskId) {
      const node = nodeMap.get(taskId)
      const parent = nodeMap.get(fromMainTaskId)

      if (node && parent) {
        const rootIndex = roots.indexOf(node)
        if (rootIndex > -1) roots.splice(rootIndex, 1)
        parent.children.push(node)
      } else if (node && !parent) {
        if (!roots.includes(node)) roots.push(node)
      }
    }
  })

  // Orphan adoption by processesId prefix match
  const orphansToRemove: TreeNode[] = []
  roots.forEach((node) => {
    const pid = node.processesId || ''
    if (pid && pid.includes('_')) {
      const prefix = pid.substring(0, pid.indexOf('_'))
      const nodeStartTime = new Date(node.startTime || 0).getTime()
      let bestParent: TreeNode | null = null
      let bestTime = -Infinity
      for (const n of nodeMap.values()) {
        if (n === node) continue
        const nPid = n.processesId || ''
        if (nPid !== prefix) continue
        const nTime = new Date(n.startTime || 0).getTime()
        if (nTime <= nodeStartTime && nTime > bestTime) {
          bestParent = n
          bestTime = nTime
        }
      }
      if (!bestParent) {
        for (const n of nodeMap.values()) {
          if (n === node) continue
          const nPid = n.processesId || ''
          if (nPid === prefix) {
            bestParent = n
            break
          }
        }
      }
      if (bestParent) {
        bestParent.children.push(node)
        orphansToRemove.push(node)
      }
    }
  })
  orphansToRemove.forEach((node) => {
    const idx = roots.indexOf(node)
    if (idx > -1) roots.splice(idx, 1)
  })

  // Group children by ProcessesID
  function groupChildrenByProcessesId(nodes: TreeNode[]) {
    nodes.forEach((node) => {
      if (node.children && node.children.length > 0) {
        const grouped: Record<string, TreeNode[]> = {}
        node.children.forEach((child) => {
          const processesId = child.processesId || ''
          if (processesId && processesId.includes('_')) {
            if (!grouped[processesId]) grouped[processesId] = []
            grouped[processesId].push(child)
          } else {
            if (!grouped['_single_']) grouped['_single_'] = []
            grouped['_single_'].push(child)
          }
        })

        const hasGroups = Object.keys(grouped).some(
          (key) => key !== '_single_' && grouped[key].length > 0,
        )
        if (hasGroups) {
          node.childrenGroups = []

          if (grouped['_single_']) {
            grouped['_single_'].forEach((child) => {
              node.childrenGroups!.push({ processesId: null, nodes: [child] })
            })
          }

          Object.keys(grouped).forEach((processesId) => {
            if (processesId !== '_single_') {
              node.childrenGroups!.push({ processesId, nodes: grouped[processesId] })
            }
          })

          node.children = []
        }

        if (node.children.length > 0) groupChildrenByProcessesId(node.children)
        if (node.childrenGroups) {
          node.childrenGroups.forEach((group) => groupChildrenByProcessesId(group.nodes))
        }
      }
    })
  }

  groupChildrenByProcessesId(roots)
  return roots
}

function togglePanel() {
  panelCollapsed.value = !panelCollapsed.value
}

function handleRefreshClick() {
  emit('retry')
}

function handleRetryNode(node: NormalizedRecord) {
  emit('retryNode', node)
}

provide('onRetryNode', handleRetryNode)
</script>

<style lang="scss" scoped>
.process-status {
  margin: 8px 0;
  background: transparent;
  overflow: hidden;
}

.process-header {
  display: flex;
  align-items: center;
  padding: 3px 0;
  background: transparent;
  cursor: pointer;
  user-select: none;
}

.process-title {
  font-size: 11px;
  font-weight: 400;
  color: #9ca3af;
  display: flex;
  align-items: center;
  gap: 6px;
  width: 100%;
}

.status-badge {
  display: inline-block;
  padding: 1px 4px;
  border-radius: 6px;
  font-size: 9px;
  font-weight: 400;
}

.status-running {
  background: transparent;
  color: #6b7280;
}

.status-success {
  background: transparent;
  color: #10b981;
}

.status-failed {
  background: transparent;
  color: #ef4444;
}

.status-unknown {
  background: transparent;
  color: #9ca3af;
}

.process-actions {
  display: flex;
  gap: 2px;
  margin-left: auto;
}

.action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #9ca3af;
  cursor: pointer;
  transition: color 0.2s;
  padding: 2px;

  &:hover {
    color: #6b7280;
  }
}

.process-body {
  padding: 2px;
  max-height: 500px;
  overflow-y: auto;
  background: #fff;

  &::-webkit-scrollbar {
    width: 6px;
  }
  &::-webkit-scrollbar-track {
    background: #f3f4f6;
  }
  &::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 3px;
  }
  &::-webkit-scrollbar-thumb:hover {
    background: #9ca3af;
  }
}
</style>
