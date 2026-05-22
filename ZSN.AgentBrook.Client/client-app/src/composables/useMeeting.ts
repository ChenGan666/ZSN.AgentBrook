import { ref } from 'vue'
import { platform } from '@/platform'
import http from '@/services/http'
import type { TranscriptSegment, MeetingResult } from '@/types/meeting'

export function useMeeting() {
  const isProcessing = ref(false)
  const progress = ref(0)
  const result = ref<MeetingResult | null>(null)
  const error = ref<string | null>(null)

  async function transcribeFile(file: File): Promise<MeetingResult> {
    isProcessing.value = true
    progress.value = 0
    error.value = null

    try {
      progress.value = 5
      const wavData = await platform.audio.convertToWav(file)
      progress.value = 30

      const segments = await sendToAsr(wavData)
      progress.value = 80

      const processed = await postProcess(segments)
      progress.value = 100

      result.value = processed
      return processed
    } catch (e: any) {
      error.value = e.message
      throw e
    } finally {
      isProcessing.value = false
    }
  }

  async function sendToAsr(wavData: ArrayBuffer): Promise<TranscriptSegment[]> {
    const CHUNK_DURATION = 10
    const sampleRate = 16000
    const bytesPerSample = 2
    const chunkBytes = CHUNK_DURATION * sampleRate * bytesPerSample
    const totalChunks = Math.ceil(wavData.byteLength / chunkBytes)
    const segments: TranscriptSegment[] = []

    for (let i = 0; i < totalChunks; i++) {
      const start = i * chunkBytes
      const end = Math.min(start + chunkBytes, wavData.byteLength)
      const chunk = wavData.slice(start, end)

      const { data } = await http.post('/Voice/Transcribe', chunk, {
        headers: { 'Content-Type': 'application/octet-stream' },
        params: { chunkIndex: i, totalChunks },
      })

      if (data.data?.segments) {
        segments.push(...data.data.segments)
      }

      progress.value = 30 + Math.round((i / totalChunks) * 50)
    }

    return segments
  }

  async function postProcess(segments: TranscriptSegment[]): Promise<MeetingResult> {
    const fullText = segments.map((s) => `[${s.speaker}] ${s.text}`).join('\n')

    const { data } = await http.post('/Voice/PostProcess', {
      text: fullText,
      tasks: ['summary', 'action_items'],
    })

    return {
      segments,
      summary: data.data?.summary,
      actionItems: data.data?.actionItems,
      duration: segments.length > 0
        ? segments[segments.length - 1].endTime
        : 0,
    }
  }

  async function exportResult(format: 'txt' | 'md') {
    if (!result.value) return

    let content = ''
    const { segments, summary, actionItems } = result.value

    if (format === 'md') {
      content = `# 会议记录\n\n`
      if (summary) content += `## 摘要\n\n${summary}\n\n`
      if (actionItems?.length) {
        content += `## 待办事项\n\n`
        actionItems.forEach((item) => { content += `- [ ] ${item}\n` })
        content += '\n'
      }
      content += `## 详细记录\n\n`
      segments.forEach((s) => {
        const time = formatTime(s.startTime)
        content += `**[${time}] ${s.speaker}**: ${s.text}\n\n`
      })
    } else {
      segments.forEach((s) => {
        const time = formatTime(s.startTime)
        content += `[${time}] ${s.speaker}: ${s.text}\n`
      })
      if (summary) content += `\n--- 摘要 ---\n${summary}\n`
    }

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
    const fileName = `会议记录_${new Date().toISOString().slice(0, 10)}.${format}`
    await platform.file.save(blob, fileName)
  }

  function formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60)
    const s = Math.floor(seconds % 60)
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
  }

  return { transcribeFile, exportResult, isProcessing, progress, result, error, formatTime }
}
