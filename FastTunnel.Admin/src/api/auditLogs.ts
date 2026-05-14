import request from './request'
import type { ApiResponse, PaginatedResult } from '@/types/api'
import type { AuditLogEntity } from '@/types/auditLog'

export async function getAuditLogs(params?: Record<string, unknown>): Promise<PaginatedResult<AuditLogEntity>> {
  const res = await request.get<ApiResponse<PaginatedResult<AuditLogEntity>>>('/audit-logs', { params })
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}
