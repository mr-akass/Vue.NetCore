# CLAUDE.md

本文件给 Claude Code 提供本仓库的工作指引。

## 项目概览

VOL(Vue.NetCore) 框架的二次开发版本。**实际使用的后端只有 `vol.api.sqlsugar`（SqlSugar 版，net10.0）**，
EF 版 `vol.api/` 已于 2026-08-10 整体删除，套官方升级包时必须跳过该目录。

| 目录 | 说明 |
|---|---|
| `vol.api.sqlsugar/` | 后端解决方案 `VOL.sln`（VOL.Core 框架层 / VOL.Entity / VOL.Sys 业务 / VOL.Builder 代码生成器 / VOL.WebApi 启动项） |
| `vol.web/` | 前端 Vue3 + Vite + Element Plus |
| `DB/sqlserver/` | 数据库升级脚本（每个自研功能一个，均幂等） |
| `进度报告.md` | 功能开发进度（按时间顺序 1~22 节，每节含改动文件清单 + E2E 验证记录；本文件是精简索引，细节看它） |
| `环境配置使用说明.md` | 多环境配置与启动方式 |

- 后端 9991 端口，前端 9990 端口，数据库 SQL Server（开发库 `vol_v3`）
- 启动：根目录 `run.bat` 选环境（分窗口起前后端）；单独起后端 `vol.api.sqlsugar/VOL.WebApi/start_api.bat [环境]`
- 注意 `dotnet run` 会被 `launchSettings.json` 覆盖端口/环境，要换端口加 `--no-launch-profile`

## 构建与验证

```bash
dotnet build vol.api.sqlsugar/VOL.sln     # 后端编译
cd vol.web && npm run build               # 前端构建(vite)
```

改动后必须跑通这两条。需要验证带权限的接口时，用 `appsettings.json` 的 JWT Secret 自签 HS256 token
（claims: `jti`=用户ID、`iss`/`aud` 同配置；单点登录校验在 `ApiAuthorizeFilter` 中已注释），
不必走登录流程。浏览器端到端验证用 playwright-core + 本机 msedge（免下载），hash 路由必须带 `#`。

## 已完成的自研功能

全部在 `vol.api.sqlsugar` + `vol.web` 中实现，**尚未提交**（工作区约 978 项变更）。
涉及表结构的都有对应的 `DB/sqlserver/升级脚本_*.sql`，**只在开发库 vol_v3 执行过，STG/PRD 均未执行**。
下面是精简索引，每项的改动文件清单与验证记录见 `进度报告.md` 对应小节。

### 1. 表头筛选（框架级，2026-07-30）
列去重值弹窗。通用接口 `VOL.Core/Controllers/Basic/ApiBaseController.cs → getColumnDistinctValues`
+ `VOL.Core/BaseProvider/ServiceBase.cs → GetColumnDistinctValuesAsync`；
代码生成器勾选 `Sys_TableColumn.HeaderFilter` 即生效。字典列用 bind.data 做选项；日期列按天去重，
in 查询遇 `yyyy-MM-dd`（10 位）值时按天区间匹配（`LambdaExtensions.GetDateRangeInExpression`）。
说明文档 `表头筛选功能说明.md`。中文筛不出的修复见功能 16（字符串 in 必须参数化）。

### 2. 多环境配置 + NLog 日志（2026-07-30）
`appsettings.{Development,Staging,Production}.json` 按 `ASPNETCORE_ENVIRONMENT` 覆盖基础配置；
`launchSettings.json` 有 `VOL.WebApi.{环境}` 三个 profile。
日志：`VOL.Core/Utilities/CustomConsole.cs`、`LogHelper.cs`、`Enums/NlogLoggerType.cs`。

**全解决方案唯一的日志写法（硬性约定）**：`CustomConsole.WriteLine(NlogLoggerType.X, "msg")`——
一次调用同时落盘 `Logs/{类别}/yyyy-MM-dd.txt` 并输出控制台，一举两得（内部 `LogHelper` → NLog，
用法是 log4 那种风格）。新增 `NlogLoggerType` 枚举值**必须同步在 `VOL.WebApi/Config/Log/nlog.config`
加 target + rule**，否则只有控制台没有文件。目前 `CustomConsole` 只有 Info 级，错误落盘用
`LogHelper.Error(NlogLoggerType.Error.ToString(), msg)`。
- **`Sys_Log`（日志写数据库）已弃用，不要再往里写**：每个请求一条入库开销太大（`Logger.cs` 的队列 +
  每秒 `BulkCopy`，还要跨库双提交），换来的信息文件日志一样能给。官方原有的 `Logger.Info/OK/Error` 调用点
  （`ApiBaseController`、`ActionPermissionFilter` 等）暂时留着不动，但**新代码一律用 `CustomConsole`**。
  顺带一个事实：`Sys_Log.ElapsedTime` 列从来没被赋值过——算它的 `DequeueToTable` 是死代码，
  写库走的是 `Fastest<Sys_Log>().BulkCopy(list)`，所以库里这列全是 NULL，别拿它做耗时分析。

### 3. 多数据库连接（2026-07-31，实体级路由 2026-08-21 补完）
`appsettings.json` 的 `Connections` 节点，节点名 = SqlSugar ConfigId = 字典/代码生成器/实体特性 `DBServer` 的值。
核心入口 `DbManger.GetDbClient(dbServer)`（空/未注册回退默认库），注册在 `SqlSugarRegister.GetAllConnectionConfigs()`。
字典切库在 `Sys_DictionaryService` + `DictionaryManager`；代码生成器切库在 `Sys_TableInfoService`
（`GetConnectionKey` 返回连接串供 MySQL 解析库名）。

**实体级分库**：业务模块运行时按 `[Entity(DBServer="连接名")]` 走对应库（代码生成器早就把表的 DBServer
写进实体特性，之前运行时没人读它——从B库生成的模块跑起来查的是默认库）。唯一路由收口点
`VOL.Core/DBManager/EntityDbRouter.cs`：`Route(entityType, client)` 只在传入 `SqlSugarScope`（注册了全部连接的总入口）
时切换，已指定具体连接的调用方保持原样。接入点 `RepositoryBase.GetClient<T>()` / `BaseDbContext.GetClient<TEntity>()` /
`SqlSugarExtension` 的 Add/Update/Set / `IdentityCode` / `WorkFlowManager`。
- 两条硬规则：① `VOL.Entity` 程序集里、**且表名在 `EntityDbRouter._frameworkTables` 名单里**的框架表强制留默认库
  （权限/菜单/字典/工作流都依赖，被误路由等于系统瘫痪，官方 `Sys_User` 自带 `DBServer="SysDbContext"` 历史值）。
  **判断依据必须是写死的名单，不能用 `Sys_` 前缀**——代码生成器把业务实体也生成到 `VOL.Entity/DomainModels` 下，
  用户可以在别的库建一张叫 `Sys_Area` 的业务表，按前缀判断会把它强留默认库，表现是查询报
  `Invalid object name`（功能 15 就是这个坑）；② **主表与所有层级明细表必须同库**，
  跨库没有事务，不满足直接抛异常而不是写坏数据。运行时校验在 `EntityDbRouter.EnsureDetailSameDb`（递归到二、三级明细：
  删主表是先删三级，只校验直接子表的话三级配错库会在校验前就把 DELETE 发到别的库），
  生成代码时校验在 `Sys_TableInfoService.ValidColumnString`。
