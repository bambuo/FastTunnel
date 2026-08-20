## 功能：总览页新增"按 Token 的流量折线图"

**架构**：Core 新增按 Token 的内存滑动窗口流量统计（24 个 1 小时桶，仿 WebList/ForwardList 挂到 FastTunnelServer 单例）；TCP/UDP/Web 三条转发链路在数据桥接处计数（Token 从 ApplyForwardConfig/ApplyWebConfig 透传）；StatsController 新增流量接口；前端引入 echarts 画多折线图（每 Token 一条线，近 24 小时）。

### 改动清单

**FastTunnel.Core（流量采集）**
1. **新建 `Models/TrafficStats.cs`**：按 Token 的内存聚合——每 Token 24 个 1 小时桶（lock 保护，跨小时自动滚动清零过期桶）；`Add(token, bytes)`、`Snapshot(hours)` 返回每 Token 最近 N 桶（时间顺序）
2. **新建 `Utilitys/CountingStream.cs`**：包装 Stream，Read/Write 均计数（双向合计），转发到 TrafficStats
3. `Client/FastTunnelServer.cs`：加 `TrafficStats Traffic { get; }`；`ApplyForwardConfig` 创建 `ForwardDispatcher`/`UdpProxyListener` 时透传 `client.Token`；`ApplyWebConfig` 创建 WebInfo 时写 `Token = client.Token`
4. `Models/WebInfo.cs`：加 `Token` 属性
5. `Handlers/ForwardDispatcher.cs`：主构造加 `string token` 参数；`DispatchAsync` 把隧道流 `stream1` 包 `CountingStream`（双向 Copy 都被计数）；暴露 `Traffic` 供 UDP 会话使用
6. `Listener/UdpProxyListener.cs`：构造加 token 参数并传给 `UdpSession`
7. `Listener/UdpSession.cs`：构造加 token；`SendLoopAsync` 写 payload 时计数、`ReceiveLoopAsync` 收到数据时计数（经 dispatcher.Traffic）
8. `Forwarder/FastTunnelForwarderHttpClientFactory.cs`：`ProxyAsync` 返回前把隧道流包 `CountingStream`（token 从 `web.Token` 取）

**FastTunnel.Api（统计接口）**
9. `Controllers/StatsController.cs`：加 `GET /api/stats/traffic?hours=24` → `{ hours: string[], series: [{ token, label(掩码), data: number[] }] }`（只返回近窗口内有流量的 Token）

**FastTunnel.Admin（前端）**
10. `bun add echarts`
11. `src/types/stats.ts`：加流量响应类型；`src/api/stats.ts`：加 `getTokenTraffic()`
12. `views/DashboardView.vue`：统计卡片区下方插入图表卡片（全宽 a-card + 300px 容器），echarts 多折线（x 轴小时、每 Token 一条线、系列名用 Token 掩码、tooltip 字节格式化 KB/MB/GB、ResizeObserver 自适应）；数据加载并入现有 `Promise.all`；loading 分支同步加骨架卡片
13. i18n 四语（zh-CN/en-US/eo/vi）：`dashboard.traffic` 标题词条

### 行为说明
- 统计窗口：近 24 小时、小时粒度，**进程内存**保存（服务重启清零）
- 计数范围：端口转发（TCP/UDP）与网站隧道（Web）的实际数据字节（双向合计）；控制消息不计
- Token 掩码显示（与令牌管理页一致）；无流量的 Token 不出现在图中

### 验证
1. `dotnet build FastTunnel.slnx` + 前端 `bun run build` 通过
2. 冒烟：起 server + client，创建 TCP/UDP/Web 隧道 → curl 与 UDP 发包制造流量 → `/api/stats/traffic` 返回对应 Token 的字节数据 → 前端图表渲染多折线；停客户端再连验证统计按 Token 隔离
3. 前端 dev 模式目视图表（tooltip、resize、i18n 切换）