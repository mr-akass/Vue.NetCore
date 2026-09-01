/*
 *数据库管理扩展接口：测试连接、查看当前已注册连接
 *连接字符串含账号密码，两个接口都限超级管理员
 */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VOL.Core.Filters;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_DbConnectionController
    {
        private readonly ISys_DbConnectionService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Sys_DbConnectionController(
            ISys_DbConnectionService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 测试连接(保存前先验证连接串能不能连上)
        /// </summary>
        [HttpPost, Route("TestConnection")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionModel model)
        {
            return Json(await _service.TestConnectionAsync(model?.ConnName, model?.DBType, model?.ConnectionString));
        }

        /// <summary>
        /// 当前已注册到SqlSugar的连接(排查"配了却切不过去"时用)
        /// </summary>
        [HttpPost, Route("GetRegistered")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<IActionResult> GetRegistered()
        {
            return JsonNormal(new { status = true, data = await _service.GetRegisteredAsync() });
        }

        public class TestConnectionModel
        {
            public string ConnName { get; set; }
            public string DBType { get; set; }
            public string ConnectionString { get; set; }
        }
    }
}
