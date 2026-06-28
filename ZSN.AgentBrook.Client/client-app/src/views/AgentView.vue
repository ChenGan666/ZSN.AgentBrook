<template>
  <!--
    Agent 主视图：编排大脑工作台。
    布局：顶部（模型选择）+ 中部（过程化消息区）+ 底部（@ chip 栏 + 输入框 + 发送/停止）。
    与 Chat 平行、零侵入：编排循环由 composables/useAgentOrchestrator 驱动（L3 接入）。
  -->
  <div class="agent-view">
    <!-- 顶部：编排模型选择 + 任务清单切换 -->
    <div class="agent-topbar">
      <ModelSelector />
      <div class="topbar-hint">{{ t('agent.topHint') }}</div>
      <el-tooltip :content="t('agent.taskPanel', '任务清单')" placement="bottom">
        <el-button
          text
          :type="taskPanelVisible ? 'primary' : ''"
          :icon="List"
          @click="taskPanelVisible = !taskPanelVisible"
        />
      </el-tooltip>
    </div>

    <div class="agent-body">
      <!-- 中部：消息区 -->
      <div ref="scrollRef" class="agent-messages" @scroll="onScroll">
        <div v-if="agentStore.messages.length === 0" class="empty-state">
          <el-icon class="empty-icon"><MagicStick /></el-icon>
          <div class="empty-title">{{ t('agent.emptyTitle', 'Agent 编排工作台') }}</div>
          <div class="empty-desc">
            {{ t('agent.emptyDesc') }}
          </div>
        </div>
        <AgentMessage
          v-for="msg in agentStore.messages"
          :key="msg.id"
          :msg="msg"
        />
      </div>

      <!-- 任务清单侧栏（L4） -->
      <transition name="el-zoom-in-right">
        <div v-show="taskPanelVisible" class="task-sidebar">
          <AgentTaskPanel ref="taskPanelRef" />
        </div>
      </transition>
    </div>

    <!-- 底部：输入框（@App 内嵌为彩色标签，contenteditable 支持内联 tag） -->
    <div class="agent-input-area">
      <!-- @ 浮层 -->
      <AgentMentionPopover
        :visible="mentionVisible"
        :pos="{ bottom: 80 }"
        @done="closeMention"
        @pick="onPickApp"
      />
      <LocalAuthorizePanel />

      <div class="input-row">
        <div
          ref="inputRef"
          class="agent-input"
          contenteditable="true"
          :data-placeholder="t('agent.inputPlaceholder')"
          @input="onInput"
          @keydown="onKeydown"
          @paste="onPaste"
          @blur="onBlur"
        />
        <el-button
          v-if="!currentSessionRunning"
          type="primary"
          :icon="Promotion"
          circle
          @click="handleSend"
        />
        <el-button v-else type="danger" :icon="VideoPause" circle @click="handleStop" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref, reactive, watch, computed } from 'vue'
import { MagicStick, Promotion, VideoPause, List } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import ModelSelector from '@/components/agent/ModelSelector.vue'
import AgentMentionPopover from '@/components/agent/AgentMentionPopover.vue'
import AgentMessage from '@/components/agent/AgentMessage.vue'
import AgentTaskPanel from '@/components/agent/AgentTaskPanel.vue'
import LocalAuthorizePanel from '@/components/agent/LocalAuthorizePanel.vue'
import { useAgentStore } from '@/stores/agent'
import { useChatStore } from '@/stores/chat'
import type { AgentMessage as AgentMessageType, StepEvent, RunInput } from '@/types/agent'
import type { AppInfo } from '@/types/chat'

const { t } = useI18n()
const agentStore = useAgentStore()
const chatStore = useChatStore()

// 当前会话是否运行中：按会话呈现状态，切换界面不中断
const currentSessionRunning = computed(() => agentStore.currentSession?.running ?? false)

