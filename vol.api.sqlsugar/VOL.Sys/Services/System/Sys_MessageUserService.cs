/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下Sys_MessageUserService与ISys_MessageUserService中编写
 */
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Services
{
    public partial class Sys_MessageUserService : ServiceBase<Sys_MessageUser, ISys_MessageUserRepository>
    , ISys_MessageUserService, IDependency
    {
    public static ISys_MessageUserService Instance
    {
      get { return AutofacContainerModule.GetService<ISys_MessageUserService>(); } }
    }
 }
