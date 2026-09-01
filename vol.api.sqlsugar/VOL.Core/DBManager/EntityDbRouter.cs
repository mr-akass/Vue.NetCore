using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SqlSugar;
using VOL.Core.DbSqlSugar;
using VOL.Entity;

namespace VOL.Core.DBManager
{
    /// <summary>
    /// 实体级分库路由：按实体上的 [Entity(DBServer = "连接名")] 把数据访问指向对应的库
    ///
    /// 背景：代码生成器早就会把表配置的 DBServer 写进实体特性(Sys_TableInfoService.CreateDomainModel)，
    /// 但运行时从来没有人读它——生成的 Repository 构造函数固定注入 VOLContext(默认库)，
    /// 导致"从B库生成的业务模块，跑起来查的是默认库"(默认库没这张表就报错，有同名表则静默写错库)。
    /// 这里补上这条链路：连接名 = SqlSugar 的 ConfigId = Sys_DbConnection.ConnName。
    ///
    /// 两条硬性规则：
    ///  1) 框架自身的 Sys_* 表强制留在默认库(权限/菜单/字典/工作流都依赖它们，被误路由等于系统瘫痪)
    ///  2) 主表与子表必须在同一个库(跨库没有事务，主子表分库会半提交)，不满足直接抛异常而不是默默写坏数据
    /// </summary>
    public static class EntityDbRouter
    {
        /// <summary>
        /// 实体类型 => 实体特性上配的连接名(null 表示默认库)。
        /// 这里只缓存"反射+主子表校验"的结果，不缓存"该连接是否已注册"——
        /// 数据库管理页可以在运行时新增连接，把未注册的结论也缓存下来会让新连接一直不生效
        /// </summary>
        private static readonly ConcurrentDictionary<Type, string> _routes = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// 取实体所属的连接名，返回 null 表示走默认库
        /// </summary>
        public static string GetDbServer(Type entityType)
        {
            if (entityType == null) return null;
            string dbServer = _routes.GetOrAdd(entityType, ResolveDbServer);
            if (dbServer == null) return null;
            //未注册的连接名回退默认库：兼容 DBServer="SysDbContext"/"ServiceDbContext" 这类历史值,
            //与 DbManger.GetDbClient 的回退行为保持一致(否则老项目升级后整片实体直接报错)
            return Configuration.AppSetting.GetConnection(dbServer) == null ? null : dbServer;
        }

        public static string GetDbServer<TEntity>()
        {
            return GetDbServer(typeof(TEntity));
        }

        /// <summary>
        /// 取实体对应的数据库连接(未配置/未注册/框架表都返回默认库连接)
        /// </summary>
        public static ISqlSugarClient GetClient(Type entityType)
        {
            return DbManger.GetDbClient(GetDbServer(entityType));
        }

        public static ISqlSugarClient GetClient<TEntity>()
        {
            return GetClient(typeof(TEntity));
        }

        /// <summary>
        /// 在已有连接的基础上按实体路由：这是框架内部最常用的入口
        /// (BaseDbContext/RepositoryBase 手里已经有一个 client，只有实体确实配了别的库时才切换)
        ///
        /// 只对 SqlSugarScope(注册了全部连接的"总入口")做切换：
        /// 传进来的若已经是某个具体连接(SqlSugarProvider)，说明调用方明确指定过库，保持原样不抢方向盘，
        /// 同时也避免"已路由的连接再路由一次"
        /// </summary>
        public static ISqlSugarClient Route(Type entityType, ISqlSugarClient client)
        {
            string dbServer = GetDbServer(entityType);
            if (dbServer == null)
            {
                //默认库：原样返回，框架表/未配置 DBServer 的实体行为与改造前完全一致
                return client ?? DbManger.GetDbClient(null);
            }
            if (client is not SqlSugarScope scope)
            {
                return client ?? DbManger.GetDbClient(dbServer);
            }
            return scope.IsAnyConnection(dbServer) ? scope.GetConnection(dbServer) : scope;
        }

        public static ISqlSugarClient Route<TEntity>(ISqlSugarClient client)
        {
            return Route(typeof(TEntity), client);
        }

        /// <summary>
        /// 该实体是否被路由到了非默认库(排障用)
        /// </summary>
        public static bool IsRouted(Type entityType)
        {
            return GetDbServer(entityType) != null;
        }

