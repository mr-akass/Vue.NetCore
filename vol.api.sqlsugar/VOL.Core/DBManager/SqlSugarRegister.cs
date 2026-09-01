using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using VOL.Core.Configuration;
using VOL.Core.DBManage;
using VOL.Core.DBManager;
using VOL.Core.DbSqlSugar;

namespace VOL.Core.DBManager
{
    public static class SqlSugarRegister
    {

        /// <summary>
        ///系统库链接
        /// </summary>
        /// <returns></returns>
        public static ConnectionConfig GetSysConnectionConfig()
        {
            var dbType = DbManger.GetDbType();
            return new ConnectionConfig()
            {
                DbType = dbType,// SqlSugar.DbType.SqlServer,
                ConnectionString = DBServerProvider.GetConnectionString(null),
                IsAutoCloseConnection = true,
                ConfigId = "default",
                MoreSettings = new ConnMoreSettings()
                {
                    PgSqlIsAutoToLower = false,
                    IsAutoToUpper = IsAutoToUpper(dbType)
                },
                ConfigureExternalServices = GetConfigureExternalServices(dbType),
            };
        }

        /// <summary>
        /// 获取所有数据库连接配置：默认连接(Connection节点) + 命名连接(Connections节点)
        /// 命名连接的ConfigId即为配置节点名称，字典/代码生成器通过DBServer字段按此名称切换数据库
        /// </summary>
        /// <returns></returns>
        public static List<ConnectionConfig> GetAllConnectionConfigs()
        {
            var configs = new List<ConnectionConfig>() { GetSysConnectionConfig() };
            foreach (var conn in AppSetting.Connections)
            {
                var dbType = SqlSugarDbType.GetType(conn.DBType);
                configs.Add(new ConnectionConfig()
                {
                    DbType = dbType,
                    ConnectionString = conn.DbConnectionString,
                    IsAutoCloseConnection = true,
                    ConfigId = conn.Name,
                    MoreSettings = new ConnMoreSettings()
                    {
                        PgSqlIsAutoToLower = false,
                        IsAutoToUpper = IsAutoToUpper(dbType)
                    },
                    ConfigureExternalServices = GetConfigureExternalServices(dbType),
                });
            }
            return configs;
        }


        public static IServiceCollection UseSqlSugar(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<ISqlSugarClient>(s =>
            {
                //注册默认连接与appsettings中Connections节点下的所有命名连接
                SqlSugarScope sqlSugar = new SqlSugarScope(
                 GetAllConnectionConfigs(),
               db =>
               {
                   //单例参数配置，所有上下文生效
                   db.Aop.OnLogExecuting = (sql, pars) =>
                   {
                       if (AppSetting.ShowSqlLog)
                       {
                           Console.Write(sql);
                       }
                   };

               });
                return sqlSugar;
            });
            return services;
        }
        private static bool IsAutoToUpper(DbType dbType)
        {
            return dbType == DbType.Dm || dbType == DbType.Oracle;
        }
        /// <summary>
        /// 设置字段全大写
        /// </summary>
        /// <returns></returns>
        private static ConfigureExternalServices GetConfigureExternalServices(DbType dbType)
        {
            //https://www.donet5.com/Home/Doc?typeId=1182
            return new ConfigureExternalServices()
            {
                EntityNameService = (type, entityInfo) => { },
                EntityService = (property, column) =>
                {
                    if (IsAutoToUpper(dbType))
                    {
                        column.DbColumnName = property.Name.ToUpper();
                        //这里限制的Oralce数据库，DM数据库也会执行？
                        //按框架表名单判断而不是 Sys_ 前缀：业务表也可以叫 Sys_xxx(代码生成器生成到同一个程序集),
                        //给它按框架表规则指定一个不存在的序列会让插入直接失败
                        if (dbType == DbType.Oracle && column.PropertyInfo.PropertyType == typeof(int)
                           && EntityDbRouter.IsFrameworkTableName(property.DeclaringType.Name))
                        {
                            //oralce系统表设置自增
                            if (column.PropertyInfo.GetCustomAttribute<KeyAttribute>() != null)
                            {
                                column.IsIdentity = false;
                                column.OracleSequenceName = $"T_{property.DeclaringType.Name.ToUpper()}_SEQ";
                            }
                        }
                    }
                }
            };
        }
    }
}
