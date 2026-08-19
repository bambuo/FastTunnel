<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { IconSettings, IconSafe } from '@arco-design/web-vue/es/icon'
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
  <div class="settings-page">
    <div class="page-header">
      <h1 class="page-title">{{ $t('page.systemSettings') }}</h1>
      <p class="page-desc">{{ $t('config.pageDesc') }}</p>
    </div>

    <a-spin :loading="loading" style="width: 100%">
      <div class="card-list">
        <!-- 通用设置 -->
        <a-card class="config-card" :bordered="true">
          <template #title>
            <div class="card-title">
              <IconSettings class="card-icon" />
              <span>{{ $t('config.general') }}</span>
            </div>
          </template>
          <a-form :model="form" layout="vertical">
            <a-row :gutter="24">
              <a-col :span="12">
                <a-form-item :label="$t('config.webDomain')">
                  <a-input v-model="form.webDomain" :placeholder="$t('config.webDomainPlaceholder')" allow-clear />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item :label="$t('config.webAllowAccessIps')">
                  <a-input-tag v-model="form.webAllowAccessIps" :placeholder="$t('config.webAllowAccessIpsPlaceholder')" allow-clear />
                </a-form-item>
              </a-col>
              <a-col :span="24">
                <a-form-item :label="$t('config.enableForward')">
                  <div class="switch-row">
                    <a-switch v-model="form.enableForward" />
                  </div>
                  <div class="field-tip">{{ $t('config.enableForwardTip') }}</div>
                </a-form-item>
              </a-col>
            </a-row>
          </a-form>
        </a-card>

        <!-- JWT 认证 -->
        <a-card class="config-card" :bordered="true">
          <template #title>
            <div class="card-title">
              <IconSafe class="card-icon" />
              <span>{{ $t('config.jwt') }}</span>
            </div>
          </template>
          <a-form :model="form" layout="vertical">
            <a-row :gutter="24">
              <a-col :span="12">
                <a-form-item :label="$t('config.clockSkew')">
                  <a-input-number v-model="form.clockSkew" :min="0" style="width: 100%" />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item :label="$t('config.expires')">
                  <a-input-number v-model="form.expires" :min="1" style="width: 100%" />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item :label="$t('config.validAudience')">
                  <a-input v-model="form.validAudience" allow-clear />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item :label="$t('config.validIssuer')">
                  <a-input v-model="form.validIssuer" allow-clear />
                </a-form-item>
              </a-col>
              <a-col :span="24">
                <a-form-item :label="$t('config.issuerSigningKey')" required>
                  <a-input-password v-model="form.issuerSigningKey" />
                </a-form-item>
              </a-col>
            </a-row>
          </a-form>
          <a-alert type="warning" show-icon>
            {{ $t('config.jwtTip') }}
          </a-alert>
        </a-card>
      </div>
    </a-spin>

    <!-- 底部固定操作栏 -->
    <div class="action-bar">
      <a-button type="primary" :loading="saving" @click="handleSave">{{ $t('config.save') }}</a-button>
    </div>
  </div>
</template>

<style scoped>
.settings-page {
  max-width: 860px;
}

.page-header {
  margin-bottom: 16px;
}

.page-title {
  font-size: 20px;
  margin: 0;
  color: var(--color-text-1);
}

.page-desc {
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--color-text-3);
}

.card-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding-bottom: 72px; /* 为底部操作栏留出空间 */
}

.config-card {
  border-radius: var(--border-radius-medium);
}

.card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
}

.card-icon {
  font-size: 16px;
  color: var(--color-primary-6);
}

.switch-row {
  height: 32px;
  display: flex;
  align-items: center;
}

.field-tip {
  font-size: 12px;
  color: var(--color-text-3);
  margin-top: 4px;
}

.action-bar {
  position: fixed;
  bottom: 0;
  left: 220px; /* 侧边栏宽度，与主内容区对齐 */
  right: 0;
  padding: 12px 24px;
  background: var(--color-bg-2);
  border-top: 1px solid var(--color-border-2);
  display: flex;
  justify-content: flex-end;
  z-index: 10;
}

@media (max-width: 768px) {
  .action-bar {
    left: 0;
  }
}
</style>
