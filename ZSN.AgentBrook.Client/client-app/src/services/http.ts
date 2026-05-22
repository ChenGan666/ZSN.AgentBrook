import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { secureStorage } from '@/utils/storage'
import { createApiRequest, APP_SECRET } from '@/utils/crypto'
import { getAccessToken, refreshMemberToken } from './auth'
import { getBaseUrl } from './base'
import router from '@/router'

const http: AxiosInstance = axios.create({
  timeout: 60000,
  headers: {
    'Content-Type': 'application/json',
  },
})

const TOKEN_CHECK_ERROR = 80001
const MEMBER_TOKEN_CHECK_ERROR = 80002
const DATA_FORMAT_ERROR = 60001

function isDecryptError(data: unknown): boolean {
  if (!data || !Array.isArray(data)) return false
  return data.some((x: any) => {
    if (!x || x.Role !== 'system' || !x.Content) return false
    try {
      const parsed = typeof x.Content === 'string' ? JSON.parse(x.Content) : x.Content
      return parsed && parsed.status === -1 && parsed.msg === 'Decrypt Error!'
    } catch {
      return String(x.Content).includes('Decrypt Error!')
    }
  })
}

function isTokenError(errorCode: number): boolean {
  return errorCode === TOKEN_CHECK_ERROR || errorCode === MEMBER_TOKEN_CHECK_ERROR
}

function selectKeys(accessToken: string | null, memberToken: string | null) {
  if (memberToken) {
    return {
      encryptKey: memberToken.substring(0, 16),
      signKey: memberToken,
    }
  }
  if (accessToken) {
    return { encryptKey: accessToken, signKey: accessToken }
  }
  return { encryptKey: APP_SECRET, signKey: APP_SECRET }
}

http.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  config.baseURL = getBaseUrl()

  const accessToken = await secureStorage.get('access_token')
  const memberToken = await secureStorage.get('member_token')

  if (accessToken) {
    config.headers.set('bearer', accessToken)
  }
  if (memberToken) {
    config.headers.set('memberbearer', memberToken)
  }

  if (config.data && config.method !== 'get') {
    ;(config as any)._originalData = config.data
    const { encryptKey, signKey } = selectKeys(accessToken, memberToken)
    const apiRequest = createApiRequest(config.data, encryptKey, signKey)
    config.data = apiRequest
  }

  return config
})

http.interceptors.response.use(
  (response) => {
    const data = response.data

    if (data && !data.Success) {
      if (isTokenError(data.ErrorCode)) {
        return handleTokenExpired()
      }

      if (data.ErrorCode === DATA_FORMAT_ERROR && isDecryptError(data.Data)) {
        if (!(response.config as any)._decryptRetry) {
          return handleDecryptError(response.config)
        }
      }
    }

    return response
  },
  async (error: AxiosError) => {
    if (error.response?.status === 401) {
      return handleTokenExpired()
    }
    return Promise.reject(error)
  },
)

async function handleTokenExpired() {
  await secureStorage.remove('member_token')
  await secureStorage.remove('access_token')
  const currentPath = router.currentRoute.value.path
  if (currentPath !== '/login') {
    router.push('/login')
  }
  return Promise.reject(new Error('Token expired'))
}

async function handleDecryptError(config: any) {
  try {
    await getAccessToken()
    const result = await refreshMemberToken()
    if (!result?.Success) {
      return handleTokenExpired()
    }

    config.data = config._originalData
    delete config._originalData
    config._decryptRetry = true

    return http.request(config)
  } catch (retryError) {
    return handleTokenExpired()
  }
}

export default http