- **`DBServer` 归一化规则必须各层一致**：空白 / `default` / **未注册的连接名**都视为默认库（`EntityDbRouter.Normalize`、
  `Sys_TableInfoService.NormalizeDbServer`、`DbManger.GetDbClient` 同一套）。这是为了兼容 `ServiceDbContext`/`SysDbContext`
  这类历史值——按字面比较会把"主表历史值 + 明细空值"误判成跨库而生成不了代码。
  `SqlSugarDbType.GetType` 也跟着回退：实体被退回默认库时**方言必须一起退回**，否则默认库是 mysql/pgsql 却按 sqlserver 拼 sql。
- `AddQueue`/`SaveQueues` 是按连接分开的 → `SaveChanges` 双提交（先业务库、再默认库，`Queues.Count>0` 时），
  事务是**链式本地事务**（无分布式事务）：业务库先提交，回滚用 `RollbackQuietly` 不吞原异常。
  典型表现是业务数据落 ShardDb、`Sys_Log` 仍落默认库。
- 缓存策略：`EntityDbRouter` 只缓存"反射特性+同库校验"的结果，**不缓存"连接是否已注册"**——
  数据库管理页可以运行时新增连接（功能 13），缓存了会让新连接一直不生效。
- 已验证（ShardDb 指向 tempdb，`MES_Bom_Main`/`MES_Bom_Detail`/`TestService`）：分页/明细分页/增改删及其 async 版本、
  级联删除、导出、表头筛选去重、`IdentityCode` 流水号按业务库自增、明细失败整体回滚、跨库配置报错、字典三个接口。
- **未验证**：审批流写回状态到分库实体（现有唯一配了流程的表在默认库，分库的三张表没有 `AuditStatus` 列，
  要测得改库结构/流程数据）。代码路径已按同一套 `SaveChanges<T>()` 收口。

### 4. 多角色（2026-07-31）
`Sys_UserRole` 中间表（`Enable=0` 软删除），权限 = 启用角色 ∪ 主角色(`Sys_User.Role_Id`)，
合并在 `UserContext.GetPermissions(int[])`。接口 `/api/User/getUserRoles`、`/api/User/saveRole`（body=角色ID数组）。
前端"设置角色"弹窗 `extension/sys/system/Sys_UserGridHeader.vue`。
**已知限制**：工作流找审批人、用户列表数据权限过滤仍只按主角色。

### 5. 站内消息入库 + 已读未读（2026-07-31）
两表：`Sys_Message`（消息主体）+ `Sys_MessageUser`（每收件人一条已读记录 IsRead/ReadDate）。
发送走 Hub `SendHomeMessage` → `Sys_MessageService.CreateMessageAsync` 事务入库 → 推送在线连接；
收件接口在 `Sys_MessageUserController`（GetMyMessages/GetMyUnreadCount/MarkAsRead/MarkAllAsRead）。
前端首页铃铛 `views/index/Message.vue` + 共享状态 `MessageState.js`，收到推送后**从服务端刷新列表**
（payload 里的 id 是消息ID不是收件人记录ID，不能本地插入）。
业务代码里不入库的推送用 `HomePageMessageHub.SendMessageAsync(hubContext,...)`。

### 6. 定时任务"设置频率"生成 cron（2026-07-31）
`extension/sys/quartz/CronBuilderDialog.vue` 4 种频率生成/反解 Quartz 6 段 cron，
通过扩展 jsx 的 `modelBody` 挂载、`CronExpression` 字段 `extra` 链接打开；
后端 `Sys_QuartzOptionsService.GenerateCronDescr` 在 Add/Update 时生成中文描述（新列 CronDescr/CronStr）。

### 7. 多应用 / 子系统（2026-08-03）
角色属于应用（`Sys_Role.AppID` 单值，Update 时级联子角色），用户靠多角色获得多应用。
应用菜单范围 = `Sys_Application.RootMenuIds`（逗号分隔多个根菜单）指向的**多棵子树的并集**；
加载时所有根菜单隐藏、子菜单 `parentId` 置 0——**"同名一级菜单隐藏"是非显式约定**。
超管：不带 appId=全量菜单；带 appId=子树视角（全权限）。接口层权限校验始终是所有角色并集，按应用区分的只是显示。
前端链路：登录返回 isSuperAdmin/appIds → `Login.vue` 分支 → 多应用进 `/guide`（`views/Guide.vue`）→
`saveAppId` 写 localStorage `current_app_id_{userId}` 并整页刷新 → 拉菜单带 `?appId=`。
定制首页 = 应用的 `DataPanel` 组件名 → `Home.vue` 动态 import `views/home/{DataPanel}.vue`。

### 8. 多语言 / 国际化（2026-08-04，参考 C:\vol.pro.ts_vite-master）
前端 i18n 基础设施是 2026.05 官方升级包自带的（translator/`$ts`、components/lang、43 个组件已用）；
本次补的是 `main.js` 的 `$global.lang: true` 开关（不开则切换入口隐藏）+ `views/sys/lang/Sys_Language.vue`
+ 扩展 + 路由。后端全新：`Sys_Language` 五件套 + `GET api/Sys_Language/createLanguagePack`，
生成逻辑 `Sys_LanguageService.cs`——按 ZHCN 为 key 写 `wwwroot/lang/{zh-tw,en,fr,es,ru,ar}.js`，
**纯 JSON、UTF-8 无 BOM**（前端直接 JSON.parse，有 BOM 会炸）。
**坑**：语言包是跨域 XHR 且带 Authorization 头（触发预检），`app.UseCors("cors")` 必须在 `UseStaticFiles` **之前**。

