/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹Sys_DbConnectionController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.Sys.IServices;
namespace VOL.Sys.Controllers
{
    [Route("api/Sys_DbConnection")]
    [PermissionTable(Name = "Sys_DbConnection")]
    public partial class Sys_DbConnectionController : ApiBaseController<ISys_DbConnectionService>
    {
        public Sys_DbConnectionController(ISys_DbConnectionService service)
        : base(service)
        {
        }
    }
}
