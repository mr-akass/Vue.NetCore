/*
 *站内消息主表(SignalR消息入库)：消息内容一条，收件人已读状态见Sys_MessageUser
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
using VOL.Entity.SystemModels;

namespace VOL.Entity.DomainModels
{
    [Entity(TableCnName = "站内消息",TableName = "Sys_Message")]
    public partial class Sys_Message:BaseEntity
    {
        /// <summary>
       ///
       /// </summary>
       [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
       [Key]
       [Display(Name ="ID")]
       [Column(TypeName="int")]
       [Required(AllowEmptyStrings=false)]
       public int ID { get; set; }

       /// <summary>
       ///消息标题
       /// </summary>
       [Display(Name ="消息标题")]
       [MaxLength(255)]
       [Column(TypeName="nvarchar(255)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string Title { get; set; }

       /// <summary>
       ///消息内容
       /// </summary>
       [Display(Name ="消息内容")]
       [Column(TypeName="nvarchar(max)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string Content { get; set; }

       /// <summary>
       ///消息类型(1=系统通知,2=公告,预留扩展)
       /// </summary>
       [Display(Name ="消息类型")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int MessageType { get; set; }

       /// <summary>
       ///发送人用户名
       /// </summary>
       [Display(Name ="发送人用户名")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string SenderUserName { get; set; }

       /// <summary>
       ///发送人ID
       /// </summary>
       [Display(Name ="发送人ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int SenderUserId { get; set; }

       /// <summary>
       ///收件人数量
       /// </summary>
       [Display(Name ="收件人数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int RecipientCount { get; set; }

       /// <summary>
       ///发送时间
       /// </summary>
       [Display(Name ="发送时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreateDate { get; set; }


    }
}
