<script setup lang="ts">
import { reactive, ref } from 'vue'
import type { WebTunnel, WebTunnelRequest, ForwardTunnel, ForwardTunnelRequest } from '@/types/tunnel'
import type { TokenEntity } from '@/types/token'
import { getTokens } from '@/api/tokens'
import { useI18n } from 'vue-i18n'

const props = defineProps<{
  visible: boolean
  type: 'web' | 'forward'
  editData?: WebTunnel | ForwardTunnel | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  submit: [data: WebTunnelRequest | ForwardTunnelRequest]
}>()

const { t } = useI18n()

const tokens = ref<TokenEntity[]>([])

const webForm = reactive<WebTunnelRequest>({
  subDomain: '',
  localIp: '',
  localPort: undefined,
  wwws: [],
  clientToken: '',
})

const forwardForm = reactive<ForwardTunnelRequest>({
  remotePort: undefined,
  localIp: '',
  localPort: undefined,
  protocol: 'TCP',
  clientToken: '',
})

function initForm() {
  loadTokens()
  if (props.type === 'web') {
    webForm.subDomain = ''
    webForm.localIp = ''
    webForm.localPort = undefined
    webForm.wwws = []
    webForm.clientToken = ''
    if (props.editData) {
      const d = props.editData as WebTunnel
      webForm.subDomain = d.subDomain
      webForm.localIp = d.localIp
      webForm.localPort = d.localPort
      webForm.wwws = d.wwws
      webForm.clientToken = d.clientToken
    }
  } else {
    forwardForm.remotePort = undefined
    forwardForm.localIp = ''
    forwardForm.localPort = undefined
    forwardForm.protocol = 'TCP'
    forwardForm.clientToken = ''
    if (props.editData) {
      const d = props.editData as ForwardTunnel
      forwardForm.remotePort = d.remotePort
      forwardForm.localIp = d.localIp
      forwardForm.localPort = d.localPort
      forwardForm.protocol = d.protocol
      forwardForm.clientToken = d.clientToken
    }
  }
}

async function loadTokens() {
  try {
    // Token 是配置实体：客户端离线也能配置隧道，数据源用 Token 管理列表而非在线客户端
    tokens.value = (await getTokens()).filter(t => !t.isDeleted)
  } catch {
    tokens.value = []
  }
}

function maskToken(value: string): string {
  if (!value) return t('placeholder.notSet')
  return value.length <= 8 ? value : `${value.slice(0, 4)}****${value.slice(-4)}`
}

function handleOk() {
  if (props.type === 'web') {
    emit('submit', {
      subDomain: webForm.subDomain,
      localIp: webForm.localIp,
      localPort: webForm.localPort,
      wwws: webForm.wwws,
      clientToken: webForm.clientToken,
    })
  } else {
    emit('submit', {
      remotePort: forwardForm.remotePort,
      localIp: forwardForm.localIp,
      localPort: forwardForm.localPort,
      protocol: forwardForm.protocol,
      clientToken: forwardForm.clientToken,
    })
  }
  emit('update:visible', false)
}
</script>

<template>
  <a-modal
    :visible="visible"
    :title="editData ? t('tunnelForm.title.edit') : t('tunnelForm.title.create')"
    :width="500"
    @ok="handleOk"
    @cancel="() => emit('update:visible', false)"
    @before-open="initForm"
  >
    <template v-if="type === 'web'">
      <a-form :model="webForm" layout="vertical">
        <a-form-item :label="$t('tunnelForm.label.subDomain')" field="subDomain" required>
          <a-input v-model="webForm.subDomain" :placeholder="$t('placeholder.subDomain')" />
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.localIp')" field="localIp" required>
          <a-input v-model="webForm.localIp" :placeholder="$t('placeholder.localIp')" />
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.localPort')" field="localPort" required>
          <a-input-number v-model="webForm.localPort" :min="1" :max="65535" :placeholder="$t('placeholder.localPort')" style="width:100%" />
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.clientToken')" field="clientToken" required>
          <a-select v-model="webForm.clientToken" :placeholder="$t('placeholder.clientToken')" allow-clear>
            <a-option v-for="tk in tokens" :key="tk.value" :value="tk.value">{{ maskToken(tk.value) }}{{ $t('tunnelForm.tokenOption', { desc: tk.description || $t('tunnelForm.label.clientToken') }) }}</a-option>
          </a-select>
        </a-form-item>
      </a-form>
    </template>

    <template v-else>
      <a-form :model="forwardForm" layout="vertical">
        <a-form-item :label="$t('tunnelForm.label.remotePort')" field="remotePort" required>
          <a-input-number v-model="forwardForm.remotePort" :min="1" :max="65535" :placeholder="$t('placeholder.remotePort')" style="width:100%" />
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.localIp')" field="localIp" required>
          <a-input v-model="forwardForm.localIp" :placeholder="$t('placeholder.localIp')" />
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.localPort')" field="localPort" required>
          <a-input-number v-model="forwardForm.localPort" :min="1" :max="65535" :placeholder="$t('placeholder.sshPort')" style="width:100%" />
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.protocol')" field="protocol">
          <a-radio-group v-model="forwardForm.protocol">
            <a-radio value="TCP">TCP</a-radio>
            <a-radio value="UDP">UDP</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item :label="$t('tunnelForm.label.clientToken')" field="clientToken" required>
          <a-select v-model="forwardForm.clientToken" :placeholder="$t('placeholder.clientToken')" allow-clear>
            <a-option v-for="tk in tokens" :key="tk.value" :value="tk.value">{{ maskToken(tk.value) }}{{ $t('tunnelForm.tokenOption', { desc: tk.description || $t('tunnelForm.label.clientToken') }) }}</a-option>
          </a-select>
        </a-form-item>
      </a-form>
    </template>
  </a-modal>
</template>
