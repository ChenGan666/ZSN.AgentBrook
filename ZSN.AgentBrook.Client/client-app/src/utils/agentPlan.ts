/**
 * Agent 任务清单持久化与序列化（L4）。
 *
 * 职责：
 *  1) 订阅 L3 StepEvent，把编排过程/结果实时镜像到 IndexedDB（agentPlans store）。
 *  2) 序列化为可读清单文件（Markdown 默认 / JSON 可选），复用 platform.file.save 导出。
 *  3) 历史清单查询（按 sessionId）。
 *
 * 与 L3 解耦：本模块只消费 StepEvent，不感知编排实现；与 Chat 零侵入。
 */
import { getDB, type AgentPlanMirror, type AppStepMirror } from './db'
import type { StepEvent } from '@/types/agent'
import { toRaw } from 'vue'

const RESULT_STORE_LIMIT = 2000 // 存储时 result 截断长度

// ── CRUD ──────────────────────────────────────────────────────
export async function savePlan(plan: AgentPlanMirror): Promise<void> {
  const db = await getDB()
  plan.updatedAt = Date.now()
  // 剥离 Vue 响应式 Proxy（IndexedDB structured clone 不能序列化 Proxy）
  await db.put('agentPlans', JSON.parse(JSON.stringify(toRaw(plan))))
}

export async function getPlan(planId: string): Promise<AgentPlanMirror | undefined> {
  const db = await getDB()
  return db.get('agentPlans', planId)
}

export async function getPlansBySession(sessionId: string): Promise<AgentPlanMirror[]> {
  const db = await getDB()
  return db.getAllFromIndex('agentPlans', 'by-session', sessionId)
}

export async function getAllPlans(): Promise<AgentPlanMirror[]> {
  const db = await getDB()
  return db.getAll('agentPlans')
}

export async function deletePlan(planId: string): Promise<void> {
  const db = await getDB()
  await db.delete('agentPlans', planId)
}

// ── 事件 → 镜像映射（L4 §4.2） ─────────────────────────────────
/**
 * 将一次编排会话维护成一个 plan 镜像。用法：
 *   const m = createPlanMirror(sessionId, goal)
 *   applyStepEvent(m, event)  // 每次 StepEvent 调用
 *   savePlan(m)               // 持久化
 */
export function createPlanMirror(sessionId: string, goal: string): AgentPlanMirror {
  return {
    planId: `plan_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
    sessionId,
    goal,
    status: 'planning',
    apps: [],
    updatedAt: Date.now(),
  }
}

export function applyStepEvent(plan: AgentPlanMirror, e: StepEvent): void {
  switch (e.type) {
    case 'planning':
      plan.status = 'planning'
      break
    case 'stepStart': {
      plan.status = 'executing'
      plan.apps.push({
        stepIndex: e.stepIndex ?? plan.apps.length,
        appID: e.appID || '',
        appName: e.appName || '',
        status: 'executing',
        input: e.payload,
        startTime: Date.now(),
      })
      break
    }
    case 'stepDelta': {
      // 增量更新当前步的 result（可选，用于实时预览）
      const cur = currentStep(plan)
      if (cur && e.delta) {
        cur.result = (cur.result || '') + e.delta
      }
      break
    }
    case 'stepDone': {
      const cur = currentStep(plan)
      if (cur) {
        cur.status = 'completed'
        cur.endTime = Date.now()
        if (e.payload) cur.result = truncate(e.payload)
      }
      break
    }
    case 'reflecting':
      plan.status = 'executing'
      break
    case 'completed':
      plan.status = 'completed'
      // 先把所有仍在 executing 的步骤统一收别为 completed，避免已结束的任务仍显示执行中
      plan.apps.forEach((s) => {
        if (s.status === 'executing') {
          s.status = 'completed'
          s.endTime = Date.now()
        }
      })
      // 持久化最终 answer 到最后一步的备注（若有）
      if (e.payload) {
        const last = plan.apps[plan.apps.length - 1]
        if (last && last.status === 'completed') {
          last.result = truncate(e.payload)
        }
      }
      break
    case 'failed':
      plan.status = 'failed'
      {
        const cur = currentStep(plan)
        if (cur) {
          cur.status = 'failed'
          cur.endTime = Date.now()
          if (e.payload) cur.result = truncate(e.payload)
        }
      }
      break
    case 'aborted':
      plan.status = 'paused'
      {
        const cur = currentStep(plan)
        if (cur) {
          cur.status = 'skipped'
          cur.endTime = Date.now()
          if (e.payload) cur.result = truncate(e.payload)
        }
      }
      break
  }
}

function currentStep(plan: AgentPlanMirror): AppStepMirror | undefined {
  for (let i = plan.apps.length - 1; i >= 0; i--) {
    if (plan.apps[i].status === 'executing') return plan.apps[i]
  }
  return plan.apps[plan.apps.length - 1]
}

function truncate(text: string): string {
  if (!text) return ''
  return text.length > RESULT_STORE_LIMIT
    ? `${text.slice(0, RESULT_STORE_LIMIT)}…（已截断）`
    : text
}

// ── 序列化（Markdown 默认 / JSON 可选） ─────────────────────────
export function planToMarkdown(plan: AgentPlanMirror): string {
  const done = plan.apps.filter((s) => s.status === 'completed').length
  const total = plan.apps.length
  const lines: string[] = []
  lines.push('# 任务清单')
  lines.push(`- **目标**：${plan.goal}`)
  lines.push(`- **状态**：${plan.status}（${done}/${total} 步完成）`)
  lines.push(`- **更新时间**：${formatTime(plan.updatedAt)}`)
  lines.push('')
  lines.push('## 步骤')
  if (total === 0) {
    lines.push('- （暂无步骤）')
  } else {
    plan.apps.forEach((s, i) => {
      const check = s.status === 'completed' ? '[x]' : '[ ]'
      const icon =
        s.status === 'completed' ? '✅' : s.status === 'executing' ? '⏳' : s.status === 'failed' ? '❌' : '⏸'
      lines.push(`- ${check} ${i + 1}. 调用《${s.appName}》 ${icon}`)
      if (s.input) lines.push(`  > 输入：${short(s.input)}`)
      if (s.result) lines.push(`  > 结果：${short(s.result)}`)
    })
  }
  return lines.join('\n')
}

export function planToJson(plan: AgentPlanMirror): string {
  return JSON.stringify(plan, null, 2)
}

function formatTime(ts: number): string {
  try {
    return new Date(ts).toLocaleString()
  } catch {
    return String(ts)
  }
}

function short(text: string): string {
  if (!text) return ''
  const one = text.replace(/\s+/g, ' ').trim()
  return one.length > 120 ? `${one.slice(0, 120)}…` : one
}
