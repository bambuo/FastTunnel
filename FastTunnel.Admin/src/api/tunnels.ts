import request from './request'
import type { ApiResponse } from '@/types/api'
import type { WebTunnel, WebTunnelRequest, ForwardTunnel, ForwardTunnelRequest } from '@/types/tunnel'

export async function getWebTunnels(params?: Record<string, unknown>): Promise<WebTunnel[]> {
  const res = await request.get<ApiResponse<WebTunnel[]>>('/tunnels/webs', { params })
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function createWebTunnel(data: WebTunnelRequest): Promise<WebTunnel> {
  const res = await request.post<ApiResponse<WebTunnel>>('/tunnels/webs', data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function updateWebTunnel(id: number, data: WebTunnelRequest): Promise<WebTunnel> {
  const res = await request.put<ApiResponse<WebTunnel>>(`/tunnels/webs/${id}`, data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function deleteWebTunnel(id: number): Promise<void> {
  const res = await request.delete<ApiResponse<null>>(`/tunnels/webs/${id}`)
  if (!res.data.success) throw new Error(res.data.message)
}

export async function toggleWebTunnel(id: number, isEnabled: boolean): Promise<{ id: number; isEnabled: boolean }> {
  const res = await request.patch<ApiResponse<{ id: number; isEnabled: boolean }>>(`/tunnels/webs/${id}/toggle`, { isEnabled })
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function getForwardTunnels(params?: Record<string, unknown>): Promise<ForwardTunnel[]> {
  const res = await request.get<ApiResponse<ForwardTunnel[]>>('/tunnels/forwards', { params })
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function createForwardTunnel(data: ForwardTunnelRequest): Promise<ForwardTunnel> {
  const res = await request.post<ApiResponse<ForwardTunnel>>('/tunnels/forwards', data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function updateForwardTunnel(id: number, data: ForwardTunnelRequest): Promise<ForwardTunnel> {
  const res = await request.put<ApiResponse<ForwardTunnel>>(`/tunnels/forwards/${id}`, data)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function deleteForwardTunnel(id: number): Promise<void> {
  const res = await request.delete<ApiResponse<null>>(`/tunnels/forwards/${id}`)
  if (!res.data.success) throw new Error(res.data.message)
}

export async function toggleForwardTunnel(id: number, isEnabled: boolean): Promise<{ id: number; isEnabled: boolean }> {
  const res = await request.patch<ApiResponse<{ id: number; isEnabled: boolean }>>(`/tunnels/forwards/${id}/toggle`, { isEnabled })
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}
