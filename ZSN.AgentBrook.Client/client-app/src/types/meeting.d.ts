export interface TranscriptSegment {
  speaker: string
  text: string
  startTime: number
  endTime: number
}

export interface MeetingResult {
  segments: TranscriptSegment[]
  summary?: string
  actionItems?: string[]
  duration: number
}
