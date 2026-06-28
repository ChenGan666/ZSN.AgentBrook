/**
 * 本地能力授权（L5）。
 *
 * 在客户端按需执行本地文件读写/命令，原则：
 *  - 默认最小权限、按需授权（预授权范围 / 单次授权 / 自动批准）。
 *  - 危险命令拦截（rm -rf / format / del /f 等）+ 拒绝提权（sudo/runas）。
 *  - 命令用参数数组执行（防注入），默认无网络、超时、输出截断。
 *  - 每次调用写本地审计日志。
 *
 * 流程：请求 → 命中预授权范围？(自动批准) : 弹 LocalAuthorizePanel(HITL) → 执行 → 审计。
 */
import { useAgentStore } from '@/stores/agent'
import type { DirectoryGrant, CommandGrant } from '@/stores/agent'
import { platform } from '@/platform'

/** 危险命令模式（拦截）。 */
const DANGEROUS_PATTERNS: RegExp[] = [
  /\brm\s+-rf\b/i,
  /\brmdir\s+\/[sq]\b/i,
  /\bformat\b/i,
  /\bdel\s+\/[fsq]/i,
  /\bshutdown\b/i,
  /\bmkfs\b/i,
  /\bdd\s+if=/i,
]

/** 提权命令（拒绝）。 */
const PRIVILEGE_PATTERNS: RegExp[] = [
  /\bsudo\b/i,
  /\brunas\b/i,
  /\bdoas\b/i,
  /\bgksudo\b/i,
]

/** 单次授权回调队列（HITL 弹窗用） */
type AuthorizeResolver = (granted: boolean) => void
let pendingAuthorize: { detail: string; resolve: AuthorizeResolver } | null = null

