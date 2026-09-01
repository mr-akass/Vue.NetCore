/*
*站内消息(收件方)业务接口：我的消息、未读数量、标记已读
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Threading.Tasks;
namespace VOL.Sys.IServices
{
    public partial interface ISys_MessageUserService
    {
        Task<object> GetMyUnreadCountAsync();
        Task<object> GetMyMessagesAsync(int page = 1, int rows = 20);
        Task<WebResponseContent> MarkAsReadAsync(int id);
        Task<WebResponseContent> MarkAllAsReadAsync();
    }
}
