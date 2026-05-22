import { getDB } from '@/utils/db'
import { useAppStore } from '@/stores/app'
import http from '@/services/http'
import { watch } from 'vue'

export function useOfflineQueue() {
  const appStore = useAppStore()

  async function enqueue(content: string, sessionId: string, appId: string) {
    const db = await getDB()
    await db.add('offlineQueue', {
      type: 'chat_message',
      payload: { content, sessionId, appId },
      createdAt: Date.now(),
    })
  }

  async function processQueue() {
    const db = await getDB()
    const items = await db.getAll('offlineQueue')

    for (const item of items) {
      if (item.type === 'chat_message') {
        try {
          const { content, sessionId, appId } = item.payload
          await http.post('/Chat/SendMessage', { content, sessionId, appId })
          if (item.id != null) {
            await db.delete('offlineQueue', item.id)
          }
        } catch {
          break
        }
      }
    }
  }

  async function getQueueSize(): Promise<number> {
    const db = await getDB()
    return db.count('offlineQueue')
  }

  watch(
    () => appStore.connectionStatus,
    (status, oldStatus) => {
      if (oldStatus === 'disconnected' && status === 'connected') {
        processQueue()
      }
    },
  )

  return { enqueue, processQueue, getQueueSize }
}