const inputRef = ref<HTMLDivElement | null>(null)
const scrollRef = ref<HTMLElement | null>(null)
const mentionVisible = ref(false)
/** 保存 @ 触发时的选区与文本节点（点击浮层后 contenteditable 失焦选区会丢失） */
let savedRange: Range | null = null
let savedTextNode: Text | null = null
const taskPanelVisible = ref(false)
const taskPanelRef = ref<InstanceType<typeof AgentTaskPanel> | null>(null)
const autoScroll = ref(true)

// L3 接入点：编排引擎
const orchestrator = ref<null | {
  run: (input: RunInput, onEvent: (e: StepEvent) => void) => Promise<{ answer: string }>
  abort: () => void
  running: import('vue').Ref<boolean>
}>(null)

// ── contenteditable 辅助 ──────────────────────────────────

/** 获取输入框纯文本（所有 textContent，@tag 的 text 即为 @AppName） */
function getPlainText(): string {
  const el = inputRef.value
  if (!el) return ''
  return (el.textContent || '').replace(/\u00A0/g, ' ').trim()
}

/** 从 contenteditable 内的 mention-tag span 提取 appIDs（已去重）。 */
function getInputAppIDs(): string[] {
  const el = inputRef.value
  if (!el) return []
  const tags = el.querySelectorAll<HTMLSpanElement>('.mention-tag')
  const ids: string[] = []
  tags.forEach((t) => {
    const id = t.dataset.appId
    if (id && !ids.includes(id)) ids.push(id)
  })
  return ids
}

/** 把光标移到 contenteditable 末尾。 */
function moveCursorToEnd() {
  const el = inputRef.value
  if (!el) return
  el.focus()
  const range = document.createRange()
  range.selectNodeContents(el)
  range.collapse(false)
  const sel = window.getSelection()
  if (sel) {
    sel.removeAllRanges()
    sel.addRange(range)
  }
}

// ── 事件处理 ───────────────────────────────────────────

function onInput() {
  detectMention()
}

function onPaste(e: ClipboardEvent) {
  // 粘贴纯文本，避免带入富格式破坏 contenteditable 结构
  e.preventDefault()
  const text = e.clipboardData?.getData('text/plain')
  if (text) {
    const sel = window.getSelection()
    if (sel?.rangeCount) {
      const range = sel.getRangeAt(0)
      range.deleteContents()
      range.insertNode(document.createTextNode(text))
      range.collapse(false)
      sel.removeAllRanges()
      sel.addRange(range)
    }
  }
  detectMention()
}

function onBlur() {
  // 失焦时重新加入最后的零宽空格，确保光标和 placeholder 行为正常
  nextTick(normalizeContent)
}

/** 获取 range 内的纯文本内容（用于 detectMention） */
function getRangeText(range: Range): string {
  const fragment = range.cloneContents()
  const div = document.createElement('div')
  div.appendChild(fragment)
  return (div.textContent || '').replace(/\u00A0/g, ' ')
}

/** 检测光标前是否有孤立 @ 触发浮层，同时保存选区（点击浮层后会丢失） */
function detectMention() {
  const sel = window.getSelection()
  const el = inputRef.value
  if (!sel?.rangeCount || !el) { mentionVisible.value = false; return }
  const node = sel.anchorNode
  if (node?.nodeType !== Node.TEXT_NODE) {
    mentionVisible.value = false
    return
  }
  // 基于输入框内光标前的完整文本检测 @，支持在任意文字后（含已有 mention tag 后）继续输入多个 @
  const range = sel.getRangeAt(0).cloneRange()
  range.setStart(el, 0)
  const before = getRangeText(range)
  const hit = /@$/.test(before)
  if (hit) {
    // 保存触发位置以便 onPickApp 使用
    savedRange = sel.getRangeAt(0).cloneRange()
    savedTextNode = node as Text
  } else {
    savedRange = null
    savedTextNode = null
  }
  mentionVisible.value = hit
}

