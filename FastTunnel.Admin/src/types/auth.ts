export interface LoginRequest {
  name: string
  password: string
}

export interface LoginResponse {
  requiresMfa: true
  mfaBound: boolean
  preAuthToken: string
  username: string
}
