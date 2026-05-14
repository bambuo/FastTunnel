import request from './request'
import type { ApiResponse } from '@/types/api'
import type { LoginRequest, LoginResponse } from '@/types/auth'

export async function login(data: LoginRequest): Promise<LoginResponse> {
  const res = await request.post<ApiResponse<LoginResponse>>('/account/token', data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}
