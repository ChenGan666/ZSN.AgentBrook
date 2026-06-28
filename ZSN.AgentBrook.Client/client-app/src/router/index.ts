import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { secureStorage } from '@/utils/storage'

function dateTimeToTimestamp(date: Date): number {
  return Math.floor(date.getTime() / 1000)
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false },
    },
    {
      path: '/',
      component: () => import('@/components/layout/AppLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          redirect: '/chat',
        },
        {
          path: 'chat',
          name: 'Chat',
          component: () => import('@/views/ChatView.vue'),
        },
        {
          path: 'agent',
          name: 'Agent',
          component: () => import('@/views/AgentView.vue'),
        },
        {
          path: 'settings',
          name: 'Settings',
          component: () => import('@/views/SettingsView.vue'),
        },
      ],
    },
    {
      path: '/mini-chat',
      name: 'MiniChat',
      component: () => import('@/views/MiniChatView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/meeting-transcribe',
      name: 'MeetingTranscribe',
      component: () => import('@/views/MeetingTranscribeView.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

let userInfoFetched = false

router.beforeEach(async (to, _from, next) => {
  const userStore = useUserStore()

  if (!userStore.isLoggedIn) {
    const memberToken = await secureStorage.get('member_token')
    const expireinStr = await secureStorage.get('member_token_expirein')
    const expirein = expireinStr ? parseInt(expireinStr, 10) : 0

    if (memberToken && expirein > dateTimeToTimestamp(new Date())) {
      userStore.setLoggedIn(true)

      if (!userStore.userInfo && !userInfoFetched) {
        userInfoFetched = true
        try {
          const { getMemberInfo } = await import('@/services/member')
          const { data } = await getMemberInfo()
          if (data.Success && data.Data) {
            const s = data.Data
            userStore.setUser({
              id: s.ID,
              name: s.MNickName,
              phone: s.MPhoneNumber,
              avatar: s.MAvatar,
              email: s.MEmail,
            })
          }
        } catch {
          await secureStorage.remove('member_token')
          await secureStorage.remove('access_token')
          userStore.reset()
          userInfoFetched = false
          next({ path: '/login' })
          return
        }
      }
    }
  }

  if (to.meta.requiresAuth !== false && !userStore.isLoggedIn) {
    next({ path: '/login', query: { redirect: to.fullPath } })
  } else if (to.path === '/login' && userStore.isLoggedIn) {
    next('/chat')
  } else {
    next()
  }
})

export default router