### 9. 登录页/Guide 改版 + 去验证码（2026-08-05 / 08-10）
`Login.vue`、`Guide.vue` 深色极光玻璃风（#0b1022 底 + 玻璃卡片，主色 #6366f1→#8b5cf6）。
验证码已关闭：`Sys_UserController.Login` 传 `verificationCode:false`，`ValidationContainer.cs` 去掉
VerificationCode/UUID 校验器，Login 提交体也删了这两个字段——**恢复验证码要同时改回这三处**。
超管登录时 `removeSavedAppId()` 清除残留应用视角（否则表现为"菜单被扁平化"）。

### 10. 代码生成器编辑模式（2026-08-10）
`Sys_TableInfo.EditType`：0/null=弹出框、1=新页面(`newTabEdit:true`)、2=表格行内编辑(`editTable:true`)。
生成逻辑在 `Sys_TableInfoService.CreateVuePage`（`editLine` 变量 + `{$false}`/`{#editTable}` 替换），
模板 `VOL.WebApi/Template/Page/VueOptions.html`；运行时 `vol.web/src/components/basic/ViewGrid/ViewGridEditTable.js`
（loadTableAfter 快照 → 保存时比对脏行逐行调 add/update，按 Add/Update 权限门控），
接线在 `ViewGridEvent.js` / `ViewGrid.vue`(`initEditTable`) / `ViewGridFilter.js`（新钩子 tableEditSaveBefore/After）。
演示：开发库 `MES_Customer`(客户管理) EditType=2。
**最易踩的坑**：页面 `options.js` 里必须有 `editTable: true`，缺这行 `initEditTable` 直接 return（不注入保存按钮），
表现为"能进编辑态但没法保存"。`EditType` 只在**生成页面时**被读取，改完必须重新生成页面。
排查顺序：看 options.js 有无 editTable → 看菜单有无 Add/Update 权限（两者都无时会剥掉所有 edit 配置）。

### 11. 系统配置表（2026-08-20）
`Sys_ConfigSetting`（ConfigKey 唯一键值对）。首个用途：代码生成器的生成路径
`builder.vuePath` / `builder.appPath` 从前端 localStorage 改为存库——**原因**：复制框架做新项目时
同一个 localhost 域名下会读到旧项目的路径，代码生成到错误目录。
接口 `BuilderController.GetBuilderPaths`（登录可读）/ `SaveBuilderPaths`（限超管，路径决定文件写到哪）。

### 12. 首页快捷导航（2026-08-20）
`/home` 页的用户自定义快捷菜单，支持拖拽排序。
- 表 `Sys_UserShortcut`（UserId+AppId+MenuId 唯一索引），**只存 MenuId 不存 name/url/icon**：
  显示信息由前端从 `store.state.permission` join 出来 → 菜单改名自动跟随、权限被收回时快捷项自动消失、零额外请求。
  `MenuName` 是纯排障用的冗余列。`AppId=0` 表示不区分应用。
- 后端 `Sys_UserShortcutService.cs`：GetMyShortcuts / AddShortcut（一次多个，上限 30，
  校验菜单在权限范围内——复用 `Sys_MenuService.GetCurrentMenuActionListByAppId`，天然带多应用+多角色逻辑）/
  RemoveShortcut / SaveSort（按传入 ID 数组下标重写 SortOrder）。所有接口只操作 `UserContext.Current.UserId` 自己的数据。
- 前端 `views/home/HomeShortcut.vue`（+ `views/home/DataPanelDefault.vue`），在 `Home.vue` 中引入。
- **权限约定**：`ApiBaseController` 类级已有 `[JWTAuthorize]`，自定义 action **不加** `[ApiActionPermission]`
  就只校验 JWT、不校验菜单权限——所以这张表不用在菜单里建权限记录，所有登录用户可用（同类先例 `GetEnabledApps`）。
- **坑**：`vue-draggable-next` 只在挂载时读一次 Sortable 配置，`:disabled` 动态改不生效 → 用 `updateOptions`，别换 key 重建。
  `getSavedAppId()` 未选应用时返回 null（和默认为 1 的 `store.state.currentAppId` 不同），所以用 `getSavedAppId() || 0`。

### 13. 多数据库管理（2026-08-21）
界面上新增数据库连接，**不用改 appsettings.json、不用重启**，与功能 3 的 `Connections` 节点合并生效。
- 表 `Sys_DbConnection`（ConnName 唯一），连接串按 `Secret.DB` 做 DES 加密落库，列表/编辑时密码显示为 `******`
  （未改动则保持原值）。保存前强制 `TestConnection` 通过才允许入库。
- 注册中枢 `VOL.Core/DBManager/DbConnectionManager.cs`：
  - `Initialize()`——`Program.cs` 中在 `AppSetting.Init` 之后、`UseSqlSugar` 之前调用，把库里启用的连接合并进
    `AppSetting.Connections`（appsettings.json 同名连接优先）；**表不存在时静默跳过**，不能因此起不来
  - `RegisterRuntime()`——界面新增/改连接串后立即注册，当次请求即可用
  - `TestConnection()`——带 20 秒墙上时钟上限（连接串里 `Connect Timeout=500` 会把请求挂死）
- 业务 `Sys_DbConnectionService.cs`；控制器扩展接口 `TestConnection`、`GetRegistered`（排查"配了但用不了"，均限超管）。
- 字典 `dbServer` 改为 `DbSql` 从 `Sys_DbConnection` 取数，代码生成器/字典的"所在数据库"下拉自动同步。
- 前端 `views/sys/system/Sys_DbConnection.vue` + `extension/sys/system/Sys_DbConnection.jsx`
  （view 按钮"已注册连接"、box 按钮"测试连接"）+ `router/viewGird.js`。
- **只增不删是硬性要求**（删掉会导致项目崩、代码生成失败）。三层兜底：菜单 Auth 没有 Delete、
  Service 覆写 `Del`/`DelAsync` 直接返回错误、`DbConnectionManager` 不提供任何移除 API。
  连接名 = ConfigId，被 `[Entity(DBServer)]`/字典 DBServer/`Sys_TableInfo.DBServer` 引用，
  **保存后也不允许改名**（前端 `readonlyUpdate` + 后端拒绝）。停用走 `Enabled=0`。

