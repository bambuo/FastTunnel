export interface TokenEntity {
  id: number
  value: string
  description: string
  isEnabled: boolean
  isDeleted: boolean
  createdAt: string
}

export interface TokenRequest {
  value?: string
  description?: string
}
