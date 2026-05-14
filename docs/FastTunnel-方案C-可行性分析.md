# FastTunnel 方案C 可行性分析与技术选型报告

## 零、从现状到目标

```mermaid
graph TB
    subgraph 当前架构["当前：纯文件的配置系统"]
        JSON1["appsettings.json<br/>(服务端)"]
        JSON2["appsettings.json<br/>(客户端)"]
        JSON1 -->|"启动时加载"| SERVER["FastTunnel.Server"]
        JSON2 -->|"启动时加载"| CLIENT["FastTunnel.Client"]
        USER["管理员"] -->|"手动编辑 JSON"| JSON1
        USER -->|"手动编辑 JSON"| JSON2
    end

    subgraph 目标架构["方案C：数据库 + API + 管理面板"]
        DB[("SQLite<br/>持久化存储")]
        API["REST API<br/>(CRUD 穿透/Token/配置)"]
        ADMIN["Web 管理面板<br/>(SPA Dashboard)"]
        SIGNAL["WebSocket 信令通道<br/>(服务端→客户端推送)"]

        USER2["管理员"] -->|"浏览器操作"| ADMIN
        ADMIN -->|"HTTP 请求"| API
        API -->|"读写"| DB
        API -->|"推送变更"| SIGNAL
        SIGNAL -->|"通知新配置"| CLIENT2["FastTunnel.Client"]
        DB -->|"启动时加载"| SERVER2["FastTunnel.Server"]
    end

    当前架构 -.->|"迁移"| 目标架构
```

---

## 一、实现路径拆解

```mermaid
flowchart LR
    subgraph Phase1["Phase 1 · 基础设施"]
        P1A["新增 FastTunnel.Domain<br/>实体模型层"]
        P1B["新增 FastTunnel.Infrastructure<br/>EF Core + 数据库"]
        P1C["配置启动迁移<br/>自动建库建表"]
    end

    subgraph Phase2["Phase 2 · API 写操作"]
        P2A["Token CRUD API"]
        P2B["Web隧道 CRUD API"]
        P2C["Forward隧道 CRUD API"]
        P2D["客户端配置管理 API"]
    end

    subgraph Phase3["Phase 3 · 信令推送"]
        P3A["新增 MessageType 指令"]
        P3B["客户端新增 CommandHandler"]
        P3C["服务端推送变更通知"]
    end

    subgraph Phase4["Phase 4 · 管理面板"]
        P4A["Vue SPA 前端"]
        P4B["Dashboard 仪表盘"]
        P4C["隧道管理界面"]
    end

    Phase1 --> Phase2 --> Phase3 --> Phase4
```

---

## 二、技术选型

### 2.1 数据库选型

```mermaid
quadrantChart
    title 数据库适配性对比
    x-axis "部署简单" --> "功能丰富"
    y-axis "性能较低" --> "性能强劲"
    quadrant-1 "生产级"
    quadrant-2 "过度设计"
    quadrant-3 "适用"
    quadrant-4 "受限"
    "SQLite": [0.9, 0.35]
    "PostgreSQL": [0.35, 0.85]
    "MySQL": [0.3, 0.8]
```

| 维度 | SQLite | PostgreSQL | MySQL |
|------|--------|------------|-------|
| 部署复杂度 | 零依赖，单文件 | 需安装服务 | 需安装服务 |
| 并发能力 | 中等 (WAL模式) | 极强 | 强 |
| .NET 10 适配 | 官方内置 | Npgsql | Pomelo |
| Docker 友好 | 单文件挂载 | 需额外容器 | 需额外容器 |
| 本项目数据量 | 万级以内 ✅ | 远超需求 | 远超需求 |

> **推荐：SQLite** — 本项目是单实例穿透服务，数据量小（百级隧道、十级客户端），SQLite 零依赖、单文件、Docker 友好，完美契合。后期如需扩展可用 `dotnet ef migrations` 一键切换到 PostgreSQL。

### 2.2 ORM 选型

```mermaid
graph TD
    subgraph EF["Entity Framework Core"]
        EF1["强类型 LINQ 查询"]
        EF2["Code First 迁移"]
        EF3["Change Tracker"]
        EF4["约定优于配置"]
    end

    subgraph Dapper
        D1["手写 SQL"]
        D2["极高性能"]
        D3["无迁移支持"]
        D4["弱类型"]
    end

    subgraph Linq2DB
        L1["LINQ 查询"]
        L2["高性能"]
        L3["生态较小"]
        L4[".NET 10 适配未知"]
    end

    style EF fill:#4a9,stroke:#333
    style Dapper fill:#aaa,stroke:#333
    style Linq2DB fill:#aaa,stroke:#333
```

