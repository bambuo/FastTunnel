# FastTunnel Admin — 前端管理面板

> FastTunnel 内网穿透管理面板，提供 Web 界面对隧道、Token、客户端进行可视化管理。

---

## 技术栈

| 层 | 选型 |
|----|------|
| 运行时 & 包管理 | Bun |
| 框架 | Vue 3 (Composition API + `<script setup>`) |
| 语言 | TypeScript |
| UI 组件库 | Arco Design Vue (`@arco-design/web-vue`) |
| 状态管理 | Pinia (Setup Store) |
| 路由 | Vue Router 4 |
| 构建 | Vite |
| HTTP | Axios |
| 测试 | Vitest + Vue Test Utils + Playwright |

---

## 文档索引

| 编号 | 文档 | 说明 |
|------|------|------|
| 01 | [需求文档](./01-需求文档.md) | 首次部署初始化、MFA 两步验证、功能需求、用户故事 |
| 02 | [设计规范](./02-设计规范.md) | 布局、色彩、字体、间距、组件用法、响应式 |
| 03 | [代码规范](./03-代码规范.md) | 项目结构、Vue 规范、TS 规范、Pinia、命名、禁止事项 |
| 04 | [接口文档](./04-接口文档.md) | REST API 完整定义（含初始化接口）、类型汇总、拦截器行为 |
| 05 | [测试规范](./05-测试规范.md) | 测试金字塔、Vitest 用例、组件测试、Playwright E2E |

---

## 文档阅读顺序（按角色）

### 开发者（新加入）

```
03 代码规范 → 04 接口文档 → 01 需求文档 → 02 设计规范 → 05 测试规范
```

### 后端开发者（写 API）

```
04 接口文档
```

### 设计师

```
02 设计规范 → 01 需求文档
```

### QA / 测试

```
01 需求文档 → 05 测试规范 → 04 接口文档
```

---

## 快速开始

```bash
# 安装依赖
bun install

# 启动开发服务器
bun run dev

# 构建生产包
bun run build

# 运行单元测试
bun run test

# 运行 E2E 测试
bun run test:e2e
```

---

## 页面清单

| 页面 | 路由 | 状态 |
|------|------|------|
| 初始化 | `/setup` | 规划中 |
| 登录 | `/login` | 规划中 |
| Dashboard | `/` | 规划中 |
| Web 隧道管理 | `/tunnels/web` | 规划中 |
| Forward 隧道管理 | `/tunnels/forward` | 规划中 |
| Token 管理 | `/tokens` | 规划中 |
| 客户端管理 | `/clients` | 规划中 |
| 审计日志 | `/audit-logs` | 规划中 |

---

## 约定速查

| 类别 | 约定 |
|------|------|
| 组件文件 | PascalCase, `XxxView.vue` / `XxxForm.vue` |
| Composable | `useXxx.ts` |
| Store | `useXxxStore`（含 `useSetupStore` 管理初始化状态） |
| API 函数 | `getXxx()`, `createXxx()`, `updateXxx()`, `deleteXxx()` |
| 类型 | `interface Xxx { }` |
| 路由 name | PascalCase: `WebTunnels` |
| SFC 顺序 | `<script setup lang="ts">` → `<template>` → `<style scoped>` |
| 样式 | Arco Design Token 变量，不硬编码色值 |
| Props | TypeScript 泛型 `defineProps<{}>()` |
| Store 解构 | 必须用 `storeToRefs()` |
