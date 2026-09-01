using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;

namespace VOL.Sys.IServices
{
    public partial interface ISys_MenuService
    {
        Task<object> GetMenu();
        List<Sys_Menu> GetCurrentMenuList();

        List<Sys_Menu> GetUserMenuList(int roleId);

        object GetCurrentMenuActionList();

        /// <summary>
        /// 按应用获取当前用户的菜单与权限(多应用支持)
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        object GetCurrentMenuActionListByAppId(int? appId);

        object GetMenuActionList(int roleId);

        /// <summary>
        /// 根据多个角色获取菜单与权限(权限并集，可按应用子树限制范围并隐藏根菜单)
        /// </summary>
        /// <param name="roleIds"></param>
        /// <param name="rootMenuIds"></param>
        /// <param name="subTreeIds"></param>
        /// <returns></returns>
        object GetMenuActionList(int[] roleIds, HashSet<int> rootMenuIds = null, HashSet<int> subTreeIds = null);
        Task<WebResponseContent> Save(Sys_Menu menu);

        Task<WebResponseContent> DelMenu(int menuId);


        Task<object> GetTreeItem(int menuId);
    }
}

