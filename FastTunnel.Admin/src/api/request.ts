import axios from 'axios'
import { Message } from '@arco-design/web-vue'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'

const request = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

request.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

request.interceptors.response.use(
  (response) => {
    const { success, message: msg } = response.data ?? {}
    if (success === false) {
      Message.error(msg || '请求失败')
      return Promise.reject(new Error(msg))
    }
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      const authStore = useAuthStore()
      authStore.logout()
      if (router.currentRoute.value.name !== 'Login') {
        router.push('/login')
      }
    }
    if (error.response?.status === 403) {
      Message.error(error.response.data?.message || '权限不足')
    }
    Message.error(error.message || '网络错误')
    return Promise.reject(error)
  },
)

export default request
