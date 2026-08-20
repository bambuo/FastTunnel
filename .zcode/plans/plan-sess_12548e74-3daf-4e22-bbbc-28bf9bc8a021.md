## 功能：系统配置可视化——FastTunnel 配置迁移数据库并由管理台管理

**架构**：新建 `SystemConfig` 键值表（配置权威），通过自定义 `IConfigurationProvider` 挂进 `FastTunnel` 配置段，保留现有 `IOptionsMonitor` 热更新机制；管理台新增"系统配置"页面读写数据库，保存后触发配置重载（EnableForward/WebDomain 立即生效，JWT 需重启）。appsettings.json 的 FastTunnel 段移除。

### 配置项（存库键，相对 FastTunnel 段）
`EnableForward`(bool)、`WebDomain`(string)、`WebAllowAccessIps`(string[] JSON)、`Api:JWT:ClockSkew`、`Api:JWT:ValidAudience`、`Api:JWT:ValidIssuer`、`Api:JWT:IssuerSigningKey`、`Api:JWT:Expires`。Api.Accounts（废弃）与 Token（已迁库）不在此范围。内置默认值（与现 appsettings 一致：JWT 用现密钥等）作为首次部署兜底，播种写入数据库。

### 改动清单

**FastTunnel.Api**
1. `Models/Entities/SystemConfigEntity.cs`（新）：Id、Key（唯一索引）、Value、UpdatedAt
2. `Data/FastTunnelDbContext.cs`：加 `DbSet<SystemConfigEntity> SystemConfigs` + Key 唯一索引
3. **新建 `Data/SystemConfigProvider.cs`**：`IConfigurationSource` + `ConfigurationProvider`
   - 构造接收 DbContextOptions（配置阶段无 DI，手动建 DbContext，路径与 Program.cs 一致）
   - `Load()`：查 SystemConfigs 表 → 键带 `FastTunnel:` 前缀 Set 进配置；**表空/表不存在时用内置默认字典兜底**（保证首次启动 JWT 有效，容错捕获异常返回默认）
   - 提供公开 `ReloadConfig()`（重新 Load + 触发 ChangeToken → OptionsMonitor 热重绑定）
4. `Controllers/SystemController.cs`：加两个接口（复用 BaseController 鉴权）：
   - `GET api/System/config`：读库返回全部配置项 DTO
   - `PUT api/System/config`：校验 → upsert 写库 → `provider.ReloadConfig()` → AuditService 审计（detail 含变更项，JWT 密钥掩码）
5. EF 迁移 `AddSystemConfig`

**FastTunnel.Server**
6. `Program.cs`：
   - db 路径计算提前到配置阶段，`builder.Configuration.Add(SystemConfigProvider)`（放最后，覆盖 appsettings 残留）
   - 迁移后播种：SystemConfigs 表空时写入默认配置（与 provider 默认字典同源）
   - JWT 快照读取（现有 L66 逻辑）不变——数据库 provider 加载后自动拿到值
7. `appsettings.json`：移除 FastTunnel 段（注释说明"配置由管理台管理"）

**FastTunnel.Admin（前端）**
8. **新建 `src/views/settings/SystemSettingsView.vue`**：表单页（a-form）
   - 端口转发开关 EnableForward（a-switch）
   - WebDomain（a-input）、WebAllowAccessIps（a-input-tag 数组，注明预留字段）
   - JWT 组：ClockSkew/Expires（数字）、ValidAudience/ValidIssuer/IssuerSigningKey（输入，注明修改需重启生效）
   - 保存 → PUT → Message 提示（含"JWT 修改需重启"）
9. **新建 `src/api/systemConfig.ts` + `src/types/systemConfig.ts`**：getConfig/saveConfig
10. 菜单接入（4 处）：`router/index.ts` 加 `/settings` 路由；`AppLayout.vue` menuItems 加"系统配置"项（IconSettings）+ selectedKey 列表；i18n 加 `nav.settings`、`page.settings`、`configForm.label.*`、`message.*`（中英文）

### 行为说明
- 保存 EnableForward/WebDomain → 保存即热生效（OptionsMonitor reload），已连接客户端按新值重连后应用新 WebDomain
- JWT 修改 → 页面提示"需重启服务端生效"（JwtBearer 参数为启动快照）
- WebAllowAccessIps 目前代码无消费点（预留配置），页面可编辑并注明
- 首次部署：表空 → 内置默认值生效并播种入库，管理台可改；生产部署后建议修改默认 JWT 密钥
- 配置修改全部记录审计日志

### 验证
1. `dotnet build FastTunnel.slnx` 通过；前端 `bun run build` 通过
2. 冒烟测试：全新数据库启动（表空 → 默认 JWT 生效、服务正常启动）→ 迁移播种确认 → GET config 返回默认值 → PUT 修改 EnableForward=false + WebDomain → 验证 OptionsMonitor 热生效（服务端行为变化）与审计日志记录 → JWT 修改返回重启提示
3. 管理台页面（dev 模式）验证表单回显与保存提示；菜单出现"系统配置"