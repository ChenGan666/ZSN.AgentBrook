import { defineStore } from 'pinia'

export type ThemeMode = 'light' | 'dark' | 'system'
export type SendKey = 'enter' | 'ctrl-enter'

interface SettingsState {
  theme: ThemeMode
  fontSize: number
  sendKey: SendKey
  apiBaseUrl: string
}

export const useSettingsStore = defineStore('settings', {
  state: (): SettingsState => ({
    theme: 'system',
    fontSize: 14,
    sendKey: 'enter',
    apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5003/api',
  }),

  actions: {
    setTheme(theme: ThemeMode) {
      this.theme = theme
      applyTheme(theme)
    },
    setFontSize(size: number) {
      this.fontSize = Math.max(12, Math.min(22, size))
      document.documentElement.style.setProperty('--chat-font-size', `${this.fontSize}px`)
    },
  },

  persist: {
    key: 'agentbrook-settings',
    storage: localStorage,
  },
})

function applyTheme(theme: ThemeMode) {
  const root = document.documentElement
  if (theme === 'system') {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    root.setAttribute('data-theme', prefersDark ? 'dark' : 'light')
  } else {
    root.setAttribute('data-theme', theme)
  }
}
