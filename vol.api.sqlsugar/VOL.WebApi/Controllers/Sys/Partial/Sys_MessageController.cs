/*
 *站内消息(发送方)接口：发送记录、收件人阅读状态
 */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_MessageController
    {
        private readonly ISys_MessageService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Sys_MessageController(
            ISys_MessageService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 我的发送记录(含每条消息已读/未读人数)
        /// </summary>
        [HttpGet, Route("GetSentMessages")]
        public async Task<IActionResult> GetSentMessages(int page = 1, int rows = 20)
        {
            return JsonNormal(await _service.GetSentMessagesAsync(page, rows));
        }

        /// <summary>
        /// 消息的收件人阅读状态
        /// </summary>
        [HttpGet, Route("GetRecipientStatus/{messageId}")]
        public async Task<IActionResult> GetRecipientStatus(int messageId)
        {
            return JsonNormal(await _service.GetRecipientStatusAsync(messageId));
        }
    }
}
