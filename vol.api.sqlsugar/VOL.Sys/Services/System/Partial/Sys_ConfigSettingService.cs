/*
*系统配置(键值对)业务实现
*说明：这类配置是"项目级"设置(如代码生成器的Vue路径),原来存在浏览器localStorage,
*      换机器/换浏览器要重填,复制框架做新项目时还会读到旧项目的路径把代码生成到错误目录,
*      因此改为落库。读接口只需登录,写接口在Controller上限制超级管理员。
*/
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;

namespace VOL.Sys.Services
{
    public partial class Sys_ConfigSettingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_ConfigSettingRepository _repository;//访问数据库

        [ActivatorUtilitiesConstructor]
        public Sys_ConfigSettingService(
            ISys_ConfigSettingRepository dbRepository,
            IHttpContextAccessor httpContextAccessor
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        public async Task<string> GetValueAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var entity = await _repository.FindAsIQueryable(x => x.ConfigKey == key).FirstAsync();
            return entity?.ConfigValue;
        }

        public async Task<Dictionary<string, string>> GetValuesAsync(params string[] keys)
        {
            if (keys == null || keys.Length == 0) return new Dictionary<string, string>();
            var list = await _repository.FindAsIQueryable(x => keys.Contains(x.ConfigKey)).ToListAsync();
            return list.GroupBy(x => x.ConfigKey)
                .ToDictionary(g => g.Key, g => g.First().ConfigValue);
        }

        public async Task<WebResponseContent> SetValueAsync(string key, string value, string remark = null)
        {
            var webResponse = new WebResponseContent();
            if (string.IsNullOrWhiteSpace(key))
            {
                return webResponse.Error("配置键不能为空");
            }
            return await SetValuesAsync(new Dictionary<string, string> { { key, value } }, remark);
        }

        public async Task<WebResponseContent> SetValuesAsync(Dictionary<string, string> values)
        {
            return await SetValuesAsync(values, null);
        }

        private async Task<WebResponseContent> SetValuesAsync(Dictionary<string, string> values, string remark)
        {
            var webResponse = new WebResponseContent();
            if (values == null || values.Count == 0)
            {
                return webResponse.Error("没有要保存的配置");
            }

            var keys = values.Keys.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (keys.Length == 0)
            {
                return webResponse.Error("配置键不能为空");
            }

            var exists = await _repository.FindAsIQueryable(x => keys.Contains(x.ConfigKey)).ToListAsync();
            var now = DateTime.Now;

            var toUpdate = new List<Sys_ConfigSetting>();
            var toInsert = new List<Sys_ConfigSetting>();
            foreach (string key in keys)
            {
                var entity = exists.FirstOrDefault(x => x.ConfigKey == key);
                if (entity == null)
                {
                    toInsert.Add(new Sys_ConfigSetting
                    {
                        ConfigKey = key,
                        ConfigValue = values[key],
                        Remark = remark,
                        CreateDate = now
                    });
                }
                else
                {
                    entity.ConfigValue = values[key];
                    entity.ModifyDate = now;
                    toUpdate.Add(entity);
                }
            }

            if (toInsert.Count > 0)
            {
                await _repository.SqlSugarClient.Insertable(toInsert).ExecuteCommandAsync();
            }
            if (toUpdate.Count > 0)
            {
                await _repository.SqlSugarClient.Updateable(toUpdate)
                    .UpdateColumns(x => new { x.ConfigValue, x.ModifyDate })
                    .ExecuteCommandAsync();
            }
            return webResponse.OK("保存成功");
        }
    }
}
