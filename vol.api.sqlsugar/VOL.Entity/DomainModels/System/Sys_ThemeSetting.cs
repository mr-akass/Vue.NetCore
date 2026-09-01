/*
 *主题配置表：每个用户每个应用一套主题(颜色/效果/布局/密度/圆角/字号/背景图)
 *UserId=0 的记录表示"该应用的默认主题"(超管配置),用户自己没配置时前端用它渲染
 *主题项全部放在 ThemeJson 里,新增开关不用改表结构;BgImage 另存一列便于换图时清理旧文件
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
    [Entity(TableCnName = "主题配置", TableName = "Sys_ThemeSetting")]
    public partial class Sys_ThemeSetting : BaseEntity
    {
        /// <summary>
        ///
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        [Key]
        [Display(Name = "ID")]
        [Column(TypeName = "int")]
        [Required(AllowEmptyStrings = false)]
        public int ID { get; set; }

        /// <summary>
        ///所属用户ID(0表示该应用的默认主题)
        /// </summary>
        [Display(Name = "所属用户ID")]
        [Column(TypeName = "int")]
        [Editable(true)]
        [Required(AllowEmptyStrings = false)]
        public int UserId { get; set; }

        /// <summary>
        ///所属应用ID(0表示不区分应用)
        /// </summary>
        [Display(Name = "所属应用ID")]
        [Column(TypeName = "int")]
        [Editable(true)]
        [Required(AllowEmptyStrings = false)]
        public int AppId { get; set; }

        /// <summary>
        ///主题配置JSON(颜色/效果/布局/密度/圆角/字号/背景遮罩等)
        /// </summary>
        [Display(Name = "主题配置")]
        [Column(TypeName = "nvarchar(max)")]
        [Editable(true)]
        public string ThemeJson { get; set; }

        /// <summary>
        ///背景图相对路径(如 /Upload/theme/1/xxx.jpg)
        /// </summary>
        [Display(Name = "背景图")]
        [MaxLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        [Editable(true)]
        public string BgImage { get; set; }

        /// <summary>
        ///记录创建时间
        /// </summary>
        [Display(Name = "记录创建时间")]
        [Column(TypeName = "datetime")]
        [Editable(true)]
        [Required(AllowEmptyStrings = false)]
        public DateTime CreateDate { get; set; }

        /// <summary>
        ///最后修改时间
        /// </summary>
        [Display(Name = "最后修改时间")]
        [Column(TypeName = "datetime")]
        [Editable(true)]
        public DateTime? ModifyDate { get; set; }
    }
}
