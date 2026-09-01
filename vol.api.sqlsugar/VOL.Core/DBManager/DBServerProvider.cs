using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VOL.Core.Configuration;
using VOL.Core.Const;
using VOL.Core.DbContext;
using VOL.Core.DbSqlSugar;
using VOL.Core.Enums;
using VOL.Core.Extensions;

namespace VOL.Core.DBManager
{
    public partial class DBServerProvider: DbManger
    {
        //运行时(数据库管理页新增连接)会往里写，同时可能有其他请求在读，
        //普通Dictionary并发读写会破坏内部结构(表现为偶发死循环/取不到已注册的连接)，所以用并发字典
        private static ConcurrentDictionary<string, string> ConnectionArray = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string DefaultConnName = "defalut";

        static DBServerProvider()
        {
            SetConnection(DefaultConnName, AppSetting.DbConnectionString);
            //注册appsettings中Connections节点下的所有命名连接
            foreach (var conn in AppSetting.Connections)
            {
                SetConnection(conn.Name, conn.DbConnectionString);
            }
        }
        public static void SetConnection(string key, string val)
        {
            ConnectionArray[key] = val;
        }
        /// <summary>
        /// 设置默认数据库连接
        /// </summary>
        /// <param name="val"></param>
        public static void SetDefaultConnection(string val)
        {
            SetConnection(DefaultConnName, val);
        }

        public static string GetConnectionString(string key)
        {
            key = key ?? DefaultConnName;
            if (ConnectionArray.TryGetValue(key, out string connectionString))
            {
                return connectionString;
            }
            return key;
        }
        /// <summary>
        /// 获取默认数据库连接
        /// </summary>
        /// <returns></returns>
        public static string GetConnectionString()
        {
            return GetConnectionString(DefaultConnName);
        }
        public static VOLContext DbContext
        {
            get { return Utilities.HttpContext.Current.RequestServices.GetService(typeof(VOLContext)) as VOLContext; }
        }
    }
}
