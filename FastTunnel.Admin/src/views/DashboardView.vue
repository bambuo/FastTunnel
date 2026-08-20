<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { getStatsOverview, getTokenTraffic } from '@/api/stats'
import { getAuditLogs } from '@/api/auditLogs'
import type { StatsOverview, TrafficResponse } from '@/types/stats'
import type { AuditLogEntity } from '@/types/auditLog'
import { IconDesktop, IconLanguage, IconSwap, IconSafe } from '@arco-design/web-vue/es/icon'
import { formatDateTime } from '@/utils/format'
import TrafficChart from '@/components/charts/TrafficChart.vue'

const { t } = useI18n()

const stats = ref<StatsOverview>({
  onlineClientCount: 0,
  activeWebTunnelCount: 0,
  activeForwardTunnelCount: 0,
  totalTokenCount: 0,
})
const recentLogs = ref<AuditLogEntity[]>([])
const loading = ref(true)
const traffic = ref<TrafficResponse | null>(null)

onMounted(async () => {
  try {
    const [statsData, logsData, trafficData] = await Promise.all([
      getStatsOverview(),
      getAuditLogs({ page: 1, pageSize: 10 }),
      getTokenTraffic(24),
    ])
    stats.value = statsData
    recentLogs.value = logsData.items
    traffic.value = trafficData
  } catch {
    // handled
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="dashboard">
    <h1 class="page-title">{{ $t('dashboard.title') }}</h1>

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
      <a-card :title="$t('dashboard.recentOps')">
        <a-skeleton animation>
          <a-skeleton-line :rows="5" />
        </a-skeleton>
      </a-card>
      <a-card :title="$t('dashboard.traffic')" class="traffic-card">
        <a-skeleton animation>
          <a-skeleton-line :rows="6" />
        </a-skeleton>
      </a-card>
    </template>

    <template v-else>
      <a-row :gutter="16" class="stats-row">
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconDesktop :size="28" class="stat-icon icon-online" />
              <a-statistic :title="$t('dashboard.stat.onlineClients')" :value="stats.onlineClientCount" />
            </div>
          </a-card>
        </a-col>
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconLanguage :size="28" class="stat-icon icon-web" />
              <a-statistic :title="$t('dashboard.stat.activeWebTunnels')" :value="stats.activeWebTunnelCount" />
            </div>
          </a-card>
        </a-col>
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconSwap :size="28" class="stat-icon icon-forward" />
              <a-statistic :title="$t('dashboard.stat.activeForwardTunnels')" :value="stats.activeForwardTunnelCount" />
            </div>
          </a-card>
        </a-col>
        <a-col :xs="12" :lg="6">
          <a-card>
            <div class="stat-item">
              <IconSafe :size="28" class="stat-icon icon-token" />
              <a-statistic :title="$t('dashboard.stat.totalTokens')" :value="stats.totalTokenCount" />
            </div>
          </a-card>
        </a-col>
      </a-row>

      <a-card v-if="traffic && traffic.series.length > 0" :title="$t('dashboard.traffic')" class="traffic-card">
        <TrafficChart :traffic="traffic" />
      </a-card>

      <a-card :title="$t('dashboard.recentOps')" class="logs-card">
        <a-table
          :columns="[
            { title: t('table.column.actions'), dataIndex: 'action', width: 80 },
            { title: t('table.column.entity'), dataIndex: 'entity', width: 110 },
            { title: t('table.column.detail'), dataIndex: 'detail', ellipsis: true, width: 260 },
            { title: t('table.column.operator'), dataIndex: 'operator', width: 120, ellipsis: true },
            { title: t('table.column.time'), slotName: 'createdAt', width: 170 },
          ]"
          :data="recentLogs"
          :pagination="false"
          row-key="id"
          size="small"
        >
          <template #empty>
            <a-empty :description="$t('dashboard.table.empty')" />
          </template>
              <template #createdAt="{ record }">{{ formatDateTime(record.createdAt) }}</template>
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

.stat-item :deep(.arco-statistic) {
  flex: 1;
  min-width: 0;
}

.stat-item :deep(.arco-statistic-title) {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
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

.traffic-card {
  width: 100%;
  margin-bottom: 24px;
}

</style>
