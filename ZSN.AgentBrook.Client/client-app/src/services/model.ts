/**
 * Agent 模式服务端"暴露层"封装。
 *
 *  - getList()        → N1：获取脱敏的"编排大脑"模型列表（POST /Model/GetList）。
 *  - getCompletionUrl() → N2：裸 LLM 代理 SSE 端点（POST /Model/Completion）。
 *
 * SSE 端点不走 axios（无法流式），由编排引擎用原生 fetch + createApiRequest 调用
 * （与 services/chat.ts 的 getChatCompletionsUrl 同模式）。
 */
import http from './http'
import { getBaseUrl } from './base'
import type { ApiResponse } from '@/types/api'
import type { AgentModelDTO } from '@/types/agent'

/** N1：获取可用的"编排大脑"模型列表（脱敏）。 */
export function getModelList() {
  return http.post<ApiResponse<AgentModelDTO[]>>('/Model/GetList', {})
}

/** N2：裸 LLM 代理 SSE 端点 URL（编排大脑调用）。 */
export function getModelCompletionUrl() {
  return `${getBaseUrl()}/Model/Completion`
}
