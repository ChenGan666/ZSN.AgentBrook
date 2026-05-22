<template>
  <div class="hitl-panel" @click.stop>
    <div class="hitl-title">{{ hitlData.askContent }}</div>
    <div class="hitl-inputs">
      <div
        v-for="opt in normalizedOptions"
        :key="opt.id || opt.value || opt.name"
        class="hitl-input-row"
      >
        <label class="hitl-input-label">
          {{ opt.name || opt.id }}
          <span v-if="opt.isRequired" class="hitl-required">*</span>
        </label>
        <template v-if="(opt.valueType || 'string') === 'string'">
          <input
            type="text"
            class="hitl-input-control"
            :value="form[opt.id] ?? ''"
            :disabled="isSubmitting || isSubmitted"
            @input="onInput(opt, ($event.target as HTMLInputElement).value)"
          />
        </template>
        <template v-else-if="opt.valueType === 'number'">
          <input
            type="number"
            class="hitl-input-control"
            :value="form[opt.id] ?? ''"
            :disabled="isSubmitting || isSubmitted"
            @input="onInput(opt, ($event.target as HTMLInputElement).value)"
          />
        </template>
        <template v-else-if="opt.valueType === 'date'">
          <input
            type="date"
            class="hitl-input-control"
            :value="form[opt.id] ?? ''"
            :disabled="isSubmitting || isSubmitted"
            @input="onInput(opt, ($event.target as HTMLInputElement).value)"
          />
        </template>
        <template v-else-if="opt.valueType === 'time'">
          <input
            type="time"
            class="hitl-input-control"
            :value="form[opt.id] ?? ''"
            :disabled="isSubmitting || isSubmitted"
            @input="onInput(opt, ($event.target as HTMLInputElement).value)"
          />
        </template>
        <template v-else-if="opt.valueType === 'bool'">
          <select
            class="hitl-input-control"
            :value="formatBoolForSelect(form[opt.id])"
            :disabled="isSubmitting || isSubmitted"
            @change="onInput(opt, ($event.target as HTMLSelectElement).value)"
          >
            <option value="">请选择</option>
            <option value="true">是</option>
            <option value="false">否</option>
          </select>
        </template>
        <template v-else>
          <input
            type="text"
            class="hitl-input-control"
            :value="form[opt.id] ?? ''"
            :disabled="isSubmitting || isSubmitted"
            @input="onInput(opt, ($event.target as HTMLInputElement).value)"
          />
        </template>
      </div>
    </div>
    <button
      class="hitl-btn"
      :disabled="isSubmitting || isSubmitted"
      @click="handleSubmit"
    >
      <span v-if="isSubmitting" class="spinner" />
      <span>提交</span>
    </button>
    <div v-if="isSubmitted" class="hitl-tip">已提交</div>
    <div v-if="submitErrorKey === nodeKey && submitErrorMsg" class="hitl-error">
      {{ submitErrorMsg }}
    </div>
    <div v-if="localErrorMsg" class="hitl-error">{{ localErrorMsg }}</div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, watch, toRefs, ref } from 'vue'

interface HitlOption {
  id: string
  name?: string
  value?: any
  valueType?: string
  isRequired?: boolean
}

const props = defineProps<{
  hitlData: {
    askContent: string
    options: HitlOption[]
    reCallUrl: string
  }
  nodeKey: string
  isSubmitting?: boolean
  isSubmitted?: boolean
  submitErrorKey?: string | null
  submitErrorMsg?: string
}>()

const emit = defineEmits<{
  submit: [payload: { nodeKey: string; reCallUrl: string; inputOptions: any[] }]
}>()

const localErrorMsg = ref('')

const state = reactive<{ initialized: boolean; form: Record<string, any> }>({
  initialized: false,
  form: {},
})
const { form } = toRefs(state)

const normalizedOptions = computed(() => {
  const options = props.hitlData?.options || []
  return Array.isArray(options) ? options : []
})

function formatBackendDatetimeToInput(val: any): string {
  if (!val) return ''
  const s = String(val).trim()
  const [datePart] = s.split(' ')
  if (datePart.length !== 10 || !/^\d{4}-\d{2}-\d{2}$/.test(datePart)) return ''
  return datePart
}

function formatBackendTimeToInput(val: any): string {
  if (!val) return ''
  const s = String(val).trim()
  const parts = s.split(':')
  if (parts.length < 2) return ''
  return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
}

function formatInputDatetimeToBackend(val: any): string {
  if (!val) return ''
  const s = String(val).trim()
  if (s.length !== 10 || !/^\d{4}-\d{2}-\d{2}$/.test(s)) return ''
  return s
}

