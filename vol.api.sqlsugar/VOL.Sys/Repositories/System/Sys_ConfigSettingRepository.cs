/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹Sys_ConfigSettingRepository编写代码
 */
using VOL.Sys.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.DbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Repositories
{
    public partial class Sys_ConfigSettingRepository : RepositoryBase<Sys_ConfigSetting>
    , ISys_ConfigSettingRepository
    {
    public Sys_ConfigSettingRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static ISys_ConfigSettingRepository Instance
    {
    get {  return AutofacContainerModule.GetService<ISys_ConfigSettingRepository>
        (); } }
        }
        }
