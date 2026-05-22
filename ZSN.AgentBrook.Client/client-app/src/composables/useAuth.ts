import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { secureStorage } from '@/utils/storage'
import * as authApi from '@/services/auth'
import * as memberApi from '@/services/member'
import { useUserStore } from '@/stores/user'
import type { ServerMemberInfo, MemberInfo } from '@/types/user'

function dateTimeToTimestamp(date: Date): number {
  return Math.floor(date.getTime() / 1000)
}

function mapMemberInfo(s: ServerMemberInfo): MemberInfo {
  return {
    id: s.ID,
    name: s.MNickName,
    phone: s.MPhoneNumber,
    avatar: s.MAvatar,
    email: s.MEmail,
  }
}

export function useAuth() {
  const router = useRouter()
  const userStore = useUserStore()
  const loading = ref(false)
  const error = ref<string | null>(null)
  let refreshTimer: ReturnType<typeof setTimeout> | null = null

  function clearRefreshTimer() {
    if (refreshTimer) {
      clearTimeout(refreshTimer)
      refreshTimer = null
    }
  }

  async function ensureAccessToken(): Promise<boolean> {
    try {
      const token = await secureStorage.get('access_token')
      const expireinStr = await secureStorage.get('access_token_expirein')
      const expirein = expireinStr ? parseInt(expireinStr, 10) : 0

      if (token && expirein > dateTimeToTimestamp(new Date())) {
        return true
      }
    } catch { /* storage error, proceed to get new token */ }

    try {
      const result = await authApi.getAccessToken()
      return result.Success && !!result.Data?.AccessToken
    } catch {
      return false
    }
  }

  async function login(phone: string, password: string) {
    loading.value = true
    error.value = null
    try {
      const tokenOk = await ensureAccessToken()
      if (!tokenOk) {
        throw new Error('获取通信令牌失败，请检查网络')
      }

      const memberRes = await authApi.getMemberToken(phone, password)
      if (!memberRes.Success) {
        throw new Error(memberRes.ErrorDesc || '登录失败')
      }

      const memberToken = memberRes.Data?.MemberToken
      if (!memberToken) {
        throw new Error('登录返回数据异常')
      }

      try {
        const { data } = await memberApi.getMemberInfo()
        if (data.Success && data.Data) {
          userStore.setUser(mapMemberInfo(data.Data))
        }
      } catch { /* non-critical */ }

      scheduleRefresh()
      userStore.setLoggedIn(true)
      router.push('/chat')
    } catch (e: any) {
      error.value = e.message || '登录失败，请检查网络'
    } finally {
      loading.value = false
    }
  }

  async function doRefresh(retryCount = 0) {
    try {
      const result = await authApi.refreshMemberToken()
      if (result.Success) {
        scheduleRefresh()
      } else if (retryCount < 3) {
        setTimeout(() => doRefresh(retryCount + 1), 2000)
      } else {
        await logout()
        router.push('/login')
      }
    } catch {
      if (retryCount < 3) {
        setTimeout(() => doRefresh(retryCount + 1), 2000)
      } else {
        await logout()
        router.push('/login')
      }
    }
  }

  function scheduleRefresh() {
    clearRefreshTimer()
    secureStorage.get('member_token_expirein').then((expireinStr) => {
      const expirein = expireinStr ? parseInt(expireinStr, 10) : 0
      const refreshAt = (expirein - 5 * 60) * 1000 - Date.now()

      if (refreshAt <= 0) {
        doRefresh()
        return
      }
      refreshTimer = setTimeout(() => doRefresh(), refreshAt)
    }).catch(() => { /* storage error */ })
  }

  async function logout() {
    clearRefreshTimer()
    await secureStorage.remove('access_token')
    await secureStorage.remove('access_token_expirein')
    await secureStorage.remove('member_token')
    await secureStorage.remove('member_token_expirein')
    await secureStorage.remove('member_refresh_token')
    userStore.reset()
    router.push('/login')
  }

  async function checkAuth() {
    let memberToken: string | null = null
    let expireinStr: string | null = null
    try {
      memberToken = await secureStorage.get('member_token')
      expireinStr = await secureStorage.get('member_token_expirein')
    } catch { return }

    const expirein = expireinStr ? parseInt(expireinStr, 10) : 0

    if (memberToken && expirein > dateTimeToTimestamp(new Date())) {
      userStore.setLoggedIn(true)
      scheduleRefresh()
      try {
        const { data } = await memberApi.getMemberInfo()
        if (data.Success && data.Data) {
          userStore.setUser(mapMemberInfo(data.Data))
        }
      } catch { /* ignore */ }
    } else if (memberToken) {
      await doRefresh()
    }
  }

  onMounted(() => checkAuth())
  onUnmounted(() => clearRefreshTimer())

  return { login, logout, loading, error, checkAuth }
}
