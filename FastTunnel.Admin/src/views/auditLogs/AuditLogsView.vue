<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { getAuditLogs } from '@/api/auditLogs'
import type { AuditLogEntity } from '@/types/auditLog'

const data = ref<AuditLogEntity[]>([])
const total = ref(0)
const loading = ref(false)
const page = ref(1)
const pageSize = ref(20)

const filterAction = ref('')
const filterEntity = ref('')
const filterStartDate = ref('')
const filterEndDate = ref('')

const columns = computed(() => [
  { title: '操作', dataIndex: 'action', width: 80 },
  { title: '对象', dataIndex: 'entity', width: 120 },
  { title: '详情', dataIndex: 'detail', ellipsis: true },
  { title: '操作人', dataIndex: 'operator', width: 100 },
  { title: '时间', dataIndex: 'createdAt', width: 180 },
])

const pagination = computed(() => ({
  current: page.value,
  pageSize: pageSize.value,
  total: total.value,
  showTotal: true,
  showPageSize: true,
}))

onMounted(() => fetchList())

async function fetchList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = { page: page.value, pageSize: pageSize.value }
    if (filterAction.value) params.action = filterAction.value
    if (filterEntity.value) params.entity = filterEntity.value
    if (filterStartDate.value) params.startDate = filterStartDate.value
    if (filterEndDate.value) params.endDate = filterEndDate.value
    const res = await getAuditLogs(params)
    data.value = res.items
    total.value = res.total
  } catch {
    // handled
  } finally {
    loading.value = false
  }
}

function handleSearch() { page.value = 1; fetchList() }
function handleReset() {
  filterAction.value = ''
  filterEntity.value = ''
  filterStartDate.value = ''
  filterEndDate.value = ''
  page.value = 1
  fetchList()
}
function handlePageChange(current: number) { page.value = current; fetchList() }
function handlePageSizeChange(size: number) { pageSize.value = size; page.value = 1; fetchList() }
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">审计日志</h1>
    </div>

    <div class="filter-bar">
      <a-space wrap>
        <a-select v-model="filterAction" placeholder="操作类型" allow-clear style="width: 120px" @change="handleSearch" @clear="handleSearch">
          <a-option value="create">create</a-option>
          <a-option value="update">update</a-option>
          <a-option value="delete">delete</a-option>
          <a-option value="toggle">toggle</a-option>
        </a-select>
        <a-select v-model="filterEntity" placeholder="操作对象" allow-clear style="width: 140px" @change="handleSearch" @clear="handleSearch">
          <a-option value="web_tunnel">web_tunnel</a-option>
          <a-option value="forward_tunnel">forward_tunnel</a-option>
          <a-option value="token">token</a-option>
        </a-select>
        <a-date-picker v-model="filterStartDate" placeholder="开始日期" style="width: 160px" @change="handleSearch" @clear="handleSearch" />
        <a-date-picker v-model="filterEndDate" placeholder="结束日期" style="width: 160px" @change="handleSearch" @clear="handleSearch" />
        <a-button @click="handleReset">重置</a-button>
      </a-space>
    </div>

    <a-table
      :columns="columns"
      :data="data"
      :loading="loading"
      :pagination="pagination"
      row-key="id"
      @page-change="handlePageChange"
      @page-size-change="handlePageSizeChange"
    >
      <template #action="{ record }">
        <a-tag
          :color="record.action === 'create' ? 'green' : record.action === 'delete' ? 'red' : record.action === 'toggle' ? 'orange' : 'arcoblue'"
          size="small"
        >
          {{ record.action }}
        </a-tag>
      </template>
      <template #empty>
        <a-empty description="暂无审计日志" />
      </template>
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
.filter-bar {
  margin-bottom: 16px;
}
</style>
