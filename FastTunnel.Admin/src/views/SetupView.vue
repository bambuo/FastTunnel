<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Message } from '@arco-design/web-vue'
import { initSystem } from '@/api/setup'
import { useSetupStore } from '@/stores/setup'

const router = useRouter()
const setupStore = useSetupStore()

const form = ref({ name: '', password: '', confirmPassword: '' })
const loading = ref(false)

function validatePassword(pw: string): boolean {
  return pw.length >= 8 && /[a-zA-Z]/.test(pw) && /[0-9]/.test(pw)
}

const passwordValid = () => form.value.password.length === 0 || validatePassword(form.value.password)
const confirmValid = () => form.value.password === form.value.confirmPassword

const canSubmit = () =>
  form.value.name.length >= 3 &&
  validatePassword(form.value.password) &&
  form.value.password === form.value.confirmPassword

async function handleSubmit() {
  if (!canSubmit()) return
  loading.value = true
  try {
    await initSystem({ name: form.value.name, password: form.value.password })
    setupStore.markInitialized()
    Message.success('初始化完成，即将跳转登录页')
    setTimeout(() => router.push('/login'), 1500)
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : '初始化失败'
    Message.error(msg)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="setup-container">
    <a-card class="setup-card" :bordered="false">
      <template #title>
        <div class="setup-header">
          <h2>FastTunnel 系统初始化</h2>
          <p class="setup-desc">首次部署，请创建管理员账号</p>
        </div>
      </template>

      <a-form :model="form" layout="vertical">
        <a-form-item label="管理员用户名">
          <a-input
            v-model="form.name"
            placeholder="请输入用户名"
            :min-length="3"
            :max-length="64"
            allow-clear
          />
        </a-form-item>

        <a-form-item label="管理员密码">
          <a-input-password
            v-model="form.password"
            placeholder="至少 8 位，包含字母和数字"
            allow-clear
          />
          <template #extra>
            <span :class="{ 'pw-error': form.password.length > 0 && !passwordValid() }">
              <template v-if="form.password.length > 0 && !passwordValid()">
                密码强度不足：至少 8 位，包含字母和数字
              </template>
              <template v-else>
                推荐使用字母 + 数字组合
              </template>
            </span>
          </template>
        </a-form-item>

        <a-form-item label="确认密码">
          <a-input-password
            v-model="form.confirmPassword"
            placeholder="请再次输入密码"
            allow-clear
          />
          <template #extra>
            <span v-if="form.confirmPassword.length > 0 && !confirmValid()" class="pw-error">
              两次输入密码不一致
            </span>
          </template>
        </a-form-item>

        <a-form-item>
          <a-button
            type="primary"
            long
            :loading="loading"
            :disabled="!canSubmit()"
            @click="handleSubmit"
          >
            完成初始化
          </a-button>
        </a-form-item>
      </a-form>
    </a-card>
  </div>
</template>

<style scoped>
.setup-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: var(--color-bg-2);
}

.setup-card {
  width: 420px;
}

.setup-header {
  text-align: center;
}

.setup-header h2 {
  margin: 0 0 8px;
  font-size: 20px;
  color: var(--color-text-1);
}

.setup-desc {
  margin: 0;
  font-size: 14px;
  color: var(--color-text-3);
}

.pw-error {
  color: rgb(var(--danger-6));
}
</style>
