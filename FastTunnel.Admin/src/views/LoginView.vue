<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { Message } from '@arco-design/web-vue'
import { useMfaLogin } from '@/composables/useMfaLogin'
import { useSetupStore } from '@/stores/setup'
import MfaSetupCard from '@/components/login/MfaSetupCard.vue'
import TotpInput from '@/components/login/TotpInput.vue'

const router = useRouter()
const setupStore = useSetupStore()

const {
  form, totpCode, step, secret, qrCodeUrl, mfaCountdown,
  handleLogin, handleBindMfa, handleVerifyMfa, backToLogin,
} = useMfaLogin()

const loginLoading = ref(false)

const cardTitle = computed(() => {
  switch (step.value) {
    case 'credentials': return '登录管理面板'
    case 'mfaSetup': return '绑定两步验证'
    case 'mfaVerify': return '两步验证'
    default: return 'FastTunnel'
  }
})

const countdownText = computed(() => {
  const m = Math.floor(mfaCountdown.value / 60)
  const s = mfaCountdown.value % 60
  return `${m}:${s.toString().padStart(2, '0')}`
})

const countdownUrgent = computed(() => mfaCountdown.value <= 60 && step.value !== 'credentials')

async function onSubmitLogin() {
  loginLoading.value = true
  await handleLogin()
  loginLoading.value = false
}

async function onCopySecret(secretVal: string) {
  try {
    await navigator.clipboard.writeText(secretVal)
    Message.success('密钥已复制到剪贴板')
  } catch {
    Message.warning('复制失败，请手动选择复制')
  }
}
</script>

<template>
  <div class="login-container">
    <a-card class="login-card" :bordered="false">
      <template #title>
        <div class="login-header">
          <h2>FastTunnel</h2>
          <p>{{ cardTitle }}</p>
        </div>
      </template>

      <a-alert v-if="setupStore.error" type="warning" class="setup-hint">
        无法连接服务器，如果是首次部署请先
        <a-link @click="router.push('/setup')">进入初始化页面</a-link>
      </a-alert>

      <a-form v-if="step === 'credentials'" :model="form" layout="vertical">
        <a-form-item label="用户名">
          <a-input v-model="form.name" placeholder="请输入用户名" allow-clear />
        </a-form-item>
        <a-form-item label="密码">
          <a-input-password
            v-model="form.password"
            placeholder="请输入密码"
            allow-clear
            @keyup.enter="onSubmitLogin"
          />
        </a-form-item>
        <a-form-item>
          <a-button type="primary" long :loading="loginLoading" @click="onSubmitLogin">
            登录
          </a-button>
        </a-form-item>
      </a-form>

      <div v-if="step === 'mfaSetup'">
        <MfaSetupCard :secret="secret" :qr-code-url="qrCodeUrl" @copy="onCopySecret" />
        <div class="totp-section">
          <p class="totp-label">请输入应用显示的 6 位验证码</p>
          <TotpInput v-model="totpCode" :countdown="mfaCountdown" @submit="handleBindMfa" @back="backToLogin" />
        </div>
        <div class="footer-row">
          <span :class="['countdown-text', { urgent: countdownUrgent }]">令牌有效期 {{ countdownText }}</span>
          <a-button type="text" size="small" @click="backToLogin">返回登录页</a-button>
        </div>
      </div>

      <div v-if="step === 'mfaVerify'">
        <p class="mfa-hint">请输入 Authenticator 应用的 6 位验证码</p>
        <div class="totp-section">
          <TotpInput v-model="totpCode" :countdown="mfaCountdown" @submit="handleVerifyMfa" @back="backToLogin" />
        </div>
        <div class="footer-row">
          <span :class="['countdown-text', { urgent: countdownUrgent }]">令牌有效期 {{ countdownText }}</span>
          <a-button type="text" size="small" @click="backToLogin">返回登录页</a-button>
        </div>
      </div>
    </a-card>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: var(--color-bg-2);
}

.login-card {
  width: 420px;
}

.setup-hint {
  margin-bottom: 16px;
}

.login-header {
  text-align: center;
}

.login-header h2 {
  margin: 0 0 4px;
  font-size: 20px;
  color: var(--color-text-1);
}

.login-header p {
  margin: 0;
  font-size: 13px;
  color: var(--color-text-3);
}

.mfa-hint {
  font-size: 13px;
  color: var(--color-text-2);
  text-align: center;
  margin: 0 0 8px;
}

.totp-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  margin: 16px 0;
}

.totp-label {
  font-size: 13px;
  color: var(--color-text-2);
  margin: 0;
}

.footer-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 12px;
}

.countdown-text {
  font-size: 12px;
  color: var(--color-text-3);
  font-family: "SF Mono", Menlo, Monaco, Consolas, monospace;
  transition: color 0.3s;
}

.countdown-text.urgent {
  color: rgb(var(--danger-6));
  font-weight: 600;
  animation: blink 1s ease-in-out infinite;
}

@keyframes blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}
</style>
