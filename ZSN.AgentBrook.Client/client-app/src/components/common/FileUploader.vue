<template>
  <div class="file-uploader">
    <div class="file-list" v-if="selectedFiles.length">
      <div v-for="(file, index) in selectedFiles" :key="index" class="file-item">
        <el-icon class="file-icon"><Document /></el-icon>
        <span class="file-name">{{ file.name }}</span>
        <span class="file-size">{{ formatSize(file.size) }}</span>
        <el-button text size="small" @click="removeFile(index)">
          <el-icon><Close /></el-icon>
        </el-button>
      </div>
    </div>

    <div
      class="upload-area"
      :class="{ dragging }"
      @dragover.prevent="dragging = true"
      @dragleave="dragging = false"
      @drop.prevent="handleDrop"
      @click="handlePick"
    >
      <el-icon :size="24"><Upload /></el-icon>
      <span>点击或拖拽文件到此处</span>
    </div>

    <div v-for="task in uploadStore.activeTasks" :key="task.id" class="upload-progress">
      <span>{{ task.fileName }}</span>
      <el-progress :percentage="task.progress" :status="task.status === 'error' ? 'exception' : undefined" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Document, Close, Upload } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useFileUpload } from '@/composables/useFileUpload'
import { useUploadStore } from '@/stores/upload'

const props = defineProps<{
  maxFiles?: number
}>()

const emit = defineEmits<{
  filesReady: [files: File[]]
}>()

const { pickFiles, validateFile } = useFileUpload()
const uploadStore = useUploadStore()
const selectedFiles = ref<File[]>([])
const dragging = ref(false)
const maxFiles = props.maxFiles ?? 5

async function handlePick() {
  const files = await pickFiles({ multiple: true })
  addFiles(files)
}

function handleDrop(e: DragEvent) {
  dragging.value = false
  const files = Array.from(e.dataTransfer?.files || [])
  addFiles(files)
}

function addFiles(files: File[]) {
  for (const file of files) {
    if (selectedFiles.value.length >= maxFiles) break
    const error = validateFile(file)
    if (error) {
      ElMessage.warning(`${file.name}: ${error}`)
      continue
    }
    selectedFiles.value.push(file)
  }
  emit('filesReady', selectedFiles.value)
}

function removeFile(index: number) {
  selectedFiles.value.splice(index, 1)
  emit('filesReady', selectedFiles.value)
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes}B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)}KB`
  return `${(bytes / 1024 / 1024).toFixed(1)}MB`
}
</script>

<style lang="scss" scoped>
.upload-area {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 16px;
  border: 2px dashed var(--border-color, #dcdfe6);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  color: var(--text-secondary);
  font-size: 13px;

  &:hover, &.dragging {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
  }
}

.file-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 8px;
}

.file-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 4px;
  font-size: 12px;
}

.file-size {
  color: var(--text-secondary);
}

.upload-progress {
  margin-top: 8px;
  font-size: 12px;
  color: var(--text-secondary);
}
</style>
