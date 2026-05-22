import http from './http'
import type { ApiResponse } from '@/types/api'
import type { ServerMemberInfo } from '@/types/user'

export function getMemberInfo() {
  return http.post<ApiResponse<ServerMemberInfo>>('/Member/Get', {})
}

export function saveMemberInfo(data: Partial<ServerMemberInfo>) {
  return http.post<ApiResponse<ServerMemberInfo>>('/Member/Save', data)
}
