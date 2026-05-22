import { isTauri } from '@/platform'

export async function openMiniWindow() {
  if (!isTauri()) return

  const { WebviewWindow } = await import('@tauri-apps/api/webviewWindow')
  const existing = await WebviewWindow.getByLabel('mini-chat')
  if (existing) {
    await existing.show()
    await existing.setFocus()
    return
  }

  new WebviewWindow('mini-chat', {
    url: '/mini-chat',
    title: '快速对话',
    width: 360,
    height: 480,
    alwaysOnTop: true,
    decorations: false,
    resizable: true,
    center: true,
  })
}

export async function openTranscribeWindow() {
  if (!isTauri()) return

  const { WebviewWindow } = await import('@tauri-apps/api/webviewWindow')
  const existing = await WebviewWindow.getByLabel('transcribe')
  if (existing) {
    await existing.show()
    await existing.setFocus()
    return
  }

  new WebviewWindow('transcribe', {
    url: '/meeting-transcribe',
    title: '会议转写',
    width: 400,
    height: 600,
    alwaysOnTop: true,
    decorations: false,
    resizable: true,
    center: true,
  })
}
