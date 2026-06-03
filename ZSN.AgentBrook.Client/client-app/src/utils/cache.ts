import { getDB } from './db'
import type { SessionInfo, ChatMessage } from '@/types/chat'

const SESSION_CACHE_TTL = 5 * 60 * 1000

export const sessionCache = {
  async set(sessions: SessionInfo[]) {
    const db = await getDB()
    const tx = db.transaction('sessions', 'readwrite')
    for (const session of sessions) {
      await tx.store.put({
        id: session.ChatSessionID,
        data: JSON.parse(JSON.stringify(session)),
        updatedAt: Date.now(),
      })
    }
    await tx.done
  },

  async getAll(): Promise<SessionInfo[]> {
    const db = await getDB()
    const entries = await db.getAll('sessions')
    return entries
      .filter((e) => Date.now() - e.updatedAt < SESSION_CACHE_TTL)
      .map((e) => e.data)
      .sort((a, b) => new Date(b.CreateTime).getTime() - new Date(a.CreateTime).getTime())
  },

  async clear() {
    const db = await getDB()
    await db.clear('sessions')
  },
}

export const messageCache = {
  async set(sessionId: string, messages: ChatMessage[]) {
    const db = await getDB()
    const tx = db.transaction('messages', 'readwrite')

    // Clear ALL existing messages for this session before writing.
    // Without this, old cached entries with stale IDs (e.g. ai_xxx from
    // a previous SSE stream) accumulate indefinitely and cause duplicate
    // messages on every selectSession call.
    const index = tx.store.index('by-session')
    let cursor = await index.openCursor(sessionId)
    while (cursor) {
      await cursor.delete()
      cursor = await cursor.continue()
    }

    for (const msg of messages) {
      await tx.store.put({
        id: msg.id,
        sessionId,
        data: JSON.parse(JSON.stringify(msg)),
        createdAt: Date.now(),
      })
    }
    await tx.done
  },

  async getBySession(sessionId: string): Promise<ChatMessage[]> {
    const db = await getDB()
    const entries = await db.getAllFromIndex('messages', 'by-session', sessionId)
    return entries.map((e) => e.data)
  },

  async clear() {
    const db = await getDB()
    await db.clear('messages')
  },

  async clearSession(sessionId: string) {
    const db = await getDB()
    const tx = db.transaction('messages', 'readwrite')
    const index = tx.store.index('by-session')
    let cursor = await index.openCursor(sessionId)
    while (cursor) {
      await cursor.delete()
      cursor = await cursor.continue()
    }
    await tx.done
  },
}

export async function getCacheSize(): Promise<number> {
  if ('storage' in navigator && 'estimate' in navigator.storage) {
    const estimate = await navigator.storage.estimate()
    return estimate.usage ?? 0
  }
  return 0
}

export async function clearAllCache() {
  const db = await getDB()
  await db.clear('sessions')
  await db.clear('messages')
  await db.clear('offlineQueue')
}
