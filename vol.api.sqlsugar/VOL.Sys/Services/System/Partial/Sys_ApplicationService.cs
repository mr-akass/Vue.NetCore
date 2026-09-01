/*
*应用/子系统业务实现：按当前用户角色过滤有权限的应用列表
*参照 ShelfLifeMgt 迁移，适配本项目多角色(UserContext.RoleIds)
*/
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;

namespace VOL.Sys.Services
{
    public partial class Sys_ApplicationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_ApplicationRepository _repository;//访问数据库

        [ActivatorUtilitiesConstructor]
        public Sys_ApplicationService(
            ISys_ApplicationRepository dbRepository,
            IHttpContextAccessor httpContextAccessor
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 获取当前用户有权限的应用列表(超级管理员返回所有启用应用，其他用户按角色的AppID过滤)
        /// </summary>
        public async Task<List<object>> GetEnabledAppsAsync()
        {
            var query = _repository.FindAsIQueryable(x => x.Enabled == true);

            //如果不是超级管理员，根据角色过滤应用
            if (!UserContext.Current.IsSuperAdmin)
            {
                var appIds = Sys_RoleService.Instance.GetAppIdsByRoleIds(UserContext.Current.RoleIds);
                if (appIds == null || appIds.Count == 0)
                {
                    return new List<object>();
                }
                query = query.Where(x => appIds.Contains(x.AppID));
            }

            var apps = await query
                .OrderBy(x => x.SortOrder)
                .Select(x => new
                {
                    appId = x.AppID,
                    appCode = x.AppCode,
                    appName = x.AppName,
                    title = x.Title,
                    icon = x.Icon,
                    theme = x.Theme,
                    primaryColor = x.PrimaryColor,
                    dataPanel = x.DataPanel,
                    sortOrder = x.SortOrder,
                    enabled = x.Enabled
                })
                .ToListAsync();

            return apps.Cast<object>().ToList();
        }
    }
}
