<script setup lang="ts">
import QRCode from 'qrcode'
import { ref, onMounted } from 'vue'
import { IconCheckCircle } from '@arco-design/web-vue/es/icon'

const props = defineProps<{
  secret: string
  qrCodeUrl: string
}>()

const emit = defineEmits<{
  copy: [secret: string]
}>()

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
      <IconCheckCircle :size="32" style="color: rgb(var(--success-6))" />
      <span>账号验证通过，请绑定两步验证</span>
    </div>

    <p class="setup-tip">
      请使用 <strong>Google Authenticator</strong> 或 <strong>Microsoft Authenticator</strong> 扫描下方二维码
    </p>

    <div class="qr-wrapper">
      <canvas ref="canvasRef" class="qr-canvas" data-test="mfa-qr" />
    </div>

    <div class="secret-row">
      <span class="secret-label">无法扫码？手动输入密钥：</span>
      <a-tag color="arcoblue" class="secret-tag" data-test="mfa-secret">{{ secret }}</a-tag>
      <a-button type="text" size="small" @click="handleCopy">复制</a-button>
    </div>
  </div>
</template>

<style scoped>
.mfa-setup {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.setup-intro {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: var(--color-text-1);
}

.setup-tip {
  font-size: 13px;
  color: var(--color-text-2);
  text-align: center;
  margin: 0;
}

.qr-wrapper {
  padding: 12px;
  background: #fff;
  border-radius: 8px;
  border: 1px solid var(--color-border-2);
}

.qr-canvas {
  display: block;
}

.secret-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
}

.secret-label {
  color: var(--color-text-3);
}

.secret-tag {
  font-family: "SF Mono", Menlo, Monaco, Consolas, monospace;
  user-select: all;
}
</style>
