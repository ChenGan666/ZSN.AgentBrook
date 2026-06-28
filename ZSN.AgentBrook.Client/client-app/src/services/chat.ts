import http from './http'
import { getBaseUrl } from './base'
import type { ApiResponse } from '@/types/api'

export function getChatList(sessionId: string) {
  return http.post<ApiResponse<any[]>>('/Chat/GetList', { sessionID: sessionId })
}

export function getSummaryList(sessionId: string) {
  return http.post<ApiResponse<any>>('/Chat/GetSummaryList', { sessionID: sessionId })
}

export function getChatCompletionsUrl() {
  return `${getBaseUrl()}/Chat/completions`
}

/** Regenerate (重新生成) endpoint URL. Consumed as an SSE stream like completions. */
export function getChatRegenerateUrl() {
  return `${getBaseUrl()}/Chat/Regenerate`
}

export function getNodeExecutionRecordUrl() {
  return `${getBaseUrl()}/Chat/GetNodeExecutionRecord`
}

export interface RetryNodeParams {
  NodeID: string
  SessionID: string
  ProcessesID: string
  TaskID?: string
  AppID?: string
}

export function retryNode(params: RetryNodeParams) {
  return http.post<ApiResponse<any>>('/Chat/ReExecuteNode', params)
}

/** Fetch all workflow execution records for a session, grouped by ProcessesID */
export function getSessionExecutionRecords(sessionId: string) {
  return http.post<ApiResponse<any[]>>('/Chat/GetSessionExecutionRecords', { sessionID: sessionId })
}
