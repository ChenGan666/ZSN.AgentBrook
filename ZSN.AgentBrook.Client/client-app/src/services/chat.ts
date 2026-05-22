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