| 维度 | EF Core | Dapper | LinqToDB |
|------|---------|--------|----------|
| .NET 10 适配 | 官方 10.0 | ✅ | 需验证 |
| Code First 迁移 | `dotnet ef migrations` | ❌ | 第三方工具 |
| 与现有 DI 集成 | `AddDbContext` | ✅ | ✅ |
| 开发效率 | 最高 | 中等 | 中等 |
| 本项目查询复杂度 | 低（简单 CRUD） | 低 | 低 |
| 学习成本 | 低（.NET 开发者熟悉） | 中 | 高 |

> **推荐：EF Core** — 项目数据模型简单（5-6 张表），无需极致性能优化，EF Core 的 Code First 迁移、强类型 LINQ、与 ASP.NET Core DI 深度集成是第一选择。

### 2.3 管理面板前端选型

```mermaid
graph TB
    subgraph 方案对比
        A["Vue 3 + Bun<br/>+ Arco Design Vue"]
        B["Blazor Server<br/>内嵌"]
        C["Blazor WASM<br/>独立部署"]
    end

    A --> A1["Arco Design 字节跳动出品"]
    A --> A2["打包为静态文件，服务端直接 serve"]
    A --> A3["Bun 原生 TS 运行时，安装极快"]
    A --> A4["需要 Bun 开发环境"]

    B --> B1["纯 C# 技术栈统一"]
    B --> B2["需要 SignalR 长连接"]
    B --> B3["每个用户占用服务端资源"]

    C --> C1["纯 C# 前端"]
    C --> C2["首屏加载大"]
    C --> C3[".NET 10 WASM 稳定性待验证"]

    style A fill:#4a9,stroke:#333
```

| 维度 | Vue 3 + Arco Design Vue | Blazor Server | Blazor WASM |
|------|-------------------------|---------------|-------------|
| 生态成熟度 | 极成熟 | 成熟 | 可用 |
| 部署方式 | 静态文件 (wwwroot) | 需 WebSocket | 静态文件 + API |
| 服务端压力 | 无（纯静态） | 每个用户一个连接 | 无 |
| 组件库 | Arco Design Vue | Fluent UI / MudBlazor | Fluent UI / MudBlazor |
| 打包体积 | ~200KB gzip | 无前端体积 | ~2MB+ |
| 开发人员要求 | 需 JS/TS 经验 | 纯 C# | 纯 C# |
| 本项目适配 | 已用 wwwroot 托管静态文件 | 改造大 | 改造大 |

> **推荐：Vue 3 + Bun + Arco Design Vue** — 项目已有 `wwwroot/index.html`，直接替换为 Vue SPA 静态产物即可，零服务端架构变更。Arco Design Vue 由字节跳动出品，设计语言统一。Bun 作为 JS/TS 运行时替代 Node.js，安装速度极快，原生支持 TypeScript。

---

## 三、数据模型设计

```mermaid
erDiagram
    Token {
        int Id PK
        string Value "Token 值"
        string Description "备注"
        bool IsEnabled "是否启用"
        datetime CreatedAt
    }

    WebTunnel {
        int Id PK
        string SubDomain "子域名"
        string LocalIp "内网IP"
        int LocalPort "内网端口"
        string WWWs "备用域名,JSON数组"
        int ClientId FK "所属客户端"
        bool IsEnabled
        datetime CreatedAt
    }

    ForwardTunnel {
        int Id PK
        int RemotePort UK "服务端监听端口"
        string LocalIp "内网IP"
        int LocalPort "内网端口"
        string Protocol "TCP/UDP"
        int ClientId FK "所属客户端"
        bool IsEnabled
        datetime CreatedAt
    }

    Client {
        int Id PK
        string Name "客户端名称"
        string Token FK "认证Token"
        bool IsOnline "是否在线"
        datetime LastSeen "最后在线时间"
    }

    AuditLog {
        int Id PK
        string Action "操作类型"
        string Entity "操作对象"
        string Detail "详情JSON"
        string Operator "操作人"
        datetime CreatedAt
    }

    Client ||--o{ WebTunnel : "拥有"
    Client ||--o{ ForwardTunnel : "拥有"
    Client }o--|| Token : "使用"
```

---

## 四、运行时交互流程

### 新增隧道流程

```mermaid
sequenceDiagram
    actor Admin as 管理员
    participant Panel as 管理面板
    participant API as REST API
    participant DB as SQLite
    participant Server as FastTunnelServer
    participant WS as WebSocket
    participant Client as FastTunnel.Client

    Admin->>Panel: 填写隧道表单并提交
    Panel->>API: POST /api/tunnels/webs
    API->>DB: INSERT INTO WebTunnels
    DB-->>API: OK
    API->>Server: 注册到 WebList + YARP 路由
    API->>WS: 推送 CMD_AddTunnel 指令
    WS-->>Client: 收到新隧道配置
    Client->>Client: 无需重启，即时生效
    API-->>Panel: 200 OK
    Panel-->>Admin: 隧道创建成功
```

### 删除隧道流程

