import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const username = ref('')
  const preAuthToken = ref<string | null>(null)
  const mfaBound = ref<boolean | null>(null)

  const isAuthenticated = computed(() => token.value !== null)
  const needsMfa = computed(() => preAuthToken.value !== null && token.value === null)

  function setPreAuth(newToken: string, name: string, bound: boolean) {
    preAuthToken.value = newToken
    username.value = name
    mfaBound.value = bound
  }

  function setAuth(newToken: string, name: string) {
    token.value = newToken
    username.value = name
    preAuthToken.value = null
    mfaBound.value = null
    localStorage.setItem('token', newToken)
  }

  function logout() {
    token.value = null
    username.value = ''
    preAuthToken.value = null
    mfaBound.value = null
    localStorage.removeItem('token')
  }

  return { token, username, preAuthToken, mfaBound, isAuthenticated, needsMfa, setPreAuth, setAuth, logout }
})