function formatBoolForSelect(val: any): string {
  if (val === true) return 'true'
  if (val === false) return 'false'
  const s = String(val ?? '').trim()
  if (s === 'true') return 'true'
  if (s === 'false') return 'false'
  return ''
}

function initForm() {
  if (state.initialized) return
  const options = normalizedOptions.value || []
  const formObj: Record<string, any> = {}
  options.forEach((opt) => {
    if (!opt?.id) return
    const t = opt.valueType || 'string'
    const raw = opt.value
    if (t === 'date') {
      formObj[opt.id] = formatBackendDatetimeToInput(raw)
    } else if (t === 'time') {
      formObj[opt.id] = formatBackendTimeToInput(raw)
    } else if (t === 'bool') {
      if (raw === true || raw === false) formObj[opt.id] = raw
      else if (String(raw).trim() === 'true') formObj[opt.id] = 'true'
      else if (String(raw).trim() === 'false') formObj[opt.id] = 'false'
      else formObj[opt.id] = ''
    } else {
      formObj[opt.id] = raw || ''
    }
  })
  state.form = formObj
  state.initialized = true
}

watch(
  () => props.hitlData,
  () => {
    state.initialized = false
    state.form = {}
    localErrorMsg.value = ''
    initForm()
  },
  { immediate: true, deep: true },
)

function onInput(opt: HitlOption, value: any) {
  if (!opt?.id) return
  const t = opt.valueType || 'string'
  let nextVal = value
  if (t === 'bool') {
    if (value === 'true' || value === true) nextVal = true
    else if (value === 'false' || value === false) nextVal = false
    else nextVal = ''
  }
  state.form = { ...state.form, [opt.id]: nextVal }
}

function handleSubmit() {
  const options = normalizedOptions.value || []
  const formValues = state.form || {}
  localErrorMsg.value = ''
  for (const opt of options) {
    if (!opt?.id || !opt.isRequired) continue
    const t = opt.valueType || 'string'
    const raw = formValues[opt.id]
    if (t === 'bool') {
      if (raw === '' || raw === null || raw === undefined) {
        localErrorMsg.value = `请选择【${opt.name || opt.id}】`
        return
      }
    } else {
      const v = (raw ?? '').toString().trim()
      if (!v) {
        localErrorMsg.value = `请输入【${opt.name || opt.id}】`
        return
      }
    }
  }

  const inputOptions = options.map((opt) => {
    if (!opt?.id) return opt
    const t = opt.valueType || 'string'
    const raw = formValues[opt.id]
    let v: any = raw
    if (t === 'date') v = formatInputDatetimeToBackend(raw)
    else if (t === 'bool') {
      if (raw === true || raw === 'true') v = 'true'
      else if (raw === false || raw === 'false') v = 'false'
      else v = ''
    } else v = raw != null ? raw : ''
    return { ...opt, value: v }
  })

  localErrorMsg.value = ''
  emit('submit', {
    nodeKey: props.nodeKey,
    reCallUrl: props.hitlData?.reCallUrl,
    inputOptions,
  })
}
</script>

<style lang="scss" scoped>
.hitl-panel {
  margin-top: 8px;
  padding: 8px;
  border-radius: 6px;
  background: #fefce8;
  border: 1px solid #facc15;
}

.hitl-title {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 8px;
  color: #854d0e;
}

.hitl-inputs {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 8px;
}

.hitl-input-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.hitl-input-label {
  font-size: 13px;
  color: #713f12;
}

.hitl-required {
  color: #dc2626;
  margin-left: 4px;
}

.hitl-input-control {
  padding: 6px 8px;
  border-radius: 4px;
  border: 1px solid #e5e7eb;
  font-size: 13px;
}

.hitl-btn {
  margin-top: 4px;
  padding: 6px 12px;
  border-radius: 999px;
  background: #facc15;
  color: #713f12;
  border: none;
  font-size: 13px;
  cursor: pointer;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.hitl-tip {
  margin-top: 4px;
  font-size: 12px;
  color: #16a34a;
}

.hitl-error {
  margin-top: 4px;
  font-size: 12px;
  color: #dc2626;
}

.spinner {
  display: inline-block;
  width: 12px;
  height: 12px;
  border-radius: 999px;
  border: 2px solid rgba(0, 0, 0, 0.1);
  border-top-color: rgba(0, 0, 0, 0.5);
  animation: hitl-spin 0.8s linear infinite;
  margin-right: 4px;
}

@keyframes hitl-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
