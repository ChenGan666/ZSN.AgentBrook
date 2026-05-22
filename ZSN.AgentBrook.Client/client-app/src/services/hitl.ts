import http from './http'
import type { ApiResponse } from '@/types/api'

export function execHumanInTheLoop(params: {
  sessionID: string
  taskID: string
  recordID: string
  data: any
}) {
  const { sessionID, taskID, recordID, data } = params
  const query: string[] = []
  if (sessionID) query.push(`sessionID=${encodeURIComponent(sessionID)}`)
  if (taskID) query.push(`taskID=${encodeURIComponent(taskID)}`)
  if (recordID) query.push(`recordID=${encodeURIComponent(recordID)}`)
  const endpoint = `/Chat/execHumanInTheLoop${query.length ? '?' + query.join('&') : ''}`
  return http.post<ApiResponse>(endpoint, data || {})
}

export function execHumanInTheLoopByForm(params: {
  sessionID: string
  taskID: string
  recordID: string
  data: any
}) {
  const { sessionID, taskID, recordID, data } = params
  const query: string[] = []
  if (sessionID) query.push(`sessionID=${encodeURIComponent(sessionID)}`)
  if (taskID) query.push(`taskID=${encodeURIComponent(taskID)}`)
  if (recordID) query.push(`recordID=${encodeURIComponent(recordID)}`)
  const endpoint = `/Chat/execHumanInTheLoopByForm${query.length ? '?' + query.join('&') : ''}`
  return http.post<ApiResponse>(endpoint, data || {})
}
