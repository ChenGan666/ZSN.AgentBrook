import { useI18n } from 'vue-i18n'
import { platform } from '@/platform'

/**
 * Notification coordinator — the single funnel for session-completion
 * notifications across BOTH the heartbeat path (`useConnection`) and the SSE
 * stream-end path (`useChat`). Without this, multiple sessions finishing near
 * simultaneously each produced their own system notification, AND the two
 * sources could notify the SAME session twice (heartbeat + SSE).
 *
 * Strategy:
 *  - Dedup: a session is notified at most once per flush window.
 *  - Short-window aggregation: completions arriving within FLUSH_DELAY ms of
 *    each other are merged into ONE notification. A single completion keeps
 *    the original per-session title/body; two or more collapse into a
 *    "{count} sessions completed" summary.
 */

interface PendingItem {
  sessionId: string
  isFailed: boolean
  /** Raw (pre-i18n) summary text from the server/SSE. */
  summary: string
}

const FLUSH_DELAY = 300

const pending: PendingItem[] = []
/** Session ids already queued in the current window. Prevents the heartbeat
 * and SSE from double-notifying the same completion. */
const seen = new Set<string>()
let flushTimer: ReturnType<typeof setTimeout> | null = null

/**
 * Strip markdown to a plain-text preview suitable for the notification body.
 * Kept in sync with the stripMarkdown helpers in useChat/useConnection so the
 * aggregated body looks identical to a non-aggregated one.
 */
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

/**
 * Build the per-session title/body for a single completion. Mirrors the
 * original notification text so a lone completion looks exactly as before.
 */
function buildSingleText(t: (k: string, v?: any) => string, item: PendingItem): { title: string; body: string } {
  const isFailed = item.isFailed
  const title = isFailed ? t('chat.notifyFailed') : t('chat.notifyCompleted')
  const preview = item.summary
    ? stripMarkdown(item.summary).slice(0, 120)
    : isFailed
      ? t('chat.notifyViewDetail')
      : t('chat.notifyViewReply')
  return { title, body: preview }
}

/**
 * Flush the pending queue. Called by the timer after FLUSH_DELAY ms of quiet.
 * Renders i18n at flush time (not enqueue time) so a language switch during
 * the window still produces correctly-localized text.
 */
function flush(t: (k: string, v?: any) => string) {
  flushTimer = null
  if (pending.length === 0) return

  // Single completion — keep the original per-session behavior.
  if (pending.length === 1) {
    const item = pending[0]
    const { title, body } = buildSingleText(t, item)
    platform.notification.show(title, body, { sessionId: item.sessionId })
  } else {
    // Two or more — collapse into one summary notification.
    const count = pending.length
    const title = t('chat.notifyMultiTitle', { count })
    // Show the first two previews, then an "and N more" suffix for the rest.
    const previews = pending.slice(0, 2).map((item) => {
      const p = item.summary ? stripMarkdown(item.summary).slice(0, 60) : buildSingleText(t, item).body
      return `· ${p}`
    })
    const rest = count - previews.length
    const body = rest > 0
      ? `${previews.join('\n')}\n${t('chat.notifyMultiMore', { count: rest })}`
      : previews.join('\n')
    // Tauri's notification adapter keeps a single pending-sessionId slot, so
    // we point the click at the first completed session.
    platform.notification.show(title, body, { sessionId: pending[0].sessionId })
  }

  pending.length = 0
  seen.clear()
}

/**
 * Enqueue a session completion. Safe to call from both useChat (SSE end) and
 * useConnection (heartbeat); the same sessionId is deduped within the window.
 */
function enqueue(t: (k: string, v?: any) => string, item: PendingItem) {
  if (seen.has(item.sessionId)) return
  seen.add(item.sessionId)
  pending.push(item)

  if (flushTimer) clearTimeout(flushTimer)
  flushTimer = setTimeout(() => flush(t), FLUSH_DELAY)
}

/**
 * Composable wrapping the coordinator with i18n. Exposes two semantic methods
 * so callers don't need to know about titles/bodies.
 *
 * Usage:
 *   const { notifySessionCompleted, notifySessionFailed } = useNotifications()
 *   notifySessionCompleted(sessionId, summary)
 */
export function useNotifications() {
  const { t } = useI18n()

  function notifySessionCompleted(sessionId: string, isFailed: boolean, summary: string) {
    if (!sessionId) return
    enqueue(t, { sessionId, isFailed, summary })
  }

  return { notifySessionCompleted }
}
