using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.CacheManager;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Sys.IServices;

namespace VOL.WebApi.Controllers.Hubs
{
    /// <summary>
    /// 站内消息Hub：发送前先入库(Sys_Message+Sys_MessageUser)，再推送给在线连接；
    /// 离线用户登录后可在消息中心查看未读消息
    /// https://docs.microsoft.com/zh-cn/aspnet/core/signalr/introduction?view=aspnetcore-3.1
    /// https://docs.microsoft.com/zh-cn/aspnet/core/signalr/javascript-client?view=aspnetcore-6.0&tabs=visual-studio
    /// </summary>
    public class HomePageMessageHub : Hub
    {
        private readonly ICacheService _cacheService;
        private readonly ISys_MessageService _messageService;


        private static ConcurrentDictionary<string, string> _connectionIds = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 构造 注入
        /// </summary>
        public HomePageMessageHub(ICacheService cacheService, ISys_MessageService messageService)
        {
            _cacheService = cacheService;
            _messageService = messageService;
        }

        /// <summary>
        /// 建立连接时异步触发
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            //Console.WriteLine($"建立连接{Context.ConnectionId}");
            _connectionIds[Context.ConnectionId] = Context.GetHttpContext().Request.Query["userName"].ToString();
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 离开连接时异步触发
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            //Console.WriteLine($"断开连接{Context.ConnectionId}");
            await UserOffline();
            await base.OnDisconnectedAsync(ex);
        }

        /// <summary>
        /// 根据用户名获取所有的客户端
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private IEnumerable<string> GetCnnectionIds(string username)
        {
            foreach (var item in _connectionIds)
            {
                if (item.Value == username)
                {
                    yield return item.Key;
                }
            }
        }

        /// <summary>
        /// 只有admin帐号才能发送站内消息
        /// </summary>
        private bool IsAdminSender()
        {
            return _connectionIds.TryGetValue(Context.ConnectionId, out string currentUser)
                && string.Equals(currentUser, "admin", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCurrentUserName()
        {
            return _connectionIds.TryGetValue(Context.ConnectionId, out string currentUser)
                ? currentUser
                : string.Empty;
        }

        /// <summary>
        /// 发送站内消息入参
        /// </summary>
        public class HomePageMessageInput
        {
            public List<string> UserNames { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// 发送给指定的人：先入库，再推送给在线连接
        /// </summary>
        /// <param name="input">收件人(sys_user表的登陆帐号，可多个)、标题、内容</param>
        /// <returns></returns>
        public async Task<object> SendHomeMessage(HomePageMessageInput input)
        {
            if (!IsAdminSender())
            {
                return new { success = false, message = "只有admin可以发送站内消息" };
            }

            //消息持久化：Sys_Message + Sys_MessageUser(每个收件人一条已读状态记录)
            var saveResult = await _messageService.CreateMessageAsync(new MessageSendInput
            {
                UserNames = input?.UserNames,
                Title = input?.Title,
                Content = input?.Message,
                MessageType = 1,
                SenderUserName = GetCurrentUserName(),
                SenderUserId = 0
            });

            if (!saveResult.Status)
            {
                return new
                {
                    success = false,
                    message = saveResult.Message
                };
            }

            var data = saveResult.Data as MessageSendResult;
            if (data == null)
            {
                return new { success = false, message = "消息保存失败" };
            }

            var connectionIds = (data.UserNames ?? new List<string>())
                .SelectMany(GetCnnectionIds)
                .Distinct()
                .ToArray();

            var payload = new
            {
                id = data.MessageId,
                title = data.Title,
                message = data.Message,
                content = data.Message,
                date = data.Date,
                fromUser = data.FromUser,
                senderUserId = data.SenderUserId,
                recipientCount = data.RecipientCount,
                userNames = data.UserNames
            };

            if (connectionIds.Length > 0)
            {
                await Clients.Clients(connectionIds).SendAsync("ReceiveHomePageMessage", payload);
            }

            return new
            {
                success = true,
                message = connectionIds.Length > 0
                    ? $"已发送给{data.RecipientCount}个用户，{connectionIds.Length}个在线连接已收到通知"
                    : $"已发送给{data.RecipientCount}个用户，当前没有在线连接，用户登录后仍可查看",
                onlineConnectionCount = connectionIds.Length,
                recipientCount = data.RecipientCount
            };
        }

        /// <summary>
        /// 服务端业务代码主动推送消息给指定用户(所有在线客户端)，不入库
        /// 用法：构造函数注入IHubContext&lt;HomePageMessageHub&gt;后调用此方法；username为空时推送给全部在线用户
        /// 如需入库，请先调用ISys_MessageService.CreateMessageAsync再调用此方法
        /// </summary>
        /// <param name="hubContext">IHubContext&lt;HomePageMessageHub&gt;(依赖注入获取)</param>
        /// <param name="username">sys_user表的登陆帐号，为空发送给所有人</param>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        public static async Task SendMessageAsync(IHubContext<HomePageMessageHub> hubContext, string username, string title, string message)
        {
            var payload = new
            {
                title,
                message,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            if (string.IsNullOrEmpty(username))
            {
                await hubContext.Clients.All.SendAsync("ReceiveHomePageMessage", payload);
                return;
            }
            var connectionIds = _connectionIds.Where(x => x.Value == username).Select(s => s.Key).ToArray();
            if (connectionIds.Length > 0)
            {
                await hubContext.Clients.Clients(connectionIds).SendAsync("ReceiveHomePageMessage", payload);
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UserOffline()
        {
            var cid = Context.ConnectionId;//也可以从缓存中获取ConnectionId
            //移除缓存
            if (_connectionIds.TryRemove(cid, out string value))
            {
            }
            await Task.CompletedTask;
            return true;
        }


    }
}