```mermaid
sequenceDiagram
    actor Admin as 管理员
    participant Panel as 管理面板
    participant API as REST API
    participant DB as SQLite
    participant Server as FastTunnelServer

    Admin->>Panel: 点击删除隧道
    Panel->>API: DELETE /api/tunnels/webs/123
    API->>DB: UPDATE SET IsEnabled=0 (软删除)
    API->>Server: 从 WebList + YARP 路由移除
    API->>Panel: 200 OK
```

---

## 五、项目结构变更图

```mermaid
graph LR
    subgraph 现有项目
        CORE["FastTunnel.Core"]
        CORE_CLIENT["FastTunnel.Core.Client"]
        API_MOD["FastTunnel.Api"]
        SERVER["FastTunnel.Server"]
        CLIENT["FastTunnel.Client"]
    end

    subgraph 新增项目
        DOMAIN["FastTunnel.Domain<br/>实体模型"]
        INFRA["FastTunnel.Infrastructure<br/>EF Core DbContext"]
    end

    SERVER --> INFRA
    INFRA --> DOMAIN
    API_MOD --> INFRA
    CORE --> DOMAIN

    style DOMAIN fill:#f9f,stroke:#333
    style INFRA fill:#f9f,stroke:#333
```

新增 NuGet 包依赖：

| 包名 | 用途 | 所在项目 |
|------|------|----------|
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite 数据库驱动 | Infrastructure |
| `Microsoft.EntityFrameworkCore.Design` | 迁移工具 (dev) | Infrastructure |
| `Microsoft.EntityFrameworkCore.Tools` | CLI 工具 (dev) | Infrastructure |

---

## 六、风险与对策

```mermaid
graph TD
    R1["风险：配置热更新时<br/>正在传输的连接会中断"] -->|"对策"| S1["只做软性变更：<br/>新连接走新配置<br/>旧连接自然消亡"]
    
    R2["风险：客户端离线时<br/>收到推送指令"] -->|"对策"| S2["客户端重连后<br/>拉取全量配置<br/>(Reconciliation)"]
    
    R3["风险：SQLite 文件<br/>并发写入冲突"] -->|"对策"| S3["单实例部署<br/>WAL 模式<br/>写入操作量极少"]
    
    R4["风险：InvariantGlobalization<br/>= true 与 EF Core 兼容"] -->|"对策"| S4["SQLite 不依赖 ICU<br/>完全兼容"]

    style R1 fill:#faa,stroke:#333
    style R2 fill:#faa,stroke:#333
    style R3 fill:#ffa,stroke:#333
    style R4 fill:#ffa,stroke:#333
    style S1 fill:#afa,stroke:#333
    style S2 fill:#afa,stroke:#333
    style S3 fill:#afa,stroke:#333
    style S4 fill:#afa,stroke:#333
```

---

## 七、总体评估

```mermaid
pie title 方案C 实现工作量分布
    "Phase1 基础设施" : 15
    "Phase2 API 写操作" : 30
    "Phase3 信令推送" : 20
    "Phase4 管理面板" : 35
```

| 评估维度 | 结论 |
|----------|------|
| **技术可行性** | 完全可行，无技术阻塞点 |
| **架构兼容性** | 仅新增项目，不破坏现有代码 |
| **数据迁移** | 首次启动自动建库，配置文件作为初始种子数据 |
| **向后兼容** | 保留配置文件作为 fallback，数据库优先读取 |
| **推荐技术栈** | **SQLite + EF Core + Vue 3 + Bun + Arco Design Vue** |
| **总工作量** | 约 4 个 Phase，可迭代交付 |

---

## 八、API 接口规划

```mermaid
graph LR
    subgraph Token管理
        T1["GET /api/tokens"]
        T2["POST /api/tokens"]
        T3["DELETE /api/tokens/{id}"]
        T4["PUT /api/tokens/{id}"]
    end

    subgraph Web隧道
        W1["GET /api/tunnels/webs"]
        W2["POST /api/tunnels/webs"]
        W3["PUT /api/tunnels/webs/{id}"]
        W4["DELETE /api/tunnels/webs/{id}"]
    end

    subgraph Forward隧道
        F1["GET /api/tunnels/forwards"]
        F2["POST /api/tunnels/forwards"]
        F3["PUT /api/tunnels/forwards/{id}"]
        F4["DELETE /api/tunnels/forwards/{id}"]
    end

    subgraph 客户端管理
        C1["GET /api/clients"]
        C2["GET /api/clients/{id}/status"]
        C3["GET /api/clients/online/count"]
    end

    subgraph 审计日志
        A1["GET /api/audit-logs"]
    end
```

---

> **最终推荐**：方案 C 完全可行。建议从 Phase 1+2 开始，快速交付 API 管理能力，Phase 3 的信令推送让配置变更即时生效而不需重启客户端，Phase 4 的管理面板打造完整的用户体验闭环。
