/**
 * Agent 编排引擎核心（L3，项目核心层）。
 *
 * 客户端"编排大脑"：以 Plan-Act-Reflect 循环驱动——
 *   [规划] 调 N2（/Model/Completion, responseFormat=json）→ Decision {callApp|finish}
 *   [执行] Decision.callApp → 调 App（/Chat/completions SSE）读至终态 → result
 *   [反思] 回喂已完成步骤与结果，调 N2 → 继续/换 App/补步骤/收尾
 *   [循环] until finish || 触达上限 || 用户中断
 *   [交付] emit(completed, answer)
 *
 * 复用：N2 与 App 执行共用 utils/sseRequest（与 Chat SSE 同模式），与 Chat 零侵入。
 * 防失控：maxSteps / maxCallsPerApp / maxReflections 上限，可随时 abort。
 */
import { ref } from 'vue'
import type { Ref } from 'vue'
import { sseRequest } from '@/utils/sseRequest'
import { getModelCompletionUrl } from '@/services/model'
import { getAppExecUrl } from '@/services/agent'
import type {
  RunInput,
  StepEvent,
  OrchestratorLimits,
  Decision,
  AgentChatMessage,
} from '@/types/agent'
import type { SSEMessage } from '@/types/chat'

export type { RunInput, StepEvent, OrchestratorLimits }

/** 默认防失控上限（可由调用方覆盖）。 */
export const DEFAULT_LIMITS: OrchestratorLimits = {
  maxSteps: 20,
  maxCallsPerApp: 5,
  maxReflections: 20,
}

/** 单步结果在回喂上下文时的最大字符数（防 prompt 膨胀）。 */
const RESULT_SUMMARY_LIMIT = 2000

