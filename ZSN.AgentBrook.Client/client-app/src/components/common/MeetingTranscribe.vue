<template>
  <div class="meeting-transcribe">
    <div v-if="!result" class="upload-section">
      <el-upload
        drag
        :auto-upload="false"
        :on-change="handleFileChange"
        accept="audio/*,video/*"
      >
        <el-icon :size="40"><Headset /></el-icon>
        <div>拖拽音频/视频文件到此处</div>
        <div class="upload-tip">支持 mp3, wav, mp4, avi, mov 等格式</div>
      </el-upload>

      <el-button
        v-if="selectedFile"
        type="primary"
        :loading="isProcessing"
        style="margin-top: 12px"
        @click="startTranscribe"
      >
        开始转写
      </el-button>

      <el-progress
        v-if="isProcessing"
        :percentage="progress"
        :format="progressFormat"
        style="margin-top: 12px"
      />
    </div>

    <div v-else class="result-section">
      <el-card v-if="result.summary" class="summary-card">
        <template #header>会议摘要</template>
        {{ result.summary }}
      </el-card>

      <el-card v-if="result.actionItems?.length" class="action-card">
        <template #header>待办事项</template>
        <ul>
          <li v-for="(item, i) in result.actionItems" :key="i">{{ item }}</li>
        </ul>
      </el-card>

      <div class="transcript-list">
        <div v-for="(seg, i) in result.segments" :key="i" class="transcript-item">
          <span class="time">{{ formatTime(seg.startTime) }}</span>
          <span class="speaker">{{ seg.speaker }}</span>
          <span class="text">{{ seg.text }}</span>
        </div>
      </div>

      <div class="export-actions">
        <el-button @click="exportResult('txt')">导出 TXT</el-button>
        <el-button @click="exportResult('md')">导出 Markdown</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Headset } from '@element-plus/icons-vue'
import { useMeeting } from '@/composables/useMeeting'

const { transcribeFile, exportResult, isProcessing, progress, result, formatTime } = useMeeting()
const selectedFile = ref<File | null>(null)

function handleFileChange(uploadFile: any) {
  selectedFile.value = uploadFile.raw
}

async function startTranscribe() {
  if (!selectedFile.value) return
  await transcribeFile(selectedFile.value)
}

function progressFormat(percentage: number) {
  if (percentage < 30) return '音频处理中...'
  if (percentage < 80) return '语音识别中...'
  if (percentage < 100) return 'AI 分析中...'
  return '完成'
}
</script>

<style lang="scss" scoped>
.meeting-transcribe {
  padding: 20px;
}

.upload-section {
  text-align: center;
}

.upload-tip {
  font-size: 12px;
  color: var(--text-secondary, #909399);
  margin-top: 4px;
}

.result-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.summary-card,
.action-card {
  :deep(.el-card__header) {
    font-weight: 600;
  }
}

.action-card ul {
  padding-left: 20px;
  margin: 0;
}

.transcript-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.transcript-item {
  display: flex;
  gap: 8px;
  padding: 8px 0;
  border-bottom: 1px solid var(--border-color, #e4e7ed);

  .time {
    color: var(--text-secondary, #909399);
    font-size: 12px;
    flex-shrink: 0;
    width: 48px;
  }

  .speaker {
    font-weight: 500;
    flex-shrink: 0;
    min-width: 40px;
  }

  .text {
    flex: 1;
  }
}

.export-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 8px;
}
</style>
