<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { IconSearch } from '@arco-design/web-vue/es/icon'
import {
  getWebTunnels, createWebTunnel, updateWebTunnel,
  deleteWebTunnel, toggleWebTunnel,
} from '@/api/tunnels'
import type { WebTunnel, WebTunnelRequest } from '@/types/tunnel'
import TunnelForm from '@/components/common/TunnelForm.vue'

const { t } = useI18n()

const data = ref<WebTunnel[]>([])
const loading = ref(false)
const modalVisible = ref(false)
const editTunnel = ref<WebTunnel | null>(null)
const searchKeyword = ref('')

const columns = computed(() => [
  { title: t('table.column.subDomain'), dataIndex: 'subDomain', width: 120 },
  { title: t('table.column.localAddress'), slotName: 'address', width: 200 },
  { title: t('table.column.client'), dataIndex: 'clientName', width: 120 },
  { title: t('table.column.status'), slotName: 'status', width: 80 },
  { title: t('table.column.createdAt'), dataIndex: 'createdAt', width: 160 },
  { title: t('table.column.actions'), slotName: 'action', width: 220, fixed: 'right' as const },
])

onMounted(() => fetchList())

async function fetchList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = {}
    if (searchKeyword.value) params.keyword = searchKeyword.value
    data.value = await getWebTunnels(params)
  } catch {
    // handled
  } finally {
    loading.value = false
  }
}

function handleSearch() { fetchList() }

function handleAdd() {
  editTunnel.value = null
  modalVisible.value = true
}

function handleEdit(record: WebTunnel) {
  editTunnel.value = record
  modalVisible.value = true
}

async function handleSubmit(formData: WebTunnelRequest) {
  try {
    if (editTunnel.value) {
      const r = await updateWebTunnel(editTunnel.value.id, formData)
      Message.success(r.message || t('message.tunnel.updated'))
    } else {
      const r = await createWebTunnel(formData)
      Message.success(r.message || t('message.tunnel.created'))
    }
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : t('message.operationFailed')
    Message.error(msg)
  }
}

async function handleDelete(id: number) {
  try {
    const r = await deleteWebTunnel(id)
    Message.success(r.message || t('message.tunnel.deleted'))
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : t('message.deleteFailed')
    Message.error(msg)
  }
}

async function handleToggle(record: WebTunnel) {
  try {
    const r = await toggleWebTunnel(record.id, !record.isEnabled)
    Message.success(r.message || (record.isEnabled ? t('action.disabled') : t('action.enabled')))
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : t('message.operationFailed')
    Message.error(msg)
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">{{ $t('page.webTunnels') }}</h1>
      <a-button type="primary" data-test="add-tunnel-btn" @click="handleAdd">{{ $t('action.newTunnel') }}</a-button>
    </div>

    <div class="search-bar">
      <a-input-search
        v-model="searchKeyword"
        :placeholder="$t('placeholder.searchSubDomain')"
        allow-clear
        @search="handleSearch"
        @clear="handleSearch"
        style="width: 320px"
      >
        <template #prefix><IconSearch /></template>
      </a-input-search>
    </div>

    <a-table
      :columns="columns"
      :data="data"
      :loading="loading"
      :pagination="{ pageSize: 20, showTotal: true }"
      row-key="id"
    >
      <template #address="{ record }">
        <code class="address-code">{{ record.localIp }}:{{ record.localPort }}</code>
      </template>
      <template #status="{ record }">
        <a-switch
          :model-value="record.isEnabled"
          size="small"
          @change="() => handleToggle(record)"
        />
      </template>
      <template #action="{ record }">
        <a-space>
          <a-button type="text" size="small" @click="handleEdit(record)">{{ $t('action.edit') }}</a-button>
          <a-popconfirm :content="$t('popconfirm.deleteTunnel')" @ok="() => handleDelete(record.id)">
            <a-button type="text" size="small" status="danger">{{ $t('action.delete') }}</a-button>
          </a-popconfirm>
        </a-space>
      </template>
      <template #empty>
        <a-empty :description="$t('empty.noWebTunnels')" />
      </template>
    </a-table>

    <TunnelForm
      v-model:visible="modalVisible"
      type="web"
      :edit-data="editTunnel"
      @submit="handleSubmit"
    />
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

.search-bar {
  margin-bottom: 16px;
}

.address-code {
  font-family: "SF Mono", Menlo, Monaco, Consolas, monospace;
  font-size: 13px;
  color: var(--color-text-2);
}
</style>
