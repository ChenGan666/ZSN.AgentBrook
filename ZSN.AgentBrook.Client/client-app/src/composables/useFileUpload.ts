import { ref } from 'vue'
import { useUploadStore } from '@/stores/upload'
import { platform } from '@/platform'
import { secureStorage } from '@/utils/storage'
import { createApiRequest, APP_SECRET } from '@/utils/crypto'
import { getBaseUrl } from '@/services/base'
import axios from 'axios'

interface UploadOptions {
  maxSize?: number
  maxConcurrency?: number
  chunkSize?: number
  maxRetries?: number
}

const DEFAULTS: Required<UploadOptions> = {
  maxSize: 50 * 1024 * 1024,
  maxConcurrency: 2,
  chunkSize: 1 * 1024 * 1024,
  maxRetries: 3,
}

const ALLOWED_TYPES: Record<string, string[]> = {
  image: ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'],
  document: ['pdf', 'doc', 'docx'],
  text: ['txt', 'json', 'csv', 'md'],
  audio: ['mp3', 'wav', 'm4a', 'ogg'],
  video: ['mp4', 'avi', 'mov', 'mkv'],
}

export function useFileUpload(options: UploadOptions = {}) {
  const opts = { ...DEFAULTS, ...options }
  const uploadStore = useUploadStore()
  const activeUploads = ref(0)

  function validateFile(file: File): string | null {
    if (file.size > opts.maxSize) {
      return `文件过大，最大 ${(opts.maxSize / 1024 / 1024).toFixed(0)}MB`
    }
    const ext = file.name.split('.').pop()?.toLowerCase() || ''
    const allExts = Object.values(ALLOWED_TYPES).flat()
    if (!allExts.includes(ext)) {
      return `不支持的文件类型: .${ext}`
    }
    return null
  }

  async function compressImage(file: File): Promise<File> {
    if (file.size <= 1024 * 1024) return file
    if (!file.type.startsWith('image/')) return file

    return new Promise((resolve) => {
      const img = new Image()
      const canvas = document.createElement('canvas')
      const ctx = canvas.getContext('2d')!

      img.onload = () => {
        let { width, height } = img
        const maxDim = 2048
        if (width > maxDim || height > maxDim) {
          const ratio = Math.min(maxDim / width, maxDim / height)
          width *= ratio
          height *= ratio
        }

        canvas.width = width
        canvas.height = height
        ctx.drawImage(img, 0, 0, width, height)

        canvas.toBlob(
          (blob) => {
            if (blob) {
              resolve(new File([blob], file.name, { type: 'image/jpeg' }))
            } else {
              resolve(file)
            }
          },
          'image/jpeg',
          0.8,
        )
      }

      img.src = URL.createObjectURL(file)
    })
  }

  async function uploadFile(file: File): Promise<{ fileCode: string; url: string }> {
    const error = validateFile(file)
    if (error) throw new Error(error)

    let processedFile = file
    if (file.type.startsWith('image/')) {
      processedFile = await compressImage(file)
    }

    const fileId = `upload_${Date.now()}_${Math.random().toString(36).slice(2)}`
    uploadStore.addTask(fileId, processedFile.name, processedFile.size)

    try {
      while (activeUploads.value >= opts.maxConcurrency) {
        await new Promise((r) => setTimeout(r, 100))
      }
      activeUploads.value++

      const result = await uploadDirect(fileId, processedFile)
      uploadStore.setStatus(fileId, 'success')
      return result
    } catch (e: any) {
      uploadStore.setStatus(fileId, 'error', e.message)
      throw e
    } finally {
      activeUploads.value--
    }
  }

  async function uploadDirect(fileId: string, file: File): Promise<{ fileCode: string; url: string }> {
    const accessToken = await secureStorage.get('access_token')
    const memberToken = await secureStorage.get('member_token')
    const encryptKey = memberToken || accessToken || APP_SECRET
    const secret = memberToken || accessToken || APP_SECRET

    const apiRequest = createApiRequest({}, encryptKey, secret)
    const dataJson = JSON.stringify(apiRequest)

    const formData = new FormData()
    formData.append('Data', dataJson)
    formData.append('file0', file)

    const headers: Record<string, string> = {}
    if (accessToken) headers['bearer'] = accessToken
    if (memberToken) headers['memberbearer'] = memberToken

    const { data } = await axios.post(`${getBaseUrl()}/File/Upload`, formData, {
      headers: { ...headers, 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (e) => {
        if (e.total) {
          uploadStore.setProgress(fileId, Math.round((e.loaded / e.total) * 100))
        }
      },
    })

    return {
      fileCode: data.Data?.FileCode || '',
      url: data.Data?.Url || '',
    }
  }

  async function pickFiles(options?: { multiple?: boolean; accept?: string[] }) {
    return platform.file.pick(options)
  }

  return {
    uploadFile,
    pickFiles,
    validateFile,
  }
}
