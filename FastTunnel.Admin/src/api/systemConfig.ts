import request from './request'
import type { ApiResponse } from '@/types/api'
import type { SystemConfig, SystemConfigSaveRequest } from '@/types/systemConfig'

export async function getSystemConfig(): Promise<SystemConfig> {
  const res = await request.get<ApiResponse<SystemConfig>>('/system/config')
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function saveSystemConfig(data: SystemConfigSaveRequest): Promise<{ message?: string }> {
  const res = await request.put<ApiResponse<null>>('/system/config', data)
  if (!res.data.success) throw new Error(res.data.message)
  return { message: res.data.message }
}