/** 关闭 @ 浮层 */
function closeMention() {
  mentionVisible.value = false
  savedRange = null
  savedTextNode = null
}

/**
 * 选中 App：
 * 1) 用保存的选区删除触发符 @
 * 2) 插入内联标签（蓝色 tag + SVG 关闭按钮，整体不可编辑）
 * 3) 标签后插空格，光标移到空格后
 */
function onPickApp(app: AppInfo) {
  const el = inputRef.value
  if (!el) return
  // 恢复到触发时的选区
  el.focus()
  const sel = window.getSelection()
  if (savedRange && savedTextNode && sel) {
    sel.removeAllRanges()
    sel.addRange(savedRange)
  }
  if (!sel?.rangeCount) return
  const range = sel.getRangeAt(0)

  // 删除光标前的 @
  const curNode = range.startContainer
  const curOffset = range.startOffset
  if (curNode.nodeType === Node.TEXT_NODE && curOffset > 0) {
    const tn = curNode as Text
    if (tn.textContent?.[curOffset - 1] === '@') {
      tn.deleteData(curOffset - 1, 1)
      range.setStart(tn, curOffset - 1)
      range.collapse(true)
    }
  }

  // 创建 tag
  const tag = document.createElement('span')
  tag.className = 'mention-tag'
  tag.contentEditable = 'false'
  tag.dataset.appId = app.AppID
  tag.dataset.appName = app.Name
  tag.textContent = `@${app.Name}`

  // 关闭按钮（SVG，不污染 textContent）
  const closeBtn = document.createElement('span')
  closeBtn.className = 'tag-close'
  closeBtn.innerHTML = '<svg viewBox="0 0 12 12" width="10" height="10"><line x1="2" y1="2" x2="10" y2="10" stroke="currentColor" stroke-width="1.5"/><line x1="10" y1="2" x2="2" y2="10" stroke="currentColor" stroke-width="1.5"/></svg>'
  closeBtn.addEventListener('mousedown', (e) => {
    e.preventDefault()
    e.stopPropagation()
    tag.remove()
    el.focus()
  })
  tag.appendChild(closeBtn)

  // 插入 tag + NBSP
  range.insertNode(tag)
  range.setStartAfter(tag)
  const space = document.createTextNode('\u00A0')
  range.insertNode(space)
  range.setStartAfter(space)
  range.collapse(true)
  sel.removeAllRanges()
  sel.addRange(range)

  // 清理：关闭浮层并清空选区缓存，避免影响后续 @ 触发
  savedRange = null
  savedTextNode = null
  mentionVisible.value = false
}

function onKeydown(e: KeyboardEvent) {
  if (mentionVisible.value) {
    if (e.key === 'Escape') closeMention()
    if (e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'Enter') return // 交给 popover
    return
  }
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    if (!currentSessionRunning.value) handleSend()
  }
}

/** 确保 contenteditable 内至少有一个文本节点（否则 placeholder/光标异常） */
function normalizeContent() {
  const el = inputRef.value
  if (!el) return
  if (!el.textContent?.trim()) {
    el.innerHTML = ''
    el.appendChild(document.createTextNode(''))
  }
}

onMounted(async () => {
  if (chatStore.apps.length === 0) await chatStore.fetchApps()
  // 惰性加载编排引擎
  try {
    const mod = await import('@/composables/useAgentOrchestrator')
    orchestrator.value = mod.useAgentOrchestrator()
  } catch { /* L3 未就绪, handleSend 会提示 */ }
  // 问题 2：首次进入 Agent 模式时恢复会话：有历史会话则切到最近一条，否则新建空会话
  if (!agentStore.currentSessionId) {
    if (agentStore.sessions.length > 0) {
      const latest = [...agentStore.sessions].sort((a, b) => b.updatedAt - a.updatedAt)[0]
      agentStore.switchSession(latest.id)
    } else {
      agentStore.createSession(t('agent.newSession'))
    }
  } else {
    // 已存在当前会话时，主动从会话恢复消息，避免切换视图/刷新后右侧空白
    agentStore.switchSession(agentStore.currentSessionId)
  }
  // contenteditable 初始归一化
  nextTick(normalizeContent)
})

