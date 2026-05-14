export interface MfaSetupResponse {
  secret: string
  qrCodeUrl: string
}

export interface MfaBindRequest {
  code: string
}

export interface MfaVerifyRequest {
  code: string
}

export interface MfaCompleteResponse {
  token: string
  username: string
}
