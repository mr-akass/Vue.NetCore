/*
*站内消息(发送方)业务接口：发送入库、发送记录、收件人阅读状态
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace VOL.Sys.IServices
{
    /// <summary>
    /// 发送站内消息入参
    /// </summary>
    public class MessageSendInput
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; } = 1;
        public List<string> UserNames { get; set; }
        public string SenderUserName { get; set; }
        public int? SenderUserId { get; set; }
    }

    /// <summary>
    /// 发送站内消息结果(用于SignalR推送payload)
    /// </summary>
    public class MessageSendResult
    {
        public int MessageId { get; set; }
        public int RecipientCount { get; set; }
        public List<string> UserNames { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Date { get; set; }
        public string FromUser { get; set; }
        public int SenderUserId { get; set; }
    }

    public partial interface ISys_MessageService
    {
        Task<WebResponseContent> CreateMessageAsync(MessageSendInput input);
        Task<object> GetSentMessagesAsync(int page = 1, int rows = 20);
        Task<object> GetRecipientStatusAsync(int messageId);
    }
}
