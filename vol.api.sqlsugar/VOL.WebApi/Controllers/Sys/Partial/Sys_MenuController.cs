using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.Core.ManageUser;
using VOL.Core.UserManager;
using VOL.Entity.DomainModels;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_MenuController
    {
        /// <summary>
        /// 获取当前用户的菜单树(多应用支持：传appId按应用过滤并隐藏应用同名一级菜单；超级管理员不过滤)
        /// </summary>
        /// <param name="appId">应用ID(可选)</param>
        [HttpGet, HttpPost, Route("getTreeMenu")]
        public IActionResult GetTreeMenu(int? appId = null)
        {
            var menu = _service.GetCurrentMenuActionListByAppId(appId);
            return Json(new
            {
                menu,
                asyncApi = TableColumnContext.TableInfo.Where(x => x.AsyncApi == 1).Select(s => s.TableName).ToList(),
            });
        }
        [HttpPost, Route("getMenu")]
        [ApiActionPermission("Sys_Menu", ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetMenu()
        {
            return Json(await _service.GetMenu());
        }

        [HttpPost, Route("getTreeItem")]
        [ApiActionPermission("Sys_Menu", "1", ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetTreeItem(int menuId)
        {
            return Json(await _service.GetTreeItem(menuId));
        }

        //[ActionPermission("Sys_Menu", "1", ActionPermissionOptions.Add)]
        //只有角色ID为1的才能进行保存操作
        [HttpPost, Route("save"), ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<ActionResult> Save([FromBody] Sys_Menu menu)
        {
            return Json(await _service.Save(menu));
        }

        /// <summary>
        /// 限制只能超级管理员才删除菜单 
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost, Route("delMenu")]
        public async Task<ActionResult> DelMenu(int menuId)
        {
            return Json(await Service.DelMenu(menuId));
        }

    }
}
