import { getBaseUrl } from './base'
import { secureStorage } from '@/utils/storage'

/**
 * 获取 FunASR WebSocket URL（通过 API 代理，携带 AccessToken + MemberToken 认证）
 */
export async function getFunAsrWsUrl(): Promise<string> {
  const baseUrl = getBaseUrl()
  const accessToken = await secureStorage.get('access_token')
  const memberToken = await secureStorage.get('member_token')

  // 将 HTTP URL 转换为 WebSocket URL
  const wsBaseUrl = baseUrl
    .replace(/^https?/, match => (match === 'https' ? 'wss' : 'ws'))

  const url = new URL(`${wsBaseUrl}/Chat/FunASR`)
  if (accessToken) {
    url.searchParams.set('bearer', accessToken)
  }
  if (memberToken) {
    url.searchParams.set('memberbearer', memberToken)
  }

  return url.toString()
}
