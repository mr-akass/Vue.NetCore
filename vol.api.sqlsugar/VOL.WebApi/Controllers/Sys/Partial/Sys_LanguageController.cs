/*
 *多语言接口：生成语言包(语言管理页面[生成语言包]按钮调用)
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_LanguageController
    {
        /// <summary>
        /// 生成语言包文件wwwroot/lang/{lang}.js
        /// </summary>
        [HttpGet, Route("createLanguagePack")]
        public IActionResult CreateLanguagePack()
        {
            return Json(Service.CreateLanguagePack());
        }
    }
}