// 防御：localStorage 持久化恢复可能滞后于 onMounted，监听 sessions 变化，
// 一旦当前会话可用且 messages 仍为空，自动重新加载，避免点击会话右侧空白。
watch(
  [() => agentStore.sessions, () => agentStore.currentSessionId],
  ([sessions, currentSessionId]) => {
    if (!currentSessionId || sessions.length === 0) return
    const session = sessions.find((s) => s.id === currentSessionId)
    if (!session || agentStore.messages.length > 0) return
    agentStore.switchSession(currentSessionId)
  },
  { immediate: true },
)

async function handleSend() {
  const goal = getPlainText()
  if (!goal) return
  if (agentStore.modelID == null) {
    ElMessage.warning(t('agent.needModel'))
    return
  }
  const mentionedIDs = getInputAppIDs()
  if (mentionedIDs.length === 0) {
    ElMessage.warning(t('agent.needApp'))
    return
  }

  // 当前已有其他会话在运行：单编排器实例，不支持并发，提示用户切换停止
  if (agentStore.anySessionRunning && !agentStore.currentSession?.running) {
    ElMessage.warning(t('agent.busyOtherSession', '当前已有会话在执行中，请切换至该会话停止后再发送'))
    return
  }

  // 从 chatStore.apps 查找选中的 App 详情
  const apps = chatStore.apps || []
  const selectedApps = mentionedIDs
    .map((id) => apps.find((a) => a.AppID === id))
    .filter(Boolean)
    .map((a) => ({ appID: a!.AppID, name: a!.Name, icon: a!.AICON }))

  // 问题 2：当前没有会话时，用用户目标作为标题自动创建会话
  if (!agentStore.currentSessionId) {
    agentStore.createSession(goal, goal)
  }

  // 同步 store
  agentStore.setSelectedApps(selectedApps)
  agentStore.setActiveSessionId(agentStore.currentSessionId!)

  // 用户消息
  const userMsg: AgentMessageType = {
    id: `u_${Date.now()}`,
    role: 'user',
    goal,
    selectedApps,
    stages: [],
    running: false,
    createdAt: Date.now(),
  }
  agentStore.addMessage(userMsg)

  // 清空输入框
  const el = inputRef.value
  if (el) el.innerHTML = ''
  nextTick(() => {
    normalizeContent()
    el?.focus()
  })

  // L4 任务清单：使用当前 Agent 会话 ID
  const sessionId = agentStore.currentSessionId || `agent_${Date.now()}`
  taskPanelRef.value?.startPlan(sessionId, goal)
  taskPanelVisible.value = true

  if (!orchestrator.value) {
    ElMessage.warning(t('agent.engineNotReady'))
    return
  }

  // assistant 占位：用 reactive() 包装，确保 stages 变化直接触发 Vue 重渲染
  const assistantMsg = reactive<AgentMessageType>({
    id: `a_${Date.now()}`,
    role: 'assistant',
    stages: [],
    finalAnswer: undefined,
    running: true,
    createdAt: Date.now(),
  })
  agentStore.addMessage(assistantMsg)
  agentStore.setRunning(true)

  try {
    const { answer } = await orchestrator.value.run(
      { goal, appIDs: mentionedIDs, apps: selectedApps, modelID: agentStore.modelID },
      (e) => applyStepEvent(assistantMsg, e),
    )
    // 网络/失败时返回空字符串，不展示空最终结果区域
    if (answer) assistantMsg.finalAnswer = answer
    assistantMsg.running = false
  } catch (err: any) {
    assistantMsg.running = false
    assistantMsg.stages.push({
      id: `s_err_${Date.now()}`,
      kind: 'failed',
      title: t('agent.runFailed', '编排失败'),
      status: 'failed',
      collapsed: false,
      content: '',
      error: err?.message || String(err),
    })
    // L4：同步终态到任务清单，避免意外异常时步骤卡在执行中
    taskPanelRef.value?.handleStepEvent({ type: 'failed', payload: err?.message || String(err) })
    ElMessage.error(err?.message || t('agent.runFailed', '编排失败'))
  } finally {
    agentStore.setRunning(false)
    agentStore.setActiveSessionId(null)
    nextTick(scrollToBottom)
  }
}

