import { defineStore } from 'pinia'
import type { VoiceState, VoiceLanguage } from '@/types/voice'

interface VoiceStoreState {
  state: VoiceState
  recognizedText: string
  interimText: string
  language: VoiceLanguage
  vadTimeout: number
  isVadEnabled: boolean
}

export const useVoiceStore = defineStore('voice', {
  state: (): VoiceStoreState => ({
    state: 'idle',
    recognizedText: '',
    interimText: '',
    language: 'zh',
    vadTimeout: 3,
    isVadEnabled: true,
  }),

  actions: {
    setState(s: VoiceState) {
      this.state = s
    },
    appendText(text: string, isFinal: boolean) {
      if (isFinal) {
        this.recognizedText += text
        this.interimText = ''
      } else if (this.interimText && text.startsWith(this.interimText)) {
        // 累积文本：新文本以之前的开头，直接替换（如 "你好" → "你好世界"）
        this.interimText = text
      } else {
        // 增量文本：追加（如 "你好" + "世界" = "你好世界"）
        this.interimText += text
      }
    },
    clearText() {
      this.recognizedText = ''
      this.interimText = ''
    },
  },
})
