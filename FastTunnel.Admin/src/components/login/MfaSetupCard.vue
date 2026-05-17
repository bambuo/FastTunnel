<script setup lang="ts">
import QRCode from 'qrcode'
import { ref, onMounted } from 'vue'
import { IconCheckCircle, IconCopy } from '@arco-design/web-vue/es/icon'
import { useI18n } from 'vue-i18n'

const props = defineProps<{
  secret: string
  qrCodeUrl: string
}>()

const emit = defineEmits<{
  copy: [secret: string]
}>()

useI18n()

const canvasRef = ref<HTMLCanvasElement>()

onMounted(async () => {
  if (canvasRef.value) {
    await QRCode.toCanvas(canvasRef.value, props.qrCodeUrl, { width: 200, margin: 1 })
  }
})

function handleCopy() {
  emit('copy', props.secret)
}
</script>

<template>
  <div class="mfa-setup">
    <div class="setup-intro">
      <IconCheckCircle :size="28" style="color: rgb(var(--success-6))" />
      <span>{{ $t('mfa.setup.title') }}</span>
    </div>

    <p class="setup-tip">
      {{ $t('mfa.setup.tip', { app1: 'Google Authenticator', app2: 'Microsoft Authenticator' }) }}
    </p>

    <div class="qr-wrapper">
      <canvas ref="canvasRef" class="qr-canvas" data-test="mfa-qr" />
    </div>

    <div class="secret-row">
      <span class="secret-label">{{ $t('mfa.setup.secretLabel') }}</span>
      <a-tag color="arcoblue" class="secret-tag" data-test="mfa-secret">{{ secret }}</a-tag>
      <a-button type="text" size="small" @click="handleCopy">
        <template #icon>
          <IconCopy />
        </template>
        {{ $t('mfa.setup.copy') }}
      </a-button>
    </div>
  </div>
</template>

<style scoped>
.mfa-setup {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
}

.setup-intro {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 500;
  color: var(--color-text-1);
}

.setup-tip {
  font-size: 13px;
  color: var(--color-text-2);
  text-align: center;
  margin: 0;
  line-height: 1.6;
}

.qr-wrapper {
  padding: 16px;
  background: #fff;
  border-radius: 12px;
  border: 1px solid var(--color-border-2);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.qr-canvas {
  display: block;
}

.secret-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px;
  font-size: 13px;
}

.secret-label {
  color: var(--color-text-3);
}

.secret-tag {
  font-family: "SF Mono", Menlo, Monaco, Consolas, monospace;
  user-select: all;
  font-size: 12px;
}
</style>