/** StepEvent → 过程化阶段渲染（L2 §4.5.5 映射） */
function applyStepEvent(msg: AgentMessageType, e: StepEvent) {
  switch (e.type) {
    case 'planning':
      // 规划阶段：首次创建，后续 delta 复用同一阶段（编排大脑可能多次 emit）
      ensureStage(msg, 'planning', t('agent.stagePlanning', '正在规划…'), e.delta)
      break
    case 'stepStart': {
      const name = e.appName || e.appID || t('agent.unknownApp', '未知 App')
      pushStage(msg, 'callApp', t('agent.stageCallApp', { app: name }), undefined, e.appID, e.appName)
      break
    }
    case 'stepDelta':
      appendDelta(msg, e.delta)
      break
    case 'stepDone':
      finalizeStage(msg, 'completed', e.payload)
      break
    case 'reflecting':
      ensureStage(msg, 'reflecting', t('agent.stageReflecting', '反思…'), e.delta)
      break
    case 'completed':
      // 先把仍在 running 的最后一个过程阶段（通常是反思/规划）收别为完成，再展示最终结果
      finalizeStage(msg, 'completed')
      msg.finalAnswer = e.payload
      msg.running = false
      break
    case 'failed':
      finalizeStage(msg, 'failed', e.payload)
      msg.running = false
      break
    case 'aborted':
      finalizeStage(msg, 'aborted', e.payload)
      msg.running = false
      break
  }
  if (autoScroll.value) nextTick(scrollToBottom)
  // L4：同步事件到任务清单镜像（activePlan 与显示计划分离，后台运行不影响当前视图）
  taskPanelRef.value?.handleStepEvent(e)
}

/**
 * 确保存在一个同 kind 的 running 阶段；存在则追加 delta，否则新建。
 * 用于 planning/reflecting：编排大脑多次 emit 同类事件时不重复创建阶段。
 */
function ensureStage(
  msg: AgentMessageType,
  kind: AgentMessageType['stages'][number]['kind'],
  title: string,
  delta?: string,
) {
  const existing = [...msg.stages].reverse().find((s) => s.kind === kind && s.status === 'running')
  if (existing) {
    if (delta) existing.content += delta
  } else {
    pushStage(msg, kind, title, delta)
  }
}

