using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using VOL.Core.Configuration;
using VOL.Core.DBManage;
using VOL.Core.DbSqlSugar;
using VOL.Core.Extensions;

namespace VOL.Core.DBManager
{
    /// <summary>
    /// 多数据库管理：把 Sys_DbConnection 表里的连接合并进 AppSetting.Connections
    /// 原来加一个库要改 appsettings.json 的 Connections 节点再重启，现在界面上新增即可
    ///
    /// 两个注册时机：
    ///  1.启动时 Initialize()：在 AppSetting.Init 之后、UseSqlSugar 之前调用，
    ///    这样 SqlSugarRegister.GetAllConnectionConfigs()/DBServerProvider 静态构造都能读到
    ///  2.运行时 RegisterRuntime()：界面上新增连接后立即注册到已存在的 SqlSugarScope，不用重启
    ///
    /// 有意不提供删除/移除：连接名被实体的[Entity(DBServer)]、字典DBServer、
    /// 代码生成器 Sys_TableInfo.DBServer 引用，移除会让这些功能直接抛异常
    /// </summary>
    public static class DbConnectionManager
    {
        /// <summary>
        /// 连接表不存在(未执行升级脚本)时静默跳过，不能因此起不来
        /// </summary>
        private const string LoadSql = "SELECT ConnName,DBType,ConnectionString FROM Sys_DbConnection WHERE Enabled=1";

        /// <summary>
        /// 启动时把数据库里配置的连接合并进 AppSetting.Connections
        /// appsettings.json 中已配置的同名连接优先(便于本地临时覆盖)
        /// </summary>
        public static void Initialize()
        {
            try
            {
                foreach (var conn in LoadFromDb())
                {
                    if (AppSetting.GetConnection(conn.Name) != null) continue;
                    AppSetting.AddConnection(conn);
                }
            }
            catch (Exception ex)
            {
                //升级脚本没执行/表不存在时不影响启动，只是界面上的多数据库管理不可用
                Console.WriteLine($"加载Sys_DbConnection失败(不影响启动)：{ex.Message}");
            }
        }

        /// <summary>
        /// 从默认库读取启用的连接配置(用独立的SqlSugarClient,避免与DbManger静态字段初始化互相依赖)
        /// </summary>
        private static List<NamedConnection> LoadFromDb()
        {
            var list = new List<NamedConnection>();
            using (var db = new SqlSugarClient(SqlSugarRegister.GetSysConnectionConfig()))
            {
                var rows = db.Ado.SqlQuery<DbConnectionRow>(LoadSql);
                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.ConnName) || string.IsNullOrWhiteSpace(row.ConnectionString))
                    {
                        continue;
                    }
                    list.Add(new NamedConnection
                    {
                        Name = row.ConnName.Trim(),
                        DBType = string.IsNullOrWhiteSpace(row.DBType) ? null : row.DBType.Trim(),
                        //历史数据可能存进了带不可见字符的连接串(见NormalizeConnectionString)，这里一并兜住
                        DbConnectionString = NormalizeConnectionString(Decrypt(row.ConnectionString))
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// 连接字符串落库时是DES密文，兼容历史明文(解密失败即视为明文)
        /// </summary>
        public static string Decrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return value.TryDecryptDES(AppSetting.Secret.DB, out string plain) ? plain : value;
        }

        public static string Encrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return value.EncryptDES(AppSetting.Secret.DB);
        }

