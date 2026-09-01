/*
*数据库管理业务实现(多数据库支持)：新增连接后立即注册到SqlSugar,不用重启
*刻意不支持删除：连接名(ConfigId)被实体[Entity(DBServer)]、字典DBServer、
*代码生成器Sys_TableInfo.DBServer引用，删掉这些功能会直接抛异常/生成失败，
*需要停用时把"是否启用"关掉(已有引用会回退到默认库而不是崩溃)。
*/
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Configuration;
using VOL.Core.DBManage;
using VOL.Core.DBManager;
using VOL.Core.DbSqlSugar;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Infrastructure;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VOL.Core.CacheManager;

namespace VOL.Sys.Services
{
    public partial class Sys_DbConnectionService
    {
        /// <summary>
        /// 连接串里的密码在列表/编辑时用它代替，用户不改动就保持原值
        /// </summary>
        private const string PasswordMask = "******";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_DbConnectionRepository _repository;//访问数据库

        [ActivatorUtilitiesConstructor]
        public Sys_DbConnectionService(
            ISys_DbConnectionRepository dbRepository,
            IHttpContextAccessor httpContextAccessor
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 列表不能把连接串明文(含密码)发到前端
        /// </summary>
        public override PageGridData<Sys_DbConnection> GetPageData(PageDataOptions options)
        {
            GetPageDataOnExecuted = MaskRows;
            return base.GetPageData(options);
        }

        public override async Task<PageGridData<Sys_DbConnection>> GetPageDataAsync(PageDataOptions options)
        {
            GetPageDataOnExecuted = MaskRows;
            return await base.GetPageDataAsync(options);
        }

        private void MaskRows(PageGridData<Sys_DbConnection> grid)
        {
            grid?.rows?.ForEach(x => x.ConnectionString = MaskPassword(DbConnectionManager.Decrypt(x.ConnectionString)));
        }

        public override WebResponseContent Add(SaveModel saveModel)
        {
            var check = CheckAndNormalize(saveModel, null);
            if (!check.Status) return check;

            AddOnExecuted = (Sys_DbConnection entity, object list) =>
            {
                //新增成功后立即注册到运行中的SqlSugar,当次请求就能用新库(不用重启)
                RegisterAndRefreshCache(entity);
                return new WebResponseContent(true);
            };
            return base.Add(saveModel);
        }

        public override WebResponseContent Update(SaveModel saveModel)
        {
            int id = saveModel?.MainData != null && !saveModel.MainData.DicKeyIsNullOrEmpty("ID")
                ? saveModel.MainData["ID"].GetInt() : 0;
            var origin = id > 0 ? _repository.FindAsIQueryable(x => x.ID == id).First() : null;
            if (origin == null)
            {
                return new WebResponseContent().Error("未找到要修改的连接");
            }
            //连接名是ConfigId,改名等于把老名字删掉,会让已引用它的实体/字典/代码生成器失效
            if (saveModel.MainData.ContainsKey("ConnName"))
            {
                string newName = (saveModel.MainData["ConnName"] ?? "").ToString().Trim();
                if (!string.Equals(newName, origin.ConnName, StringComparison.OrdinalIgnoreCase))
                {
                    return new WebResponseContent().Error("连接名称不能修改(其他配置以名称引用此连接),请新增一个连接");
                }
            }
            var check = CheckAndNormalize(saveModel, origin);
            if (!check.Status) return check;

            UpdateOnExecuted = (Sys_DbConnection entity, object addList, object editList, List<object> delKeys) =>
            {
                var latest = _repository.FindAsIQueryable(x => x.ID == entity.ID).First();
                RegisterAndRefreshCache(latest);
                return new WebResponseContent(true);
            };
            return base.Update(saveModel);
        }

        //前端(ViewGrid)可能走异步接口addAsync/updateAsync，逻辑完全一致，直接复用同步实现
        public override async Task<WebResponseContent> AddAsync(SaveModel saveModel)
        {
            await Task.CompletedTask;
            return Add(saveModel);
        }

        public override async Task<WebResponseContent> UpdateAsync(SaveModel saveModel)
        {
            await Task.CompletedTask;
            return Update(saveModel);
        }

        /// <summary>
        /// 禁止删除(前端菜单也没给删除权限,这里是兜底:直接调接口同样删不掉)
        /// </summary>
        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            return new WebResponseContent().Error("数据库连接不支持删除(会导致引用该连接的字典/代码生成器/实体报错),请改为停用");
        }

