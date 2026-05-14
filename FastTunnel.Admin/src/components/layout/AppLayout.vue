<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import {
  IconDashboard, IconLanguage, IconSwap, IconSafe,
  IconDesktop, IconFile, IconMenuFold, IconMenuUnfold,
} from '@arco-design/web-vue/es/icon'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const collapsed = ref(false)

const menuItems = [
  { key: 'Dashboard', icon: IconDashboard, label: 'Dashboard', path: '/' },
  { key: 'WebTunnels', icon: IconLanguage, label: 'Web 隧道', path: '/tunnels/web' },
  { key: 'ForwardTunnels', icon: IconSwap, label: 'Forward 隧道', path: '/tunnels/forward' },
  { key: 'Tokens', icon: IconSafe, label: 'Token 管理', path: '/tokens' },
  { key: 'Clients', icon: IconDesktop, label: '客户端', path: '/clients' },
  { key: 'AuditLogs', icon: IconFile, label: '审计日志', path: '/audit-logs' },
]

const selectedKey = () => {
  const name = route.name as string
  if (name === 'Dashboard' || name === 'WebTunnels' || name === 'ForwardTunnels' ||
      name === 'Tokens' || name === 'Clients' || name === 'AuditLogs') {
    return name
  }
  return 'Dashboard'
}

function handleMenuClick(key: string) {
  const item = menuItems.find(m => m.key === key)
  if (item) router.push(item.path)
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <a-layout class="app-layout">
    <a-layout-sider
      :collapsed="collapsed"
      collapsible
      :trigger="null"
      breakpoint="lg"
      @collapse="collapsed = $event"
    >
      <div class="logo">
        <span v-if="!collapsed">FastTunnel</span>
        <span v-else>FT</span>
      </div>
      <a-menu
        :selected-keys="[selectedKey()]"
        :collapsed="collapsed"
        @menu-item-click="(key: string) => handleMenuClick(key)"
      >
        <a-menu-item v-for="item in menuItems" :key="item.key">
          <template #icon><component :is="item.icon" /></template>
          {{ item.label }}
        </a-menu-item>
      </a-menu>
    </a-layout-sider>

    <a-layout>
      <a-layout-header class="app-header">
        <div class="header-left">
          <a-button
            type="text"
            :icon="collapsed ? IconMenuUnfold : IconMenuFold"
            @click="collapsed = !collapsed"
          />
          <a-breadcrumb>
            <a-breadcrumb-item v-for="item in $route.matched" :key="item.path">
              {{ item.meta?.title || item.name }}
            </a-breadcrumb-item>
          </a-breadcrumb>
        </div>
        <div class="header-right">
          <span class="username">{{ authStore.username }}</span>
          <a-button type="text" size="small" @click="handleLogout">退出</a-button>
        </div>
      </a-layout-header>

      <a-layout-content class="app-content">
        <router-view />
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
}

.logo {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-1);
  font-size: 18px;
  font-weight: 600;
  border-bottom: 1px solid var(--color-border-2);
}

.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  height: 56px;
  background: var(--color-bg-1);
  border-bottom: 1px solid var(--color-border-2);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.username {
  font-size: 13px;
  color: var(--color-text-2);
}

.app-content {
  padding: 24px;
  overflow-y: auto;
}
</style>
