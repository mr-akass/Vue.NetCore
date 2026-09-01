/*
*数据库管理业务接口(多数据库支持,只增不删)
*/
using System.Threading.Tasks;
using VOL.Core.Utilities;

namespace VOL.Sys.IServices
{
    public partial interface ISys_DbConnectionService
    {
        /// <summary>
        /// 测试连接是否能连上(不落库)
        /// </summary>
        Task<WebResponseContent> TestConnectionAsync(string connName, string dbType, string connectionString);

        /// <summary>
        /// 当前已注册到SqlSugar的连接明细
        /// </summary>
        Task<object> GetRegisteredAsync();
    }
}
