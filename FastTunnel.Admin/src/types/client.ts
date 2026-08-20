export interface ClientInfo {
  os: string
  osVersion: string
  architecture: string
  cpuCores: number
  availableMemoryMB: number
  dotnetVersion: string
}

export interface ClientEntity {
  id: number
  name: string
  ip: string
  token: string
  tokenPreview: string
  isOnline: boolean
  lastSeen: string
  clientInfo?: ClientInfo | null
}

export interface ClientTunnels {
  webs: { id: number; subDomain: string; isEnabled: boolean }[]
  forwards: { id: number; remotePort: number; isEnabled: boolean }[]
}
