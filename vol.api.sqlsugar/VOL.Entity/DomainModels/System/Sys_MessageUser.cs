/*
 *站内消息收件人表(已读/未读状态)：每个收件人一条记录
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
    [Entity(TableCnName = "站内消息收件人",TableName = "Sys_MessageUser")]
    public partial class Sys_MessageUser:BaseEntity
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
       ///关联消息ID
       /// </summary>
       [Display(Name ="关联消息ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int MessageId { get; set; }

       /// <summary>
       ///收件人用户名
       /// </summary>
       [Display(Name ="收件人用户名")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string UserName { get; set; }

       /// <summary>
       ///收件人ID
       /// </summary>
       [Display(Name ="收件人ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int UserId { get; set; }

       /// <summary>
       ///是否已读(默认0)
       /// </summary>
       [Display(Name ="是否已读")]
       [Column(TypeName="bit")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public bool IsRead { get; set; }

       /// <summary>
       ///阅读时间
       /// </summary>
       [Display(Name ="阅读时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? ReadDate { get; set; }

       /// <summary>
       ///记录创建时间
       /// </summary>
       [Display(Name ="记录创建时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreateDate { get; set; }


    }
}
