/** FunASR 识别结果 */
export interface AsrResult {
  text: string
  isFinal: boolean
  timestamp?: number
}

/** 语音录制状态 */
export type VoiceState = 'idle' | 'recording' | 'connecting' | 'error'

/** 语言选择 */
export type VoiceLanguage = 'zh' | 'en' | 'mixed'

/** FunASR WebSocket 消息 */
export interface FunAsrMessage {
  mode: 'online' | 'offline' | '2pass'
  wav_name: string
  wav_format: string
  is_speaking: boolean
  chunk_size: number[]
  audio_fs: number
  hotwords?: string
  itn?: boolean
}
