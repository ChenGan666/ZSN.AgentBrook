import type { PlatformAdapter } from './adapter'
import { TauriAdapter } from './tauri.adapter'
import { WebAdapter } from './web.adapter'

export const isTauri = (): boolean => '__TAURI_INTERNALS__' in window

let _platform: PlatformAdapter | null = null

export const platform: PlatformAdapter = (() => {
  if (!_platform) {
    _platform = isTauri() ? new TauriAdapter() : new WebAdapter()
  }
  return _platform
})()

export type { PlatformAdapter, FilePickOptions } from './adapter'
