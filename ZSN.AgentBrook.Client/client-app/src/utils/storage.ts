import { platform } from '@/platform'
import { aesEncrypt, aesDecrypt } from './crypto'

const STORAGE_PASSWORD = 'zsn-ab-client-key'

export const secureStorage = {
  async get(key: string): Promise<string | null> {
    if (platform.system.platform === 'tauri') {
      return platform.storage.get(key)
    }
    const encrypted = localStorage.getItem(`secure_${key}`)
    if (!encrypted) return null
    try {
      return await aesDecrypt(encrypted, STORAGE_PASSWORD)
    } catch {
      localStorage.removeItem(`secure_${key}`)
      return null
    }
  },

  async set(key: string, value: string): Promise<void> {
    if (platform.system.platform === 'tauri') {
      return platform.storage.set(key, value)
    }
    const encrypted = await aesEncrypt(value, STORAGE_PASSWORD)
    localStorage.setItem(`secure_${key}`, encrypted)
  },

  async remove(key: string): Promise<void> {
    if (platform.system.platform === 'tauri') {
      return platform.storage.remove(key)
    }
    localStorage.removeItem(`secure_${key}`)
  },
}