### 14. 新增数据库连接的报错自查性改善（2026-08-24）
新增连接失败时的报错以前是驱动原文，指不到病根，全靠人猜。两个实际踩到的坑：
① 自签证书 → 连接串必须带 `TrustServerCertificate=True;`（`Microsoft.Data.SqlClient` 4.0+ 默认 `Encrypt=True` 且校验证书）；
② `不支持的关键字:"user id"`——**连接串本身是对的**，是驱动层"关键字不认识"：要么 [数据库类型] 没选 SqlServer
（别的驱动去解析 SqlServer 语法的串），要么键名里混进了**肉眼看不见的字符**（从文档/聊天工具复制时带进
U+00A0 不换行空格、全角空格、零宽空格、换行，`Microsoft.Data.SqlClient` 一律报同一句，打印出来和正常的完全一样）。
- `DbConnectionManager.NormalizeConnectionString`：**只规范化等号左边的键名**（零宽字符删掉、各类空白统一成单个
  普通空格），值一律不动（密码里可能真有空格）。`TestConnection`/`RegisterRuntime`/`LoadFromDb` 三个入口都先过一遍——
  测试与实际注册必须用同一份串，否则会出现"测试通过、注册后连不上"。
- `DbConnectionManager.Suggest`：报错含证书/SSL 时追加"加 `TrustServerCertificate=True`"；报错是关键字类
  且连接串含 SqlServer 独有键名（`Initial Catalog`/`TrustServerCertificate`/`Persist Security Info`/`Integrated Security`）
  而选的类型不是 SqlServer 时追加"请改选 SqlServer"。**建议写在后端**：保存路径和测试按钮是两个入口，
  只在按钮里拼文案，直接点保存的人看不到。有意**不自动纠正类型**——类型决定整个方言，猜错比报错危害大。
- `SqlSugarDbType.IsSupportedName`：`GetType` 对不认识的类型名是**静默回退默认库类型**的（为兼容历史 `DBServer` 值），
  保存/测试连接时不能跟着静默，否则等于埋一个"连的库不是你选的那种"。
- 前端：连接串 placeholder 换成可照抄的 SqlServer 示例；测试前强制先选类型（空类型后端会回退默认类型，
  测出来的"成功"未必是你要连的库）；失败改 `$alert`（证书类报错很长，`$message` 会截断且不能复制）；
  `modelOpenAfter` 的 `DBType` 兜底从"仅新增"扩到编辑（历史数据类型为空会被新加的校验拦下）。
- 排查手法（下次遇到"连接串肉眼没错却报不支持的关键字"）：拿同一份串逐个 dbType 打 `TestConnection`。
  实测 `MsSql`/`SqlServer`/空值→成功，`Kdbndp`→`Keyword not supported: data source`（与中文那句同族）、
  `PgSql`/`GaussDB`→`Couldn't set data source`、`MySql`→`Option 'trustservercertificate' not supported`、
  `Oracle`→`ORA-50008`、`Dm`→`initial catalog does not exist`。

### 15. 业务表名叫 `Sys_xxx` 时分库路由失效（2026-08-24，对应报告第 18 节）
用户在新连接 `shelflife` 里建表 `Sys_Area`，代码生成器正常生成，但页面查询报 `服务器处理异常`
（真实异常 `Invalid object name 'Sys_Area'`，被 `ExceptionHandlerMiddleWare` 在非 Development 下统一替换掉了，
原文看 Development 响应或 `Logs/Error`）。实体本身没问题——`DBServer="shelflife"` 是生成器自动写进 `[Entity]` 的。
- 根因：`EntityDbRouter.IsFrameworkEntity` 原来判"框架表"= `Sys_` 前缀 + 在 `VOL.Entity` 程序集里，
  而**代码生成器把业务实体也生成到 `VOL.Entity/DomainModels`**，业务表两个条件全中 → 被强留默认库、DBServer 被无声丢弃。
- 修法：改成写死的 27 张框架表名单 `EntityDbRouter._frameworkTables` + public `IsFrameworkTableName`。
  同一个前缀假设在 `SqlSugarRegister` 的 Oracle 自增序列分支里也有一份（会给业务表指定不存在的 `T_xxx_SEQ` 导致插入失败），
  一起改掉。`Sys_TableInfoService.ValidColumnString` 新增**真重名拦截**：表名确实和框架表撞名（如 `Sys_Log`）时
  生成阶段就报"请把业务表改个名字再生成"（否则实体同名同命名空间编译不过）。
- 官方升级包以后新增的 `Sys_*` 表不在名单里也无妨：它们本来没配 `DBServer`，`Normalize` 后就是默认库。

### 16. 表头筛选中文筛不出结果（2026-08-24，对应报告第 19 节）
表头筛选勾选英文/数字能筛出、勾选**中文**结果为空。不是编码配置问题，也和分库无关（默认库的框架表一样复现）。
- 根因：`in` 查询走的是集合 `Contains`，**SqlSugar 对集合 Contains 生成的是把值直接拼进 sql 的 `IN ('值')` 字面量**
  （不是参数）。SqlServer 对**不带 `N` 前缀**的字面量按数据库排序规则的代码页转换，本项目库是
  `SQL_Latin1_General_CP1_CI_AS`（非 Unicode）→ 中文在比较前就变成 `??`，条件永远不成立。
  证据：`CAST('性别' AS VARBINARY)` = `0x3F3F` vs `CAST(N'性别' AS VARBINARY)` = `0x27602B52`。
  `like` 一直正常，因为它生成的是 `like '%'+@参数+'%'`。
- 修法：`LambdaExtensions.GetContainsExpression` 里**字符串列改走 `GetStringInExpression`**——拆成参数化的
  等值 or 链 `p=>p.Col==v1||p.Col==v2`（notIn 外面套 `Not`，值全被过滤掉时返回 `False<T>()`/`True<T>()`，
  不能返回无条件全表）。非字符串列（int/日期/Guid）保持原来的 `IN`。
  **不改数据库排序规则**（影响全库字符串比较与索引，且框架要能跑在别人的库上），
  也不去改方言层给字面量加 `N`（只能解决 SqlServer）。
- 顺带修好所有 `CreateExpression(values, In)` 的调用方：批量删除按主键 in、明细按外键 in、`WorkFlowFilter` 条件筛选。
- 同时修掉一个潜伏问题：`SearchParameters.Value` 是逗号拼接的，值本身含逗号（如地区名 `北京市,新疆`）会被
  `Split(',')` 拆坏 → 新增 `SearchParameters.Values` 数组，`ConvertQueryFilter` 优先取它，
  前端 `VolTable.applyColumnFilter` 同时提交两者（老页面/老 `wheres` 字符串写法不受影响）。