export function useAgentOrchestrator() {
  const running = ref(false)
  let abortFlag = false
  let abortController: AbortController | null = null

  function abort() {
    abortFlag = true
    abortController?.abort()
  }

  async function run(input: RunInput, onEvent: (e: StepEvent) => void): Promise<{ answer: string }> {
    running.value = true
    abortFlag = false
    abortController = new AbortController()

    const limits = { ...DEFAULT_LIMITS, ...(input.limits || {}) }
    const callCountByApp = new Map<string, number>()

    // 编排上下文：记录已完成步骤，供规划/反思回喂
    const completedSteps: { appID: string; appName: string; input: string; result: string }[] = []

    const emit = onEvent

    try {
      let stepIndex = 0
      let reflections = 0

      // ── 主循环 ──────────────────────────────────────────────
      while (!abortFlag) {
        if (stepIndex >= limits.maxSteps) {
          emit({ type: 'failed', payload: `已达最大步数上限（${limits.maxSteps}），强制收尾` })
          break
        }

        // ① 规划 / 反思决策（首次为"规划"，后续为"反思"决定下一步）
        const isPlanning = stepIndex === 0
        emit({ type: isPlanning ? 'planning' : 'reflecting' })

        let decision: Decision
        try {
          decision = await decide(input, completedSteps, isPlanning, emit)
        } catch (err: any) {
          if (abortFlag) {
            emit({ type: 'aborted', payload: '用户已中断' })
            return { answer: composeAnswer(completedSteps) }
          }
          emit({ type: 'failed', payload: `编排大脑调用失败：${err?.message || err}` })
          return { answer: '' }
        }

        if (abortFlag) {
          emit({ type: 'aborted', payload: '用户已中断' })
          return { answer: composeAnswer(completedSteps) }
        }

        reflections++
        if (reflections >= limits.maxReflections) {
          emit({ type: 'failed', payload: `已达最大反思次数上限（${limits.maxReflections}），强制收尾` })
          break
        }

        // ② 终止出口
        if (decision.action === 'finish') {
          emit({ type: 'completed', payload: decision.answer })
          return { answer: decision.answer }
        }

        // ③ 执行 App
        const { appID, input: appInput, reason } = decision
        // LLM 可能记错 appID，用精确匹配 + 模糊匹配兜底
        let app = input.apps.find((a) => a.appID === appID)
        if (!app) {
          app = fuzzyMatchApp(appID, input.apps)
        }
        if (!app) {
          // 模型选了不存在/未授权的 App，记为失败步并继续反思
          completedSteps.push({ appID, appName: appID, input: appInput, result: `错误：App ${appID} 不在可用清单内` })
          stepIndex++
          continue
        }

        const count = (callCountByApp.get(appID) || 0) + 1
        if (count > limits.maxCallsPerApp) {
          completedSteps.push({ appID, appName: app.name, input: appInput, result: `错误：已达单 App 调用上限（${limits.maxCallsPerApp}）` })
          stepIndex++
          continue
        }
        callCountByApp.set(appID, count)

        emit({ type: 'stepStart', stepIndex, appID: app.appID, appName: app.name, payload: reason || appInput })

        let result: string
        try {
          result = await executeApp(app.appID, app.name, appInput, emit, abortController!)
        } catch (err: any) {
          if (abortFlag) {
            emit({ type: 'aborted', payload: '用户已中断' })
            return { answer: composeAnswer(completedSteps) }
          }
          const errMsg = err?.message || String(err)
          // 网络类错误：连接不可恢复，应直接终止编排而不是继续尝试
          if (isNetworkError(errMsg)) {
            emit({ type: 'failed', payload: `执行失败：${errMsg}` })
            return { answer: '' }
          }
          result = `执行失败：${errMsg}`
          emit({ type: 'stepDone', stepIndex, appID: app.appID, appName: app.name, payload: result })
          completedSteps.push({ appID: app.appID, appName: app.name, input: appInput, result })
          stepIndex++
          continue
        }

        emit({ type: 'stepDone', stepIndex, appID: app.appID, appName: app.name, payload: summarize(result) })
        completedSteps.push({ appID: app.appID, appName: app.name, input: appInput, result })
        stepIndex++
      }

      // 循环因上限退出：尝试综合已有结果
      const answer = composeAnswer(completedSteps)
      emit({ type: 'completed', payload: answer })
      return { answer }
    } finally {
      running.value = false
    }
  }

  // ── 规划/反思：调 N2，要求 JSON 决策 ──────────────────────────
  async function decide(
    input: RunInput,
    completedSteps: { appID: string; appName: string; input: string; result: string }[],
    isPlanning: boolean,
    emit: (e: StepEvent) => void,
  ): Promise<Decision> {
    const sysPrompt = buildOrchestratorSystemPrompt(input.apps)
    const userPrompt = buildOrchestratorUserPrompt(input.goal, input.apps, completedSteps, isPlanning)

    const messages: AgentChatMessage[] = [
      { role: 'system', content: sysPrompt },
      { role: 'user', content: userPrompt },
    ]

    // 流式累积完整 JSON，期间 emit delta 供 UI 实时展示
    let full = ''
    await sseRequest(getModelCompletionUrl(), {
      body: {
        status: 0,
        modelID: input.modelID,
        messages,
        stream: true,
        temperature: 30,
        responseFormat: 'json',
      },
      signal: abortController!.signal,
      onMessage: (msg: { delta?: string; done?: boolean }) => {
        if (msg.delta) {
          full += msg.delta
          emit({ type: isPlanning ? 'planning' : 'reflecting', delta: msg.delta })
        }
      },
      onTokenExpired: () => {
        emit({ type: 'failed', payload: '登录已过期，请重新登录' })
      },
    })

    return parseDecision(full)
  }

  // ── 执行 App：调 Chat/completions，读 SSE 至终态 ──────────────
  async function executeApp(
    appID: string,
    appName: string,
    appInput: string,
    emit: (e: StepEvent) => void,
    ac: AbortController,
  ): Promise<string> {
    const businessData = {
      status: 0,
      stream: true,
      messages: {
        role: 'User',
        content: appInput,
        Attachments: [],
        AdditionalOptions: {},
      },
      sessionID: '',
      appid: appID,
      SSE_TimeOut: 30,
    }

    let result = ''
    let streamDelta = ''
    let resolved = false

    await sseRequest(getAppExecUrl(), {
      body: businessData,
      signal: ac.signal,
      onMessage: (msg: SSEMessage) => {
        // 错误帧
        if (msg && msg.Error) {
          if (!resolved) {
            result = `错误 (${msg.ErrorCode}): ${msg.ErrorDesc || '未知错误'}`
          }
          return
        }

        const proc = msg.ProcessInfo
        if (proc) {
          const status = String(proc.Status || '').toLowerCase()
          // 提取最终文本：Results 中 type==='string' 的 value（与 useChat 一致）
          if (Array.isArray(proc.Results) && proc.Results.length > 0) {
            const strItem = proc.Results.find((r) => r && r.type === 'string')
            if (strItem) {
              const v = normalizeResultValue(strItem.value)
              if (v) result = v
            }
          }
          // 流式增量（StreamEnvelope）展示执行过程
          const envelope = (proc as any).StreamEnvelope
          if (Array.isArray(envelope)) {
            for (const item of envelope) {
              if (item && item.type === 'delta' && item.content) {
                streamDelta += item.content
                emit({ type: 'stepDelta', appID, appName, delta: item.content })
              }
            }
          }
          // 终态
          if (status === 'success' || status === 'failed' || status === 'error') {
            resolved = true
          }
        }
      },
      onTokenExpired: () => {
        emit({ type: 'failed', payload: '登录已过期，请重新登录' })
      },
    })

    // 优先用 Results 终态文本；无则用累积的流式 delta
    if (!result && streamDelta) result = streamDelta
    if (!result) result = '(该 App 未返回文本产出)'
    return result
  }

  return { run, abort, running: running as Ref<boolean> }
}