export function useLocalCapability() {
  const agentStore = useAgentStore()

  // ── 目录授权判定 ──
  function matchDirectory(path: string, scope: 'read' | 'readWrite'): DirectoryGrant | null {
    const norm = normalizePath(path)
    for (const g of agentStore.directoryGrants) {
      if (scope === 'readWrite' && g.scope === 'read') continue
      const gNorm = normalizePath(g.path)
      if (g.recursive ? path.startsWith(gNorm) : norm === gNorm) {
        return g
      }
    }
    return null
  }

  // ── 命令安全检查（CommandGuard） ──
  /** 返回错误信息则拦截，返回 null 则放行。 */
  function checkCommand(command: string, args: string[]): string | null {
    const full = [command, ...args].join(' ')
    for (const p of DANGEROUS_PATTERNS) {
      if (p.test(full)) return `危险命令已被拦截：${full}`
    }
    for (const p of PRIVILEGE_PATTERNS) {
      if (p.test(full)) return `提权命令已被拒绝：${full}`
    }
    // 黑名单参数
    for (const g of agentStore.commandGrants) {
      if (g.command === command && g.argBlacklist) {
        for (const bad of g.argBlacklist) {
          if (args.some((a) => a.includes(bad))) {
            return `参数被黑名单拦截：${bad}`
          }
        }
      }
    }
    return null
  }

  function matchCommand(command: string, args: string[]): CommandGrant | null {
    for (const g of agentStore.commandGrants) {
      if (g.command !== command) continue
      // 白名单参数校验（若配置）
      if (g.argWhitelist && g.argWhitelist.length > 0) {
        const allOk = args.every((a) => g.argWhitelist!.some((w) => a === w || a.startsWith(w)))
        if (!allOk) continue
      }
      return g
    }
    return null
  }

  // ── 审计 ──
  function audit(type: 'readFile' | 'writeFile' | 'listDir' | 'exec', detail: string, status: 'approved' | 'denied' | 'blocked' | 'error') {
    agentStore.addAudit({
      id: `audit_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
      type,
      detail,
      status,
      at: Date.now(),
    })
  }

  // ── 授权请求（命中预授权→自动批准；否则 HITL 弹窗） ──
  async function requestAuthorize(detail: string): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      pendingAuthorize = { detail, resolve }
      // 触发 LocalAuthorizePanel 显示（由组件订阅 pendingAuthorize）
      // 这里通过 window 事件通知，避免与组件强耦合
      window.dispatchEvent(new CustomEvent('agent-authorize-request', { detail }))
    })
  }

  /** 由 LocalAuthorizePanel 调用：用户做出授权决策。 */
  function resolveAuthorize(granted: boolean) {
    if (pendingAuthorize) {
      pendingAuthorize.resolve(granted)
      pendingAuthorize = null
    }
  }

  function getPendingDetail(): string | null {
    return pendingAuthorize?.detail ?? null
  }

  // ── 公共能力：读文件 ──
  async function readFile(path: string): Promise<string> {
    if (!platform.local.available) {
      audit('readFile', path, 'denied')
      throw new Error('当前环境不支持本地文件读取')
    }
    const grant = matchDirectory(path, 'read')
    if (!grant) {
      const ok = await requestAuthorize(`读取文件：${path}`)
      if (!ok) {
        audit('readFile', path, 'denied')
        throw new Error('用户拒绝读取文件')
      }
    }
    try {
      const { content } = await platform.local.readFile(path)
      audit('readFile', path, 'approved')
      return content
    } catch (e) {
      audit('readFile', path, 'error')
      throw e
    }
  }

  // ── 公共能力：写文件 ──
  async function writeFile(path: string, content: string): Promise<void> {
    if (!platform.local.available) {
      audit('writeFile', path, 'denied')
      throw new Error('当前环境不支持本地文件写入')
    }
    const grant = matchDirectory(path, 'readWrite')
    if (!grant) {
      const ok = await requestAuthorize(`写入文件：${path}`)
      if (!ok) {
        audit('writeFile', path, 'denied')
        throw new Error('用户拒绝写入文件')
      }
    }
    try {
      await platform.local.writeFile(path, content)
      audit('writeFile', path, 'approved')
    } catch (e) {
      audit('writeFile', path, 'error')
      throw e
    }
  }

  // ── 公共能力：列目录 ──
  async function listDir(path: string): Promise<string[]> {
    if (!platform.local.available) {
      audit('listDir', path, 'denied')
      throw new Error('当前环境不支持本地目录读取')
    }
    const grant = matchDirectory(path, 'read')
    if (!grant) {
      const ok = await requestAuthorize(`列出目录：${path}`)
      if (!ok) {
        audit('listDir', path, 'denied')
        throw new Error('用户拒绝列出目录')
      }
    }
    try {
      const entries = await platform.local.listDir(path)
      audit('listDir', path, 'approved')
      return entries
    } catch (e) {
      audit('listDir', path, 'error')
      throw e
    }
  }

  // ── 公共能力：执行命令 ──
  async function exec(
    command: string,
    args: string[],
    cwd?: string,
  ): Promise<{ code: number; stdout: string; stderr: string }> {
    if (!platform.local.available) {
      audit('exec', `${command} ${args.join(' ')}`, 'denied')
      throw new Error('当前环境不支持命令执行')
    }
    // 1) 危险/提权拦截
    const blocked = checkCommand(command, args)
    if (blocked) {
      audit('exec', `${command} ${args.join(' ')}`, 'blocked')
      throw new Error(blocked)
    }
    // 2) 命中命令白名单？(自动批准)；否则 HITL
    const grant = matchCommand(command, args)
    if (!grant) {
      const ok = await requestAuthorize(`执行命令：${command} ${args.join(' ')}`)
      if (!ok) {
        audit('exec', `${command} ${args.join(' ')}`, 'denied')
        throw new Error('用户拒绝执行命令')
      }
    }
    // 3) 工作目录授权校验
    if (cwd && !matchDirectory(cwd, 'read')) {
      const ok = await requestAuthorize(`命令工作目录：${cwd}`)
      if (!ok) {
        audit('exec', `cwd=${cwd}`, 'denied')
        throw new Error('用户拒绝该工作目录')
      }
    }
    const timeoutSec = grant?.timeoutSec ?? 60
    try {
      const result = await platform.local.exec(command, args, cwd, timeoutSec)
      audit('exec', `${command} ${args.join(' ')}`, 'approved')
      return result
    } catch (e) {
      audit('exec', `${command} ${args.join(' ')}`, 'error')
      throw e
    }
  }

  // ── 工具：用户选择目录授权整个目录 ──
  async function grantDirectoryViaPicker(scope: 'read' | 'readWrite'): Promise<boolean> {
    if (!platform.local.available) return false
    const path = await platform.local.pickDirectory()
    if (!path) return false
    agentStore.addDirectoryGrant({
      id: `dir_${Date.now()}`,
      path,
      scope,
      recursive: true,
      grantedAt: Date.now(),
    })
    return true
  }

  return {
    readFile,
    writeFile,
    listDir,
    exec,
    grantDirectoryViaPicker,
    // HITL 面板用
    resolveAuthorize,
    getPendingDetail,
    // 安全检查（供测试/调试）
    checkCommand,
    matchDirectory,
    matchCommand,
  }
}

function normalizePath(p: string): string {
  return p.replace(/\\/g, '/').replace(/\/+$/, '').toLowerCase()
}
