<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { Message } from '@arco-design/web-vue'
import { IconSearch } from '@arco-design/web-vue/es/icon'
import { getTokens, createToken, updateToken, deleteToken, toggleToken } from '@/api/tokens'
import type { TokenEntity, TokenRequest } from '@/types/token'

const data = ref<TokenEntity[]>([])
const loading = ref(false)
const modalVisible = ref(false)
const editRecord = ref<TokenEntity | null>(null)
const form = ref({ value: '', description: '' })
const searchKeyword = ref('')

const filteredData = computed(() => {
  if (!searchKeyword.value) return data.value
  const kw = searchKeyword.value.toLowerCase()
  return data.value.filter(t =>
    t.description.toLowerCase().includes(kw) || t.value.toLowerCase().includes(kw)
  )
})

const columns = computed(() => [
  { title: 'Token 值', dataIndex: 'value', ellipsis: true, width: 280 },
  { title: '备注', dataIndex: 'description', width: 160 },
  { title: '引用客户端数', dataIndex: 'clientCount', width: 100 },
  { title: '状态', slotName: 'status', width: 80 },
  { title: '创建时间', dataIndex: 'createdAt', width: 160 },
  { title: '操作', slotName: 'action', width: 200, fixed: 'right' as const },
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
      Message.success('Token 创建成功')
    } else {
      await updateToken(editRecord.value.id, req)
      Message.success('Token 更新成功')
    }
    modalVisible.value = false
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : '操作失败'
    Message.error(msg)
  }
}

async function handleDelete(id: number) {
  try {
    await deleteToken(id)
    Message.success('Token 已删除')
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : '删除失败'
    Message.error(msg)
  }
}

async function handleCopy(val: string) {
  try {
    await navigator.clipboard.writeText(val)
    Message.success('已复制到剪贴板')
  } catch {
    Message.warning('复制失败')
  }
}

async function handleToggle(record: TokenEntity) {
  try {
    await toggleToken(record.id, !record.isEnabled)
    Message.success(record.isEnabled ? '已停用' : '已启用')
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : '操作失败'
    Message.error(msg)
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">Token 管理</h1>
      <a-button type="primary" @click="handleAdd">新建 Token</a-button>
    </div>

    <div class="search-bar">
      <a-input
        v-model="searchKeyword"
        placeholder="搜索备注或 Token 值"
        allow-clear
        style="width: 320px"
      >
        <template #prefix><IconSearch /></template>
      </a-input>
    </div>

    <a-table :columns="columns" :data="filteredData" :loading="loading" :pagination="{ pageSize: 20, showTotal: true }" row-key="id">
      <template #status="{ record }">
        <a-switch
          :model-value="record.isEnabled"
          size="small"
          @change="() => handleToggle(record)"
        />
      </template>
      <template #action="{ record }">
        <a-space>
          <a-button type="text" size="small" @click="handleCopy(record.value)">复制</a-button>
          <a-button type="text" size="small" @click="handleEdit(record)">编辑</a-button>
          <a-popconfirm content="确定删除？" @ok="() => handleDelete(record.id)">
            <a-button type="text" size="small" status="danger">删除</a-button>
          </a-popconfirm>
        </a-space>
      </template>
    </a-table>

    <a-modal v-model:visible="modalVisible" :title="editRecord ? '编辑 Token' : '新建 Token'" @ok="handleOk">
      <a-form :model="form" layout="vertical">
        <a-form-item v-if="!editRecord" label="Token 值">
          <a-input v-model="form.value" placeholder="留空则自动生成 UUID" />
        </a-form-item>
        <a-form-item label="备注">
          <a-input v-model="form.description" placeholder="如：办公室服务器" />
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
}
</style>
