<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { Message } from '@arco-design/web-vue'
import { useI18n } from 'vue-i18n'
import { IconUser, IconLock, IconSafe } from '@arco-design/web-vue/es/icon'
import { useMfaLogin } from '@/composables/useMfaLogin'
import { useSetupStore } from '@/stores/setup'
import MfaSetupCard from '@/components/login/MfaSetupCard.vue'
import TotpInput from '@/components/login/TotpInput.vue'

const router = useRouter()
const setupStore = useSetupStore()
const { t } = useI18n()

const {
  form, totpCode, step, secret, qrCodeUrl, mfaCountdown,
  handleLogin, handleBindMfa, handleVerifyMfa, backToLogin,
} = useMfaLogin()

const loginLoading = ref(false)

const cardTitle = computed(() => {
  switch (step.value) {
    case 'credentials': return t('login.title.credentials')
    case 'mfaSetup': return t('login.title.mfaSetup')
    case 'mfaVerify': return t('login.title.mfaVerify')
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
    Message.success(t('message.secret.copied'))
  } catch {
    Message.warning(t('message.secret.copyFailed'))
  }
}
</script>

<template>
  <div class="login-container">
    <div class="login-card-wrapper">
      <div class="login-brand">
        <div class="brand-icon">
          <IconSafe :size="32" />
        </div>
        <h1 class="brand-name">FastTunnel</h1>
        <p class="brand-desc">{{ $t('login.brand.desc') }}</p>
      </div>

      <a-card class="login-card" :bordered="false">
        <template #title>
          <div class="login-header">
            <h2>{{ cardTitle }}</h2>
          </div>
        </template>

        <a-alert v-if="setupStore.error" type="warning" class="setup-hint">
          {{ $t('login.setup.hint') }}
          <a-link @click="router.push('/setup')">{{ $t('login.setup.link') }}</a-link>
        </a-alert>

        <Transition name="fade-slide" mode="out-in">
          <a-form
            v-if="step === 'credentials'"
            key="credentials"
            :model="form"
            layout="vertical"
            class="login-form"
          >
            <a-form-item :label="$t('login.form.username')">
              <a-input v-model="form.name" :placeholder="$t('login.form.usernamePlaceholder')" allow-clear size="large">
                <template #prefix>
                  <IconUser />
                </template>
              </a-input>
            </a-form-item>
            <a-form-item :label="$t('login.form.password')">
              <a-input-password
                v-model="form.password"
                :placeholder="$t('login.form.passwordPlaceholder')"
                allow-clear
                size="large"
                @keyup.enter="onSubmitLogin"
              >
                <template #prefix>
                  <IconLock />
                </template>
              </a-input-password>
            </a-form-item>
            <a-form-item>
              <a-button type="primary" long size="large" :loading="loginLoading" @click="onSubmitLogin">
                {{ $t('login.form.submit') }}
              </a-button>
            </a-form-item>
          </a-form>

          <div v-else-if="step === 'mfaSetup'" key="mfaSetup" class="mfa-step">
            <MfaSetupCard :secret="secret" :qr-code-url="qrCodeUrl" @copy="onCopySecret" />
            <div class="totp-section">
              <p class="totp-label">{{ $t('login.mfa.totpLabel') }}</p>
              <TotpInput v-model="totpCode" :countdown="mfaCountdown" @submit="handleBindMfa" @back="backToLogin" />
            </div>
            <div class="footer-row">
              <span :class="['countdown-text', { urgent: countdownUrgent }]">
                <IconSafe /> {{ $t('login.mfa.countdown') }} {{ countdownText }}
              </span>
              <a-button type="text" size="small" @click="backToLogin">{{ $t('login.mfa.back') }}</a-button>
            </div>
          </div>

          <div v-else-if="step === 'mfaVerify'" key="mfaVerify" class="mfa-step">
            <div class="mfa-header-icon">
              <IconSafe :size="40" style="color: rgb(var(--primary-6))" />
            </div>
            <p class="mfa-hint">{{ $t('login.mfa.totpHint') }}</p>
            <div class="totp-section">
              <TotpInput v-model="totpCode" :countdown="mfaCountdown" @submit="handleVerifyMfa" @back="backToLogin" />
            </div>
            <div class="footer-row">
              <span :class="['countdown-text', { urgent: countdownUrgent }]">
                <IconSafe /> {{ $t('login.mfa.countdown') }} {{ countdownText }}
              </span>
              <a-button type="text" size="small" @click="backToLogin">{{ $t('login.mfa.back') }}</a-button>
            </div>
          </div>
        </Transition>
      </a-card>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #0b0f1e 0%, #1a1a2e 50%, #16213e 100%);
  padding: 24px;
}

.login-card-wrapper {
  width: 100%;
  max-width: 420px;
}

.login-brand {
  text-align: center;
  margin-bottom: 32px;
}

.brand-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  margin: 0 auto 16px;
  background: linear-gradient(135deg, #165DFF 0%, #4080FF 100%);
  border-radius: 14px;
  color: #fff;
  box-shadow: 0 8px 24px rgba(22, 93, 255, 0.3);
}

.brand-name {
  font-size: 24px;
  font-weight: 700;
  color: #fff;
  margin: 0 0 6px;
  letter-spacing: 1px;
}

.brand-desc {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.5);
  margin: 0;
}

.login-card {
  border-radius: 12px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.3);
  background: var(--color-bg-1);
}

.login-card :deep(.arco-card-body) {
  padding: 24px;
}

.login-header {
  text-align: center;
}

.login-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--color-text-1);
}

.setup-hint {
  margin-bottom: 16px;
}

.login-form {
  margin-top: 4px;
}

.mfa-header-icon {
  text-align: center;
  margin-bottom: 12px;
}

.mfa-hint {
  font-size: 13px;
  color: var(--color-text-2);
  text-align: center;
  margin: 0 0 16px;
}

.totp-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  margin: 20px 0;
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
  padding-top: 12px;
  border-top: 1px solid var(--color-border-2);
}

.countdown-text {
  display: flex;
  align-items: center;
  gap: 4px;
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

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(12px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-12px);
}

@media (max-width: 480px) {
  .login-container {
    padding: 16px;
    background: var(--color-bg-2);
  }

  .login-brand {
    margin-bottom: 24px;
  }

  .brand-icon {
    width: 48px;
    height: 48px;
  }

  .brand-name {
    font-size: 20px;
  }

  .brand-desc {
    font-size: 12px;
  }

  .login-card {
    box-shadow: none;
    border-radius: 8px;
  }

  .login-card :deep(.arco-card-body) {
    padding: 20px 16px;
  }
}
</style>