        /// <summary>
        /// 框架自身的表名(小写)：这些表必须留在默认库，不接受实体特性上的 DBServer。
        /// 权限/菜单/字典/工作流/代码生成器全依赖它们，被路由到别的库等于系统瘫痪；
        /// 官方 Sys_User 自带 DBServer="SysDbContext" 历史值，一旦有人恰好建了同名连接就会中招。
        ///
        /// 为什么是写死的名单而不是"Sys_ 前缀 + VOL.Entity 程序集"：
        /// 代码生成器把**所有**业务实体也生成到 VOL.Entity/DomainModels 下(同一个程序集)，
        /// 用户完全可以在别的库里建一张叫 Sys_Area 的业务表——按前缀判断会把它当框架表强留默认库，
        /// 表现就是页面查询报 Invalid object name(实际踩到过)。
        /// 官方升级包新增的 Sys_* 表不在名单里也无妨：它们本来就没配 DBServer，Normalize 后就是默认库。
        /// </summary>
        private static readonly HashSet<string> _frameworkTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Sys_User", "Sys_Role", "Sys_RoleAuth", "Sys_Menu", "Sys_Department", "Sys_UserDepartment",
            "Sys_UserRole", "Sys_UserShortcut", "Sys_Dictionary", "Sys_DictionaryList", "vSys_Dictionary",
            "Sys_Log", "Sys_Message", "Sys_MessageUser", "Sys_Language", "Sys_Application",
            "Sys_ConfigSetting", "Sys_DbConnection", "Sys_TableInfo", "Sys_TableColumn",
            "Sys_QuartzOptions", "Sys_QuartzLog",
            "Sys_WorkFlow", "Sys_WorkFlowStep", "Sys_WorkFlowTable", "Sys_WorkFlowTableStep",
            "Sys_WorkFlowTableAuditLog"
        };

        /// <summary>
        /// 是否是框架自身的表(代码生成器也要用它拦"业务表占用框架表名")
        /// </summary>
        public static bool IsFrameworkTableName(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) && _frameworkTables.Contains(tableName.Trim());
        }

        /// <summary>
        /// 框架自身的表：这些表必须在默认库，不接受实体特性上的 DBServer
        /// </summary>
        private static bool IsFrameworkEntity(Type entityType)
        {
            //必须同时满足"名字在框架表名单里"和"定义在 VOL.Entity 程序集里"：
            //业务项目自己另建程序集放同名实体时不受影响
            return IsFrameworkTableName(entityType.Name)
                && entityType.Assembly == typeof(EntityAttribute).Assembly;
        }

        private static string ResolveDbServer(Type entityType)
        {
            if (IsFrameworkEntity(entityType)) return null;

            var attribute = entityType.GetCustomAttribute<EntityAttribute>();
            //未注册的连接名回退默认库：兼容 DBServer="SysDbContext"/"ServiceDbContext" 这类历史值,
            //与 DbManger.GetDbClient 的回退行为保持一致(否则老项目升级后整片实体直接报错)
            string dbServer = Normalize(attribute?.DBServer);

            //主表在默认库、子表却配了别的库同样是跨库，所以回退后仍要做这一步校验
            EnsureDetailSameDb(entityType, attribute, dbServer);
            return dbServer;
        }

        /// <summary>
        /// 规范化连接名：空白/默认库名/未注册的连接都归一成 null(默认库)
        /// </summary>
        private static string Normalize(string dbServer)
        {
            if (string.IsNullOrWhiteSpace(dbServer)) return null;
            dbServer = dbServer.Trim();
            if (string.Equals(dbServer, DBManage.DbName.Default, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return Configuration.AppSetting.GetConnection(dbServer) == null ? null : dbServer;
        }

        /// <summary>
        /// 主子表必须同库：跨库没有事务，主表提交、子表失败就是脏数据，所以在第一次路由时就拦掉。
        /// 抛异常而不是回退默认库——回退等于把子表写进另一个库，比直接报错更难查。
        /// 二、三级明细一起校验：删除主表时是"先删三级、再删二级"，三级明细先被路由，
        /// 只校验直接子表的话三级配错库会在校验之前就把删除语句发到别的库去
        /// </summary>
        private static void EnsureDetailSameDb(Type mainType, EntityAttribute attribute, string mainDbServer)
        {
            var visited = new HashSet<Type> { mainType };
            EnsureDetailSameDb(mainType, attribute, mainDbServer, visited);
        }

        private static void EnsureDetailSameDb(Type mainType, EntityAttribute attribute, string mainDbServer, HashSet<Type> visited)
        {
            var detailTypes = attribute?.DetailTable;
            if (detailTypes == null || detailTypes.Length == 0) return;

            foreach (var detailType in detailTypes.Where(x => x != null))
            {
                //明细表互相引用(A的明细是B、B的明细又是A)会无限递归，走过的类型不再展开
                if (!visited.Add(detailType)) continue;

                //子表按同一套规则解析(不能直接递归 GetDbServer：主表还在 GetOrAdd 的委托里，
                //子表若反向引用主表会死锁)，这里只需要它自己的特性值
                var detailAttribute = detailType.GetCustomAttribute<EntityAttribute>();
                string detailDbServer = IsFrameworkEntity(detailType)
                    ? null
                    : Normalize(detailAttribute?.DBServer);
                if (!string.Equals(detailDbServer ?? "", mainDbServer ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"主表[{mainType.Name}]所在数据库为[{mainDbServer ?? "默认库"}]," +
                        $"子表[{detailType.Name}]为[{detailDbServer ?? "默认库"}]。" +
                        "主子表跨库无法保证事务一致性,请把它们配置到同一个数据库。");
                }
                EnsureDetailSameDb(detailType, detailAttribute, mainDbServer, visited);
            }
        }
    }
}
