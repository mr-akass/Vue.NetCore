/*
 *应用/子系统接口：获取当前用户有权限的应用列表(guide选择页/首页数据来源)
 */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_ApplicationController
    {
        private readonly ISys_ApplicationService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Sys_ApplicationController(
            ISys_ApplicationService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取当前用户有权限的应用列表(需要登录)
        /// </summary>
        [HttpGet, Route("GetEnabledApps")]
        public async Task<IActionResult> GetEnabledApps()
        {
            var result = await _service.GetEnabledAppsAsync();
            return JsonNormal(new { status = true, data = result });
        }
    }
}
