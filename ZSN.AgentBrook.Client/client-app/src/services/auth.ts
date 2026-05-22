import axios from 'axios'
import { createApiRequest, APP_ID, APP_SECRET, doubleMd5 } from '@/utils/crypto'
import type { ApiResponse, TokenResponse } from '@/types/api'
import type { DeviceInfo } from '@/types/user'
import { secureStorage } from '@/utils/storage'
import { getBaseUrl } from './base'

function getDeviceTime() {
  return new Date().toISOString()
}

export async function getDeviceInfo(): Promise<DeviceInfo> {
  let systemId = ''
  let computerName = ''
  let osDescription = ''
  let osArchitecture = ''
  let is64BitOS = true
  let currentUserName = ''

  try {
    const platform = navigator.platform || ''
    const userAgent = navigator.userAgent || ''

    computerName = platform
    osDescription = userAgent
    osArchitecture = platform.includes('Win') ? 'x64' : platform.includes('Mac') ? 'arm64' : 'x64'
    currentUserName = 'TauriUser'

    const { hostname } = window.location
    systemId = hostname || 'unknown'
  } catch {
    systemId = 'unknown'
  }

  let primaryIpAddress = ''
  let primaryMacAddress = ''
  let hostName = computerName

  try {
    const pc = await import('@tauri-apps/plugin-process')
    hostName = computerName
  } catch {
    // web fallback
  }

  return {
    SystemId: systemId,
    ComputerName: computerName,
    OSDescription: osDescription,
    OSArchitecture: osArchitecture,
    Is64BitOS: is64BitOS,
    DotNetVersion: 'N/A (Tauri)',
    SystemBootTime: '',
    CurrentUserName: currentUserName,
    SystemDirectory: '',
    CpuInfo: navigator.hardwareConcurrency ? `${navigator.hardwareConcurrency} cores` : 'Unknown',
    MemoryInfo: (navigator as any).deviceMemory ? `${(navigator as any).deviceMemory} GB` : 'Unknown',
    HostName: hostName,
    PrimaryIpAddress: primaryIpAddress,
    PrimaryMacAddress: primaryMacAddress,
    IsNetworkAvailable: navigator.onLine,
    NetworkInterfaces: [],
    UpTimeMilliseconds: 0,
    FormattedUpTime: '',
    CollectionTime: getDeviceTime(),
  }
}

export async function getAccessToken(): Promise<ApiResponse<TokenResponse>> {
  const baseUrl = getBaseUrl()
  const businessData = { AppID: APP_ID }
  const apiRequest = createApiRequest(businessData, APP_SECRET, APP_SECRET)

  const response = await axios.post<ApiResponse<TokenResponse>>(
    `${baseUrl}/Token/Get`,
    apiRequest,
    { headers: { 'Content-Type': 'application/json' } },
  )

  if (response.data.Success && response.data.Data?.AccessToken) {
    await secureStorage.set('access_token', response.data.Data.AccessToken)
    await secureStorage.set('access_token_expirein', String(response.data.Data.Expirein))
  }

  return response.data
}

export async function getMemberToken(
  phone: string,
  password: string,
): Promise<ApiResponse<TokenResponse>> {
  const baseUrl = getBaseUrl()
  const accessToken = await secureStorage.get('access_token')

  const device = await getDeviceInfo()
  const businessData = {
    PhoneNumber: phone,
    PWD: doubleMd5(password),
    Device: device,
  }

  const encryptKey = accessToken || APP_SECRET
  const secret = accessToken || APP_SECRET
  const apiRequest = createApiRequest(businessData, encryptKey, secret)

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }
  if (accessToken) {
    headers['bearer'] = accessToken
  }

  const response = await axios.post<ApiResponse<TokenResponse>>(
    `${baseUrl}/Token/GetMemberToken`,
    apiRequest,
    { headers },
  )

  if (response.data.Success && response.data.Data?.MemberToken) {
    await secureStorage.set('member_token', response.data.Data.MemberToken)
    await secureStorage.set('member_token_expirein', String(response.data.Data.Expirein))
    await secureStorage.set('member_refresh_token', response.data.Data.MemberRefreshToken || '')
  }

  return response.data
}

export async function refreshMemberToken(): Promise<ApiResponse<TokenResponse>> {
  const baseUrl = getBaseUrl()
  const accessToken = await secureStorage.get('access_token')
  const refreshToken = await secureStorage.get('member_refresh_token')

  const businessData = { RefreshToken: refreshToken || '' }

  const encryptKey = accessToken || APP_SECRET
  const secret = accessToken || APP_SECRET
  const apiRequest = createApiRequest(businessData, encryptKey, secret)

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }
  if (accessToken) {
    headers['bearer'] = accessToken
  }

  const response = await axios.post<ApiResponse<TokenResponse>>(
    `${baseUrl}/Token/RefreshMemberToken`,
    apiRequest,
    { headers },
  )

  if (response.data.Success && response.data.Data?.MemberToken) {
    await secureStorage.set('member_token', response.data.Data.MemberToken)
    await secureStorage.set('member_token_expirein', String(response.data.Data.Expirein))
    await secureStorage.set('member_refresh_token', response.data.Data.MemberRefreshToken || '')
  }

  return response.data
}
