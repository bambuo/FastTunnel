export interface WebTunnel {
  id: number
  subDomain: string
  localIp: string
  localPort: number
  wwws: string[]
  clientToken: string
  clientName?: string
  isEnabled: boolean
  createdAt: string
}

export interface WebTunnelRequest {
  subDomain?: string
  localIp?: string
  localPort?: number
  wwws?: string[]
  clientToken?: string
}

export interface ForwardTunnel {
  id: number
  remotePort: number
  localIp: string
  localPort: number
  protocol: 'TCP' | 'UDP'
  clientToken: string
  clientName?: string
  isEnabled: boolean
  createdAt: string
}

export interface ForwardTunnelRequest {
  remotePort?: number
  localIp?: string
  localPort?: number
  protocol?: 'TCP' | 'UDP'
  clientToken?: string
}
