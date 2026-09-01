/*
*首页快捷导航业务接口：我的快捷菜单、添加、删除、拖动排序
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Threading.Tasks;
namespace VOL.Sys.IServices
{
    public partial interface ISys_UserShortcutService
    {
        Task<object> GetMyShortcutsAsync(int? appId = null);
        Task<WebResponseContent> AddShortcutAsync(int[] menuIds, int? appId = null);
        Task<WebResponseContent> RemoveShortcutAsync(int id);
        Task<WebResponseContent> SaveSortAsync(int[] ids);
    }
}
