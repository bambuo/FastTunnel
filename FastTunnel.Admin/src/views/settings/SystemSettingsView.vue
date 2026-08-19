<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { getSystemConfig, saveSystemConfig } from '@/api/systemConfig'
import type { SystemConfigSaveRequest } from '@/types/systemConfig'

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)

const form = reactive<SystemConfigSaveRequest>({
  enableForward: true,
  webDomain: '',
  webAllowAccessIps: [],
  clockSkew: 10,
  validAudience: '',
  validIssuer: '',
  issuerSigningKey: '',
  expires: 120,
})

onMounted(fetchConfig)

async function fetchConfig() {
  loading.value = true
  try {
    const c = await getSystemConfig()
    form.enableForward = c.enableForward
    form.webDomain = c.webDomain
    form.webAllowAccessIps = c.webAllowAccessIps
    form.clockSkew = c.jwt.clockSkew
    form.validAudience = c.jwt.validAudience
    form.validIssuer = c.jwt.validIssuer
    form.issuerSigningKey = c.jwt.issuerSigningKey
    form.expires = c.jwt.expires
  } catch (err: unknown) {
    Message.error(err instanceof Error ? err.message : t('message.operationFailed'))
  } finally {
    loading.value = false
  }
}

async function handleSave() {
  saving.value = true
  try {
    const r = await saveSystemConfig({ ...form })
    Message.success(r.message || t('config.saved'))
  } catch (err: unknown) {
    Message.error(err instanceof Error ? err.message : t('message.operationFailed'))
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">{{ $t('page.systemSettings') }}</h1>
    </div>

    <a-spin :loading="loading" style="width: 100%">
      <a-form :model="form" layout="vertical" style="max-width: 640px">
        <a-divider orientation="left">{{ $t('config.general') }}</a-divider>

        <a-form-item :label="$t('config.enableForward')">
          <a-switch v-model="form.enableForward" />
          <div class="field-tip">{{ $t('config.enableForwardTip') }}</div>
        </a-form-item>

        <a-form-item :label="$t('config.webDomain')">
          <a-input v-model="form.webDomain" :placeholder="$t('config.webDomainPlaceholder')" />
          <div class="field-tip">{{ $t('config.webDomainTip') }}</div>
        </a-form-item>

        <a-form-item :label="$t('config.webAllowAccessIps')">
          <a-input-tag v-model="form.webAllowAccessIps" :placeholder="$t('config.webAllowAccessIpsPlaceholder')" allow-clear />
          <div class="field-tip">{{ $t('config.webAllowAccessIpsTip') }}</div>
        </a-form-item>

        <a-divider orientation="left">{{ $t('config.jwt') }}</a-divider>

        <a-form-item :label="$t('config.clockSkew')">
          <a-input-number v-model="form.clockSkew" :min="0" style="width: 100%" />
        </a-form-item>

        <a-form-item :label="$t('config.validAudience')">
          <a-input v-model="form.validAudience" />
        </a-form-item>

        <a-form-item :label="$t('config.validIssuer')">
          <a-input v-model="form.validIssuer" />
        </a-form-item>

        <a-form-item :label="$t('config.issuerSigningKey')" required>
          <a-input-password v-model="form.issuerSigningKey" />
        </a-form-item>

        <a-form-item :label="$t('config.expires')">
          <a-input-number v-model="form.expires" :min="1" style="width: 100%" />
        </a-form-item>

        <div class="jwt-tip">{{ $t('config.jwtTip') }}</div>

        <a-button type="primary" :loading="saving" @click="handleSave">{{ $t('config.save') }}</a-button>
      </a-form>
    </a-spin>
  </div>
</template>

<style scoped>
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.page-title {
  font-size: 20px;
  margin: 0;
  color: var(--color-text-1);
}

.field-tip {
  font-size: 12px;
  color: var(--color-text-3);
  margin-top: 4px;
}

.jwt-tip {
  font-size: 12px;
  color: var(--color-warning-6);
  margin-bottom: 16px;
}
</style>
