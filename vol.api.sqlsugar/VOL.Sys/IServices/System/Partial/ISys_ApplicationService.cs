/*
*应用/子系统业务接口
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace VOL.Sys.IServices
{
    public partial interface ISys_ApplicationService
    {
        Task<List<object>> GetEnabledAppsAsync();
    }
}
