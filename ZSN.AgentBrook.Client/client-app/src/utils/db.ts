import { openDB, type IDBPDatabase } from 'idb'

/**
 * Agent 任务清单步骤镜像（L4）。编排每步状态实时落库，驱动任务面板 UI。
 */
export interface AppStepMirror {
  stepIndex: number
  appID: string
  appName: string
  status: 'pending' | 'executing' | 'completed' | 'failed' | 'skipped'
  input?: string
  result?: string
  startTime?: number
  endTime?: number
}

export interface AgentPlanMirror {
  planId: string
  sessionId: string
  goal: string
  status: 'planning' | 'executing' | 'completed' | 'failed' | 'paused'
  apps: AppStepMirror[]
  savedFilePath?: string
  updatedAt: number
}

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
  agentPlans: {
    key: string
    value: AgentPlanMirror
    indexes: { 'by-session': string }
  }
}

let dbInstance: IDBPDatabase<AgentBrookDB> | null = null

export async function getDB(): Promise<IDBPDatabase<AgentBrookDB>> {
  if (dbInstance) return dbInstance

  // 版本 1→2：新增 agentPlans store（IndexedDB schema 升级仅在版本号变更时触发）。
  dbInstance = await openDB<AgentBrookDB>('agentbrook-client', 2, {
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
      if (!db.objectStoreNames.contains('agentPlans')) {
        const planStore = db.createObjectStore('agentPlans', { keyPath: 'planId' })
        planStore.createIndex('by-session', 'sessionId')
      }
    },
  })

  return dbInstance
}
