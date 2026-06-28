<template>
  <!-- 编排大脑模型选择器（数据来自 N1 GetList，脱敏 DTO） -->
  <el-select
    :model-value="agentStore.modelID"
    :placeholder="t('agent.selectModel', '选择编排模型')"
    size="small"
    class="model-selector"
    :loading="loading"
    @change="onChange"
    @visible-change="onVisible"
  >
    <el-option
      v-for="m in agentStore.models"
      :key="m.LargeModelID"
      :value="m.LargeModelID"
      :label="m.Name || m.ModelName"
    >
      <span class="opt-row">
        <img v-if="m.MICON" :src="m.MICON" class="opt-icon" alt="" />
        <span class="opt-name">{{ m.Name || m.ModelName }}</span>
        <el-tag v-if="m.Thinking" size="small" type="info" effect="plain">思考</el-tag>
      </span>
    </el-option>
    <template #empty>
      <span class="empty-tip">{{ loading ? t('common.loading', '加载中…') : t('agent.noModel', '暂无可用模型') }}</span>
    </template>
  </el-select>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAgentStore } from '@/stores/agent'
import { getModelList } from '@/services/model'

const { t } = useI18n()
const agentStore = useAgentStore()
const loading = ref(false)

async function fetchModels() {
  if (agentStore.models.length > 0) return
  loading.value = true
  try {
    const { data } = await getModelList()
    if (data.Success && data.Data) {
      agentStore.setModels(data.Data)
    }
  } catch {
    ElMessage.error(t('agent.loadModelFailed', '模型列表加载失败'))
  } finally {
    loading.value = false
  }
}

function onChange(val: number) {
  agentStore.setModel(val)
}

function onVisible(visible: boolean) {
  if (visible && agentStore.models.length === 0) fetchModels()
}

onMounted(fetchModels)
</script>

<style lang="scss" scoped>
.model-selector {
  width: 200px;
  flex-shrink: 0;
}
.opt-row {
  display: flex;
  align-items: center;
  gap: 6px;
}
.opt-icon {
  width: 16px;
  height: 16px;
  border-radius: 3px;
  object-fit: cover;
}
.opt-name {
  flex: 1 1 auto;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.empty-tip {
  color: var(--text-secondary, #909399);
  font-size: 12px;
}
</style>
