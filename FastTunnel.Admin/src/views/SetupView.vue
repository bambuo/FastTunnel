<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Message } from '@arco-design/web-vue'
import { useI18n } from 'vue-i18n'
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
    <a-card class="setup-card" :bordered="false">
      <template #title>
        <div class="setup-header">
          <h2>{{ $t('setup.title') }}</h2>
          <p class="setup-desc">{{ $t('setup.desc') }}</p>
        </div>
      </template>

      <a-form :model="form" layout="vertical">
        <a-form-item :label="$t('setup.form.username')">
          <a-input
            v-model="form.name"
            :placeholder="$t('setup.form.usernamePlaceholder')"
            :min-length="3"
            :max-length="64"
            allow-clear
          />
        </a-form-item>

        <a-form-item :label="$t('setup.form.password')">
          <a-input-password
            v-model="form.password"
            :placeholder="$t('setup.form.passwordPlaceholder')"
            allow-clear
          />
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
          />
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
            :loading="loading"
            :disabled="!canSubmit()"
            @click="handleSubmit"
          >
            {{ $t('setup.form.submit') }}
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
