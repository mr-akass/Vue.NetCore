using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using VOL.Core.Const;
using VOL.Core.Extensions;
using Yitter.IdGenerator;

namespace VOL.Core.Configuration
{
    public static class AppSetting
    {
        public static IConfiguration Configuration { get; private set; }

        public static string DbConnectionString
        {
            get { return _connection.DbConnectionString; }
        }

        public static string RedisConnectionString
        {
            get { return _connection.RedisConnectionString; }
        }

        public static bool UseRedis
        {
            get { return _connection.UseRedis; }
        }
        public static bool UseSignalR
        {
            get { return _connection.UseSignalR; }
        }

        /// <summary>
        /// 多数据库命名连接(appsettings.json→Connections节点)，不含默认连接
        /// </summary>
        public static List<NamedConnection> Connections { get; private set; } = new List<NamedConnection>();

        /// <summary>
        /// 根据名称(ConfigId)获取命名连接，未配置返回null
        /// </summary>
        public static NamedConnection GetConnection(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return Connections.Find(x => string.Equals(x.Name, name, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 追加/更新一个命名连接(数据库管理页新增连接时调用，见DbConnectionManager)
        /// 采用写时复制：Connections 会被其他请求同时遍历(SqlSugarRegister/DBServerProvider)，
        /// 直接往同一个List里Add会让正在foreach的线程抛"集合已修改"
        /// </summary>
        public static void AddConnection(NamedConnection connection)
        {
            if (connection == null || string.IsNullOrWhiteSpace(connection.Name)) return;
            if (string.IsNullOrWhiteSpace(connection.DBType))
            {
                connection.DBType = _connection?.DBType;
            }
            lock (_connectionsLock)
            {
                var exists = GetConnection(connection.Name);
                if (exists != null)
                {
                    exists.DBType = connection.DBType;
                    exists.DbConnectionString = connection.DbConnectionString;
                    return;
                }
                Connections = new List<NamedConnection>(Connections) { connection };
            }
        }

        private static readonly object _connectionsLock = new object();
        public static Secret Secret { get; private set; }

        public static CreateMember CreateMember { get; private set; }

        public static ModifyMember ModifyMember { get; private set; }

        private static Connection _connection;

        public static string TokenHeaderName = "Authorization";

        /// <summary>
        /// Actions权限过滤
        /// </summary>
        public static GlobalFilter GlobalFilter { get; set; }

        /// <summary>
        /// kafka配置
        /// </summary>
        public static Kafka Kafka { get; set; }


        /// <summary>
        /// JWT有效期(分钟=默认120)
        /// </summary>
        public static int ExpMinutes { get; private set; } = 120;

        /// <summary>
        /// 当前运行环境标识(DEV/STG/PRD)，由appsettings.{环境}.json中的RunningEnvironment配置
        /// </summary>
        public static string RunningEnvironment { get; private set; }

        // 是否启用雪花ID
        public static bool EnableSnowFlakeID { get; set; } = false;
        /// <summary>
        /// 是否显示sql日志
        /// </summary>
        public static bool ShowSqlLog { get; set; }
        public static string CurrentPath { get; private set; } = null;
        public static string DownLoadPath { get { return CurrentPath + "\\Download\\"; } }
        /// <summary>
        /// 国密 SM2/SM3/SM4（appsettings.json → GmCrypto）
        /// </summary>
        public static GmCryptoOptions GmCrypto { get; private set; }
        public static void Init(IServiceCollection services, IConfiguration configuration)
        {
            Configuration = configuration;
            services.Configure<Secret>(configuration.GetSection("Secret"));
            services.Configure<Connection>(configuration.GetSection("Connection"));
            services.Configure<CreateMember>(configuration.GetSection("CreateMember"));
            services.Configure<ModifyMember>(configuration.GetSection("ModifyMember"));
            services.Configure<GlobalFilter>(configuration.GetSection("GlobalFilter"));
            services.Configure<Kafka>(configuration.GetSection("Kafka"));

            var provider = services.BuildServiceProvider();
            IWebHostEnvironment environment = provider.GetRequiredService<IWebHostEnvironment>();
            CurrentPath = Path.Combine(environment.ContentRootPath, "").ReplacePath();

            Secret = provider.GetRequiredService<IOptions<Secret>>().Value;

            //设置修改或删除时需要设置为默认用户信息的字段
            CreateMember = provider.GetRequiredService<IOptions<CreateMember>>().Value ?? new CreateMember();
            ModifyMember = provider.GetRequiredService<IOptions<ModifyMember>>().Value ?? new ModifyMember();

            GlobalFilter = provider.GetRequiredService<IOptions<GlobalFilter>>().Value ?? new GlobalFilter();

            GlobalFilter.Actions = GlobalFilter.Actions ?? new string[0];
            Kafka = provider.GetRequiredService<IOptions<Kafka>>().Value ?? new Kafka();
            GmCrypto = provider.GetRequiredService<IOptions<GmCryptoOptions>>().Value ?? new GmCryptoOptions();

            _connection = provider.GetRequiredService<IOptions<Connection>>().Value;

            ShowSqlLog = configuration["ShowSqlLog"] == "1";

            RunningEnvironment = configuration["RunningEnvironment"] ?? "";


            ExpMinutes = (configuration["ExpMinutes"] ?? "120").GetInt();

            EnableSnowFlakeID = (configuration["EnableSnowFlakeID"] ?? "false").GetBool();

            if (EnableSnowFlakeID)
            {
                var options = configuration.GetSection("SnowFlakeOptions").Get<Yitter.IdGenerator.IdGeneratorOptions>();
                YitIdHelper.SetIdGenerator(options);
            }

            DBType.Name = _connection.DBType;
            if (string.IsNullOrEmpty(_connection.DbConnectionString))
                throw new System.Exception("未配置好数据库默认连接");

            try
            {
                _connection.DbConnectionString = _connection.DbConnectionString.DecryptDES(Secret.DB);
            }
            catch { }

            //加载多数据库命名连接(Connections节点，可选)，节点下每个子节点名称即为连接标识(ConfigId)
            Connections = new List<NamedConnection>();
            foreach (var section in configuration.GetSection("Connections").GetChildren())
            {
                string connStr = section["DbConnectionString"];
                if (string.IsNullOrWhiteSpace(connStr)) continue;
                //默认连接固定使用Connection节点，忽略与其同名的配置
                if (string.Equals(section.Key, "default", System.StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    connStr = connStr.DecryptDES(Secret.DB);
                }
                catch { }
                Connections.Add(new NamedConnection
                {
                    Name = section.Key,
                    DBType = section["DBType"] ?? _connection.DBType,
                    DbConnectionString = connStr
                });
            }

            if (!string.IsNullOrEmpty(_connection.RedisConnectionString))
            {
                try
                {
                    _connection.RedisConnectionString = _connection.RedisConnectionString.DecryptDES(Secret.Redis);
                }
                catch { }
            }

        }
        // 多个节点name格式 ：["key:key1"]
        public static string GetSettingString(string key)
        {
            return Configuration[key];
        }
        // 多个节点,通过.GetSection("key")["key1"]获取
        public static IConfigurationSection GetSection(string key)
        {
            return Configuration.GetSection(key);
        }
    }

    public class Connection
    {
        public string DBType { get; set; }
        public string DbConnectionString { get; set; }
        public string RedisConnectionString { get; set; }
        public bool UseRedis { get; set; }
        public bool UseSignalR { get; set; }
    }

    /// <summary>
    /// 多数据库命名连接(appsettings.json→Connections节点下的子节点)
    /// Name为连接标识(即SqlSugar的ConfigId，字典/代码生成器中DBServer字段保存的值)
    /// </summary>
    public class NamedConnection
    {
        public string Name { get; set; }
        public string DBType { get; set; }
        public string DbConnectionString { get; set; }
    }

    public class CreateMember : TableDefaultColumns
    {
    }
    public class ModifyMember : TableDefaultColumns
    {
    }

    public abstract class TableDefaultColumns
    {
        public string UserIdField { get; set; }
        public string UserNameField { get; set; }
        public string DateField { get; set; }
    }
    public class GlobalFilter
    {
        public string Message { get; set; }
        public bool Enable { get; set; }
        public string[] Actions { get; set; }
    }

    public class Kafka
    {
        public bool UseProducer { get; set; }
        public ProducerSettings ProducerSettings { get; set; }
        public bool UseConsumer { get; set; }
        public bool IsConsumerSubscribe { get; set; }
        public ConsumerSettings ConsumerSettings { get; set; }
        public Topics Topics { get; set; }
    }
    public class ProducerSettings
    {
        public string BootstrapServers { get; set; }
        public string SaslMechanism { get; set; }
        public string SecurityProtocol { get; set; }
        public string SaslUsername { get; set; }
        public string SaslPassword { get; set; }
    }
    public class ConsumerSettings
    {
        public string BootstrapServers { get; set; }
        public string SaslMechanism { get; set; }
        public string SecurityProtocol { get; set; }
        public string SaslUsername { get; set; }
        public string SaslPassword { get; set; }
        public string GroupId { get; set; }
    }
    public class Topics
    {
        public string TestTopic { get; set; }
    }
}
