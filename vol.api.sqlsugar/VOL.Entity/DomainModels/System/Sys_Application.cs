/*
 *应用/子系统表(多应用支持)：角色属于应用(Sys_Role.AppID)，用户通过多角色拥有多个应用入口
 *约定：AppName 必须与该应用的一级菜单 MenuName 一致，菜单接口按应用过滤时会隐藏该一级菜单并将其子菜单提升为一级
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
    [Entity(TableCnName = "应用管理",TableName = "Sys_Application")]
    public partial class Sys_Application:BaseEntity
    {
        /// <summary>
       ///应用ID
       /// </summary>
       [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
       [Key]
       [Display(Name ="AppID")]
       [Column(TypeName="int")]
       [Required(AllowEmptyStrings=false)]
       public int AppID { get; set; }

       /// <summary>
       ///应用代码
       /// </summary>
       [Display(Name ="应用代码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string AppCode { get; set; }

       /// <summary>
       ///应用名称(未绑定根菜单时按此名称匹配一级菜单)
       /// </summary>
       [Display(Name ="应用名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string AppName { get; set; }

       /// <summary>
       ///根菜单ID集合(逗号分隔，支持多个一级菜单；应用的菜单范围=所有根菜单子树的并集，可多应用共享公共子树)
       ///为空时回退按AppName同名匹配一级菜单
       /// </summary>
       [Display(Name ="根菜单")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string RootMenuIds { get; set; }

       /// <summary>
       ///标题(浏览器/顶栏标题)
       /// </summary>
       [Display(Name ="标题")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Title { get; set; }

       /// <summary>
       ///图标(el-icon-*)
       /// </summary>
       [Display(Name ="图标")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Icon { get; set; }

       /// <summary>
       ///默认主题
       /// </summary>
       [Display(Name ="默认主题")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Theme { get; set; }

       /// <summary>
       ///主色调
       /// </summary>
       [Display(Name ="主色调")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string PrimaryColor { get; set; }

       /// <summary>
       ///首页数据面板组件名(对应前端src/views/home/{DataPanel}.vue，为空使用默认首页)
       /// </summary>
       [Display(Name ="首页面板")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string DataPanel { get; set; }

       /// <summary>
       ///排序
       /// </summary>
       [Display(Name ="排序")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? SortOrder { get; set; }

       /// <summary>
       ///是否启用
       /// </summary>
       [Display(Name ="是否启用")]
       [Column(TypeName="bit")]
       [Editable(true)]
       public bool? Enabled { get; set; }

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