### 17. 表格快捷复制（2026-08-24，对应报告第 20 节）
表格里字段值后面显示一个复制图标，点击把值复制到粘贴板并提示`【值】已复制`。
参考项目（`ShelfLife.Web/src/extension/system/Sys_User.js` 的 `this.initCopyBtn(['NTID','Email'])`）是在扩展 js 里
**改写 `column.render`** + 复制 `row[field]` 原始值；我们做成**代码生成器的列开关**，不用每个页面写扩展代码。
- 链路（与 `HeaderFilter` 完全同一套，加新列开关照抄这条链）：`Sys_TableColumn.QuickCopy`(int? 0/null 关、1 开)
  → `builderData.jsx` 的 `quickCopy` 复选列 → `coderV2Table.vue` 的 `TAB_VISIBLE_FIELDS.advanced` **必须加字段名**
  （不加会被 `applyTableStructureColumnVisibility()` 隐藏，界面上找不到该列）→ `Sys_TableInfoService.GetGridColumns`
  在 `if(vue)`→`if(!app)` 里输出 `quickCopy:true,`（**app 端不生成**，移动端没这个交互）→ `VolTable.vue` 渲染。
- 运行时：图标追加在 `VolTable.vue` **只读模板链的最末尾**（不是覆盖 `column.render`）——这样
  `render`/`formatter`/`bind`(字典)/`link`/日期分支全部照旧生效；复制的是 `cellFormatter` 算出的**显示文本**
  而不是 `row[field]`（字典列复制出 key 没意义），helper 在 `VolTable/VolTableProvider.js`
  （`getQuickCopyText`/`hasQuickCopyText`/`quickCopyCell`，clipboard API + 隐藏 textarea `execCommand` 兜底，
  提示超 30 字符截断，文案走 `$ts`）。图标类名用 `el-icon-document-copy`（老图标字体，`assets/element-icon/icon.css`
  里有，不用引 Element-Plus SVG 图标）。
- **布局坑**：字典列/自定义 `formatter` 列的值渲染在**块级 `<div>`** 里，直接跟 `<i>` 会掉到下一行 →
  给这些列打标记类 `quick-copy-cell`（`VolTable.vue` 的 `getColumnClass`），样式里只对这些单元格内的 div
  设 `display:inline`（`VolTable.less`）。
- 和 `EditType` 一样：**`QuickCopy` 只在生成页面时被读取**，改完必须重新生成页面；排查"勾了没效果"先看
  `options.js` 里有没有 `quickCopy:true`。
- 未接线：`VolTable/VolTable-V2-Render.jsx`（虚拟表格的另一套渲染器，全仓库无人 import，启用时再补）。

### 18. 主题个性化（2026-09-01，对应报告第 21 节；背景图铺满/折叠重叠修复见报告第 23 节，圆角横条修复见第 24 节，搜索条/工具栏叠色修复见第 26 节）
用户在 [基础设置] 抽屉里自选背景图 / 主题色 / 玻璃或渐变效果 / 排版布局 / 全局字号，
**每一项都按 `(UserId, AppId)` 分别保存**（切应用会整页刷新，刷新后读到的就是新应用那份）。
- 表 `Sys_ThemeSetting`（`UX_Sys_ThemeSetting_User_App(UserId,AppId)`）：旋钮全塞在 `ThemeJson` 一列
  （后端只校验长度 8000 + 能否 `JObject.Parse` + 背景图地址合法，**不认识具体旋钮**，前端加开关后端不用动）；
  `BgImage` 另存一列是为了换图/删图时能直接查出旧路径。`UserId=0` 的行 = **该应用的默认主题**
  （超管点 [设为本应用默认] 写入，用户没配过时前端拿它渲染）；`AppId=0` = 不区分应用。
  表名已加进 `EntityDbRouter._frameworkTables`。
- 后端 `VOL.Sys/Services/System/Partial/Sys_ThemeSettingService.cs` +
  `VOL.WebApi/Controllers/Sys/Partial/Sys_ThemeSettingController.cs`（6 个接口，**只有 JWT、不加
  `[ApiActionPermission]`**，个人数据类接口的既有约定，所以不用在菜单里建权限记录）。
  背景图落 `wwwroot/Upload/theme/{userId}/{guid}{ext}`，5MB、限图片扩展名；
  **删文件前必须查"有没有别的记录还引用这个地址"**——超管的个人记录和应用默认记录可能指同一张图，
  不查就会一点重置把全应用的默认背景删掉。
- 前端核心 `vol.web/src/uitils/themeManager.js`：所有旋钮翻译成挂在 `<html>` 上的 CSS 变量
  （含 Element-Plus 的 `--el-color-primary` 及 `light-1..9`/`dark-2`、`--el-font-size-*`、`--el-border-radius-*`
  等，外壳自己的用 `--vol-*`）+ 打在 **`body`** 上的标记类。样式表 `assets/css/theme-custom.less`（**全局非 scoped**）。
  面板 `views/index/Setting.vue`（改任意项立刻预览，[保存] 才落库，直接关抽屉在 `onUnmounted` 里还原）。
- **为什么标记类打在 `body` 上、选择器以 `#vol-container` 开头**：弹窗/下拉/抽屉渲染在 body 下，
  挂容器上选不到它们；带 ID 的选择器特异性高于 scoped 编译出的属性选择器，不用满屏 `!important`。
  框架原来的 `.vol-theme-blue/-dark/...` 一行没动，没启用自定义主题的用户完全走原来那套。
- `applyTheme` 会把 `vol-layout` 和 `vol-theme`(`custom`/`custom-aside`) 写回 localStorage，
  让 `Index.vue` 原有的启动读取逻辑不用改；`main.js` 在 `createApp` **之前**先 `applyCachedTheme()` 铺一遍
  （否则先闪默认蓝再跳自定义色）。`applyCachedTheme` 里**必须清理残留的 custom 标记**：
  换用户/换应用后可能类名还在但变量没了 → 外壳变成没样式的白板。
- **坑**：`vol.web/index.html` 里有条全局 `.el-button{font-size:12px!important}`，
  "全局字号"对按钮无效就是它——`theme-custom.less` 里 `.el-button` 那条必须跟着加 `!important`，
  同时把面板自己排除掉（`.vol-theme-setting .el-button` 固定 12px，抽屉宽度固定，20px 时按钮会被挤出可视区）。
