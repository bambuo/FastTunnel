export interface JwtConfig {
  clockSkew: number
  validAudience: string
  validIssuer: string
  issuerSigningKey: string
  expires: number
}

export interface SystemConfig {
  enableForward: boolean
  webDomain: string
  webAllowAccessIps: string[]
  jwt: JwtConfig
}

export interface SystemConfigSaveRequest {
  enableForward: boolean
  webDomain?: string
  webAllowAccessIps?: string[]
  clockSkew: number
  validAudience?: string
  validIssuer?: string
  issuerSigningKey: string
  expires: number
}
