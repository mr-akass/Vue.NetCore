using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.DBManager;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Services;
using VOL.Core.Utilities;
using VOL.Entity;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Services
{
    public partial class Sys_MenuService
    {
        /// <summary>
        /// 菜单静态化处理，每次获取菜单时先比较菜单是否发生变化，如果发生变化从数据库重新获取，否则直接获取_menus菜单
        /// </summary>
        private static List<Sys_Menu> _menus { get; set; }

        /// <summary>
        /// 从数据库获取菜单时锁定的对象
        /// </summary>
        private static object _menuObj = new object();

        /// <summary>
        /// 当前服务器的菜单版本
        /// </summary>
        private static string _menuVersionn = "";

        private const string _menuCacheKey = "inernalMenu";

        /// <summary>
        /// 编辑修改菜单时,获取所有菜单
        /// </summary>
        /// <returns></returns>
        public async Task<object> GetMenu()
        {
            //  DBServerProvider.SqlDapper.q
            return (await repository.FindAsync(x => 1 == 1, a =>
             new
             {
                 id = a.Menu_Id,
                 parentId = a.ParentId,
                 name = a.MenuName,
                 a.MenuType,
                 a.OrderNo
             })).OrderByDescending(a => a.OrderNo)
                .ThenByDescending(q => q.parentId).ToList();

        }

        private List<Sys_Menu> GetAllMenu()
        {
            //每次比较缓存是否更新过，如果更新则重新获取数据
            string _cacheVersion = CacheContext.Get(_menuCacheKey);
            if (_menuVersionn != "" && _menuVersionn == _cacheVersion)
            {
                return _menus ?? new List<Sys_Menu>();
            }
            lock (_menuObj)
            {
                if (_menuVersionn != "" && _menus != null && _menuVersionn == _cacheVersion) return _menus;
                //2020.12.27增加菜单界面上不显示，但可以分配权限
                _menus = repository.FindAsIQueryable(x => x.Enable == 1 || x.Enable == 2)
                    .OrderByDescending(a => a.OrderNo)
                    .ThenByDescending(q => q.ParentId).ToList();

                _menus.ForEach(x =>
                {
                    // 2022.03.26增移动端加菜单类型
                    x.MenuType ??= 0;
                    if (!string.IsNullOrEmpty(x.Auth) && x.Auth.Length > 10)
                    {
                        try
                        {
                            x.Actions = x.Auth.DeserializeObject<List<Sys_Actions>>();
                        }
                        catch { }
                    }
                    if (x.Actions == null) x.Actions = new List<Sys_Actions>();
                });

                string cacheVersion = CacheContext.Get(_menuCacheKey);
                if (string.IsNullOrEmpty(cacheVersion))
                {
                    cacheVersion = DateTime.Now.ToString("yyyyMMddHHMMssfff");
                    CacheContext.Add(_menuCacheKey, cacheVersion);
                }
                else
                {
                    _menuVersionn = cacheVersion;
                }
            }
            return _menus;
        }

        /// <summary>
        /// 获取当前用户有权限查看的菜单
        /// </summary>
        /// <returns></returns>
        public List<Sys_Menu> GetCurrentMenuList()
        {
            int roleId = UserContext.Current.RoleId;
            return GetUserMenuList(roleId);
        }


        public List<Sys_Menu> GetUserMenuList(int roleId)
        {
            if (UserContext.IsRoleIdSuperAdmin(roleId))
            {
                return GetAllMenu();
            }
            List<int> menuIds = UserContext.Current.GetPermissions(roleId).Select(x => x.Menu_Id).ToList();
            return GetAllMenu().Where(x => menuIds.Contains(x.Menu_Id)).ToList();
        }

        /// <summary>
        /// 获取当前用户所有菜单与权限(多角色权限并集)
        /// </summary>
        /// <returns></returns>
        public object GetCurrentMenuActionList()
        {
            if (UserContext.Current.IsSuperAdmin)
            {
                return GetMenuActionList(1);
            }
            return GetMenuActionList(UserContext.Current.RoleIds, null);
        }

        /// <summary>
        /// 按应用获取当前用户的菜单与权限(多应用支持)
        /// 应用的菜单范围=RootMenuIds绑定的多个一级菜单子树的并集(未绑定时回退按AppName同名匹配)；
        /// 所有根菜单被隐藏、其子菜单提升为一级；范围外的菜单即使角色误配了权限也不会显示。
        /// 公共子树可同时绑定到多个应用实现菜单共享，同一菜单在不同应用中的按钮权限按该应用下的角色分别计算。
        /// 超级管理员：不传appId=全量菜单；传appId=以该应用子树视角查看(拥有子树内全部权限)
        /// </summary>
        /// <param name="appId">应用ID(Sys_Application.AppID)</param>
        /// <returns></returns>
        public object GetCurrentMenuActionListByAppId(int? appId)
        {
            bool isSuperAdmin = UserContext.Current.IsSuperAdmin;
            //未选择应用：超管看全量菜单，普通用户返回空菜单防止泄露
            if (!appId.HasValue)
            {
                return isSuperAdmin ? GetMenuActionList(1) : new List<object>();
            }

            var app = Repositories.Sys_ApplicationRepository.Instance.FindFirst(x => x.AppID == appId.Value);
            if (app == null)
            {
                return isSuperAdmin ? GetMenuActionList(1) : new List<object>();
            }

            var allMenus = GetAllMenu().Where(c => c.MenuType == UserContext.MenuType).ToList();

            //确定应用的根菜单集合：优先RootMenuIds显式绑定(逗号分隔多个)，未绑定回退AppName同名一级菜单(兼容旧约定)
            var rootMenuIds = new List<int>();
            if (!string.IsNullOrWhiteSpace(app.RootMenuIds))
            {
                rootMenuIds = app.RootMenuIds.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().GetInt())
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();
            }
            if (rootMenuIds.Count == 0 && !string.IsNullOrEmpty(app.AppName))
            {
                var sameNameMenu = allMenus.FirstOrDefault(m => m.ParentId == 0 &&
                    string.Equals(m.MenuName, app.AppName, StringComparison.OrdinalIgnoreCase));
                if (sameNameMenu != null)
                {
                    rootMenuIds.Add(sameNameMenu.Menu_Id);
                }
            }

            //应用的菜单范围=所有根菜单子树的并集(圈定范围，防止跨应用权限混入)
            HashSet<int> rootIdSet = rootMenuIds.Count > 0 ? new HashSet<int>(rootMenuIds) : null;
            HashSet<int> subTreeIds = null;
            if (rootMenuIds.Count > 0)
            {
                subTreeIds = new HashSet<int>();
                foreach (var rootId in rootMenuIds)
                {
                    foreach (var id in GetMenuSubTreeIds(allMenus, rootId))
                    {
                        subTreeIds.Add(id);
                    }
                }
            }

            if (isSuperAdmin)
            {
                //超管以子系统视角查看：子树内全部菜单与按钮权限
                return allMenus
                    .Where(x => subTreeIds == null || subTreeIds.Contains(x.Menu_Id))
                    .Where(x => rootIdSet == null || !rootIdSet.Contains(x.Menu_Id))
                    .OrderByDescending(x => x.OrderNo)
                    .Select(x => new
                    {
                        id = x.Menu_Id,
                        name = x.MenuName,
                        url = x.Url,
                        parentId = (rootIdSet != null && rootIdSet.Contains(x.ParentId)) ? 0 : x.ParentId,
                        icon = x.Icon,
                        x.Enable,
                        x.TableName,
                        permission = x.Actions.Select(s => s.Value).ToArray()
                    }).ToList<object>();
            }

            //普通用户：将角色缩小到该应用下的角色
            int[] filteredRoleIds = Sys_RoleService.Instance.GetRoleIdsByAppId(UserContext.Current.RoleIds, appId.Value);
            if (filteredRoleIds == null || filteredRoleIds.Length == 0)
            {
                return new List<object>();
            }

            return GetMenuActionList(filteredRoleIds, rootIdSet, subTreeIds);
        }

        /// <summary>
        /// 获取指定一级菜单的整棵子树菜单ID(含根)
        /// </summary>
        private HashSet<int> GetMenuSubTreeIds(List<Sys_Menu> allMenus, int rootMenuId)
        {
            var ids = new HashSet<int>() { rootMenuId };
            bool added = true;
            while (added)
            {
                added = false;
                foreach (var m in allMenus)
                {
                    if (!ids.Contains(m.Menu_Id) && ids.Contains(m.ParentId))
                    {
                        ids.Add(m.Menu_Id);
                        added = true;
                    }
                }
            }
            return ids;
        }

        /// <summary>
        /// 根据多个角色获取菜单与权限(权限并集)
        /// </summary>
        /// <param name="roleIds"></param>
        /// <param name="rootMenuIds">要隐藏的应用根菜单ID集合(其子菜单parentId置0提升为一级)</param>
        /// <param name="subTreeIds">应用的菜单范围(为null不限制)</param>
        /// <returns></returns>
        public object GetMenuActionList(int[] roleIds, HashSet<int> rootMenuIds = null, HashSet<int> subTreeIds = null)
        {
            if (roleIds != null && roleIds.Any(x => UserContext.IsRoleIdSuperAdmin(x)))
            {
                return GetMenuActionList(1);
            }

            var allMenus = GetAllMenu().Where(c => c.MenuType == UserContext.MenuType).ToList();

            var menu = from a in UserContext.Current.GetPermissions(roleIds)
                       join b in allMenus on a.Menu_Id equals b.Menu_Id
                       where (subTreeIds == null || subTreeIds.Contains(b.Menu_Id))
                          && (rootMenuIds == null || !rootMenuIds.Contains(b.Menu_Id))
                       orderby b.OrderNo descending
                       select new
                       {
                           id = a.Menu_Id,
                           name = b.MenuName,
                           url = b.Url,
                           parentId = (rootMenuIds != null && rootMenuIds.Contains(b.ParentId)) ? 0 : b.ParentId,
                           icon = b.Icon,
                           b.Enable,
                           b.TableName, // 2022.03.26增移动端加菜单类型
                           permission = a.UserAuthArr
                       };
            return menu.ToList();
        }

        /// <summary>
        /// 根据角色ID获取菜单与权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public object GetMenuActionList(int roleId)
        {
            if (UserContext.IsRoleIdSuperAdmin(roleId))
            {
                return GetAllMenu()
                .Where(c => c.MenuType == UserContext.MenuType)
                .Select(x =>
                new
                {
                    id = x.Menu_Id,
                    name = x.MenuName,
                    url = x.Url,
                    parentId = x.ParentId,
                    icon = x.Icon,
                    x.Enable,
                    x.TableName, // 2022.03.26增移动端加菜单类型
                    permission = x.Actions.Select(s => s.Value).ToArray()
                }).ToList();
            }

            return GetMenuActionList(new int[] { roleId }, null);
        }

        /// <summary>
        /// 新建或编辑菜单
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> Save(Sys_Menu menu)
        {
            WebResponseContent webResponse = new WebResponseContent();
            if (menu == null) return webResponse.Error("没有获取到提交的参数");
            if (menu.Menu_Id > 0 && menu.Menu_Id == menu.ParentId) return webResponse.Error("父级ID不能是当前菜单的ID");
            try
            {
                webResponse = menu.ValidationEntity(x => new { x.MenuName, x.TableName });
                if (!webResponse.Status) return webResponse;
                if (menu.TableName != "/" && menu.TableName != ".")
                {
                    // 2022.03.26增移动端加菜单类型判断
                    Sys_Menu sysMenu = await repository.FindAsyncFirst(x => x.TableName == menu.TableName);
                    if (sysMenu != null)
                    {
                        sysMenu.MenuType ??= 0;
                        if (sysMenu.MenuType == menu.MenuType)
                        {
                            if ((menu.Menu_Id > 0 && sysMenu.Menu_Id != menu.Menu_Id)
                            || menu.Menu_Id <= 0)
                            {
                                return webResponse.Error($"视图/表名【{menu.TableName}】已被其他菜单使用");
                            }
                        }
                    }
                }
                bool _changed = false;
                if (menu.Menu_Id <= 0)
                {
                    repository.AddWithSetIdentity(menu.SetCreateDefaultVal());
                }
                else
                {
                    //2020.05.07新增禁止选择上级角色为自己
                    if (menu.Menu_Id == menu.ParentId)
                    {
                        return webResponse.Error($"父级id不能为自己");
                    }
                    if (repository.Exists(x => x.ParentId == menu.Menu_Id && menu.ParentId == x.Menu_Id))
                    {
                        return webResponse.Error($"不能选择此父级id，选择的父级id与当前菜单形成依赖关系");
                    }

                    _changed = repository.FindAsIQueryable(c => c.Menu_Id == menu.Menu_Id).Select(s => s.Auth).FirstOrDefault() != menu.Auth;

                    repository.Update(menu.SetModifyDefaultVal(), p => new
                    {
                        p.ParentId,
                        p.MenuName,
                        p.Url,
                        p.Auth,
                        p.OrderNo,
                        p.Icon,
                        p.Enable,
                        p.MenuType,// 2022.03.26增移动端加菜单类型
                        p.TableName,
                        p.ModifyDate,
                        p.Modifier
                    });
                }
                await repository.SaveChangesAsync();

                CacheContext.Add(_menuCacheKey, DateTime.Now.ToString("yyyyMMddHHMMssfff"));
                if (_changed)
                {
                    UserContext.Current.RefreshWithMenuActionChange(menu.Menu_Id);
                }
                _menus = null;
                webResponse.OK("保存成功", menu);
            }
            catch (Exception ex)
            {
                webResponse.Error(ex.Message); 
            }
            finally
            {
                Logger.Info($"表:{menu.TableName},菜单：{menu.MenuName},权限{menu.Auth},{(webResponse.Status ? "成功" : "失败")}{webResponse.Message}");
            }
            return webResponse;

        }

        public async Task<WebResponseContent> DelMenu(int menuId)
        {
            WebResponseContent webResponse =new  WebResponseContent();
      
            if (await repository.ExistsAsync(x => x.ParentId == menuId))
            {
                return webResponse.Error("当前菜单存在子菜单,请先删除子菜单!");
            }
            repository.Delete(new Sys_Menu()
            {
                Menu_Id = menuId
            }, true);
            CacheContext.Add(_menuCacheKey, DateTime.Now.ToString("yyyyMMddHHMMssfff"));
            return webResponse.OK("删除成功");
        }
        /// <summary>
        /// 编辑菜单时，获取菜单信息
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public async Task<object> GetTreeItem(int menuId)
        {
            var sysMenu = (await base.repository.FindAsync(x => x.Menu_Id == menuId))
                .Select(
                p => new
                {
                    p.Menu_Id,
                    p.ParentId,
                    p.MenuName,
                    p.Url,
                    p.Auth,
                    p.OrderNo,
                    p.Icon,
                    p.Enable,
                    // 2022.03.26增移动端加菜单类型
                    MenuType = p.MenuType ?? 0,
                    p.CreateDate,
                    p.Creator,
                    p.TableName,
                    p.ModifyDate
                }).FirstOrDefault();
            return sysMenu;
        }
    }
}

