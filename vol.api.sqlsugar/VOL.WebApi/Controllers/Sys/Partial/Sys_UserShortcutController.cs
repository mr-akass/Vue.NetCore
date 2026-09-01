/*
 *首页快捷导航接口：我的快捷菜单、添加、删除、拖动排序
 *这几个接口都是用户操作自己的数据,只需登录(基类[JWTAuthorize]),不加[ApiActionPermission]菜单权限校验
 */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_UserShortcutController
    {
        private readonly ISys_UserShortcutService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Sys_UserShortcutController(
            ISys_UserShortcutService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 我的快捷菜单列表
        /// </summary>
        /// <param name="appId">当前应用ID(多应用隔离,不传表示不区分应用)</param>
        [HttpGet, Route("GetMyShortcuts")]
        public async Task<IActionResult> GetMyShortcuts(int? appId = null)
        {
            return JsonNormal(await _service.GetMyShortcutsAsync(appId));
        }

        /// <summary>
        /// 添加快捷菜单(支持一次添加多个)
        /// </summary>
        [HttpPost, Route("AddShortcut")]
        public async Task<IActionResult> AddShortcut([FromBody] AddShortcutModel model)
        {
            return Json(await _service.AddShortcutAsync(model?.MenuIds, model?.AppId));
        }

        /// <summary>
        /// 移除快捷菜单
        /// </summary>
        [HttpPost, Route("RemoveShortcut/{id}")]
        public async Task<IActionResult> RemoveShortcut(int id)
        {
            return Json(await _service.RemoveShortcutAsync(id));
        }

        /// <summary>
        /// 保存拖动排序结果(传排序后的快捷项ID数组)
        /// </summary>
        [HttpPost, Route("SaveSort")]
        public async Task<IActionResult> SaveSort([FromBody] SaveSortModel model)
        {
            return Json(await _service.SaveSortAsync(model?.Ids));
        }

        public class AddShortcutModel
        {
            /// <summary>
            /// 要添加的菜单ID集合
            /// </summary>
            public int[] MenuIds { get; set; }
            /// <summary>
            /// 当前应用ID
            /// </summary>
            public int? AppId { get; set; }
        }

        public class SaveSortModel
        {
            /// <summary>
            /// 排序后的快捷项ID数组
            /// </summary>
            public int[] Ids { get; set; }
        }
    }
}
