export interface WebTunnel {
  id: number
  subDomain: string
  localIp: string
  localPort: number
  wwws: string[]
  clientId: number
  clientName?: string
  isEnabled: boolean
  createdAt: string
}

export interface WebTunnelRequest {
  subDomain?: string
  localIp?: string
  localPort?: number
  wwws?: string[]
  clientId?: number
}

export interface ForwardTunnel {
  id: number
  remotePort: number
  localIp: string
  localPort: number
  protocol: 'TCP' | 'UDP'
  clientId: number
  clientName?: string
  isEnabled: boolean
  createdAt: string
}

export interface ForwardTunnelRequest {
  remotePort?: number
  localIp?: string
  localPort?: number
  protocol?: 'TCP' | 'UDP'
  clientId?: number
}
