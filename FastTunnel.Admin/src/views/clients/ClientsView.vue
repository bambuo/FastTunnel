<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { getClients, getClientTunnels } from '@/api/clients'
import { IconSearch, IconCheckCircle, IconCloseCircle } from '@arco-design/web-vue/es/icon'
import type { ClientEntity, ClientTunnels } from '@/types/client'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()

const data = ref<ClientEntity[]>([])
const loading = ref(false)
const expandedTunnels = ref<Record<number, ClientTunnels | null>>({})
const searchKeyword = ref('')

const filteredData = computed(() => {
  if (!searchKeyword.value) return data.value
  const kw = searchKeyword.value.toLowerCase()
  return data.value.filter(c => c.token.toLowerCase().includes(kw))
})

const columns = computed(() => [
  { title: '令牌', dataIndex: 'token', width: 320, ellipsis: true },
  { title: 'IP', dataIndex: 'ip', width: 140 },
  { title: '运行环境', slotName: 'env', width: 200 },
  { title: t('table.column.onlineStatus'), slotName: 'online', width: 100 },
  { title: t('table.column.lastSeen'), slotName: 'lastSeen', width: 160 },
])

function formatMemory(mb: number): string {
  if (!mb) return '-'
  return mb >= 1024 ? `${(mb / 1024).toFixed(1)} GB` : `${mb} MB`
}

onMounted(() => fetchList())

async function fetchList() {
  loading.value = true
  try {
    data.value = await getClients()
  } catch {
    // handled
  } finally {
    loading.value = false
  }
}

async function handleExpand(row: ClientEntity) {
  if (!expandedTunnels.value[row.id]) {
    try {
      expandedTunnels.value[row.id] = await getClientTunnels(row.id)
    } catch {
      expandedTunnels.value[row.id] = null
    }
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">{{ $t('page.clients') }}</h1>
    </div>

    <div class="search-bar">
      <a-input
        v-model="searchKeyword"
        :placeholder="$t('placeholder.searchClient')"
        allow-clear
        style="width: 320px"
      >
        <template #prefix><IconSearch /></template>
      </a-input>
    </div>

    <a-table
      :columns="columns"
      :data="filteredData"
      :loading="loading"
      :pagination="{ pageSize: 20, showTotal: true }"
      row-key="id"
      :expanded-row-keys="Object.keys(expandedTunnels).map(Number)"
      @expand="(rowKey: string | number) => handleExpand(filteredData.find(c => c.id === Number(rowKey))!)"
    >
      <template #online="{ record }">
        <a-space :size="4">
          <IconCheckCircle v-if="record.isOnline" :size="14" style="color: rgb(var(--success-6))" />
          <IconCloseCircle v-else :size="14" style="color: rgb(var(--danger-6))" />
          <span>{{ record.isOnline ? $t('action.online') : $t('action.offline') }}</span>
        </a-space>
      </template>
      <template #env="{ record }">
        <span v-if="record.clientInfo">
          <a-tooltip>
            <template #content>
              <div class="env-tip">
                <p>系统：{{ record.clientInfo.os }} {{ record.clientInfo.osVersion }}</p>
                <p>架构：{{ record.clientInfo.architecture }}</p>
                <p>CPU：{{ record.clientInfo.cpuCores }} 核</p>
                <p>内存：{{ formatMemory(record.clientInfo.totalMemoryMB) }}（可用 {{ formatMemory(record.clientInfo.availableMemoryMB) }}）</p>
                <p>.NET：{{ record.clientInfo.dotnetVersion }}</p>
              </div>
            </template>
            <span class="env-cell">{{ record.clientInfo.os }} {{ record.clientInfo.architecture }}</span>
          </a-tooltip>
        </span>
        <span v-else>-</span>
      </template>
      <template #expand-row="{ record }">
        <div class="expand-content">
          <p v-if="!expandedTunnels[record.id]">{{ $t('client.loading') }}</p>
          <template v-else-if="expandedTunnels[record.id]">
            <p class="expand-title">{{ $t('client.webTunnels') }} ({{ expandedTunnels[record.id]!.webs.length }})：</p>
            <a-tag v-for="w in expandedTunnels[record.id]!.webs" :key="w.id" color="arcoblue" size="small">
              {{ w.subDomain }}
            </a-tag>
            <p v-if="expandedTunnels[record.id]!.webs.length === 0" class="expand-empty">{{ $t('client.none') }}</p>

            <p class="expand-title">{{ $t('client.forwardTunnels') }} ({{ expandedTunnels[record.id]!.forwards.length }})：</p>
            <a-tag v-for="f in expandedTunnels[record.id]!.forwards" :key="f.id" color="orangered" size="small">
              :{{ f.remotePort }}
            </a-tag>
            <p v-if="expandedTunnels[record.id]!.forwards.length === 0" class="expand-empty">{{ $t('client.none') }}</p>
          </template>
        </div>
      </template>
          <template #lastSeen="{ record }">{{ formatDateTime(record.lastSeen) }}</template>
    </a-table>
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
.expand-content {
  padding: 12px 24px;
}
.expand-title {
  margin: 8px 0 4px;
  font-size: 13px;
  color: var(--color-text-2);
}
.expand-empty {
  margin: 2px 0;
  font-size: 13px;
  color: var(--color-text-3);
}
</style>

.env-cell {
  cursor: help;
}

.env-tip p {
  margin: 2px 0;
}
