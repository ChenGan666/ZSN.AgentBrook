import http from './http'
import type { ApiResponse, PageData } from '@/types/api'
import type { SessionInfo, AppInfo } from '@/types/chat'

export function getSessionList(index = 1, size = 10, keyWord = '') {
  return http.post<ApiResponse<PageData<SessionInfo[]>>>('/Session/GetList', {
    Index: index,
    Size: size,
    KeyWord: keyWord,
  })
}

export function deleteSession(sessionId: string) {
  return http.post<ApiResponse<string>>('/Session/Delete', {
    sessionID: sessionId,
  })
}

export function cleanUpSessions() {
  return http.post<ApiResponse<string>>('/Session/CleanUp', {})
}

export function getAppList() {
  return http.post<ApiResponse<AppInfo[]>>('/App/GetList', {})
}