        /// <summary>
        /// 规范化连接字符串里的"关键字"部分(等号左边)。
        /// 从文档/聊天工具里复制连接串时，键名中间的空格常常是不换行空格(U+00A0)、全角空格、
        /// 零宽字符或换行，肉眼与普通空格完全一样，但驱动只认普通单空格，
        /// 报出来的是 不支持的关键字:"user id" 这种"看起来明明没错"的错误，极难自查。
        /// 只处理等号左边：密码等值里的空白必须原样保留。
        /// </summary>
        public static string NormalizeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
            //值带引号时(密码里含分号只能写成 Password="a;b")按分号切会切坏，这种少见写法直接原样返回
            if (connectionString.IndexOf('"') >= 0 || connectionString.IndexOf('\'') >= 0)
            {
                return connectionString.Trim();
            }
            var parts = new List<string>();
            foreach (var segment in connectionString.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(segment)) continue;
                int index = segment.IndexOf('=');
                if (index <= 0)
                {
                    parts.Add(segment.Trim());
                    continue;
                }
                string key = NormalizeKeyword(segment.Substring(0, index));
                //值只去掉两端空白(段与段之间换行会带进来)，内部一律不动
                string value = segment.Substring(index + 1).Trim();
                parts.Add(key + "=" + value);
            }
            return string.Join(";", parts) + ";";
        }

        /// <summary>
        /// 键名规范化：零宽字符直接删掉，其余各种空白(不换行空格U+00A0、全角空格U+3000、
        /// 制表符、换行，char.IsWhiteSpace 都覆盖)统一成一个普通空格
        /// </summary>
        private static string NormalizeKeyword(string keyword)
        {
            var builder = new System.Text.StringBuilder(keyword.Length);
            bool pendingSpace = false;
            foreach (char c in keyword)
            {
                //零宽空格/零宽连接符/BOM本来就不该出现在键名里
                if (c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\uFEFF') continue;
                if (char.IsWhiteSpace(c))
                {
                    if (builder.Length > 0) pendingSpace = true;
                    continue;
                }
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }
                builder.Append(c);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 运行时注册一个新连接(界面新增后调用)，已存在则更新连接串
        /// 注册到：AppSetting.Connections、DBServerProvider、请求级与后台静态两个SqlSugarScope
        /// </summary>
        /// <param name="name">连接名(ConfigId)</param>
        /// <param name="dbType">数据库类型(MsSql/MySql/PgSql/Oracle/DM)</param>
        /// <param name="connectionString">明文连接字符串</param>
        public static void RegisterRuntime(string name, string dbType, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(connectionString)) return;
            name = name.Trim();
            //连接串可能是从库里/界面上直接拿来的，键名里的不可见字符必须先清掉(见NormalizeConnectionString)
            connectionString = NormalizeConnectionString(connectionString);

            AppSetting.AddConnection(new NamedConnection
            {
                Name = name,
                DBType = dbType,
                DbConnectionString = connectionString
            });
            DBServerProvider.SetConnection(name, connectionString);

            var config = BuildConfig(name, dbType, connectionString);
            //后台静态scope(定时任务等)
            AddToScope(DbManger.SqlSugarClient, config);
            //请求级scope(DI单例,与静态scope是两个实例)
            try
            {
                if (Utilities.HttpContext.Current?.RequestServices != null)
                {
                    AddToScope(DbManger.Db, config);
                }
            }
            catch { }
        }

        private static void AddToScope(SqlSugarScope scope, ConnectionConfig config)
        {
            if (scope == null) return;
            try
            {
                //SqlSugarScope是"一个上下文一份连接"的模型：AddConnection只对当前异步上下文(当次请求)生效，
                //后续请求会新建上下文并按内部的初始配置列表(_configs)重建连接，所以必须同时写进这个列表，
                //否则界面上新增的连接在下一个请求里又变成"未注册"(实测5.1.4.214确认)
                AddToConfigs(scope, config);
                //同名已注册时先移除再加，否则AddConnection会保留旧连接串(改了连接串保存后当次请求还是走旧库)
                if (scope.IsAnyConnection(config.ConfigId))
                {
                    //RemoveConnection要求当前上下文已经实例化过这个连接：连接只在_configs里(上一个请求注册的)
                    //而当次上下文没用过时，直接Remove会在SqlSugar内部空引用，所以先GetConnection把实例建出来。
                    //GetConnection只是构造Provider不会真正连库，坏连接串也不会在这里抛
                    scope.GetConnection(config.ConfigId);
                    scope.RemoveConnection(config.ConfigId);
                }
                scope.AddConnection(config);
            }
            catch (Exception ex)
            {
                //注册是保存成功后的附加动作，失败不能让保存的事务回滚(重启后由Initialize()补注册)
                Console.WriteLine($"运行时注册连接{config.ConfigId}失败(重启后生效)：{ex.Message}");
            }
        }

        /// <summary>
        /// 把连接写进SqlSugarScope的初始配置列表，让之后新建的上下文(后续请求)也能拿到这个连接。
        /// SqlSugar没有公开这个列表，只能反射；拿不到时降级为"仅当次请求可用"(重启后由Initialize补上)
        /// </summary>
        private static void AddToConfigs(SqlSugarScope scope, ConnectionConfig config)
        {
            try
            {
                var field = typeof(SqlSugarScope).GetField("_configs",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field?.GetValue(scope) is List<ConnectionConfig> configs)
                {
                    lock (configs)
                    {
                        configs.RemoveAll(x => Equals(x.ConfigId, config.ConfigId));
                        configs.Add(config);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入SqlSugar连接配置列表失败(新连接重启后才生效)：{ex.Message}");
            }
        }

        /// <summary>
        /// 按 SqlSugarRegister 里同一套规则构造连接配置(大小写/PgSql等设置保持一致)
        /// </summary>
        private static ConnectionConfig BuildConfig(string name, string dbType, string connectionString)
        {
            var configs = SqlSugarRegister.GetAllConnectionConfigs();
            var config = configs.FirstOrDefault(x => Equals(x.ConfigId, name));
            if (config != null) return config;
            //还没进 AppSetting.Connections 时(理论上不会走到)按类型自行构造
            var type = SqlSugarDbType.GetType(dbType);
            return new ConnectionConfig()
            {
                DbType = type,
                ConnectionString = connectionString,
                IsAutoCloseConnection = true,
                ConfigId = name,
                MoreSettings = new ConnMoreSettings() { PgSqlIsAutoToLower = false }
            };
        }

        /// <summary>
        /// 测试连接是否可用，返回失败原因(null表示成功)
        /// 连接串里的Connect Timeout可能很大(默认库就配了500秒)，各数据库改写超时参数的写法又不一样，
        /// 所以统一在外面加一个墙上时钟上限，避免保存/测试接口把请求挂死
        /// </summary>
        public static string TestConnection(string dbType, string connectionString)
        {
            const int timeoutSeconds = 20;
            //测的必须是最终会用的那一份(键名里的不可见字符已清掉)，否则"测试通过、注册后连不上"
            connectionString = NormalizeConnectionString(connectionString);
            string error = null;
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var config = new ConnectionConfig()
                    {
                        DbType = SqlSugarDbType.GetType(dbType),
                        ConnectionString = connectionString,
                        IsAutoCloseConnection = true,
                        ConfigId = "__test_" + Guid.NewGuid().ToString("N")
                    };
                    using (var db = new SqlSugarClient(config))
                    {
                        db.Ado.CommandTimeOut = timeoutSeconds;
                        db.Ado.GetScalar("SELECT 1");
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            });
            if (!task.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                //超时的连接尝试留给它自己结束(连接串里的Connect Timeout到了会退出)
                return $"连接超时({timeoutSeconds}秒),请检查服务器地址/端口/网络";
            }
            return error == null ? null : error + Suggest(dbType, connectionString, error);
        }

        /// <summary>
        /// 把驱动那句"看不懂的原文"翻成能直接照做的一句话。
        /// 驱动报错只说哪个关键字不认识，不会说"你数据库类型选错了"——
        /// 这两类错误(证书、类型选错)占了新增连接失败的绝大多数，靠原文自查很难。
        /// </summary>
        private static string Suggest(string dbType, string connectionString, string error)
        {
            string lower = (error ?? "").ToLower();
            //驱动4.0起默认Encrypt=True并校验服务器证书，自签证书必须显式信任
            if (lower.Contains("certificate") || error.Contains("证书")
                || (lower.Contains("ssl") && !lower.Contains("sslmode")))
            {
                if (!(connectionString ?? "").ToLower().Contains("trustservercertificate"))
                {
                    return "。【建议】服务器用的是自签证书，在连接字符串末尾加上 TrustServerCertificate=True;";
                }
            }
            //关键字不认识：多半是"连接串是SqlServer格式、数据库类型却选了别的"
            bool keywordError = lower.Contains("keyword not supported") || error.Contains("不支持的关键字")
                || lower.Contains("couldn't set data source") || lower.Contains("invalid connection string")
                || lower.Contains("does not exist") || lower.Contains("not supported");
            if (keywordError && LooksLikeSqlServer(connectionString)
                && SqlSugarDbType.GetType(dbType) != SqlSugar.DbType.SqlServer)
            {
                return $"。【建议】连接字符串是SqlServer的写法(Initial Catalog/TrustServerCertificate等)，"
                    + $"但[数据库类型]选的是{dbType}，请改选SqlServer";
            }
            return "";
        }

        /// <summary>
        /// 只认SqlServer独有的键名(其它库都不支持这几个)，避免误判
        /// </summary>
        private static bool LooksLikeSqlServer(string connectionString)
        {
            string lower = (connectionString ?? "").ToLower();
            return lower.Contains("initial catalog") || lower.Contains("trustservercertificate")
                || lower.Contains("persist security info") || lower.Contains("integrated security");
        }

        private class DbConnectionRow
        {
            public string ConnName { get; set; }
            public string DBType { get; set; }
            public string ConnectionString { get; set; }
        }
    }
}
