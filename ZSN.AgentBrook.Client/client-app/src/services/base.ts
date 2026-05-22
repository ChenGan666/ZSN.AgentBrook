export function getBaseUrl(): string {
  try {
    const stored = localStorage.getItem('agentbrook-settings')
    if (stored) {
      const settings = JSON.parse(stored)
      if (settings.apiBaseUrl) return settings.apiBaseUrl
    }
  } catch {}
  return import.meta.env.VITE_API_BASE_URL || ''
}
