<script setup lang="ts">
import { reactive } from 'vue'
import type { WebTunnel, WebTunnelRequest, ForwardTunnel, ForwardTunnelRequest } from '@/types/tunnel'
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

const webForm = reactive<WebTunnelRequest & { clientIdStr: string }>({
  subDomain: '',
  localIp: '',
  localPort: undefined,
  wwws: [],
  clientId: undefined,
  clientIdStr: '',
})

const forwardForm = reactive<ForwardTunnelRequest & { clientIdStr: string }>({
  remotePort: undefined,
  localIp: '',
  localPort: undefined,
  protocol: 'TCP',
  clientId: undefined,
  clientIdStr: '',
})

function initForm() {
  if (props.type === 'web') {
    webForm.subDomain = ''
    webForm.localIp = ''
    webForm.localPort = undefined
    webForm.wwws = []
    webForm.clientId = undefined
    webForm.clientIdStr = ''
    if (props.editData) {
      const d = props.editData as WebTunnel
      webForm.subDomain = d.subDomain
      webForm.localIp = d.localIp
      webForm.localPort = d.localPort
      webForm.wwws = d.wwws
      webForm.clientId = d.clientId
      webForm.clientIdStr = String(d.clientId)
    }
  } else {
    forwardForm.remotePort = undefined
    forwardForm.localIp = ''
    forwardForm.localPort = undefined
    forwardForm.protocol = 'TCP'
    forwardForm.clientId = undefined
    forwardForm.clientIdStr = ''
    if (props.editData) {
      const d = props.editData as ForwardTunnel
      forwardForm.remotePort = d.remotePort
      forwardForm.localIp = d.localIp
      forwardForm.localPort = d.localPort
      forwardForm.protocol = d.protocol
      forwardForm.clientId = d.clientId
      forwardForm.clientIdStr = String(d.clientId)
    }
  }
}

function handleOk() {
  if (props.type === 'web') {
    emit('submit', {
      subDomain: webForm.subDomain,
      localIp: webForm.localIp,
      localPort: webForm.localPort,
      wwws: webForm.wwws,
      clientId: Number(webForm.clientIdStr),
    })
  } else {
    emit('submit', {
      remotePort: forwardForm.remotePort,
      localIp: forwardForm.localIp,
      localPort: forwardForm.localPort,
      protocol: forwardForm.protocol,
      clientId: Number(forwardForm.clientIdStr),
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
        <a-form-item :label="$t('tunnelForm.label.clientId')" field="clientId" required>
          <a-input v-model="webForm.clientIdStr" :placeholder="$t('placeholder.clientId')" />
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
        <a-form-item :label="$t('tunnelForm.label.clientId')" field="clientId" required>
          <a-input v-model="forwardForm.clientIdStr" :placeholder="$t('placeholder.clientId')" />
        </a-form-item>
      </a-form>
    </template>
  </a-modal>
</template>
