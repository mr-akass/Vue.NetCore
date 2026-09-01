/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹Sys_ThemeSettingController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.Sys.IServices;
namespace VOL.Sys.Controllers
{
    [Route("api/Sys_ThemeSetting")]
    [PermissionTable(Name = "Sys_ThemeSetting")]
    public partial class Sys_ThemeSettingController : ApiBaseController<ISys_ThemeSettingService>
    {
        public Sys_ThemeSettingController(ISys_ThemeSettingService service)
        : base(service)
        {
        }
    }
}
