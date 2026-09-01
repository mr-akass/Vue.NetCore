using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.DBManager;
using VOL.Core.Enums;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;

namespace VOL.Core.DbSqlSugar
{
    public class DbManger
    {
        /// <summary>
        /// 获取系统库：后台异步使用
        /// </summary>
        public static SqlSugarScope SqlSugarClient = new SqlSugarScope(
          //注册默认连接与appsettings中Connections节点下的所有命名连接
          SqlSugarRegister.GetAllConnectionConfigs(),
         db =>
         {
             db.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine(sql);//输出sql,查看执行sql 性能无影响

            };
         });
        public static ISqlSugarClient GetSqlSugarClient(string dbContextName = null)
        {
            return GetConnection(dbContextName);
        }
        public static ISqlSugarClient GetConnection(string configId)
        {
            //其他配置文件里面的自定义数据库链接名称
            return Db.GetConnection(configId);
        }

        /// <summary>
        /// 根据DBServer名称(即Connections节点中的连接名/ConfigId)获取数据库连接
        /// dbServer为空、default或未配置时返回默认连接；请求上下文不可用时使用后台静态连接
        /// </summary>
        /// <param name="dbServer"></param>
        /// <returns></returns>
        public static ISqlSugarClient GetDbClient(string dbServer = null)
        {
            SqlSugarScope scope;
            try
            {
                scope = HttpContext.Current?.RequestServices != null ? Db : SqlSugarClient;
            }
            catch
            {
                scope = SqlSugarClient;
            }
            if (string.IsNullOrWhiteSpace(dbServer)
                || string.Equals(dbServer, DBManage.DbName.Default, StringComparison.OrdinalIgnoreCase))
            {
                return scope;
            }
            //未注册的连接名回退到默认连接，兼容DBServer字段的历史数据
            return scope.IsAnyConnection(dbServer) ? scope.GetConnection(dbServer) : scope;
        }

        public static SqlSugarScope Db
        {
            get
            {
                var obj = HttpContext.Current.RequestServices.GetService<ISqlSugarClient>();
                return (SqlSugarScope)obj;
            }
        }

        public static DbType GetDbType()
        {
            if (Const.DBType.Name == DbCurrentType.MsSql.ToString())
            {
                return DbType.SqlServer;
            }
            else if (Const.DBType.Name == DbCurrentType.MySql.ToString())
            {
                return DbType.MySql;
            }
            else if (Const.DBType.Name == DbCurrentType.PgSql.ToString())
            {
                return DbType.PostgreSQL;
            }
            else if (Const.DBType.Name == DbCurrentType.DM.ToString())
            {
                return DbType.Dm;
            }
            throw new Exception("未实现数据库");
        }
    }
}
