using SqlSugar;
using System;
using VOL.Core.Configuration;
using VOL.Core.Const;

namespace VOL.Core.DBManage
{
    public static class SqlSugarDbType
    {
        /// <summary>
        /// 根据配置文件中指定的xxDbType获取对应的数据库类型
        /// dbContextName可传：null(默认库类型)、数据库类型名(如MySql)、
        /// 或Connections节点中的连接名(如ReportDB，取该连接配置的DBType)
        /// </summary>
        /// <param name="dbContextName"></param>
        /// <returns></returns>
        public static DbType GetType(string dbContextName = null)
        {
            if (!string.IsNullOrWhiteSpace(dbContextName))
            {
                //优先按命名连接(Connections节点)解析
                var named = AppSetting.GetConnection(dbContextName);
                if (named != null)
                {
                    return Parse(named.DBType) ?? Parse(DBType.Name) ?? DbType.SqlServer;
                }
                //不是已注册的连接名时当作数据库类型名(如直接传"MySql")；
                //两者都不是就落到默认库类型——DBServer="ServiceDbContext"这类历史值走的是这条路,
                //运行时这些实体本身也被回退到了默认库(见EntityDbRouter)，类型必须跟着一起回退，
                //否则默认库是mysql/pgsql时会按sqlserver语法拼sql
                var parsed = Parse(dbContextName);
                if (parsed != null) return parsed.Value;
            }
            return Parse(DBType.Name) ?? DbType.SqlServer;
        }

        /// <summary>
        /// 是否是能识别的数据库类型名。GetType对不认识的值是静默回退到默认库类型的(兼容历史DBServer值)，
        /// 保存连接时不能跟着静默——类型选错会让驱动去解析另一种语法的连接串，
        /// 报出来的是"不支持的关键字"这种完全指不到病根的错误
        /// </summary>
        public static bool IsSupportedName(string dbType)
        {
            return Parse(dbType) != null;
        }

        /// <summary>
        /// 数据库类型名 => SqlSugar的DbType，不认识的返回null(由调用方决定回退到哪个类型)
        /// </summary>
        private static DbType? Parse(string dbType)
        {
            //配置连接不同的数据库类型，比如同时使用mysql、sqlserver、pgsql数据库
            switch ((dbType ?? "").ToLower())
            {
                case "mssql":
                case "sqlserver":
                    return DbType.SqlServer;
                case "mysql":
                    return DbType.MySql;
                case "oracle":
                    return DbType.Oracle;
                case "pgsql":
                    return DbType.PostgreSQL;
                case "kdbndp":
                    return DbType.Kdbndp;
                case "gaussdb":
                    return DbType.GaussDB;
                case "oceanbase":
                    return DbType.OceanBase;
                case "dm":
                    return DbType.Dm;
                default:
                    return null;
            }
        }
    }
}
