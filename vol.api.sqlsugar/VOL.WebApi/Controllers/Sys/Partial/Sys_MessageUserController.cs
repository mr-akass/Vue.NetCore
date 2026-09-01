/*
 *站内消息(收件方)接口：我的消息、未读数量、标记已读
 */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_MessageUserController
    {
        private readonly ISys_MessageUserService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Sys_MessageUserController(
            ISys_MessageUserService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 我的未读消息数量
        /// </summary>
        [HttpGet, Route("GetMyUnreadCount")]
        public async Task<IActionResult> GetMyUnreadCount()
        {
            return JsonNormal(await _service.GetMyUnreadCountAsync());
        }

        /// <summary>
        /// 我的消息列表
        /// </summary>
        [HttpGet, Route("GetMyMessages")]
        public async Task<IActionResult> GetMyMessages(int page = 1, int rows = 20)
        {
            return JsonNormal(await _service.GetMyMessagesAsync(page, rows));
        }

        /// <summary>
        /// 标记单条消息已读
        /// </summary>
        [HttpPost, Route("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            return Json(await _service.MarkAsReadAsync(id));
        }

        /// <summary>
        /// 全部标记已读
        /// </summary>
        [HttpPost, Route("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            return Json(await _service.MarkAllAsReadAsync());
        }
    }
}