- 和代码生成器的开关相反：**主题旋钮全是运行时读取**，改完刷新即生效，不用重新生成任何页面。
- **背景图铺满整页 + 界面通透度（报告第 23 节）**：背景图画在 `#vol-container` 上，所以"图能露多少"
  取决于压在它上面的外壳与内容区是否半透明 → `applyTheme` 里 **`translucent = 玻璃效果 || 有背景图`**，
  两种情况共用一个 body 标记类 `vol-translucent`（样式里不用把条件写两遍），透明度就是[界面通透度]滑块
  （`surfaceAlpha`，范围放宽到 **0.1~1**）算出的唯一变量 `--vol-surface`，凡是会挡住背景的白底块都从它取色。
  滑块因此不再是玻璃专属，`Setting.vue` 里 `v-show="effect==='glass' || bgImage"`。
  **`backdrop-filter` 仍只给玻璃效果**：背景图配平面效果时用户要的是看清图，糊掉就没意义（大面积模糊也很吃性能）。
  渐变效果下 `--vol-sider-bg`/`--vol-header-bg` 是把每个色标换成带透明度的版本，保住渐变外观。
  新加的白底块（业务页面自己写死的 `background:#fff`）记得改成 `var(--vol-surface, #fff)`，否则那块在背景图下是死白。
  **主题面板自己排除在半透明外**（`.el-drawer.vol-theme-drawer` 实色 + 取消模糊，`Index.vue` 上加的类）：
  通透度调到 20% 时面板文字也看不清，用户就没法把它调回来（和字号那条排除同理）。
- **坑（都在报告第 23 节）**：① Element-Plus 把 `--el-table-bg-color`/`--el-table-tr-bg-color`/`--el-table-header-bg-color`
  等**定义在 `.el-table` 元素自己身上**，挂 `<html>` 会被就近定义盖掉（表现是"变量明明设了表头还是白的"），
  只能在 `theme-custom.less` 里按 `.el-table` 选择器给；② 表头另外还要 `!important`——`VolTable.less` 是
  `background-color:#f8f8f9!important`；③ 侧边栏折叠成 63px 后项目名会换行成好几行，而 `.header` 只有 60px 高
  又不裁剪 → 文字压到下面的菜单图标上（极光玻璃这类小字号主题能叠三四行），折叠态直接 `display:none`
  （`Index.vue` 新增的 `vol-aside-collapse` 类 + `aside.less`）。
- **坑（报告第 24 节）**：卡片化/半透明那两条规则**不能通配 `#vol-container .vol-main .el-scrollbar__view > div`**——
  Vue3 页面组件的根是 fragment，`ViewGrid` 里的 `VolBox`(`.vol-dialog`)和 `el-dialog` 遮罩(`.el-overlay`)
  与页面主体是**兄弟节点**、同为该滚动区的直接子 div，通配会把这些空壳子也画成卡片，
  表现是"列表页下方多出几条 24px 高的圆角横条，页面里有几个弹窗就有几条"（用户管理 3 个 VolBox → 3 条）。
  现在写成 `> div:not(.vol-dialog):not(.el-overlay)`。**不能改用 `:first-child`**：个人中心的页面根元素排在第三个；
  也不能按根元素类名白名单列举：代码生成页(`/builder`)的根 div 根本没有 class。
- **坑（报告第 26 节）**：半透明那串选择器**只能刷框架自己上了白底的那一层**。`ViewGrid` 普通布局里
  只有 `.view-container` 是白底，`.view-header`/`.grid-container`/`.grid-body`/`.fiexd-search-box` 本来透明——
  一起刷就是半透明叠半透明（0.4 叠 0.4=0.64），表现是"搜索条和工具栏像两条更白的横带、内容贴着横带上沿"
  （框架这几层只有 `padding-bottom` 没有 `padding-top`），而且通透度滑块与所见不符。
  现在内层四个选择器加了 `.layout-container-padding` 前缀（只有间距布局才是内层白底、外层 `none`），
  并且 `…> div:not(.vol-dialog):not(.el-overlay) > .view-container` 置透明（父级已刷过就不再叠，
  但 ViewGrid 嵌在更深层级时仍保留自己的底色）。
- 已知限制：字号 18~20px 时表格工具栏在 1500px 宽下会挤（框架 ViewGrid 布局，没为此改框架）；
  登录页/Guide 页不套用主题。`Sys_Application.Theme`/`PrimaryColor` 是框架历史遗留列，本功能没用它们。
  （`layout='left'` 原来没有双栏 DOM、只是换配色，已由功能 19 补完。）

### 19. 双栏导航布局（2026-09-01，对应报告第 22 节）
最左侧一条窄栏只放一级菜单，点哪个，右边侧边栏就换成它下面的子菜单树。布局值仍是主题里的 `left`
（不新增第四种，已保存的主题和"深色沉浸"方案自动获得真双栏），纯前端改动、无数据库变更。
- 分组逻辑收口在 `vol.web/src/views/index/IndexMethods.js → groupMenuByLayout(dataConfig, layout)`
  （初始化和 `Index.vue` 里 `watch(layout)` 的实时切换都调它）；窄栏 DOM + `asideTitle` 计算属性
  （双栏时侧边栏顶部显示当前一级菜单名）在 `views/Index.vue`；结构样式 `views/index/aside.less`；
  自定义主题下的配色/宽度 `assets/css/theme-custom.less`（`--vol-rail-width`，密度 68/80/92）。
- **分组会改写菜单数据，所以这个函数必须可重复执行**：它把一级菜单的直接子菜单 `parentId` 置 0
  （让 `VolMenu` 当根渲染），原始父级在 `d.pid`。每次开头要做两件还原——① 从 `pid` 还原 `parentId`；
  ② 清掉 `children`。漏掉②的表现是切回经典布局后**孙子菜单被渲染成二级菜单**（层级散了）。
  每组的菜单列表存在自己加的 `m.groupMenus` 里，**不能借用 `m.children`**（那是 `VolMenu` 建树用的）。
- 窄栏图标要写兜底 `item.icon || 'el-icon-menu'`：`VolElementMenu` 补默认图标是在 setup 里跑一次的，窄栏不经过它。
- `.vol-aside-project-name` 全局是 `color:#fff`，而框架原生 `-aside` 主题（`blue-aside`/`dark-aside`）的**侧边栏是白底**
  （深色只给窄栏）→ 双栏下标题白字白底看不见，`aside.less` 的 `.vol-layout-left` 块里改成深色；
  自定义主题那份在 `theme-custom.less` 里以 `#vol-container` 开头（ID 特异性更高）不受影响。
- 折叠按钮折的是右边侧边栏，**窄栏本身不折**（框架原行为，窄栏只有 80px）。

## 关键技术坑（踩过，别再踩）

**SqlSugar**
- `SqlSugarScope` 是"一个异步上下文一份连接"：`AddConnection` 只对当次请求生效，下个请求会按私有字段
  `_configs`(`List<ConnectionConfig>`) 重建 → 运行时注册**必须反射往 `_configs` 里也写一份**，否则下个请求又变回"未注册"。
- `IsAnyConnection` 查的是 `_configs`，但 `RemoveConnection` 要求当前上下文已实例化过该连接，
  否则内部空引用 → 先 `GetConnection(id)` 建实例再 Remove。`GetConnection` 不会真正连库。
