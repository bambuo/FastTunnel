import request from './request'
import type { ApiResponse } from '@/types/api'

export async function getSetupStatus(): Promise<{ initialized: boolean }> {
  const res = await request.get<ApiResponse<{ initialized: boolean }>>('/setup/status')
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function initSystem(data: { name: string; password: string }): Promise<void> {
  const res = await request.post<ApiResponse<null>>('/setup/init', data)
  if (!res.data.success) throw new Error(res.data.message)
}
