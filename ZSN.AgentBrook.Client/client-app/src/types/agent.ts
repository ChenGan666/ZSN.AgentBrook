/**
 * Agent 模式类型定义。
 *
 * Agent 模式相对独立于 Chat：由客户端大模型作为"编排大脑"，把现有 Chat 的
 * App 当作"工具/动作"逐个调用并串接结果（Plan-Act-Reflect 循环）。
 * 本文件聚合 Agent 相关的 DTO、过程化消息、编排事件等类型，供 L2/L3/L4 复用。
 */

// ---------------------------------------------------------------------------
// N1 出参：编排大脑模型（脱敏，绝不包含 ModelKey/EndPoint/MConfig）
// ---------------------------------------------------------------------------
export interface AgentModelDTO {
  LargeModelID: number
  Name: string
  ModelName: string
  MICON: string
  TypeCode: number
  Thinking: boolean
  Description: string
}

// ---------------------------------------------------------------------------
// N2 入参：LLM 代理消息
// ---------------------------------------------------------------------------
export interface AgentChatMessage {
  role: 'system' | 'user' | 'assistant'
  content: string
}

// ---------------------------------------------------------------------------
// 过程化消息（一条 Agent 回复 = 若干阶段 + 一个最终结果）
// ---------------------------------------------------------------------------
export type StageKind =
  | 'planning'
  | 'callApp'
  | 'execWorkflow'
  | 'checkResult'
  | 'reflecting'
  | 'final'
  | 'aborted'
  | 'failed'

export type StageStatus = 'running' | 'completed' | 'failed' | 'aborted'

export interface AgentStage {
  id: string
  kind: StageKind
  /** 阶段标题（如“开始调用《XXX》”） */
  title: string
  status: StageStatus
  /** 默认折叠（final 阶段默认展开） */
  collapsed: boolean
  appID?: string
  appName?: string
  /** 流式追加的详情文本 */
  content: string
  startTime?: number
  endTime?: number
  /** 错误/中断原因（status=failed/aborted 时） */
  error?: string
}

export interface AgentMessage {
  id: string
  role: 'user' | 'assistant'
  /** 用户目标（仅 user 消息） */
  goal?: string
  /** 用户选中的 App（仅 user 消息） */
  selectedApps?: { appID: string; name: string; icon?: string }[]
  /** 过程阶段（仅 assistant，默认折叠） */
  stages: AgentStage[]
  /** 最终输出（默认展开） */
  finalAnswer?: string
  running: boolean
  createdAt: number
}

// ---------------------------------------------------------------------------
// 编排事件（L3 发出 → L2 渲染 + L4 持久化，数据源一致）
// ---------------------------------------------------------------------------
export type StepEventType =
  | 'planning'
  | 'stepStart'
  | 'stepDelta'
  | 'stepDone'
  | 'reflecting'
  | 'completed'
  | 'failed'
  | 'aborted'

export interface StepEvent {
  type: StepEventType
  stepIndex?: number
  appID?: string
  appName?: string
  /** 规划/反思的决策摘要、步骤 input/result、错误原因等 */
  payload?: string
  /** 流式 delta（stepDelta 专用） */
  delta?: string
}

// ---------------------------------------------------------------------------
// 编排运行入参 / 上限
// ---------------------------------------------------------------------------
export interface OrchestratorLimits {
  maxSteps: number
  maxCallsPerApp: number
  maxReflections: number
}

export interface RunInput {
  goal: string
  appIDs: string[]
  /** appID -> {name, icon}，用于渲染与提示词清单 */
  apps: { appID: string; name: string; icon?: string }[]
  modelID: number
  limits?: Partial<OrchestratorLimits>
}

// L3 决策协议（N2 用 responseFormat=json 输出）
export type Decision =
  | { action: 'callApp'; appID: string; input: string; reason?: string }
  | { action: 'finish'; answer: string }
