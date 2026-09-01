/*
*首页快捷导航业务实现：我的快捷菜单、添加、删除、拖动排序
*设计说明：
*  1) 所有接口只操作 UserContext.Current.UserId 自己的数据,不接受外部传入 userId
*  2) 添加时校验菜单必须在当前用户(当前应用)的菜单权限范围内,防止绕过前端收藏无权菜单
*  3) 接口只返回 menuId,菜单名/地址/图标由前端用已有的菜单权限数据(store.state.permission)渲染。
*     这样菜单改名/改地址自动跟随,菜单权限被收回后该快捷项前端直接过滤掉,后端无需重复拼装
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
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;

namespace VOL.Sys.Services
{
    public partial class Sys_UserShortcutService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_UserShortcutRepository _repository;//访问数据库

        [ActivatorUtilitiesConstructor]
        public Sys_UserShortcutService(
            ISys_UserShortcutRepository dbRepository,
            IHttpContextAccessor httpContextAccessor
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 一个用户单个应用下最多收藏多少个快捷菜单
        /// </summary>
        private const int MaxShortcutCount = 30;

        /// <summary>
        /// 我的快捷菜单列表(按SortOrder升序)
        /// </summary>
        public async Task<object> GetMyShortcutsAsync(int? appId = null)
        {
            int userId = UserContext.Current.UserId;
            int currentAppId = appId ?? 0;

            var list = await _repository.FindAsIQueryable(x => x.UserId == userId && x.AppId == currentAppId)
                .OrderBy(x => x.SortOrder)
                .OrderBy(x => x.ID)
                .ToListAsync();

            return list.Select(x => new
            {
                id = x.ID,
                menuId = x.MenuId,
                name = x.MenuName,
                sortOrder = x.SortOrder
            }).ToList<object>();
        }

        /// <summary>
        /// 添加快捷菜单(支持一次添加多个,已存在的跳过)
        /// </summary>
        public async Task<WebResponseContent> AddShortcutAsync(int[] menuIds, int? appId = null)
        {
            var webResponse = new WebResponseContent();
            if (menuIds == null || menuIds.Length == 0)
            {
                return webResponse.Error("请选择要添加的菜单");
            }

            int userId = UserContext.Current.UserId;
            int currentAppId = appId ?? 0;

            //只允许收藏当前用户有权限访问的菜单
            var accessibleMenus = GetAccessibleMenus(appId);
            var validMenus = menuIds.Distinct()
                .Where(x => accessibleMenus.ContainsKey(x))
                .ToArray();
            if (validMenus.Length == 0)
            {
                return webResponse.Error("没有可添加的菜单(菜单不存在或无访问权限)");
            }

            var exists = await _repository.FindAsIQueryable(x => x.UserId == userId && x.AppId == currentAppId)
                .ToListAsync();
            var existMenuIds = exists.Select(x => x.MenuId).ToHashSet();

            var toAdd = validMenus.Where(x => !existMenuIds.Contains(x)).ToArray();
            if (toAdd.Length == 0)
            {
                return webResponse.OK("所选菜单已在快捷导航中");
            }

            if (exists.Count + toAdd.Length > MaxShortcutCount)
            {
                return webResponse.Error($"快捷导航最多添加{MaxShortcutCount}个,当前已有{exists.Count}个");
            }

            int maxSort = exists.Count == 0 ? 0 : exists.Max(x => x.SortOrder);
            var now = DateTime.Now;
            var entities = toAdd.Select((menuId, index) => new Sys_UserShortcut
            {
                UserId = userId,
                MenuId = menuId,
                MenuName = accessibleMenus[menuId],
                AppId = currentAppId,
                SortOrder = maxSort + index + 1,
                CreateDate = now
            }).ToList();

            int effectRows = await _repository.SqlSugarClient.Insertable(entities).ExecuteCommandAsync();
            return effectRows > 0
                ? webResponse.OK($"已添加{effectRows}个快捷菜单")
                : webResponse.Error("添加失败");
        }

        /// <summary>
        /// 删除快捷菜单(只能删自己的)
        /// </summary>
        public async Task<WebResponseContent> RemoveShortcutAsync(int id)
        {
            var webResponse = new WebResponseContent();
            int userId = UserContext.Current.UserId;

            int effectRows = await _repository.SqlSugarClient.Deleteable<Sys_UserShortcut>()
                .Where(x => x.ID == id && x.UserId == userId)
                .ExecuteCommandAsync();

            return effectRows > 0 ? webResponse.OK("已移除") : webResponse.Error("快捷菜单不存在");
        }

        /// <summary>
        /// 保存拖动排序结果(传入排序后的快捷项ID数组,按数组下标重写SortOrder)
        /// </summary>
        public async Task<WebResponseContent> SaveSortAsync(int[] ids)
        {
            var webResponse = new WebResponseContent();
            if (ids == null || ids.Length == 0)
            {
                return webResponse.Error("排序数据为空");
            }

            int userId = UserContext.Current.UserId;
            //只处理属于当前用户的记录,过滤掉传入的非法ID
            var list = await _repository.FindAsIQueryable(x => x.UserId == userId && ids.Contains(x.ID))
                .ToListAsync();
            if (list.Count == 0)
            {
                return webResponse.Error("没有可排序的快捷菜单");
            }

            foreach (var item in list)
            {
                item.SortOrder = Array.IndexOf(ids, item.ID) + 1;
            }

            int effectRows = await _repository.SqlSugarClient.Updateable(list)
                .UpdateColumns(x => new { x.SortOrder })
                .ExecuteCommandAsync();

            return effectRows > 0 ? webResponse.OK("排序已保存") : webResponse.Error("排序保存失败");
        }

        /// <summary>
        /// 取当前用户(指定应用下)可收藏的菜单: menuId => 菜单名
        /// 直接复用 Sys_MenuService.GetCurrentMenuActionListByAppId 的多应用+多角色权限逻辑,
        /// 该方法返回匿名对象集合,这里借道JSON转成有类型的DTO(菜单数据有缓存,调用成本很低)
        /// </summary>
        private Dictionary<int, string> GetAccessibleMenus(int? appId)
        {
            var menuObj = Sys_MenuService.Instance.GetCurrentMenuActionListByAppId(appId);
            var menus = menuObj?.Serialize()?.DeserializeObject<List<AccessibleMenu>>();
            if (menus == null)
            {
                return new Dictionary<int, string>();
            }

            return menus
                //只收录能打开页面的菜单(有url的页面菜单,排除只作分组用的父级菜单)
                .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Url) && x.Url.Trim() != "#")
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
        }

        /// <summary>
        /// GetCurrentMenuActionListByAppId 返回结果中本业务用到的字段
        /// </summary>
        private class AccessibleMenu
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }
    }
}