// ── 提示词构造 ────────────────────────────────────────────────
function buildOrchestratorSystemPrompt(apps: { appID: string; name: string; icon?: string }[]): string {
  return [
    '你是一个任务编排器（Orchestrator）。用户会给出一个目标，并指定若干可用的"App"作为工具。',
    '你的职责是：根据目标与已完成的步骤结果，决定下一步动作——调用某个 App，或宣布任务完成。',
    '',
    '【输出要求】必须严格输出一个 JSON 对象，不要输出任何额外文本、Markdown 代码块或解释。',
    '- 调用 App：{"action":"callApp","appID":"<AppID>","input":"<传给该 App 的输入文本>","reason":"<一句话理由>"}',
    '- 任务完成：{"action":"finish","answer":"<最终给用户的综合答案>"}',
    '',
    '【决策原则】',
    '1. 仅可调用下方"可用 App 清单"中列出的 appID。',
    '2. 上一个 App 的产出可作为下一个 App 的输入，逐步逼近目标。',
    '3. 当目标已达成、或继续调用无意义时，输出 finish 并在 answer 中综合所有步骤的结果。',
    '4. input 应是给该 App 的清晰指令/数据，而非你自己的回答。',
    '',
    '【可用 App 清单】',
    ...apps.map((a) => `- appID: "${a.appID}"  名称: 《${a.name}》`),
  ].join('\n')
}

function buildOrchestratorUserPrompt(
  goal: string,
  apps: { appID: string; name: string; icon?: string }[],
  completedSteps: { appID: string; appName: string; input: string; result: string }[],
  isPlanning: boolean,
): string {
  const lines: string[] = []
  lines.push(`【目标】${goal}`)
  lines.push('')
  lines.push('【可用 App 清单】')
  apps.forEach((a) => lines.push(`- 《${a.name}》(appID="${a.appID}")`))
  lines.push('')

  if (completedSteps.length === 0) {
    lines.push('【已完成步骤】无（这是首次规划）。')
    lines.push('请决定第一步：调用哪个 App、传什么 input，或直接 finish。')
  } else {
    lines.push('【已完成步骤】')
    completedSteps.forEach((s, i) => {
      lines.push(`步骤 ${i + 1}：调用《${s.appName}》`)
      lines.push(`  输入：${summarize(s.input)}`)
      lines.push(`  结果：${summarize(s.result)}`)
    })
    lines.push('')
    lines.push(isPlanning ? '请规划下一步。' : '请反思已完成步骤，决定下一步：继续调用 App、换 App、补充步骤，或 finish。')
  }
  lines.push('')
  lines.push('请输出 JSON 决策。')
  return lines.join('\n')
}

// ── 辅助 ──────────────────────────────────────────────────────
/**
 * 解析 N2 流式累积的 JSON 文本为 Decision。
 * 容错策略（LLM 输出的 JSON 常有瑕疵）：
 *  1. 剥离 ```json 围栏
 *  2. 提取首个 {...}
 *  3. JSON.parse 成功 → 直接用
 *  4. 失败 → 用正则提取 action/appID/input/answer 字段（字段级容错）
 */
function parseDecision(raw: string): Decision {
  let text = raw.trim()
  // 剥离 Markdown 代码围栏
  const fence = text.match(/```(?:json)?\s*([\s\S]*?)```/i)
  if (fence) text = fence[1].trim()
  text = text.replace(/^```\w*\s*/, '').replace(/\s*```$/, '').trim()
  // 提取首个 {...}
  const start = text.indexOf('{')
  const end = text.lastIndexOf('}')
  if (start !== -1 && end !== -1 && end > start) {
    text = text.slice(start, end + 1)
  }

  // 尝试标准解析（先清洗常见的 LLM 输出瑕疵）
  try {
    // 清洗：去掉 key 引号内的多余空格（如 " action" → "action"）
    const cleaned = text.replace(/"\s*([a-zA-Z_]+)\s*"\s*:/g, '"$1":')
    const obj = JSON.parse(cleaned)
    return normalizeDecision(obj, raw)
  } catch {
    /* 落入字段级容错 */
  }

  // 字段级容错：用正则提取各字段（处理 LLM 输出的多余 }、引号内空格、引号不配对等）
  const actionMatch = text.match(/"?\s*action"?\s*:\s*"?(callApp|finish)"?/i)
  const action = actionMatch?.[1] as 'callApp' | 'finish' | undefined
  if (action === 'callApp') {
    const appID = extractJsonString(text, 'appID') || ''
    const input = extractJsonString(text, 'input') || ''
    const reason = extractJsonString(text, 'reason') || undefined
    if (appID) return { action: 'callApp', appID, input, reason }
  } else if (action === 'finish') {
    const answer = extractJsonString(text, 'answer') || raw.trim()
    return { action: 'finish', answer }
  }

  // 最后兜底：把原始输出当 finish 的答案
  return { action: 'finish', answer: raw.trim() || '（编排大脑未给出有效决策）' }
}

