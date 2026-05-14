import request from './request'
import type { ApiResponse } from '@/types/api'
import type { ClientEntity, ClientTunnels } from '@/types/client'

export async function getClients(): Promise<ClientEntity[]> {
  const res = await request.get<ApiResponse<ClientEntity[]>>('/clients')
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function getOnlineClientCount(): Promise<number> {
  const res = await request.get<ApiResponse<number>>('/clients/online/count')
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}

export async function getClientTunnels(id: number): Promise<ClientTunnels> {
  const res = await request.get<ApiResponse<ClientTunnels>>(`/clients/${id}/tunnels`)
  if (!res.data.success) throw new Error(res.data.message)
  return res.data.data
}
