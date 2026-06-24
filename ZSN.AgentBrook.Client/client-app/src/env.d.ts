/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  readonly VITE_APP_TITLE: string
  readonly VITE_APP_ID: string
  readonly VITE_APP_SECRET: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module '@tauri-apps/plugin-store' {
  export function load(path: string, options?: { autoSave?: boolean }): Promise<{
    get<T>(key: string): Promise<T | undefined>
    set(key: string, value: any): Promise<void>
    delete(key: string): Promise<void>
  }>
}

declare module '@tauri-apps/plugin-dialog' {
  export function open(options?: {
    multiple?: boolean
    filters?: { name: string; extensions: string[] }[]
  }): Promise<string | string[] | null>
  export function save(options?: { defaultPath?: string }): Promise<string | null>
}

declare module '@tauri-apps/plugin-fs' {
  export function readFile(path: string): Promise<Uint8Array>
  export function writeFile(path: string, data: Uint8Array): Promise<void>
}

declare module '@tauri-apps/plugin-shell' {
  export function open(url: string): Promise<void>
}

declare module '@tauri-apps/api/core' {
  export function invoke<T = void>(cmd: string, args?: Record<string, unknown>): Promise<T>
}

declare module '@tauri-apps/api/window' {
  export function getCurrentWindow(): {
    minimize(): Promise<void>
    toggleMaximize(): Promise<void>
    close(): Promise<void>
    hide(): Promise<void>
    show(): Promise<void>
    setFocus(): Promise<void>
    isVisible(): Promise<boolean>
    outerPosition(): Promise<{ x: number; y: number }>
    outerSize(): Promise<{ width: number; height: number }>
    setPosition(pos: any): Promise<void>
    setSize(size: any): Promise<void>
    startDragging(): Promise<void>
    setResizable(resizable: boolean): Promise<void>
    setAlwaysOnTop(alwaysOnTop: boolean): Promise<void>
  }
}

declare module '@tauri-apps/api/dpi' {
  export class LogicalPosition {
    constructor(x: number, y: number)
  }
  export class LogicalSize {
    constructor(width: number, height: number)
  }
}

declare module '@tauri-apps/plugin-global-shortcut' {
  export function register(shortcut: string, handler: () => void | Promise<void>): Promise<void>
  export function unregisterAll(): Promise<void>
}

declare module '@tauri-apps/plugin-updater' {
  export function check(): Promise<{
    version: string
    downloadAndInstall(onEvent: (event: any) => void): Promise<void>
  } | null>
}

declare module '@tauri-apps/plugin-process' {
  export function relaunch(): Promise<void>
}

declare module '@tauri-apps/api/webviewWindow' {
  export class WebviewWindow {
    constructor(label: string, options?: any)
    static getByLabel(label: string): Promise<WebviewWindow | null>
    show(): Promise<void>
    setFocus(): Promise<void>
    close(): Promise<void>
  }
}
