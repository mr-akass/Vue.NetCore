/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹RenController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.Sys.IServices;
namespace VOL.Sys.Controllers
{
    [Route("api/Ren")]
    [PermissionTable(Name = "Ren")]
    public partial class RenController : ApiBaseController<IRenService>
    {
        public RenController(IRenService service)
        : base(service)
        {
        }
    }
}