        public override async Task<WebResponseContent> DelAsync(object[] keys, bool delList = true)
        {
            await Task.CompletedTask;
            return new WebResponseContent().Error("数据库连接不支持删除(会导致引用该连接的字典/代码生成器/实体报错),请改为停用");
        }

        /// <summary>
        /// 测试连接(不落库,新增前先验证连接串是否能连上)
        /// </summary>
        public async Task<WebResponseContent> TestConnectionAsync(string connName, string dbType, string connectionString)
        {
            await Task.CompletedTask;
            var webResponse = new WebResponseContent();
            //编辑时密码被掩码了,取库里的原值来测
            if (!string.IsNullOrWhiteSpace(connName) && IsMasked(connectionString))
            {
                var origin = _repository.FindAsIQueryable(x => x.ConnName == connName).First();
                if (origin != null)
                {
                    connectionString = DbConnectionManager.Decrypt(origin.ConnectionString);
                    dbType = string.IsNullOrWhiteSpace(dbType) ? origin.DBType : dbType;
                }
            }
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return webResponse.Error("请填写连接字符串");
            }
            //测试时也校验类型：类型选错会让另一种驱动去解析这份连接串，报的错完全指不到病根
            if (!string.IsNullOrWhiteSpace(dbType) && !SqlSugarDbType.IsSupportedName(dbType))
            {
                return webResponse.Error($"不支持的数据库类型{dbType},请从下拉中选择");
            }
            string error = DbConnectionManager.TestConnection(dbType, connectionString);
            return error == null ? webResponse.OK("连接成功") : webResponse.Error($"连接失败：{error}");
        }

        /// <summary>
        /// 当前已注册到SqlSugar的连接(排查"配了但用不了"时看这里)
        /// </summary>
        public async Task<object> GetRegisteredAsync()
        {
            await Task.CompletedTask;
            var rows = _repository.FindAsIQueryable(x => true).ToList();
            var list = new List<object>
            {
                new { connName = "default", dbType = VOL.Core.Const.DBType.Name, source = "appsettings.json→Connection", enabled = true, registered = true }
            };
            foreach (var conn in AppSetting.Connections)
            {
                var db = rows.FirstOrDefault(x => string.Equals(x.ConnName, conn.Name, StringComparison.OrdinalIgnoreCase));
                list.Add(new
                {
                    connName = conn.Name,
                    dbType = conn.DBType,
                    source = db == null ? "appsettings.json→Connections" : "数据库管理",
                    enabled = db?.Enabled ?? true,
                    registered = DbManger.SqlSugarClient.IsAnyConnection(conn.Name)
                });
            }
            return list;
        }

