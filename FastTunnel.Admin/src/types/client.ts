export interface ClientEntity {
  id: number
  name: string
  token: string
  tokenPreview: string
  isOnline: boolean
  lastSeen: string
}

export interface ClientTunnels {
  webs: { id: number; subDomain: string; isEnabled: boolean }[]
  forwards: { id: number; remotePort: number; isEnabled: boolean }[]
}
