<template>
  <transition name="banner-fade">
    <div v-if="bannerState" :class="['session-status-banner', `banner-${bannerState.type}`]">
      <el-icon class="banner-icon">
        <Loading v-if="bannerState.type === 'running'" class="is-loading" />
        <WarningFilled v-else-if="bannerState.type === 'failed'" />
        <Clock v-else />
      </el-icon>
      <span class="banner-text">{{ bannerState.text }}</span>
      <span v-if="bannerState.detail" class="banner-detail">{{ bannerState.detail }}</span>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Loading, WarningFilled, Clock } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { useChatStore } from '@/stores/chat'
import type { ChatMessage } from '@/types/chat'

const { t } = useI18n()
const chatStore = useChatStore()

interface BannerState {
  type: 'running' | 'failed' | 'incomplete'
  text: string
  detail?: string
}

/**
 * Detects whether the loaded messages represent an incomplete conversation.
 * An incomplete conversation has a user message as the last entry,
 * meaning the AI didn't finish (or start) generating a response.
 */
function isConversationIncomplete(messages: ChatMessage[]): boolean {
  if (messages.length === 0) return false
  const lastMsg = messages[messages.length - 1]
  return lastMsg.role === 'user'
}

/**
 * Checks if any assistant message lacks process data when the session failed.
 */
function hasMissingProcessData(messages: ChatMessage[]): boolean {
  return messages.some(
    (m) => m.role === 'assistant' && !m.process && m.content.length === 0,
  )
}

const bannerState = computed<BannerState | null>(() => {
  const session = chatStore.currentSession
  if (!session || !chatStore.currentSessionId) return null

  const status = session.SessionStatus

  // Session is running
  if (status === 1) {
    if (isConversationIncomplete(chatStore.messages)) {
      return {
        type: 'running',
        text: t('chat.sessionRunningBanner'),
        detail: t('chat.sessionRunningDetail'),
      }
    }
    return {
      type: 'running',
      text: t('chat.sessionRunningBanner'),
    }
  }

  // Session failed
  if (status === -1) {
    const incomplete = isConversationIncomplete(chatStore.messages)
    const missingProcess = hasMissingProcessData(chatStore.messages)
    if (incomplete || missingProcess) {
      return {
        type: 'failed',
        text: t('chat.sessionFailedBanner'),
        detail: incomplete
          ? t('chat.sessionFailedIncomplete')
          : t('chat.sessionFailedNoResult'),
      }
    }
    return {
      type: 'failed',
      text: t('chat.sessionFailedBanner'),
    }
  }

  return null
})
</script>

<style lang="scss" scoped>
.session-status-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  margin: 0 16px;
  border-radius: 8px;
  font-size: 13px;
  line-height: 1.5;

  .banner-icon {
    flex-shrink: 0;
    font-size: 16px;
  }

  .banner-text {
    font-weight: 500;
  }

  .banner-detail {
    color: inherit;
    opacity: 0.8;
    font-size: 12px;

    &::before {
      content: '·';
      margin: 0 4px;
    }
  }
}

.banner-running {
  background: #e8f4fd;
  color: #2470c7;
  border: 1px solid #b3d8f0;

  .is-loading {
    animation: rotating 2s linear infinite;
  }
}

.banner-failed {
  background: #fef0f0;
  color: #c45656;
  border: 1px solid #fbc4c4;
}

.banner-incomplete {
  background: #fdf6ec;
  color: #b88230;
  border: 1px solid #f5dab1;
}

@keyframes rotating {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.banner-fade-enter-active,
.banner-fade-leave-active {
  transition: all 0.3s ease;
}
.banner-fade-enter-from,
.banner-fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
