<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Message } from '@arco-design/web-vue'
import { useI18n } from 'vue-i18n'
import { IconUser, IconLock, IconSafe } from '@arco-design/web-vue/es/icon'
import { initSystem } from '@/api/setup'
import { useSetupStore } from '@/stores/setup'

const router = useRouter()
const setupStore = useSetupStore()
const { t } = useI18n()

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
    Message.success(t('message.setup.completed'))
    setTimeout(() => router.push('/login'), 1500)
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : t('message.setup.failed')
    Message.error(msg)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="setup-container">
    <div class="setup-card-wrapper">
      <div class="setup-brand">
        <div class="brand-icon">
          <IconSafe :size="32" />
        </div>
        <h1 class="brand-name">FastTunnel</h1>
        <p class="brand-desc">{{ $t('setup.desc') }}</p>
      </div>

      <a-card class="setup-card" :bordered="false">
        <template #title>
          <div class="setup-header">
            <h2>{{ $t('setup.title') }}</h2>
          </div>
        </template>

        <Transition name="fade-slide" mode="out-in">
          <a-form :model="form" layout="vertical" class="setup-form">
            <a-form-item :label="$t('setup.form.username')">
              <a-input
                v-model="form.name"
                :placeholder="$t('setup.form.usernamePlaceholder')"
                :min-length="3"
                :max-length="64"
                allow-clear
                size="large"
              >
                <template #prefix>
                  <IconUser />
                </template>
              </a-input>
            </a-form-item>

            <a-form-item :label="$t('setup.form.password')">
              <a-input-password
                v-model="form.password"
                :placeholder="$t('setup.form.passwordPlaceholder')"
                allow-clear
                size="large"
              >
                <template #prefix>
                  <IconLock />
                </template>
              </a-input-password>
              <template #extra>
                <span :class="{ 'pw-error': form.password.length > 0 && !passwordValid() }">
                  <template v-if="form.password.length > 0 && !passwordValid()">
                    {{ $t('setup.form.passwordWeak') }}
                  </template>
                  <template v-else>
                    {{ $t('setup.form.passwordHint') }}
                  </template>
                </span>
              </template>
            </a-form-item>

            <a-form-item :label="$t('setup.form.confirm')">
              <a-input-password
                v-model="form.confirmPassword"
                :placeholder="$t('setup.form.confirmPlaceholder')"
                allow-clear
                size="large"
              >
                <template #prefix>
                  <IconLock />
                </template>
              </a-input-password>
              <template #extra>
                <span v-if="form.confirmPassword.length > 0 && !confirmValid()" class="pw-error">
                  {{ $t('setup.form.confirmMismatch') }}
                </span>
              </template>
            </a-form-item>

            <a-form-item>
              <a-button
                type="primary"
                long
                size="large"
                :loading="loading"
                :disabled="!canSubmit()"
                @click="handleSubmit"
              >
                {{ $t('setup.form.submit') }}
              </a-button>
            </a-form-item>
          </a-form>
        </Transition>
      </a-card>
    </div>
  </div>
</template>

<style scoped>
.setup-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #0b0f1e 0%, #1a1a2e 50%, #16213e 100%);
  padding: 24px;
}

.setup-card-wrapper {
  width: 100%;
  max-width: 420px;
}

.setup-brand {
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

.setup-card {
  border-radius: 12px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.3);
  background: var(--color-bg-1);
}

.setup-card :deep(.arco-card-body) {
  padding: 24px;
}

.setup-header {
  text-align: center;
}

.setup-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--color-text-1);
}

.setup-form {
  margin-top: 4px;
}

.pw-error {
  color: rgb(var(--danger-6));
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
  .setup-container {
    padding: 16px;
    background: var(--color-bg-2);
  }

  .setup-brand {
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

  .setup-card {
    box-shadow: none;
    border-radius: 8px;
  }

  .setup-card :deep(.arco-card-body) {
    padding: 20px 16px;
  }
}
</style>
