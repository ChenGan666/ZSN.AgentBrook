import { defineStore } from 'pinia'
import type { MemberInfo } from '@/types/user'

interface UserState {
  isLoggedIn: boolean
  userInfo: MemberInfo | null
}

export const useUserStore = defineStore('user', {
  state: (): UserState => ({
    isLoggedIn: false,
    userInfo: null,
  }),
  actions: {
    setLoggedIn(val: boolean) {
      this.isLoggedIn = val
    },
    setUser(info: MemberInfo) {
      this.userInfo = info
    },
    reset() {
      this.isLoggedIn = false
      this.userInfo = null
    },
  },
})
