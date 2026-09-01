/*
*站内消息(发送方)业务实现：发送入库(Sys_Message + Sys_MessageUser事务写入)、发送记录、收件人阅读状态
*参照 ShelfLifeMgt 迁移，数据访问按 SqlSugar 适配
*/
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;

namespace VOL.Sys.Services
{
    public partial class Sys_MessageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_MessageRepository _repository;//访问数据库
        private readonly ISys_MessageUserRepository _messageUserRepository;
        private readonly ISys_UserRepository _userRepository;

        [ActivatorUtilitiesConstructor]
        public Sys_MessageService(
            ISys_MessageRepository dbRepository,
            IHttpContextAccessor httpContextAccessor,
            ISys_MessageUserRepository messageUserRepository,
            ISys_UserRepository userRepository
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            _messageUserRepository = messageUserRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// 发送站内消息：Sys_Message写入消息主体，Sys_MessageUser按收件人写入已读状态记录(事务)
        /// </summary>
        public async Task<WebResponseContent> CreateMessageAsync(MessageSendInput input)
        {
            var webResponse = new WebResponseContent();
            var userNames = (input?.UserNames ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (userNames.Count == 0)
            {
                return webResponse.Error("请选择至少一个收件人");
            }

            if (string.IsNullOrWhiteSpace(input?.Title))
            {
                return webResponse.Error("消息标题不能为空");
            }

            if (string.IsNullOrWhiteSpace(input?.Content))
            {
                return webResponse.Error("消息内容不能为空");
            }

            var senderUserId = input?.SenderUserId ?? 0;
            var senderUserName = input?.SenderUserName;

            //Hub内无法拿到登录上下文时，按发送人用户名从数据库解析
            if (senderUserId <= 0 && !string.IsNullOrWhiteSpace(senderUserName))
            {
                var senderUser = await _userRepository.FindAsIQueryable(x => x.UserName == senderUserName)
                    .Select(x => new { x.User_Id, x.UserName, x.UserTrueName })
                    .FirstAsync();
                if (senderUser != null)
                {
                    senderUserId = senderUser.User_Id;
                    senderUserName = string.IsNullOrWhiteSpace(senderUser.UserTrueName) ? senderUser.UserName : senderUser.UserTrueName;
                }
            }

            if (senderUserId <= 0 || string.IsNullOrWhiteSpace(senderUserName))
            {
                return webResponse.Error("未获取到发送人信息");
            }

            var recipients = await _userRepository.FindAsIQueryable(x => x.Enable == 1 && userNames.Contains(x.UserName))
                .Select(x => new { x.User_Id, x.UserName, x.UserTrueName })
                .ToListAsync();

            if (recipients.Count == 0)
            {
                return webResponse.Error("未找到有效收件人");
            }

            var validUserNames = recipients.Select(x => x.UserName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var now = DateTime.Now;
            var message = new Sys_Message
            {
                Title = input.Title.Trim(),
                Content = input.Content.Trim(),
                MessageType = input.MessageType <= 0 ? 1 : input.MessageType,
                SenderUserName = senderUserName,
                SenderUserId = senderUserId,
                RecipientCount = validUserNames.Count,
                CreateDate = now
            };

            var transactionResult = _repository.DbContextBeginTransaction(() =>
            {
                var db = _repository.SqlSugarClient;
                //插入消息主体并取回自增ID
                int messageId = db.Insertable(message).ExecuteReturnIdentity();
                if (messageId <= 0)
                {
                    return webResponse.Error("消息保存失败");
                }
                message.ID = messageId;

                var userMessages = recipients.Select(x => new Sys_MessageUser
                {
                    MessageId = messageId,
                    UserId = x.User_Id,
                    UserName = x.UserName,
                    IsRead = false,
                    ReadDate = null,
                    CreateDate = now
                }).ToList();

                int effectRows = db.Insertable(userMessages).ExecuteCommand();
                if (effectRows <= 0)
                {
                    return webResponse.Error("收件人记录保存失败");
                }

                return webResponse.OK("消息保存成功", new MessageSendResult
                {
                    MessageId = messageId,
                    RecipientCount = validUserNames.Count,
                    UserNames = validUserNames,
                    Title = message.Title,
                    Message = message.Content,
                    Date = now.ToString("yyyy-MM-dd HH:mm:ss"),
                    FromUser = senderUserName,
                    SenderUserId = senderUserId
                });
            });

            return await Task.FromResult(transactionResult);
        }

        /// <summary>
        /// 我的发送记录(含每条消息的已读/未读人数)
        /// </summary>
        public async Task<object> GetSentMessagesAsync(int page = 1, int rows = 20)
        {
            if (page <= 0) page = 1;
            if (rows <= 0) rows = 20;

            int userId = UserContext.Current.UserId;
            RefAsync<int> total = 0;
            var list = await _repository.SqlSugarClient.Queryable<Sys_Message>()
                .Where(x => x.SenderUserId == userId)
                .OrderByDescending(x => x.ID)
                .ToPageListAsync(page, rows, total);

            var messageIds = list.Select(x => x.ID).ToList();
            var readCounts = new List<MessageReadCount>();
            if (messageIds.Count > 0)
            {
                readCounts = await _messageUserRepository.SqlSugarClient.Queryable<Sys_MessageUser>()
                    .Where(x => messageIds.Contains(x.MessageId))
                    .GroupBy(x => x.MessageId)
                    .Select(x => new MessageReadCount
                    {
                        MessageId = x.MessageId,
                        //bit列需显式比较，否则生成的CASE WHEN缺少布尔条件
                        ReadCount = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsRead == true, 1, 0)),
                        UnreadCount = SqlFunc.AggregateSum(SqlFunc.IIF(x.IsRead == false, 1, 0))
                    })
                    .ToListAsync();
            }

            var rowsData = list.Select(x =>
            {
                var count = readCounts.FirstOrDefault(c => c.MessageId == x.ID);
                return new
                {
                    id = x.ID,
                    messageId = x.ID,
                    title = x.Title,
                    content = x.Content,
                    messageType = x.MessageType,
                    senderUserName = x.SenderUserName,
                    senderUserId = x.SenderUserId,
                    recipientCount = x.RecipientCount,
                    readCount = count?.ReadCount ?? 0,
                    unreadCount = count?.UnreadCount ?? 0,
                    createDate = x.CreateDate.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }).ToList();

            return new
            {
                total = (int)total,
                rows = rowsData
            };
        }

        /// <summary>
        /// 消息的收件人阅读状态(仅发送人本人或超级管理员可看)
        /// </summary>
        public async Task<object> GetRecipientStatusAsync(int messageId)
        {
            var message = await _repository.FindFirstAsync(x => x.ID == messageId);
            if (message == null)
            {
                return new WebResponseContent().Error("消息不存在");
            }

            if (message.SenderUserId != UserContext.Current.UserId && !UserContext.Current.IsSuperAdmin)
            {
                return new WebResponseContent().Error("无权限查看此消息");
            }

            var recipientList = await _messageUserRepository.FindAsIQueryable(x => x.MessageId == messageId)
                .OrderBy(x => x.UserName)
                .ToListAsync();

            var recipients = recipientList.Select(x => new
            {
                id = x.ID,
                userId = x.UserId,
                userName = x.UserName,
                isRead = x.IsRead,
                readDate = x.ReadDate.HasValue ? x.ReadDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                createDate = x.CreateDate.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            return new
            {
                message = new
                {
                    id = message.ID,
                    title = message.Title,
                    content = message.Content,
                    recipientCount = message.RecipientCount,
                    createDate = message.CreateDate.ToString("yyyy-MM-dd HH:mm:ss")
                },
                recipients
            };
        }

        private class MessageReadCount
        {
            public int MessageId { get; set; }
            public int ReadCount { get; set; }
            public int UnreadCount { get; set; }
        }
    }
}