function pushStage(
  msg: AgentMessageType,
  kind: AgentMessageType['stages'][number]['kind'],
  title: string,
  content?: string,
  appID?: string,
  appName?: string,
) {
  // 创建新阶段前，先把之前仍在 running 的过程阶段收别为 completed
  // （planning/reflecting 在 decide 返回后没有显式 stepDone，靠这里收别）
  for (const s of msg.stages) {
    if (s.status === 'running' && s.kind !== 'final') {
      s.status = 'completed'
      s.endTime = Date.now()
    }
  }
  msg.stages.push({
    id: `s_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
    kind,
    title,
    status: 'running',
    collapsed: true,
    content: content || '',
    appID,
    appName,
    startTime: Date.now(),
  })
}

/** 向最后一个 running 阶段追加流式 delta */
function appendDelta(msg: AgentMessageType, delta?: string) {
  if (!delta) return
  const last = [...msg.stages].reverse().find((s) => s.status === 'running')
  if (last) last.content += delta
}

/** 收别当前 running 阶段 */
function finalizeStage(msg: AgentMessageType, status: 'completed' | 'failed' | 'aborted', result?: string) {
  const last = [...msg.stages].reverse().find((s) => s.status === 'running')
  if (last) {
    last.status = status
    last.endTime = Date.now()
    if (result) last.content = last.content ? `${last.content}\n${result}` : result
  }
}

function handleStop() {
  orchestrator.value?.abort()
}

// --- 滚动 ---
function onScroll() {
  const el = scrollRef.value
  if (!el) return
  const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 60
  autoScroll.value = nearBottom
}
function scrollToBottom() {
  const el = scrollRef.value
  if (el) el.scrollTop = el.scrollHeight
}
</script>

<style lang="scss" scoped>
.agent-view {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--bg-primary, #f5f7fa);
  overflow: hidden;
}
.agent-topbar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 20px;
  border-bottom: 1px solid var(--border-color, #ebeef5);
  background: var(--bg-card, #fff);
  flex-shrink: 0;
}
.topbar-hint {
  font-size: 12px;
  color: var(--text-secondary, #909399);
}
/* 消息区 + 任务侧栏并排（flex，min-height:0 允许收缩滚动） */
.agent-body {
  flex: 1 1 0;
  display: flex;
  min-height: 0;
  overflow: hidden;
}
.agent-messages {
  flex: 1 1 0;
  min-width: 0;
  overflow-y: auto;
  padding: 16px 20px 180px;
  box-sizing: border-box;
}
.task-sidebar {
  flex: 0 0 280px;
  width: 280px;
  border-left: 1px solid var(--border-color, #ebeef5);
  background: var(--bg-card, #fff);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.empty-state {
  text-align: center;
  padding: 80px 20px;
  color: var(--text-secondary, #909399);
  max-width: 600px;
  margin: 0 auto;
  .empty-icon {
    font-size: 48px;
    color: var(--el-color-primary, #409eff);
    opacity: 0.6;
  }
  .empty-title {
    margin-top: 12px;
    font-size: 18px;
    font-weight: 600;
    color: var(--text-primary, #303133);
  }
  .empty-desc {
    margin-top: 8px;
    font-size: 13px;
    line-height: 1.6;
  }
}
.agent-input-area {
  position: absolute;
  bottom: 20px;
  left: 20px;
  right: 20px;
  z-index: 10;
  max-width: 880px;
  margin: 0 auto;
}

.input-row {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  background: var(--bg-card, #fff);
  border: 1px solid var(--border-color, #ebeef5);
  border-radius: 10px;
  padding: 8px 10px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
}

/* contenteditable 输入框 */
.agent-input {
  flex: 1 1 auto;
  max-height: 160px;
  overflow-y: auto;
  padding: 6px 4px;
  font-size: 14px;
  line-height: 1.65;
  color: var(--text-primary, #303133);
  outline: none;
  word-break: break-word;
  white-space: pre-wrap;

  /* placeholder */
  &:empty::before {
    content: attr(data-placeholder);
    color: var(--text-placeholder, #c0c4cc);
    pointer-events: none;
  }
}

/* 内嵌 @App 标签（contenteditable=false，整体删除/不可编辑） */
:deep(.mention-tag) {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 1px 5px 1px 8px;
  margin: 0 2px;
  border-radius: 10px;
  background: var(--el-color-primary-light-9, #ecf5ff);
  color: var(--el-color-primary, #409eff);
  border: 1px solid var(--el-color-primary-light-7, #d9ecff);
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
  cursor: default;
  user-select: none;
  vertical-align: baseline;
}

:deep(.tag-close) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 15px;
  height: 15px;
  margin-left: 2px;
  cursor: pointer;
  opacity: 0.55;
  border-radius: 50%;
  transition: opacity 0.15s, background 0.15s;
  &:hover {
    opacity: 1;
    background: rgba(0,0,0,.08);
  }
}
</style>
