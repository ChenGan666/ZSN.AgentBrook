import { onMounted, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '@/stores/app'
import { useChatStore } from '@/stores/chat'
import { useSettingsStore } from '@/stores/settings'
import http from '@/services/http'
import { platform } from '@/platform'

const HEARTBEAT_INTERVAL_IDLE = 30000
const HEARTBEAT_INTERVAL_ACTIVE = 5000

function stripMarkdown(text: string): string {
  return text
    .replace(/```[\s\S]*?```/g, '[代码]')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/\*\*([^*]+)\*\*/g, '$1')
    .replace(/\*([^*]+)\*/g, '$1')
    .replace(/#+\s/g, '')
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
    .replace(/!\[([^\]]*)\]\([^)]+\)/g, '[图片]')
    .replace(/[#*`\[\]()]/g, '')
    .trim()
}

export function useConnection() {
  const appStore = useAppStore()
  const chatStore = useChatStore()
  const settingsStore = useSettingsStore()
  const { t } = useI18n()
  let heartbeatTimer: ReturnType<typeof setInterval> | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null
  let reconnectDelay = 1000

  function getHeartbeatInterval(): number {
    return chatStore.runningSessionIds.length > 0
      ? HEARTBEAT_INTERVAL_ACTIVE
      : HEARTBEAT_INTERVAL_IDLE
  }

  function restartHeartbeatTimer() {
    if (heartbeatTimer) clearInterval(heartbeatTimer)
    heartbeatTimer = setInterval(checkConnection, getHeartbeatInterval())
  }

  async function checkConnection() {
    const start = Date.now()
    try {
      const runningIds = chatStore.runningSessionIds.length > 0
        ? chatStore.runningSessionIds.join(',')
        : ''

      const { data } = await http.post('/Base/Get', { runningSessionIds: runningIds }, { timeout: 5000 })
      const latency = Date.now() - start
      reconnectDelay = 1000

      if (latency > 2000) {
        appStore.setConnection('slow', latency)
      } else {
        appStore.setConnection('connected', latency)
      }

      // 处理会话状态响应
      if (data?.Success && data?.Data?.SessionStatusList?.length > 0) {
        const completedList = chatStore.updateSessionStatusFromHeartbeat(data.Data.SessionStatusList)

        // 对已完成的会话发送系统通知
        if (!document.hasFocus() && settingsStore.notificationEnabled) {
          for (const item of completedList) {
            const isFailed = item.SessionStatus === -1
            const title = isFailed ? t('chat.notifyFailed') : t('chat.notifyCompleted')
            const topic = item.TopicSummary || ''
            const summary = item.Summary
              ? stripMarkdown(item.Summary).slice(0, 120)
              : (isFailed ? t('chat.notifyViewDetail') : t('chat.notifyViewReply'))
            platform.notification.show(title, `${topic}: ${summary}`, { sessionId: item.ChatSessionID })
          }
        }

        // 有会话完成，刷新会话列表
        if (completedList.length > 0) {
          chatStore.fetchSessions(1).catch(() => {})

          // 如果当前查看的会话完成了，刷新消息（替换虚拟 pending 消息为真实记录）
          const currentCompleted = completedList.find(
            (c) => c.ChatSessionID === chatStore.currentSessionId,
          )
          if (currentCompleted) {
            chatStore.refreshCurrentSessionMessages().catch(() => {})
          }
        }
      }

      // 自适应频率切换
      const currentInterval = getHeartbeatInterval()
      if (heartbeatTimer) {
        clearInterval(heartbeatTimer)
        heartbeatTimer = setInterval(checkConnection, currentInterval)
      }
    } catch {
      appStore.setConnection('disconnected')
      scheduleReconnect()
    }
  }

  function scheduleReconnect() {
    if (reconnectTimer) return
    reconnectTimer = setTimeout(async () => {
      reconnectTimer = null
      await checkConnection()
      reconnectDelay = Math.min(reconnectDelay * 2, 30000)
    }, reconnectDelay)
  }

  function startHeartbeat() {
    checkConnection()
    heartbeatTimer = setInterval(checkConnection, getHeartbeatInterval())
  }

  function stopHeartbeat() {
    if (heartbeatTimer) clearInterval(heartbeatTimer)
    if (reconnectTimer) clearTimeout(reconnectTimer)
  }

  // 有新会话加入运行列表时，重置心跳定时器。
  // 不立即触发 checkConnection()，避免异步响应在 SSE 结束后到达、
  // loading 已为 false 的情况下错误触发 refreshCurrentSessionMessages。
  watch(() => chatStore.runningSessionIds.length, (newLen, oldLen) => {
    if (newLen > oldLen) {
      restartHeartbeatTimer()
    }
  })

  onMounted(startHeartbeat)
  onUnmounted(stopHeartbeat)

  return { checkConnection, restartHeartbeatTimer }
}
