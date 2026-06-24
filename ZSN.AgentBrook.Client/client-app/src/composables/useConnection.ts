import { onMounted, onUnmounted, watch } from 'vue'
import { useAppStore } from '@/stores/app'
import { useChatStore } from '@/stores/chat'
import { useSettingsStore } from '@/stores/settings'
import http from '@/services/http'
import { useNotifications } from '@/composables/useNotifications'

const HEARTBEAT_INTERVAL_IDLE = 30000
/** Base active-mode interval. Decreases as more sessions run concurrently so
 * the sidebar status stays responsive under multi-session load. */
const HEARTBEAT_INTERVAL_ACTIVE_BASE = 5000
/** Floor for the active interval — prevents hammering the server no matter how
 * many sessions are running. */
const HEARTBEAT_INTERVAL_ACTIVE_MIN = 2000
/** How much each running session shaves off the active interval (ms). */
const HEARTBEAT_INTERVAL_STEP = 600

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
  const { notifySessionCompleted } = useNotifications()
  let heartbeatTimer: ReturnType<typeof setInterval> | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null
  let reconnectDelay = 1000
  /** The interval the running heartbeat timer was armed with. Used to skip the
   * clearInterval+setInterval pair when the interval hasn't actually changed —
   * previously every checkConnection() tore down and rebuilt the timer
   * unconditionally, which raced with runningSessionIds watcher during slow
   * requests and left brief windows with no timer at all. */
  let currentEffectiveInterval = 0

  function getHeartbeatInterval(): number {
    const count = chatStore.runningSessionIds.length
    if (count === 0) return HEARTBEAT_INTERVAL_IDLE
    return Math.max(
      HEARTBEAT_INTERVAL_ACTIVE_MIN,
      HEARTBEAT_INTERVAL_ACTIVE_BASE - count * HEARTBEAT_INTERVAL_STEP,
    )
  }

  function restartHeartbeatTimer() {
    const interval = getHeartbeatInterval()
    // Only tear down + rebuild when the interval actually changed. This closes
    // the no-timer race window and avoids needless churn.
    if (interval === currentEffectiveInterval && heartbeatTimer) return
    currentEffectiveInterval = interval
    if (heartbeatTimer) clearInterval(heartbeatTimer)
    heartbeatTimer = setInterval(checkConnection, interval)
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

        // 对已完成的会话发送系统通知（走 coordinator：跨心跳/SSE 去重 + 聚合）
        if (!document.hasFocus() && settingsStore.notificationEnabled) {
          for (const item of completedList) {
            const isFailed = item.SessionStatus === -1
            const summary = item.Summary
              ? stripMarkdown(item.Summary).slice(0, 120)
              : ''
            notifySessionCompleted(item.ChatSessionID, isFailed, summary)
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

      // 自适应频率切换 — 仅在间隔真正变化时才重建定时器（restartHeartbeatTimer
      // 内部带去重），避免每次 checkConnection 都 clear+set 造成空窗。
      restartHeartbeatTimer()
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
    currentEffectiveInterval = getHeartbeatInterval()
    checkConnection()
    heartbeatTimer = setInterval(checkConnection, currentEffectiveInterval)
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
