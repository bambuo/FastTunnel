<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getStatsOverview } from '@/api/stats'
import { getAuditLogs } from '@/api/auditLogs'
import type { StatsOverview } from '@/types/stats'
import type { AuditLogEntity } from '@/types/auditLog'
import { IconDesktop, IconLanguage, IconSwap, IconSafe } from '@arco-design/web-vue/es/icon'

const stats = ref<StatsOverview>({
  onlineClientCount: 0,
  activeWebTunnelCount: 0,
  activeForwardTunnelCount: 0,
  totalTokenCount: 0,
})
const recentLogs = ref<AuditLogEntity[]>([])
const loading = ref(true)

onMounted(async () => {
  try {
    const [statsData, logsData] = await Promise.all([
      getStatsOverview(),
      getAuditLogs({ page: 1, pageSize: 10 }),
    ])
    stats.value = statsData
    recentLogs.value = logsData.items
  } catch {
    // handled
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="dashboard">
    <h1 class="page-title">Dashboard</h1>

    <template v-if="loading">
      <a-row :gutter="16" class="stats-row">
        <a-col v-for="i in 4" :key="i" :xs="12" :lg="6">
          <a-card>
            <a-skeleton animation>
              <a-skeleton-line :rows="2" />
            </a-skeleton>
          </a-card>
        </a-col>
      </a-row>
      <a-card title="最近操作">
        <a-skeleton animation>
          <a-skeleton-line :rows="5" />
        </a-skeleton>
      </a-card>
    </template>

    <template v-else>
      <a-row :gutter="16" class="stats-row">
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconDesktop :size="28" class="stat-icon icon-online" />
              <a-statistic title="在线客户端" :value="stats.onlineClientCount" />
            </div>
          </a-card>
        </a-col>
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconLanguage :size="28" class="stat-icon icon-web" />
              <a-statistic title="活跃 Web 隧道" :value="stats.activeWebTunnelCount" />
            </div>
          </a-card>
        </a-col>
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconSwap :size="28" class="stat-icon icon-forward" />
              <a-statistic title="活跃 Forward 隧道" :value="stats.activeForwardTunnelCount" />
            </div>
          </a-card>
        </a-col>
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconSafe :size="28" class="stat-icon icon-token" />
              <a-statistic title="Token 总数" :value="stats.totalTokenCount" />
            </div>
          </a-card>
        </a-col>
      </a-row>

      <a-card title="最近操作" class="logs-card">
        <a-table
          :columns="[
            { title: '操作', dataIndex: 'action', width: 80 },
            { title: '对象', dataIndex: 'entity', width: 120 },
            { title: '详情', dataIndex: 'detail', ellipsis: true },
            { title: '操作人', dataIndex: 'operator', width: 100 },
            { title: '时间', dataIndex: 'createdAt', width: 160 },
          ]"
          :data="recentLogs"
          :pagination="false"
          row-key="id"
          size="small"
        >
          <template #empty>
            <a-empty description="暂无操作记录" />
          </template>
        </a-table>
      </a-card>
    </template>
  </div>
</template>

<style scoped>
.dashboard {
  max-width: 1200px;
}

.page-title {
  font-size: 20px;
  margin: 0 0 24px;
  color: var(--color-text-1);
}

.stats-row {
  margin-bottom: 24px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 12px;
}

.stat-icon {
  flex-shrink: 0;
}

.icon-online { color: rgb(var(--success-6)); }
.icon-web { color: rgb(var(--primary-6)); }
.icon-forward { color: rgb(var(--warning-6)); }
.icon-token { color: rgb(var(--danger-6)); }

.logs-card {
  width: 100%;
}
</style>
