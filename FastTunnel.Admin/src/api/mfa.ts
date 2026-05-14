import axios from 'axios'
import type { ApiResponse } from '@/types/api'
import type { MfaSetupResponse, MfaCompleteResponse } from '@/types/mfa'

function mfaRequest(preAuthToken: string) {
  return axios.create({
    baseURL: '/api',
    timeout: 10000,
    headers: { Authorization: `Bearer ${preAuthToken}` },
  })
}

export async function setupMfa(preAuthToken: string): Promise<ApiResponse<MfaSetupResponse>> {
  const req = mfaRequest(preAuthToken)
  const res = await req.post<ApiResponse<MfaSetupResponse>>('/account/mfa/setup')
  return res.data
}

export async function bindMfa(code: string, preAuthToken: string): Promise<ApiResponse<MfaCompleteResponse>> {
  const req = mfaRequest(preAuthToken)
  const res = await req.post<ApiResponse<MfaCompleteResponse>>('/account/mfa/bind', { code })
  return res.data
}

export async function verifyMfa(code: string, preAuthToken: string): Promise<ApiResponse<MfaCompleteResponse>> {
  const req = mfaRequest(preAuthToken)
  const res = await req.post<ApiResponse<MfaCompleteResponse>>('/account/mfa/verify', { code })
  return res.data
}