        /// <summary>
        /// 校验并把提交数据规范化：连接名合法性/重名、连接串加密、密码掩码还原、连通性测试
        /// </summary>
        private WebResponseContent CheckAndNormalize(SaveModel saveModel, Sys_DbConnection origin)
        {
            var webResponse = new WebResponseContent();
            if (saveModel?.MainData == null)
            {
                return webResponse.Error("没有提交数据");
            }
            string connName = saveModel.MainData.ContainsKey("ConnName")
                ? (saveModel.MainData["ConnName"] ?? "").ToString().Trim() : origin?.ConnName;
            if (string.IsNullOrWhiteSpace(connName))
            {
                return webResponse.Error("请填写连接名称");
            }
            //连接名会直接作为ConfigId拼到代码/配置里,限制成标识符避免奇怪字符
            if (!Regex.IsMatch(connName, "^[A-Za-z][A-Za-z0-9_]{0,49}$"))
            {
                return webResponse.Error("连接名称只能是字母、数字、下划线且以字母开头(最多50个字符)");
            }
            if (string.Equals(connName, DbName.Default, StringComparison.OrdinalIgnoreCase))
            {
                return webResponse.Error("default为默认库保留名称,请换一个");
            }
            if (origin == null && _repository.Exists(x => x.ConnName == connName))
            {
                return webResponse.Error($"连接名称{connName}已存在");
            }

            string dbType = saveModel.MainData.ContainsKey("DBType")
                ? (saveModel.MainData["DBType"] ?? "").ToString().Trim() : origin?.DBType;
            if (string.IsNullOrWhiteSpace(dbType))
            {
                return webResponse.Error("请选择数据库类型");
            }
            //类型名不认识时SqlSugarDbType.GetType会静默回退到默认库类型，存下去等于埋一个"连的库不是你选的库"
            if (!SqlSugarDbType.IsSupportedName(dbType))
            {
                return webResponse.Error($"不支持的数据库类型{dbType},请从下拉中选择");
            }

            string connectionString = saveModel.MainData.ContainsKey("ConnectionString")
                ? (saveModel.MainData["ConnectionString"] ?? "").ToString().Trim() : null;
            if (connectionString == null || IsMasked(connectionString))
            {
                //没改连接串(前端回显的是掩码)就用库里的原值
                connectionString = DbConnectionManager.Decrypt(origin?.ConnectionString);
            }
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return webResponse.Error("请填写连接字符串");
            }
            //从文档/聊天工具复制来的连接串键名里常带不可见字符(不换行空格等)，落库前清掉,
            //否则表现为"字符串肉眼完全正确却报不支持的关键字"
            connectionString = DbConnectionManager.NormalizeConnectionString(connectionString);

            //连不上的连接不允许存,否则一旦被字典/代码生成器引用会到处报错
            string error = DbConnectionManager.TestConnection(dbType, connectionString);
            if (error != null)
            {
                return webResponse.Error($"连接测试失败,请检查连接字符串：{error}");
            }

            saveModel.MainData["ConnName"] = connName;
            saveModel.MainData["DBType"] = dbType;
            saveModel.MainData["ConnectionString"] = DbConnectionManager.Encrypt(connectionString);
            //前端switch提交的是1/0，框架DicToEntity对非空bool是走Convert.ChangeType("1",bool)转不过去(异常后当null→false)，
            //会出现界面上开着、存进去却是停用的情况，这里统一规范成真正的bool
            if (saveModel.MainData.ContainsKey("Enabled"))
            {
                saveModel.MainData["Enabled"] = ToBool(saveModel.MainData["Enabled"], origin?.Enabled ?? true);
            }
            return webResponse.OK();
        }

        /// <summary>
        /// 1/0、"1"/"0"、true/false、"true"/"on"都算成开关值，识别不了就用原值
        /// </summary>
        private static bool ToBool(object value, bool defaultValue)
        {
            string text = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(text)) return defaultValue;
            if (bool.TryParse(text, out bool result)) return result;
            if (int.TryParse(text, out int number)) return number != 0;
            return string.Equals(text, "on", StringComparison.OrdinalIgnoreCase) || defaultValue;
        }

        /// <summary>
        /// 注册到运行中的SqlSugar并清字典缓存(dbServer下拉数据来自Sys_DbConnection)
        /// </summary>
        private void RegisterAndRefreshCache(Sys_DbConnection entity)
        {
            if (entity == null) return;
            if (entity.Enabled)
            {
                DbConnectionManager.RegisterRuntime(entity.ConnName, entity.DBType,
                    DbConnectionManager.Decrypt(entity.ConnectionString));
            }
            CacheContext.Remove(DictionaryManager.Key);
        }

        private static bool IsMasked(string connectionString)
        {
            return !string.IsNullOrEmpty(connectionString) && connectionString.Contains(PasswordMask);
        }

        /// <summary>
        /// 把连接串里的密码替换成掩码(各数据库的键名不同,统一按 xxx=值 匹配)
        /// </summary>
        private static string MaskPassword(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
            return Regex.Replace(connectionString,
                @"(?<key>\b(password|pwd)\b\s*=)([^;]*)",
                "${key}" + PasswordMask,
                RegexOptions.IgnoreCase);
        }
    }
}
