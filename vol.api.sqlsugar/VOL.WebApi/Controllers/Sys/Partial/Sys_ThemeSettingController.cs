/*
 *主题个性化接口：读取/保存我的主题、重置、设为应用默认、背景图上传与删除
 *都是用户操作自己的主题,只需登录(基类[JWTAuthorize]),不加[ApiActionPermission]菜单权限校验;
 *"设为应用默认"额外在Service里限超管
 */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_ThemeSettingController
    {
        private readonly ISys_ThemeSettingService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Sys_ThemeSettingController(
            ISys_ThemeSettingService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 读我的主题(含当前应用默认主题与是否超管)
        /// </summary>
        /// <param name="appId">当前应用ID(每个应用一套主题,不传表示不区分应用)</param>
        [HttpGet, Route("GetMyTheme")]
        public async Task<IActionResult> GetMyTheme(int? appId = null)
        {
            return JsonNormal(await _service.GetMyThemeAsync(appId));
        }

        /// <summary>
        /// 保存我的主题
        /// </summary>
        [HttpPost, Route("SaveMyTheme")]
        public async Task<IActionResult> SaveMyTheme([FromBody] ThemeModel model)
        {
            return Json(await _service.SaveMyThemeAsync(model?.ThemeJson, model?.AppId));
        }

        /// <summary>
        /// 重置我的主题(回落到应用默认/内置预设)
        /// </summary>
        [HttpPost, Route("ResetMyTheme")]
        public async Task<IActionResult> ResetMyTheme([FromBody] ThemeModel model)
        {
            return Json(await _service.ResetMyThemeAsync(model?.AppId));
        }

        /// <summary>
        /// 设为当前应用的默认主题(超管)
        /// </summary>
        [HttpPost, Route("SaveAppDefault")]
        public async Task<IActionResult> SaveAppDefault([FromBody] ThemeModel model)
        {
            return Json(await _service.SaveAppDefaultAsync(model?.ThemeJson, model?.AppId));
        }

        /// <summary>
        /// 上传背景图(单文件,form-data)
        /// </summary>
        [HttpPost, Route("UploadBackground")]
        public async Task<IActionResult> UploadBackground([FromForm] List<IFormFile> fileInput, int? appId = null)
        {
            return Json(await _service.UploadBackgroundAsync(fileInput, appId));
        }

        /// <summary>
        /// 删除背景图
        /// </summary>
        [HttpPost, Route("RemoveBackground")]
        public async Task<IActionResult> RemoveBackground([FromBody] ThemeModel model)
        {
            return Json(await _service.RemoveBackgroundAsync(model?.AppId));
        }

        public class ThemeModel
        {
            /// <summary>
            /// 主题配置JSON(前端定义的旋钮集合,后端只做长度/格式/背景图地址校验)
            /// </summary>
            public string ThemeJson { get; set; }
            /// <summary>
            /// 当前应用ID
            /// </summary>
            public int? AppId { get; set; }
        }
    }
}
