import request from './request'
import type { ApiResponse } from '@/types/api'
import type { TokenEntity, TokenRequest } from '@/types/token'

export async function getTokens(): Promise<TokenEntity[]> {
  const res = await request.get<ApiResponse<TokenEntity[]>>('/tokens')
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function createToken(data: TokenRequest): Promise<TokenEntity> {
  const res = await request.post<ApiResponse<TokenEntity>>('/tokens', data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function updateToken(id: number, data: TokenRequest): Promise<TokenEntity> {
  const res = await request.put<ApiResponse<TokenEntity>>(`/tokens/${id}`, data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function deleteToken(id: number): Promise<void> {
  const res = await request.delete<ApiResponse<null>>(`/tokens/${id}`)
  if (!res.data.success) throw new Error(res.data.message)
}

export async function toggleToken(id: number, isEnabled: boolean): Promise<{ id: number; isEnabled: boolean }> {
  const res = await request.patch<ApiResponse<{ id: number; isEnabled: boolean }>>(`/tokens/${id}/toggle`, { isEnabled })
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}
