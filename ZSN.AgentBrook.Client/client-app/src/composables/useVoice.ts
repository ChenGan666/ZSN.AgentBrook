import { onUnmounted } from 'vue'
import { useVoiceStore } from '@/stores/voice'
import { getFunAsrWsUrl } from '@/services/voiceApi'
import type { FunAsrMessage } from '@/types/voice'

const isTauri = '__TAURI_INTERNALS__' in window

export function useVoice() {
  const voiceStore = useVoiceStore()

  let audioContext: AudioContext | null = null
  let mediaStream: MediaStream | null = null
  let scriptProcessor: ScriptProcessorNode | null = null
  let ws: WebSocket | null = null
  let unlistenPcm: (() => void) | null = null
  let reconnectAttempts = 0
  const MAX_RECONNECT = 3

  // --- WebSocket to FunASR (via API proxy) ---

  function connectWebSocket(): Promise<void> {
    return new Promise(async (resolve, reject) => {
      try {
        const wsUrl = await getFunAsrWsUrl()
        ws = new WebSocket(wsUrl)
      } catch (e: any) {
        reject(new Error(`获取语音服务地址失败: ${e.message}`))
        return
      }
      ws.binaryType = 'arraybuffer'

      ws.onopen = () => {
        reconnectAttempts = 0
        const config: FunAsrMessage = {
          mode: '2pass',
          wav_name: 'mic_recording',
          wav_format: 'pcm',
          is_speaking: true,
          chunk_size: [5, 10, 5],
          audio_fs: 16000,
          itn: true,
        }
        ws!.send(JSON.stringify(config))
        resolve()
      }

      ws.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data)
          if (data.text) {
            const mode: string = data.mode ?? ''
            const isFinal = mode === 'offline' || mode === '2pass-offline' || data.is_final === true
            voiceStore.appendText(data.text, isFinal)
          }
        } catch { /* ignore non-JSON */ }
      }

      ws.onerror = () => {
        voiceStore.setState('error')
        reject(new Error('语音识别服务连接失败'))
      }

      ws.onclose = () => {
        if (voiceStore.state === 'recording') {
          attemptReconnect()
        }
      }
    })
  }

  async function attemptReconnect() {
    if (reconnectAttempts >= MAX_RECONNECT) {
      voiceStore.setState('error')
      return
    }
    reconnectAttempts++
    const delay = Math.min(Math.pow(2, reconnectAttempts - 1) * 1000, 5000)
    voiceStore.setState('connecting')
    await new Promise((r) => setTimeout(r, delay))

    try {
      await connectWebSocket()
      voiceStore.setState('recording')
      if (mediaStream) startBrowserAudioCapture()
    } catch {
      attemptReconnect()
    }
  }

  // --- Tauri: Rust-side audio capture ---

  async function startTauriAudioCapture() {
    const { listen } = await import('@tauri-apps/api/event')
    const { invoke } = await import('@tauri-apps/api/core')

    unlistenPcm = await listen<string>('audio-pcm-data', (event) => {
      if (!ws || ws.readyState !== WebSocket.OPEN) return
      const binary = atob(event.payload)
      const bytes = new Uint8Array(binary.length)
      for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i)
      }
      ws.send(bytes.buffer)
    })

    await invoke('start_audio_capture')
  }

  async function stopTauriAudioCapture() {
    if (unlistenPcm) {
      unlistenPcm()
      unlistenPcm = null
    }
    try {
      const { invoke } = await import('@tauri-apps/api/core')
      await invoke('stop_audio_capture')
    } catch { /* ignore */ }
  }

  // --- Browser: Web Audio API capture ---

  function startBrowserAudioCapture() {
    if (!mediaStream || !ws) return

    audioContext = new AudioContext({ sampleRate: 16000 })
    const source = audioContext.createMediaStreamSource(mediaStream)
    const processor = audioContext.createScriptProcessor(4096, 1, 1)
    scriptProcessor = processor

    processor.onaudioprocess = (e) => {
      if (!ws || ws.readyState !== WebSocket.OPEN) return
      const float32 = e.inputBuffer.getChannelData(0)
      const int16 = new Int16Array(float32.length)
      for (let i = 0; i < float32.length; i++) {
        const s = Math.max(-1, Math.min(1, float32[i]))
        int16[i] = s < 0 ? s * 0x8000 : s * 0x7fff
      }
      ws.send(int16.buffer)
    }

    source.connect(processor)
    processor.connect(audioContext.destination)
  }

  async function stopBrowserAudioCapture() {
    if (scriptProcessor) {
      scriptProcessor.disconnect()
      scriptProcessor = null
    }
    if (audioContext) {
      await audioContext.close()
      audioContext = null
    }
    if (mediaStream) {
      mediaStream.getTracks().forEach((t) => t.stop())
      mediaStream = null
    }
  }

  // --- Public API ---

  async function startRecording() {
    voiceStore.setState('connecting')
    voiceStore.clearText()

    try {
      if (isTauri) {
        await connectWebSocket()
        await startTauriAudioCapture()
      } else {
        mediaStream = await navigator.mediaDevices.getUserMedia({
          audio: {
            channelCount: 1,
            sampleRate: 16000,
            echoCancellation: true,
            noiseSuppression: true,
          },
        })
        await connectWebSocket()
        startBrowserAudioCapture()
      }
      voiceStore.setState('recording')
    } catch (e: any) {
      voiceStore.setState('error')
      throw new Error(`录音启动失败: ${e.message}`)
    }
  }

  async function stopRecording() {
    if (isTauri) {
      await stopTauriAudioCapture()
    } else {
      await stopBrowserAudioCapture()
    }

    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({ is_speaking: false }))
      setTimeout(() => {
        ws?.close()
        ws = null
      }, 2000)
    }

    voiceStore.setState('idle')
  }

  function getFullText(): string {
    return voiceStore.recognizedText + voiceStore.interimText
  }

  onUnmounted(() => {
    stopRecording()
  })

  return { startRecording, stopRecording, getFullText }
}
