<template>
  <!--
    @ App 选择浮层：搜索 + 单选内联插入。
    选中某个 App 后，把 "@App名称" 文本插入到输入框光标位置（作为指令文本的一部分），
    随后关闭浮层。支持键盘上下选择 / 回车确认 / Esc 关闭。
  -->
  <transition name="el-zoom-in-top">
    <div v-if="visible" ref="panelRef" class="mention-popover" :style="posStyle" @click.stop>
      <div class="mention-header">
        <el-input
          ref="searchInputRef"
          v-model="keyword"
          :placeholder="t('agent.searchApp', '搜索可编排 App')"
          size="small"
          clearable
          :prefix-icon="Search"
          @keydown="onSearchKeydown"
        />
      </div>
      <div class="mention-list">
        <div v-if="filteredApps.length === 0" class="mention-empty">
          {{ t('agent.noApp', '暂无可编排 App') }}
        </div>
        <div
          v-for="(app, idx) in filteredApps"
          :key="app.AppID"
          class="mention-item"
          :class="{ active: idx === activeIndex }"
          @click="pick(app)"
          @mouseenter="activeIndex = idx"
        >
          <img v-if="app.AICON" :src="app.AICON" class="app-icon" alt="" />
          <div v-else class="app-icon placeholder">{{ (app.Name || 'A').charAt(0) }}</div>
          <div class="app-info">
            <div class="app-name">{{ app.Name }}</div>
            <div class="app-desc">{{ app.Description }}</div>
          </div>
          <el-icon class="insert-hint"><TopRight /></el-icon>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { Search, TopRight } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '@/stores/chat'
import type { AppInfo } from '@/types/chat'

const props = defineProps<{
  visible: boolean
  /** 浮层定位（相对输入框） */
  pos?: { top?: number; left?: number; bottom?: number }
}>()

/** 选中 App 时触发，携带 App 信息（由父组件负责把 @名称 插入输入框） */
const emit = defineEmits<{
  done: []
  pick: [app: AppInfo]
}>()

const { t } = useI18n()
const chatStore = useChatStore()
const panelRef = ref<HTMLElement | null>(null)
const searchInputRef = ref<{ focus?: () => void; input?: HTMLInputElement } | null>(null)
const keyword = ref('')
const activeIndex = ref(0)

const posStyle = computed(() => ({
  left: props.pos?.left != null ? `${props.pos.left}px` : '0px',
  bottom: props.pos?.bottom != null ? `${props.pos.bottom}px` : undefined,
  top: props.pos?.top != null ? `${props.pos.top}px` : undefined,
}))

const filteredApps = computed<AppInfo[]>(() => {
  const kw = keyword.value.trim().toLowerCase()
  const apps = chatStore.apps || []
  if (!kw) return apps
  return apps.filter(
    (a) => a.Name?.toLowerCase().includes(kw) || a.Description?.toLowerCase().includes(kw),
  )
})

// 浮层打开时：重置关键字、聚焦搜索框、回到第一项
watch(
  () => props.visible,
  (v) => {
    if (v) {
      keyword.value = ''
      activeIndex.value = 0
      nextTick(() => searchInputRef.value?.focus?.())
    }
  },
)

// 列表变化时重置高亮项
watch(filteredApps, () => {
  activeIndex.value = 0
})

function pick(app: AppInfo) {
  emit('pick', app)
  emit('done')
}

function onSearchKeydown(e: Event | KeyboardEvent) {
  const ke = e as KeyboardEvent
  if (ke.key === 'ArrowDown') {
    ke.preventDefault()
    activeIndex.value = Math.min(activeIndex.value + 1, filteredApps.value.length - 1)
  } else if (ke.key === 'ArrowUp') {
    ke.preventDefault()
    activeIndex.value = Math.max(activeIndex.value - 1, 0)
  } else if (ke.key === 'Enter') {
    ke.preventDefault()
    const app = filteredApps.value[activeIndex.value]
    if (app) pick(app)
  } else if (ke.key === 'Escape') {
    ke.preventDefault()
    emit('done')
  }
}
</script>

<style lang="scss" scoped>
.mention-popover {
  position: absolute;
  z-index: 50;
  width: 320px;
  max-height: 360px;
  display: flex;
  flex-direction: column;
  background: var(--bg-card, #fff);
  border: 1px solid var(--border-color, #e4e7ed);
  border-radius: 8px;
  box-shadow: 0 6px 24px rgba(0, 0, 0, 0.12);
  overflow: hidden;
}
.mention-header {
  padding: 8px;
  border-bottom: 1px solid var(--border-color, #ebeef5);
}
.mention-list {
  flex: 1 1 auto;
  overflow-y: auto;
  padding: 4px;
}
.mention-empty {
  padding: 24px;
  text-align: center;
  color: var(--text-secondary, #909399);
  font-size: 13px;
}
.mention-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px;
  border-radius: 6px;
  cursor: pointer;
  &:hover {
    background: var(--bg-hover, #f5f7fa);
  }
  &.active {
    background: var(--el-color-primary-light-9, #ecf5ff);
  }
}
.app-icon {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  object-fit: cover;
  flex-shrink: 0;
  &.placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--el-color-primary, #409eff);
    color: #fff;
    font-size: 13px;
    font-weight: 600;
  }
}
.app-info {
  flex: 1 1 auto;
  min-width: 0;
}
.app-name {
  font-size: 13px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.app-desc {
  font-size: 11px;
  color: var(--text-secondary, #909399);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.insert-hint {
  color: var(--el-color-primary, #409eff);
  flex-shrink: 0;
  font-size: 14px;
  opacity: 0.7;
}
</style>
