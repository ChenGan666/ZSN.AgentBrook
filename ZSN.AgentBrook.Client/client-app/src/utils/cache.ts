import { getDB } from './db'
import type { SessionInfo, ChatMessage } from '@/types/chat'

const SESSION_CACHE_TTL = 5 * 60 * 1000

export const sessionCache = {
  async set(sessions: SessionInfo[]) {
    const db = await getDB()
    const tx = db.transaction('sessions', 'readwrite')
    // Clear the store before writing so sessions that have fallen off the
    // list (deleted, or pushed past page 1) don't accumulate. Previously this
    // only `put`-ed the new list, so stale sessions lingered until TTL expiry.
    await tx.store.clear()
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

/**
 * Per-session write serialization for the message cache.
 *
 * Multiple callers (selectSession's outgoing-save, refreshCurrentSessionMessages,
 * the SSE finalize path, updateMessageInSession) can issue messageCache.set for
 * the same sessionId concurrently. Each set clears-then-writes inside a single
 * transaction (atomic at the IndexedDB level), but a caller's read-modify-write
 * (e.g. getBySession → mutate → set) is NOT atomic across those two ops. If two
 * such sequences overlap, the second's getBySession can read the first's not-
 * yet-committed state and the second set then clobbers the first.
 *
 * Chaining writes per sessionId into a promise chain serializes them so each
 * set sees the result of the previous one. Reads are not chained (a read may
 * observe an in-flight write's predecessor, which is acceptable — the next
 * chained write will reconcile).
 */
const messageWriteChains = new Map<string, Promise<void>>()

function chainMessageWrite(sessionId: string, task: () => Promise<void>): Promise<void> {
  const prev = messageWriteChains.get(sessionId) || Promise.resolve()
  const next = prev.then(task, task)
  messageWriteChains.set(sessionId, next)
  // Drop the chain entry once settled so we don't retain references forever.
  next.finally(() => {
    if (messageWriteChains.get(sessionId) === next) {
      messageWriteChains.delete(sessionId)
    }
  })
  return next
}

async function doMessageCacheSet(sessionId: string, messages: ChatMessage[]) {
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
}

export const messageCache = {
  set(sessionId: string, messages: ChatMessage[]): Promise<void> {
    // Serialize concurrent writes for the same session to prevent torn writes
    // when multiple read-modify-write sequences overlap.
    return chainMessageWrite(sessionId, () => doMessageCacheSet(sessionId, messages))
  },

  async getBySession(sessionId: string): Promise<ChatMessage[]> {
    const db = await getDB()
    const entries = await db.getAllFromIndex('messages', 'by-session', sessionId)
    return entries.map((e) => e.data)
  },

  async clear() {
    const db = await getDB()
    await db.clear('messages')
    messageWriteChains.clear()
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
    messageWriteChains.delete(sessionId)
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
  messageWriteChains.clear()
}
