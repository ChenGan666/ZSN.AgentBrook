import { defineStore } from 'pinia'

interface UploadTask {
  id: string
  fileName: string
  fileSize: number
  progress: number
  status: 'pending' | 'uploading' | 'success' | 'error'
  error?: string
  url?: string
}

export const useUploadStore = defineStore('upload', {
  state: () => ({
    tasks: [] as UploadTask[],
  }),

  getters: {
    activeTasks(): UploadTask[] {
      return this.tasks.filter((t) => t.status === 'uploading' || t.status === 'pending')
    },
    hasActive(): boolean {
      return this.activeTasks.length > 0
    },
  },

  actions: {
    addTask(id: string, fileName: string, fileSize: number) {
      this.tasks.unshift({ id, fileName, fileSize, progress: 0, status: 'uploading' })
    },
    setProgress(id: string, progress: number) {
      const task = this.tasks.find((t) => t.id === id)
      if (task) task.progress = progress
    },
    setStatus(id: string, status: UploadTask['status'], error?: string) {
      const task = this.tasks.find((t) => t.id === id)
      if (task) {
        task.status = status
        if (error) task.error = error
        if (status === 'success') task.progress = 100
      }
    },
    removeTask(id: string) {
      this.tasks = this.tasks.filter((t) => t.id !== id)
    },
    clearCompleted() {
      this.tasks = this.tasks.filter((t) => t.status !== 'success')
    },
  },
})
