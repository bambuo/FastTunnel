import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useSetupStore } from '@/stores/setup'
import { useAuthStore } from '@/stores/auth'

const routes: RouteRecordRaw[] = [
  {
    path: '/setup',
    name: 'Setup',
    component: () => import('@/views/SetupView.vue'),
    meta: { requiresSetup: true, title: 'Setup' },
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: { requiresAuth: false, title: 'Login' },
  },
  {
    path: '/',
    component: () => import('@/components/layout/AppLayout.vue'),
    meta: { requiresAuth: true, titleKey: 'nav.brand' },
    children: [
      { path: '', name: 'Dashboard', component: () => import('@/views/DashboardView.vue'), meta: { titleKey: 'nav.dashboard' } },
      { path: 'tunnels/web', name: 'WebTunnels', component: () => import('@/views/tunnels/WebTunnelsView.vue'), meta: { titleKey: 'nav.webTunnels' } },
      { path: 'tunnels/forward', name: 'ForwardTunnels', component: () => import('@/views/tunnels/ForwardTunnelsView.vue'), meta: { titleKey: 'nav.forwardTunnels' } },
      { path: 'tokens', name: 'Tokens', component: () => import('@/views/tokens/TokensView.vue'), meta: { titleKey: 'nav.tokens' } },
      { path: 'clients', name: 'Clients', component: () => import('@/views/clients/ClientsView.vue'), meta: { titleKey: 'nav.clients' } },
      { path: 'audit-logs', name: 'AuditLogs', component: () => import('@/views/auditLogs/AuditLogsView.vue'), meta: { titleKey: 'nav.auditLogs' } },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to, _from) => {
  const setupStore = useSetupStore()

  if (!setupStore.loading && setupStore.initialized === null) {
    await setupStore.checkStatus()
  }

  // 未初始化：只放行 /setup，其余全部重定向
  if (setupStore.initialized === false) {
    if (to.name !== 'Setup') return { name: 'Setup' }
    return
  }

  // 已初始化：拦截 /setup 跳登录
  if (setupStore.initialized === true && to.name === 'Setup') {
    return { name: 'Login' }
  }

  // 未确认初始化状态 (API 不可达)：放行 /setup 和 /login，其余走 Auth 检查
  if (setupStore.initialized === null) {
    if (to.name === 'Setup' || to.name === 'Login') return
  }

  const authStore = useAuthStore()
  if (to.meta.requiresAuth !== false && !authStore.isAuthenticated) {
    return { name: 'Login', query: { redirect: to.fullPath } }
  }

  if (to.name === 'Login' && authStore.isAuthenticated) {
    return { name: 'Dashboard' }
  }
})

export default router
