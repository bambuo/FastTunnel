export interface AuditLogEntity {
  id: number
  action: 'create' | 'update' | 'delete' | 'toggle'
  entity: 'web_tunnel' | 'forward_tunnel' | 'token'
  detail: string
  operator: string
  createdAt: string
}
