import { createI18n } from 'vue-i18n'
import zhCN from './zh-CN'
import enUS from './en-US'

function getStoredLocale(): string {
  try {
    const stored = localStorage.getItem('agentbrook-settings')
    if (stored) {
      const settings = JSON.parse(stored)
      if (settings.locale) return settings.locale
    }
  } catch { /* ignore */ }
  return 'zh-CN'
}

const i18n = createI18n({
  legacy: false,
  locale: getStoredLocale(),
  fallbackLocale: 'zh-CN',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
  },
})

export default i18n
