<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { Message } from '@arco-design/web-vue'
import { IconSearch } from '@arco-design/web-vue/es/icon'
import {
  getForwardTunnels, createForwardTunnel, updateForwardTunnel,
  deleteForwardTunnel, toggleForwardTunnel,
} from '@/api/tunnels'
import type { ForwardTunnel, ForwardTunnelRequest } from '@/types/tunnel'
import TunnelForm from '@/components/common/TunnelForm.vue'

const data = ref<ForwardTunnel[]>([])
const loading = ref(false)
const modalVisible = ref(false)
const editTunnel = ref<ForwardTunnel | null>(null)
const searchKeyword = ref('')

const columns = computed(() => [
  { title: '远程端口', dataIndex: 'remotePort', width: 100 },
  { title: '内网地址', slotName: 'address', width: 200 },
  { title: '协议', dataIndex: 'protocol', width: 70 },
  { title: '客户端', dataIndex: 'clientName', width: 120 },
  { title: '状态', slotName: 'status', width: 80 },
  { title: '创建时间', dataIndex: 'createdAt', width: 160 },
  { title: '操作', slotName: 'action', width: 220, fixed: 'right' as const },
])

onMounted(() => fetchList())

async function fetchList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = {}
    if (searchKeyword.value) params.keyword = searchKeyword.value
    data.value = await getForwardTunnels(params)
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

function handleEdit(record: ForwardTunnel) {
  editTunnel.value = record
  modalVisible.value = true
}

async function handleSubmit(formData: ForwardTunnelRequest) {
  try {
    if (formData.remotePort) {
      const exists = data.value.some(
        t => t.remotePort === formData.remotePort && (!editTunnel.value || t.id !== editTunnel.value.id)
      )
      if (exists) {
        Message.error(`端口 ${formData.remotePort} 已被占用`)
        return
      }
    }

    if (editTunnel.value) {
      await updateForwardTunnel(editTunnel.value.id, formData)
      Message.success('隧道更新成功')
    } else {
      await createForwardTunnel(formData)
      Message.success('隧道创建成功')
    }
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : '操作失败'
    Message.error(msg)
  }
}

async function handleDelete(id: number) {
  try {
    await deleteForwardTunnel(id)
    Message.success('隧道已删除')
    await fetchList()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : '删除失败'
    Message.error(msg)
  }
}

async function handleToggle(record: ForwardTunnel) {
  try {
    await toggleForwardTunnel(record.id, !record.isEnabled)
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
      <h1 class="page-title">Forward 隧道管理</h1>
      <a-button type="primary" @click="handleAdd">新建隧道</a-button>
    </div>

    <div class="search-bar">
      <a-input-search
        v-model="searchKeyword"
        placeholder="搜索端口号或内网地址"
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
          <a-button type="text" size="small" @click="handleEdit(record)">编辑</a-button>
          <a-popconfirm content="确定删除此隧道？" @ok="() => handleDelete(record.id)">
            <a-button type="text" size="small" status="danger">删除</a-button>
          </a-popconfirm>
        </a-space>
      </template>
      <template #empty>
        <a-empty description="暂无 Forward 隧道" />
      </template>
    </a-table>

    <TunnelForm
      v-model:visible="modalVisible"
      type="forward"
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
