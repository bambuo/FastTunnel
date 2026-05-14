import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Message } from '@arco-design/web-vue'
import { useAuthStore } from '@/stores/auth'
import { login } from '@/api/auth'
import { setupMfa, bindMfa, verifyMfa } from '@/api/mfa'

export function useMfaLogin() {
  const router = useRouter()
  const authStore = useAuthStore()

  const form = ref({ name: '', password: '' })
  const totpCode = ref('')
  const step = ref<'credentials' | 'mfaSetup' | 'mfaVerify'>('credentials')

  const secret = ref('')
  const qrCodeUrl = ref('')
  const mfaCountdown = ref(300)
  let countdownTimer: ReturnType<typeof setInterval> | null = null

  function clearCountdown() {
    if (countdownTimer) {
      clearInterval(countdownTimer)
      countdownTimer = null
    }
  }

  function startCountdown() {
    clearCountdown()
    mfaCountdown.value = 300
    countdownTimer = setInterval(() => {
      mfaCountdown.value--
      if (mfaCountdown.value <= 0) {
        clearCountdown()
        backToLogin()
        Message.warning('预认证令牌已过期，请重新登录')
      }
    }, 1000)
  }

  async function handleLogin() {
    try {
      const data = await login({ name: form.value.name, password: form.value.password })
      authStore.setPreAuth(data.preAuthToken, data.username, data.mfaBound)
      startCountdown()

      if (data.mfaBound) {
        step.value = 'mfaVerify'
      } else {
        const mfaData = await setupMfa(data.preAuthToken)
        secret.value = mfaData.data.secret
        qrCodeUrl.value = mfaData.data.qrCodeUrl
        step.value = 'mfaSetup'
      }
    } catch {
      // handled by interceptor
    }
  }

  async function handleBindMfa() {
    try {
      const data = await bindMfa(totpCode.value, authStore.preAuthToken!)
      authStore.setAuth(data.data.token, data.data.username)
      clearCountdown()
      Message.success('MFA 绑定成功')
      router.push('/')
    } catch {
      totpCode.value = ''
    }
  }

  async function handleVerifyMfa() {
    try {
      const data = await verifyMfa(totpCode.value, authStore.preAuthToken!)
      authStore.setAuth(data.data.token, data.data.username)
      clearCountdown()
      router.push('/')
    } catch {
      totpCode.value = ''
    }
  }

  function backToLogin() {
    clearCountdown()
    authStore.logout()
    step.value = 'credentials'
    totpCode.value = ''
  }

  return {
    form, totpCode, step, secret, qrCodeUrl, mfaCountdown,
    handleLogin, handleBindMfa, handleVerifyMfa, backToLogin,
  }
}
