<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { use, init } from 'echarts/core'
import { LineChart } from 'echarts/charts'
import { GridComponent, TooltipComponent, LegendComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import type { ECharts } from 'echarts/core'
import type { TrafficResponse } from '@/types/stats'
import { formatDateTime } from '@/utils/format'

use([LineChart, GridComponent, TooltipComponent, LegendComponent, CanvasRenderer])

const props = defineProps<{
  traffic: TrafficResponse | null
}>()

const chartRef = ref<HTMLDivElement | null>(null)
let chart: ECharts | null = null
let resizeObserver: ResizeObserver | null = null

function formatBytes(bytes: number): string {
  if (!bytes) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let i = 0
  let v = bytes
  while (v >= 1024 && i < units.length - 1) { v /= 1024; i++ }
  return `${v.toFixed(v >= 100 || i === 0 ? 0 : 1)} ${units[i]}`
}

function render() {
  if (!chartRef.value || !props.traffic) return
  if (!chart) {
    chart = init(chartRef.value)
  }
  const data = props.traffic
  chart.setOption({
    tooltip: {
      trigger: 'axis',
      valueFormatter: (value: unknown) => formatBytes(Number(value) || 0),
    },
    legend: { data: data.series.map(s => s.label), top: 0 },
    grid: { left: 16, right: 16, top: 40, bottom: 8, containLabel: true },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: data.hours.map(h => formatDateTime(h)),
      axisLabel: { fontSize: 11 },
    },
    yAxis: {
      type: 'value',
      axisLabel: { fontSize: 11, formatter: (v: number) => formatBytes(v) },
      splitLine: { lineStyle: { type: 'dashed' } },
    },
    series: data.series.map(s => ({
      name: s.label,
      type: 'line',
      smooth: true,
      showSymbol: false,
      data: s.data,
    })),
  }, true)
}

watch(() => props.traffic, () => {
  if (props.traffic) render()
})

onMounted(() => {
  if (props.traffic) {
    render()
    resizeObserver = new ResizeObserver(() => chart?.resize())
    if (chartRef.value) resizeObserver.observe(chartRef.value)
  }
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  chart?.dispose()
  chart = null
})
</script>

<template>
  <div ref="chartRef" class="traffic-chart"></div>
</template>

<style scoped>
.traffic-chart {
  width: 100%;
  height: 300px;
}
</style>
