import { openDB, type IDBPDatabase } from 'idb'

interface AgentBrookDB {
  sessions: {
    key: string
    value: {
      id: string
      data: any
      updatedAt: number
    }
  }
  messages: {
    key: string
    value: {
      id: string
      sessionId: string
      data: any
      createdAt: number
    }
    indexes: { 'by-session': string }
  }
  offlineQueue: {
    key: number
    value: {
      id?: number
      type: string
      payload: any
      createdAt: number
    }
  }
}

let dbInstance: IDBPDatabase<AgentBrookDB> | null = null

export async function getDB(): Promise<IDBPDatabase<AgentBrookDB>> {
  if (dbInstance) return dbInstance

  dbInstance = await openDB<AgentBrookDB>('agentbrook-client', 1, {
    upgrade(db) {
      if (!db.objectStoreNames.contains('sessions')) {
        db.createObjectStore('sessions', { keyPath: 'id' })
      }
      if (!db.objectStoreNames.contains('messages')) {
        const msgStore = db.createObjectStore('messages', { keyPath: 'id' })
        msgStore.createIndex('by-session', 'sessionId')
      }
      if (!db.objectStoreNames.contains('offlineQueue')) {
        db.createObjectStore('offlineQueue', { keyPath: 'id', autoIncrement: true })
      }
    },
  })

  return dbInstance
}