- `MergeTable()` 不支持标量泛型（string/Nullable）：分组去重分页直接用 GroupBy+OrderBy+Select+ToPageListAsync。
- 对 bit 列做 `SqlFunc.IIF` 必须写显式比较 `x.IsRead == true`，否则生成非法 CASE WHEN。
- 表达式里的**集合 `Contains` 生成的是字面量 `IN ('值')` 而不是参数**（`.In(it=>it.Col,list)` 也一样，
  `ConnMoreSettings.DisableNvarchar` 只影响参数的 DbType，救不了字面量）。SqlServer 对不带 `N` 前缀的字面量
  按库排序规则的代码页转换 → 非 Unicode 排序规则的库（本项目 `SQL_Latin1_General_CP1_CI_AS`）里中文全变 `??`，
  表现是"英文数字筛得出、中文筛不出且不报错"。字符串列的 in 必须拆成参数化的等值 or 链
  （`LambdaExtensions.GetStringInExpression`）。`x.Col==value` 才会生成 `@参数`。
- 手写 sql 的 in 条件**不能用 Dapper 的 `in @data`**：SqlSugar 不展开数组，会把值直接拼进 sql 导致语法错误 →
  一个值一个占位符（`Sys_DictionaryService.GetTableDictionary` 就是踩这个炸的）。同一次请求里字典可能来自不同类型的库，
  转义/语法要按 `SqlSugarDbType.GetType(dbServer)` 分支，别用全局 `DBType.Name`（`key` 在 sqlserver 是 `[key]`、mysql 是 `` `key` ``）。

**框架层**
- `ObjectExtension.ChangeType` 对**非空 bool** 只认 "true"/"false"：前端 switch 提交的 `1` 会转失败 → 当 null → 存成 false
  （界面开着、库里却是停用）。自己的 Service 里要先规范成真 bool 再交给 `DicToEntity`
  （这也是框架其它表的启用字段大多用 int 而不是 bool 的原因）。
- `ApiBaseController` 用反射调 Service，且同时暴露同步和 async 两套路由（`Del`/`delAsync`）——**覆写一个等于没防住**。
- Partial 控制器不能声明与生成类同签名的构造函数（CS0111），直接用基类 `Service` 属性。
- `[Entity]` 实体无需集中注册，SqlSugar 版靠反射发现。
- **代码生成器把业务实体也生成到 `VOL.Entity/DomainModels` 下**（和框架实体同一个程序集、同一个命名空间）→
  任何"按程序集 + `Sys_` 前缀识别框架表"的逻辑都会误伤业务表，必须按写死的表名单判断
  （`EntityDbRouter.IsFrameworkTableName`）。
- 排查"接口返回 `服务器处理异常` 但看不出原因"：`ExceptionHandlerMiddleWare` 只在 Development 下返回真实异常，
  否则一律换成这句；真实异常在控制台和 `Logs/Error` 里。
- VolForm 的 `readonly` 渲染成 `disabled`；想"新增可填、编辑只读"在 editFormOptions 写 `readonlyUpdate: true`
  （`VolProvider.setFormAddOrUpdateReadonly` 在 `modelOpenAfter` 之前统一处理），不要在 jsx 里手改 option。
- ViewGrid 扩展按钮格式是 `buttons:{view:[...]}`（对象不是数组），`onClick` 的 `this` = ViewGrid 实例。
- el-dialog 挂在 body 下渲染，弹窗框架样式必须写在**非 scoped** 全局块里。想按主题/开关整体换样式，
  标记类要打在 `body` 上（打在 `#vol-container` 上选不到弹窗/下拉/抽屉），选择器则以 `#vol-container` 开头
  提高特异性，避免满屏 `!important`。
- **`vol.web/index.html` 里有一批带 `!important` 的全局内联样式**（如 `.el-button{font-size:12px!important}`）：
  改全局字号/尺寸时发现"别的都变了只有按钮不变"就是它。排查手法是遍历 `document.styleSheets`
  找出所有命中该元素的规则，别猜。
- **不是所有 `--el-*` 变量都能挂在 `<html>` 上生效**：Element-Plus 把表格那几个（`--el-table-bg-color`/
  `--el-table-tr-bg-color`/`--el-table-header-bg-color`/`--el-table-expanded-cell-bg-color`）**定义在 `.el-table`
  元素自己身上**，就近定义优先 → 挂 `<html>` 一点效果都没有（还会误以为已生效），必须按 `.el-table` 选择器覆盖。
  改这类颜色时先在 devtools 里看变量是从哪一层解析来的。表格表头还额外被 `VolTable.less` 的
  `background-color:#f8f8f9!important` 压着，覆盖要跟着加 `!important`。
- **别用 `> div` 通配"页面最外层"**：Vue3 页面组件的根是 fragment，页面主体与 `VolBox`(`.vol-dialog`)、
  `el-dialog` 遮罩(`.el-overlay`)是**兄弟节点**，都是 `.vol-main .el-scrollbar__view` 的直接子 div。
  给这层加底色/圆角/内边距会把弹窗空壳子也画出来（底部多出几条圆角横条，见功能 18 与报告第 24 节）。
  排查手法：devtools 里看 `el-scrollbar__view` 的直接子元素有几个、各自多高。
- **分页栏的间距是框架自己补的，且补漏了**（报告第 25 节）：`VolTable.vue` 的 `<el-pagination>` **没开 `background`**，
  而 Element-Plus 只在 `is-background` 模式下给页码/上下页 `margin:0 4px`，非该模式下
  `--el-pagination-item-gap:16px` 只作用于 `.btn-prev`/`__sizes`/`__total`/`__jump` → `.el-pager li` 与 `.btn-next` 零 margin。
  框架在 `VolTable.less` 只给 `.el-pager .number` 补了 `margin-left:8px`，漏了 `.btn-next` 和快速翻页的
  `…`（class 是 `.more.btn-quicknext` 不带 `.number`），表现是"页码之间有缝、`›` 和 `…` 紧贴着页码"。
  白底压白底时看不出来，自定义主题把按钮变成半透明底色后才暴露——**这类"主题一开就发现的样式问题"先摘掉 body
  标记类量一遍**，margin 不变就说明是框架原有缺陷，改框架样式而不是往主题里打补丁。
- **`vol.web/src/assets/css/common.less` 是死文件**：全仓库没有任何地方 import 它
  （只有同名的 `uitils/common.js` 被引，同名不同物）。里面 `.el-pager li{margin-right:9px;border:1px solid #eee}`
  这类样式看着像生效其实一行都没生效，排查界面问题时别拿它当依据。
