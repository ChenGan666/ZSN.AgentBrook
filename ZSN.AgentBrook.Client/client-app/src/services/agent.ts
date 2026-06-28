/**
 * Agent 模式编排所需的"工具"数据封装。
 *
 * Agent 把现有 Chat 的 App 当作"工具/动作"逐个调用，因此这里复用已有的
 * App/GetList（已在 session.ts 实现）与 Chat/completions SSE 端点（chat.ts）。
 * 本文件仅做语义聚合 + App 清单的二次封装，避免在编排引擎里直接耦合 session 模块。
 */
import { getAppList } from './session'
import { getChatCompletionsUrl } from './chat'
import type { AppInfo } from '@/types/chat'

/** 拉取已发布（SystemStatus=2）的可编排 App 清单（复用 App/GetList）。 */
export function getAgentAppList() {
  return getAppList()
}

/** App 执行（工具调用）SSE 端点 URL（复用 Chat/completions）。 */
export function getAppExecUrl() {
  return getChatCompletionsUrl()
}

export type { AppInfo }
