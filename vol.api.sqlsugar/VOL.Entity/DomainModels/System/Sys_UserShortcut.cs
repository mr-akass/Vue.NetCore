/*
 *用户快捷菜单表(首页快捷导航)：每个用户每个应用一组快捷项
 *只存MenuId,菜单名/地址/图标渲染时从用户菜单权限中取,菜单改名或权限被收回时快捷项自动跟随
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
    [Entity(TableCnName = "用户快捷菜单",TableName = "Sys_UserShortcut")]
    public partial class Sys_UserShortcut:BaseEntity
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
       ///所属用户ID
       /// </summary>
       [Display(Name ="所属用户ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int UserId { get; set; }

       /// <summary>
       ///菜单ID(Sys_Menu.Menu_Id)
       /// </summary>
       [Display(Name ="菜单ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int MenuId { get; set; }

       /// <summary>
       ///菜单名称(冗余列,便于直接查库排查,渲染以菜单权限为准)
       /// </summary>
       [Display(Name ="菜单名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string MenuName { get; set; }

       /// <summary>
       ///所属应用ID(0表示不区分应用)
       /// </summary>
       [Display(Name ="所属应用ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int AppId { get; set; }

       /// <summary>
       ///排序号(升序,拖动排序后重写)
       /// </summary>
       [Display(Name ="排序号")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int SortOrder { get; set; }

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
