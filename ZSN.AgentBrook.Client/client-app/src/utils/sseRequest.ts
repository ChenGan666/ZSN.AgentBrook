/**
 * 通用 SSE 请求工具（供 Agent 编排引擎复用）。
 *
 * 抽取自 useChat.ts 的 SSE 请求模式：
 *   1) 从 secureStorage 取 memberToken/accessToken，派生 encryptKey/signKey
 *   2) createApiRequest 加密 businessData（与 http.ts 一致的密钥选择规则）
 *   3) 原生 fetch POST SSE 端点（axios 无法流式）
 *   4) 解析 `data: <json>` 行，逐条回调 onMessage
 *
 * 这样 Agent 的 N2（/Model/Completion）与 App 执行（/Chat/completions）共用同一引擎，
 * 与 Chat 零侵入：本文件不依赖 useChat / chatStore。
 */
import { secureStorage } from '@/utils/storage'
import { createApiRequest, APP_SECRET } from '@/utils/crypto'

/** Token 错误码（与 http.ts 一致） */
export const TOKEN_CHECK_ERROR = 80001
export const MEMBER_TOKEN_CHECK_ERROR = 80002
export const DATA_FORMAT_ERROR = 60001

export interface SseRequestOptions {
  /** 业务数据（会被 AES 加密成 ApiRequest） */
  body: unknown
  /** 中断信号 */
  signal: AbortSignal
  /** 每条解析出的 SSE 消息回调 */
  onMessage: (msg: any) => void
  /** token 失效回调（编排引擎据此中止） */
  onTokenExpired?: () => void
}

/** 派生加密密钥（与 http.ts selectKeys 一致）。 */
function deriveKeys(memberToken: string | null, accessToken: string | null) {
  if (memberToken) {
    return { encryptKey: memberToken.substring(0, 16), signKey: memberToken }
  }
  if (accessToken) {
    return { encryptKey: accessToken, signKey: accessToken }
  }
  return { encryptKey: APP_SECRET, signKey: APP_SECRET }
}

/**
 * 发起一次 SSE 请求并流式解析。
 * @returns 流自然结束（reader done）；抛错由调用方处理（AbortError 等）
 */
export async function sseRequest(url: string, opts: SseRequestOptions): Promise<void> {
  const memberToken = await secureStorage.get('member_token')
  const accessToken = await secureStorage.get('access_token')

  const { encryptKey, signKey } = deriveKeys(memberToken, accessToken)
  const apiRequest = createApiRequest(opts.body, encryptKey, signKey)

  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (accessToken) headers['bearer'] = accessToken
  if (memberToken) headers['memberbearer'] = memberToken

  const response = await fetch(url, {
    method: 'POST',
    headers,
    body: JSON.stringify(apiRequest),
    signal: opts.signal,
  })

  // 401 / JSON 错误体（非 SSE）
  if (response.status === 401) {
    opts.onTokenExpired?.()
    throw new Error('Token expired (401)')
  }
  if (!response.ok) {
    const text = await response.text().catch(() => '')
    let code = 0
    try {
      const json = JSON.parse(text)
      code = json.ErrorCode ?? 0
    } catch { /* not JSON */ }
    if (code === TOKEN_CHECK_ERROR || code === MEMBER_TOKEN_CHECK_ERROR) {
      opts.onTokenExpired?.()
    }
    throw new Error(`HTTP ${response.status}: ${text.slice(0, 200)}`)
  }

  // 可能是 JSON 错误而非 SSE 流
  const contentType = response.headers.get('content-type') || ''
  if (contentType.includes('application/json')) {
    const json = await response.json()
    if (json.ErrorCode === TOKEN_CHECK_ERROR || json.ErrorCode === MEMBER_TOKEN_CHECK_ERROR) {
      opts.onTokenExpired?.()
    }
    throw new Error(json.ErrorDesc || `JSON error (code=${json.ErrorCode ?? 'unknown'})`)
  }

  const reader = response.body!.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })
    const lines = buffer.split('\n')
    buffer = lines.pop() || ''
    for (const line of lines) {
      const trimmed = line.trimStart()
      if (trimmed.startsWith('data:')) {
        const data = trimmed.slice(5).trim()
        if (data === '[DONE]') continue
        try {
          opts.onMessage(JSON.parse(data))
        } catch {
          /* 忽略解析失败的行 */
        }
      }
    }
  }
}