- **半透明底色只能画一层**（报告第 26 节）：给某一层加 `--vol-surface` 前先确认**框架有没有给它上白底**——
  父子都刷就是 `0.4` 叠 `0.4=0.64`，表现是"搜索条/工具栏比周围亮一截，像两条更白的横带"，
  且通透度滑块与所见不符（设 20% 看着像 36%）。`ViewGrid` **普通布局只有 `.view-container` 是白底**，
  内层 `.view-header`/`.grid-*`/`.fiexd-search-box` 本来透明；间距布局（`$global.gridPadding`/页面传 `padding`）
  反过来是内层 `#fff`、外层 `background:none`。顺带一个通用事实：框架这几层大多**只有 `padding-bottom`、
  没有 `padding-top`**（`.view-header` 是 `0 15px 8px`），白底压白底时看不出，一旦有了可见底色
  就会显出"内容贴着上沿"——**这时别去补 `padding-top`**（`.view-header` 是 `height:40px` + `border-box`，
  按钮 32px 已吃满，加内边距会被裁），先去掉多余的那层底色。
- 界面选行必须点行首单选框，直接点行/单元格不算选中（会提示"请选择要编辑的行"）。
- 代码生成器**新增一个列级开关**要改五处才看得见效果：实体属性 → `builderData.jsx` 列 →
  `coderV2Table.vue` 的 `TAB_VISIBLE_FIELDS`（漏了这处最容易懵：数据保存正常、界面上没有这一列）→
  `Sys_TableInfoService.GetGridColumns` 输出 → 运行时组件消费。照抄 `HeaderFilter`/`QuickCopy` 那条链。
- 想给单元格值**后面**追加图标：字典列和自定义 `formatter` 列的值渲染在块级 `<div>` 里，图标会掉到下一行，
  要给列加标记类再把内部 div 设 `display:inline`（见功能 17）。改显示效果时优先在**只读模板链尾部追加**，
  别覆盖 `column.render`——那会把 `bind`/`link`/日期等分支一起冲掉。
- **`store.state.permission` 里的菜单 `parentId` 是被布局改写过的**：双栏/顶部布局分组时会把一级菜单的
  直接子菜单 `parentId` 置 0，原始父级留在 `pid`（`groupMenuByLayout`，见功能 19）。要判断层级/找父菜单用 `pid`；
  任何会重跑的分组逻辑都必须先从 `pid` 还原 `parentId` 并清 `children`，否则切布局后层级会散。

**中文编码**
- sqlcmd 执行含中文的脚本会乱码 → 用 SSMS，或另存 UTF-16 后再 sqlcmd，或 `-f 65001`；中文路径/文件名先复制到 ASCII 临时路径。
- sqlcmd **管道输出**中文必变 `?`：`FOR JSON` 导出的含中文 payload **绝不能 Save 回库**（会把中文写坏）。
  验证 DB 中文用 `CONVERT(VARCHAR(MAX), CAST(col AS VARBINARY(MAX)), 2)` 导 hex 再 node utf16le 解码。
- SQL 里含中文的字符串字面量必须加 `N` 前缀（早期漏加把 Sys_Menu.Auth 写成了 `??`）。
- bash → powershell 传中文参数会乱码，先 cp 到英文路径。
- WPS 生成的 .doc Word COM 拒开，用 node 按 UTF-16LE 扫描中文文本段提取。
- 从文档/聊天工具复制来的字符串里常混着**不可见字符**（U+00A0 不换行空格、U+3000 全角空格、零宽空格、BOM），
  打印出来和普通空格完全一样。连接串已在 `DbConnectionManager.NormalizeConnectionString` 兜住；
  其它地方遇到"值肉眼没错但解析失败"，先 `od -An -tx1` 看字节。

## 已知未修问题（每次浏览器验证都会看到，别当成新引入的回归）

1. 开发库 `Sys_Menu` 中 `Url='/Sys_Application'` 的 `MenuName` 是乱码 `搴旂敤绠＄悊`，应为"应用管理"
   （早期 sqlcmd 写中文的坑）。修法 `UPDATE ... SET MenuName=N'应用管理'`，**改 DB 数据需先问用户**。
2. `ViewGridProvider.jsx` 中 `customSearchRef` 在部分页面为 undefined → `Cannot read properties of undefined (reading 'clientHeight')`。
   框架原代码，只影响表格高度计算；`fixedSearch:true` 的页面（如 Ren）稳定复现。
3. 登录后控制台 `Failed to fetch app list`（多应用取应用列表接口 401，`src/api/http.js:130` 读 undefined.data），
   首页功能未受影响。多应用功能遗留。

**不是 bug**：开了连续添加的页面（如数据字典）弹出框保存按钮叫"保存后继续添加"。

## 约定

- **每完成一个功能，立刻同时更新两处文档**（下次接手全靠它们，别等攒着一起写）：
  1. 本文件"已完成的自研功能"加一节——只写**精简索引**：一句话说清做了什么 + 关键文件路径 + 该功能特有的坑
     和非显式约定。踩到的通用性坑另外补进"关键技术坑"。
  2. `进度报告.md` 加一节——写**完整细节**：改动文件清单（表格：文件 | 改动）、设计取舍的原因、
     DB 脚本名、使用步骤、E2E 验证结果（验证了什么、什么没验证及为什么），并同步更新"三、验证结果汇总"表、
     "四、数据库变更记录"、"五、风险提示"、顶部"更新时间"。
  两处编号保持对应（本文件按主题合并时注明，如实体级分库归到功能 3、对应报告第 16 节）。
- 新功能一律加 `DB/sqlserver/升级脚本_{YYYYMMDD}_{功能名}.sql`，必须幂等（`IF NOT EXISTS` / `IF OBJECT_ID IS NULL`）；
  纯代码改动（无表结构变化）不加脚本，但要在报告里写明"无数据库变更"。
- 用户的个人数据类接口沿用快捷导航的套路：只存业务主键，展示信息从已有权限数据补；不加 `[ApiActionPermission]`。
- **记日志只用 `CustomConsole.WriteLine(NlogLoggerType.X, msg)`**（控制台 + 落盘二合一，见功能 2）；
  不要新增写 `Sys_Log` 的代码，也不要再设计"日志入库"类功能——数据库写日志开销太大，已决定弃用。
- 注释用中文，写"为什么"而不是"做了什么"，与现有代码的注释密度保持一致。
- 为验证临时改的配置/实体特性（如给实体加 `DBServer`、往 `Connections` 加连接），验证完必须还原并复测一遍，
  在报告里记下还原状态。

