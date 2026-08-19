<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { IconSearch } from '@arco-design/web-vue/es/icon'
import { getTokens, createToken, updateToken, deleteToken, toggleToken } from '@/api/tokens'
import type { TokenEntity, TokenRequest } from '@/types/token'

const { t } = useI18n()

const data = ref<TokenEntity[]>([])
const loading = ref(false)
const modalVisible = ref(false)
const editRecord = ref<TokenEntity | null>(null)
const form = ref({ value: '', description: '' })
const searchKeyword = ref('')
const deletionFilter = ref('active')
const enabledFilter = ref('all')

const filteredData = computed(() => {
  let result = data.value

  // Keyword filter
  if (searchKeyword.value) {
    const kw = searchKeyword.value.toLowerCase()
    result = result.filter(t =>
      t.description.toLowerCase().includes(kw) || t.value.toLowerCase().includes(kw)
    )
  }

  // Deletion status filter
  if (deletionFilter.value === 'active') {
    result = result.filter(t => !t.isDeleted)
  } else if (deletionFilter.value === 'deleted') {
    result = result.filter(t => t.isDeleted)
  }

  // Enabled status filter
  if (enabledFilter.value === 'enabled') {
    result = result.filter(t => t.isEnabled)
  } else if (enabledFilter.value === 'disabled') {
    result = result.filter(t => !t.isEnabled)
  }

  return result
})

const columns = computed(() => [
  { title: t('table.column.tokenValue'), dataIndex: 'value', ellipsis: true, width: 280 },
  { title: t('table.column.description'), dataIndex: 'description', width: 160 },
  { title: t('table.column.status'), slotName: 'status', width: 80 },
  { title: t('table.column.createdAt'), dataIndex: 'createdAt', width: 160 },
  { title: t('table.column.actions'), slotName: 'action', width: 200, fixed: 'right' as const },
])

onMounted(() => fetchList())

async function fetchList() {
  loading.value = true
  try {
    data.value = await getTokens()
  } catch {
    // handled
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  editRecord.value = null
  form.value = { value: '', description: '' }
  modalVisible.value = true
}

function handleEdit(record: TokenEntity) {
  editRecord.value = record
  form.value = { value: record.value, description: record.description }
  modalVisible.value = true
}

async function handleOk() {
  try {
    const req: TokenRequest = { description: form.value.description }
    if (!editRecord.value) {
      if (form.value.value) req.value = form.value.value
      await createToken(req)
      Message.success(t('message.token.created'))
    } else {
      await updateToken(editRecord.value.id, req)
      Message.success(t('message.token.updated'))
    }
    modalVisible.value = false
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : t('message.operationFailed')
    Message.error(msg)
  }
}

async function handleDelete(id: number) {
  try {
    await deleteToken(id)
    Message.success(t('message.token.deleted'))
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : t('message.deleteFailed')
    Message.error(msg)
  }
}

async function handleCopy(val: string) {
  try {
    await navigator.clipboard.writeText(val)
    Message.success(t('message.token.copied'))
  } catch {
    Message.warning(t('message.token.copyFailed'))
  }
}

async function handleToggle(record: TokenEntity) {
  try {
    await toggleToken(record.id, !record.isEnabled)
    Message.success(record.isEnabled ? t('action.disabled') : t('action.enabled'))
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
      <h1 class="page-title">{{ $t('page.tokens') }}</h1>
      <a-button type="primary" @click="handleAdd">{{ $t('action.newToken') }}</a-button>
    </div>

    <div class="search-bar">
      <a-input
        v-model="searchKeyword"
        :placeholder="$t('placeholder.searchToken')"
        allow-clear
        style="width: 320px"
      >
        <template #prefix><IconSearch /></template>
      </a-input>
      <a-select v-model="deletionFilter" style="width: 120px; margin-left: 12px" :placeholder="$t('filter.deletionStatus')">
        <a-option value="all">{{ $t('filter.all') }}</a-option>
        <a-option value="active">{{ $t('filter.notDeleted') }}</a-option>
        <a-option value="deleted">{{ $t('filter.deleted') }}</a-option>
      </a-select>
      <a-select v-model="enabledFilter" style="width: 120px; margin-left: 12px" :placeholder="$t('filter.enabledStatus')">
        <a-option value="all">{{ $t('filter.all') }}</a-option>
        <a-option value="enabled">{{ $t('filter.enabled') }}</a-option>
        <a-option value="disabled">{{ $t('filter.disabled') }}</a-option>
      </a-select>
    </div>

    <a-table :columns="columns" :data="filteredData" :loading="loading" :pagination="{ pageSize: 20, showTotal: true }" row-key="id">
      <template #status="{ record }">
        <a-tag v-if="record.isDeleted" color="red">{{ $t('status.deleted') }}</a-tag>
        <a-switch
          v-else
          :model-value="record.isEnabled"
          size="small"
          @change="() => handleToggle(record)"
        />
      </template>
      <template #action="{ record }">
        <a-space>
          <a-button type="text" size="small" @click="handleCopy(record.value)">{{ $t('action.copy') }}</a-button>
          <a-button type="text" size="small" @click="handleEdit(record)">{{ $t('action.edit') }}</a-button>
          <a-popconfirm v-if="!record.isDeleted" :content="$t('popconfirm.deleteToken')" @ok="() => handleDelete(record.id)">
            <a-button type="text" size="small" status="danger">{{ $t('action.delete') }}</a-button>
          </a-popconfirm>
        </a-space>
      </template>
    </a-table>

    <a-modal v-model:visible="modalVisible" :title="editRecord ? $t('action.edit') + ' Token' : $t('action.newToken')" @ok="handleOk">
      <a-form :model="form" layout="vertical">
        <a-form-item v-if="!editRecord" :label="$t('table.column.tokenValue')">
          <a-input v-model="form.value" :placeholder="$t('placeholder.tokenValue')" />
        </a-form-item>
        <a-form-item :label="$t('table.column.description')">
          <a-input v-model="form.description" :placeholder="$t('placeholder.description')" />
        </a-form-item>
      </a-form>
    </a-modal>
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
  display: flex;
  align-items: center;
}
</style>