/** 规范化已解析的 JSON 对象为 Decision。 */
function normalizeDecision(obj: any, raw: string): Decision {
  if (obj && obj.action === 'finish' && typeof obj.answer === 'string') {
    return { action: 'finish', answer: obj.answer }
  }
  if (obj && obj.action === 'callApp' && typeof obj.appID === 'string') {
    return {
      action: 'callApp',
      appID: obj.appID,
      input: typeof obj.input === 'string' ? obj.input : '',
      reason: typeof obj.reason === 'string' ? obj.reason : undefined,
    }
  }
  return { action: 'finish', answer: raw.trim() || '（编排大脑未给出有效决策）' }
}

/**
 * 从可能损坏的 JSON 文本中提取字符串字段值。
 * 处理：多余的 }、未闭合的引号、字段值末尾混入的标点。
 */
function extractJsonString(text: string, field: string): string | undefined {
  // 匹配 "field": "value"，容忍引号内的空格（如 " field"）
  const re = new RegExp(`"?\\s*${field}"?\\s*:\\s*"`, 'i')
  const m = re.exec(text)
  if (!m) return undefined
  const start = m.index + m[0].length
  // 从 start 开始找配对的结束引号（支持转义）
  let i = start
  let result = ''
  while (i < text.length) {
    const ch = text[i]
    if (ch === '\\' && i + 1 < text.length) {
      result += text[i + 1]
      i += 2
      continue
    }
    if (ch === '"') break
    result += ch
    i++
  }
  // 清理末尾可能混入的多余 } 等标点
  return result.replace(/}\s*$/, '').trim() || undefined
}

/** 规范化 App 结果值（Results[].value 可能是字符串/对象/数组）。 */
function normalizeResultValue(value: any): string {
  if (value == null) return ''
  if (typeof value === 'string') return value
  try {
    return JSON.stringify(value)
  } catch {
    return String(value)
  }
}

/**
 * 模糊匹配 App：LLM 可能记错 appID（字符顺序/拼写错误）。
 * 用相似度（相同字符占比）匹配，阈值 0.7。
 */
function fuzzyMatchApp(
  appID: string,
  apps: { appID: string; name: string; icon?: string }[],
): { appID: string; name: string; icon?: string } | undefined {
  let best: { appID: string; name: string; icon?: string } | undefined
  let bestScore = 0
  const target = appID.toLowerCase().replace(/[^a-z0-9]/g, '')
  for (const a of apps) {
    const candidate = a.appID.toLowerCase().replace(/[^a-z0-9]/g, '')
    // 相似度：相同字符数 / 较长者长度
    const longer = target.length >= candidate.length ? target : candidate
    const shorter = target.length >= candidate.length ? candidate : target
    let common = 0
    for (const ch of shorter) {
      const idx = longer.indexOf(ch)
      if (idx !== -1) common++
    }
    const score = common / longer.length
    if (score > bestScore) {
      bestScore = score
      best = a
    }
  }
  return bestScore >= 0.7 ? best : undefined
}

/** 截断长文本（防 prompt 膨胀 + 存储友好）。 */
function summarize(text: string): string {
  if (!text) return ''
  return text.length > RESULT_SUMMARY_LIMIT
    ? `${text.slice(0, RESULT_SUMMARY_LIMIT)}…（已截断，共 ${text.length} 字）`
    : text
}

/** 识别 fetch 网络类错误（不可恢复）。 */
function isNetworkError(message: string): boolean {
  return /network|failed to fetch|net::err|网络|无法连接|connection refused|load failed/i.test(message)
}

/** 超限/中断时的兜底综合答案。 */
function composeAnswer(steps: { appID: string; appName: string; input: string; result: string }[]): string {
  if (steps.length === 0) return '编排未能产生有效结果。'
  const parts = steps.map(
    (s, i) => `步骤 ${i + 1}：调用《${s.appName}》\n结果：${summarize(s.result)}`,
  )
  return `编排已完成 ${steps.length} 个步骤，汇总如下：\n\n${parts.join('\n\n')}`
}
