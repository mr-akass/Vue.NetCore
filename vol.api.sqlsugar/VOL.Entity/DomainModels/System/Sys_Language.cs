/*
 *多语言翻译表：ZHCN为翻译key(简体中文)，其余列为对应语言翻译
 *语言管理页面维护数据后，点击[生成语言包]调用api/Sys_Language/createLanguagePack
 *生成wwwroot/lang/{en,zh-tw,fr,es,ru,ar}.js，前端启动时按当前语言加载(src/uitils/translator)
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
    [Entity(TableCnName = "语言设置",TableName = "Sys_Language")]
    public partial class Sys_Language:BaseEntity
    {
        /// <summary>
       ///Id
       /// </summary>
       [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
       [Key]
       [Display(Name ="Id")]
       [Column(TypeName="int")]
       [Required(AllowEmptyStrings=false)]
       public int Id { get; set; }

       /// <summary>
       ///简体中文(翻译key)
       /// </summary>
       [Display(Name ="简体中文")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ZHCN { get; set; }

       /// <summary>
       ///繁体中文
       /// </summary>
       [Display(Name ="繁体中文")]
       [MaxLength(1000)]
       [Column(TypeName="nvarchar(1000)")]
       [Editable(true)]
       public string ZHTW { get; set; }

       /// <summary>
       ///英语
       /// </summary>
       [Display(Name ="英语")]
       [MaxLength(1000)]
       [Column(TypeName="nvarchar(1000)")]
       [Editable(true)]
       public string English { get; set; }

       /// <summary>
       ///法语
       /// </summary>
       [Display(Name ="法语")]
       [MaxLength(1000)]
       [Column(TypeName="nvarchar(1000)")]
       [Editable(true)]
       public string French { get; set; }

       /// <summary>
       ///西班牙语
       /// </summary>
       [Display(Name ="西班牙语")]
       [MaxLength(1000)]
       [Column(TypeName="nvarchar(1000)")]
       [Editable(true)]
       public string Spanish { get; set; }

       /// <summary>
       ///俄语
       /// </summary>
       [Display(Name ="俄语")]
       [MaxLength(1000)]
       [Column(TypeName="nvarchar(1000)")]
       [Editable(true)]
       public string Russian { get; set; }

       /// <summary>
       ///阿拉伯语
       /// </summary>
       [Display(Name ="阿拉伯语")]
       [MaxLength(1000)]
       [Column(TypeName="nvarchar(1000)")]
       [Editable(true)]
       public string Arabic { get; set; }

       /// <summary>
       ///所属模块(预留分组)
       /// </summary>
       [Display(Name ="Module")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Module { get; set; }

       /// <summary>
       ///是否打包进语言包(1=是)
       /// </summary>
       [Display(Name ="IsPackageContent")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? IsPackageContent { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="CreateID")]
       [Column(TypeName="int")]
       public int? CreateID { get; set; }

       /// <summary>
       ///创建人
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(255)]
       [Column(TypeName="nvarchar(255)")]
       public string Creator { get; set; }

       /// <summary>
       ///创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="ModifyID")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       ///修改人
       /// </summary>
       [Display(Name ="修改人")]
       [MaxLength(255)]
       [Column(TypeName="nvarchar(255)")]
       public string Modifier { get; set; }

       /// <summary>
       ///修改时间
       /// </summary>
       [Display(Name ="修改时间")]
       [Column(TypeName="datetime")]
       public DateTime? ModifyDate { get; set; }


    }
}
