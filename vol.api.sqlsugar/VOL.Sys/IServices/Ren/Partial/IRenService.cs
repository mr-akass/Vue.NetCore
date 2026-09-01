/*
*所有关于Ren类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using System.Threading.Tasks;
namespace VOL.Sys.IServices
{
    public partial interface IRenService
    {
        //表头筛选获取列去重值的方法已提升为框架级功能，见VOL.Core/BaseProvider/ServiceBase.cs的GetColumnDistinctValuesAsync
    }
 }
