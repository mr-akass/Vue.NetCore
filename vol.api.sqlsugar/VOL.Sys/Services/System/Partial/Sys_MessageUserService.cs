/*
*站内消息(收件方)业务实现：我的消息列表、未读数量、标记已读/全部已读
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
    public partial class Sys_MessageUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_MessageUserRepository _repository;//访问数据库
        private readonly ISys_MessageRepository _messageRepository;

        [ActivatorUtilitiesConstructor]
        public Sys_MessageUserService(
            ISys_MessageUserRepository dbRepository,
            IHttpContextAccessor httpContextAccessor,
            ISys_MessageRepository messageRepository
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            _messageRepository = messageRepository;
        }

        /// <summary>
        /// 我的未读消息数量
        /// </summary>
        public async Task<object> GetMyUnreadCountAsync()
        {
            int userId = UserContext.Current.UserId;
            var count = await _repository.FindAsIQueryable(x => x.UserId == userId && !x.IsRead)
                .CountAsync();
            return new { unreadCount = count };
        }

        /// <summary>
        /// 我的消息列表(join消息主体，按消息时间倒序分页)
        /// </summary>
        public async Task<object> GetMyMessagesAsync(int page = 1, int rows = 20)
        {
            if (page <= 0) page = 1;
            if (rows <= 0) rows = 20;

            int userId = UserContext.Current.UserId;
            RefAsync<int> total = 0;
            var list = await _repository.SqlSugarClient.Queryable<Sys_MessageUser>()
                .InnerJoin<Sys_Message>((mu, m) => mu.MessageId == m.ID)
                .Where((mu, m) => mu.UserId == userId)
                .OrderByDescending((mu, m) => m.CreateDate)
                .Select((mu, m) => new MyMessageItem
                {
                    ID = mu.ID,
                    MessageId = mu.MessageId,
                    IsRead = mu.IsRead,
                    ReadDate = mu.ReadDate,
                    MessageUserCreateDate = mu.CreateDate,
                    Title = m.Title,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    SenderUserName = m.SenderUserName,
                    SenderUserId = m.SenderUserId,
                    MessageCreateDate = m.CreateDate
                })
                .ToPageListAsync(page, rows, total);

            var rowsData = list.Select(x => new
            {
                id = x.ID,
                messageId = x.MessageId,
                title = x.Title,
                message = x.Content,
                content = x.Content,
                messageType = x.MessageType,
                senderUserName = x.SenderUserName,
                senderUserId = x.SenderUserId,
                isRead = x.IsRead,
                readDate = x.ReadDate.HasValue ? x.ReadDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                date = x.MessageCreateDate.ToString("yyyy-MM-dd HH:mm:ss"),
                createDate = x.MessageUserCreateDate.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            return new
            {
                total = (int)total,
                rows = rowsData
            };
        }

        /// <summary>
        /// 标记单条消息已读(只能标记自己的消息)
        /// </summary>
        public async Task<WebResponseContent> MarkAsReadAsync(int id)
        {
            var webResponse = new WebResponseContent();
            int userId = UserContext.Current.UserId;
            var entity = await _repository.FindFirstAsync(x => x.ID == id && x.UserId == userId);
            if (entity == null)
            {
                return webResponse.Error("消息不存在");
            }

            if (entity.IsRead)
            {
                return webResponse.OK("消息已读");
            }

            entity.IsRead = true;
            entity.ReadDate = DateTime.Now;
            var effectRows = _repository.Update(entity, x => new { x.IsRead, x.ReadDate }, true);
            return effectRows > 0 ? webResponse.OK("已标记为已读") : webResponse.Error("标记已读失败");
        }

        /// <summary>
        /// 全部标记已读
        /// </summary>
        public async Task<WebResponseContent> MarkAllAsReadAsync()
        {
            var webResponse = new WebResponseContent();
            int userId = UserContext.Current.UserId;
            var now = DateTime.Now;
            var list = await _repository.FindAsIQueryable(x => x.UserId == userId && !x.IsRead).ToListAsync();
            if (list.Count == 0)
            {
                return webResponse.OK("没有未读消息");
            }

            list.ForEach(x =>
            {
                x.IsRead = true;
                x.ReadDate = now;
            });

            _repository.UpdateRange(list, x => new { x.IsRead, x.ReadDate });
            var effectRows = _repository.SaveChanges();
            return effectRows > 0 ? webResponse.OK("已全部标记为已读") : webResponse.Error("批量标记已读失败");
        }

        private class MyMessageItem
        {
            public int ID { get; set; }
            public int MessageId { get; set; }
            public bool IsRead { get; set; }
            public DateTime? ReadDate { get; set; }
            public DateTime MessageUserCreateDate { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public int MessageType { get; set; }
            public string SenderUserName { get; set; }
            public int SenderUserId { get; set; }
            public DateTime MessageCreateDate { get; set; }
        }
    }
}
