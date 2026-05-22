<template>
  <div class="settings-section">
    <h3>个人信息</h3>
    <el-form label-position="top" v-if="userStore.userInfo">
      <el-form-item label="头像">
        <el-avatar :size="64" :src="userStore.userInfo.avatar">
          {{ userStore.userInfo.name?.charAt(0) || '?' }}
        </el-avatar>
      </el-form-item>
      <el-form-item label="昵称">
        <el-input v-model="editForm.name" />
      </el-form-item>
      <el-form-item label="手机号">
        <el-input :model-value="userStore.userInfo.phone" disabled />
      </el-form-item>

      <el-divider />
      <h4>修改密码</h4>
      <el-form-item label="旧密码">
        <el-input v-model="passwordForm.oldPassword" type="password" show-password />
      </el-form-item>
      <el-form-item label="新密码">
        <el-input v-model="passwordForm.newPassword" type="password" show-password placeholder="留空则不修改密码" />
      </el-form-item>

      <el-form-item class="save-row">
        <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
      </el-form-item>
    </el-form>
    <el-empty v-else description="未登录" />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { saveMemberInfo } from '@/services/member'
import { doubleMd5 } from '@/utils/crypto'

const userStore = useUserStore()
const saving = ref(false)

const editForm = reactive({ name: '' })
const passwordForm = reactive({ oldPassword: '', newPassword: '' })

watch(
  () => userStore.userInfo,
  (info) => {
    if (info) {
      editForm.name = info.name
    }
  },
  { immediate: true },
)

async function handleSave() {
  if (passwordForm.newPassword && !passwordForm.oldPassword) {
    ElMessage.warning('请填写旧密码')
    return
  }
  saving.value = true
  try {
    const { data } = await saveMemberInfo({
      MNickName: editForm.name,
      OldPassword: passwordForm.oldPassword ? doubleMd5(passwordForm.oldPassword) : '',
      NewPassword: passwordForm.newPassword ? doubleMd5(passwordForm.newPassword) : '',
    } as any)
    if (data.Success) {
      if (userStore.userInfo) {
        userStore.userInfo.name = editForm.name
      }
      ElMessage.success('保存成功')
      passwordForm.oldPassword = ''
      passwordForm.newPassword = ''
    } else {
      ElMessage.error(data.ErrorDesc || '保存失败')
    }
  } catch {
    ElMessage.error('保存失败')
  } finally {
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
.settings-section {
  max-width: 600px;
  h3 { margin-bottom: 16px; font-size: 18px; }
  h4 { margin: 16px 0; font-size: 15px; }
}

.save-row {
  margin-top: 24px;
}
</style>
