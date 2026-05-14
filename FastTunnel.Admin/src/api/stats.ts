import request from './request'
import type { ApiResponse } from '@/types/api'
import type { StatsOverview } from '@/types/stats'

export async function getStatsOverview(): Promise<StatsOverview> {
  const res = await request.get<ApiResponse<StatsOverview>>('/stats/overview')
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}
