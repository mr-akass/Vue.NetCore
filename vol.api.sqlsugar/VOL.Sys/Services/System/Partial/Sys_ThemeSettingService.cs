/*
*主题个性化业务实现：我的主题、应用默认主题、背景图上传/删除
*设计说明：
*  1) 主键是(UserId,AppId)——同一个用户在不同应用下可以是完全不同的主题,切应用即换肤;
*     UserId=0 的行是"该应用的默认主题"(超管设置),用户没自己存过主题时前端用它兜底
*  2) 所有旋钮(颜色/效果/布局/字号/背景遮罩...)整体存 ThemeJson 一列:旋钮只会越加越多,
*     每加一个旋钮改一次表结构不现实,后端也完全不需要理解每个旋钮的含义
*  3) BgImage 单独一列(与 ThemeJson 里的 bgImage 同步):换图/重置时要能不解析JSON就找到旧文件删掉,
*     并且能判断"这个文件还有没有别的行在用"(超管上传的图既是自己的也可能是应用默认的)
*  4) 接口只操作 UserContext.Current.UserId 自己的数据,写应用默认额外要求超管
*/
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;

namespace VOL.Sys.Services
{
    public partial class Sys_ThemeSettingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_ThemeSettingRepository _repository;//访问数据库

        [ActivatorUtilitiesConstructor]
        public Sys_ThemeSettingService(
            ISys_ThemeSettingRepository dbRepository,
            IHttpContextAccessor httpContextAccessor
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// ThemeJson 长度上限(前端配置项就那几十个,超出这个量级说明是脏数据/被篡改)
        /// </summary>
        private const int MaxThemeJsonLength = 8000;

        /// <summary>
        /// 背景图单文件上限5M(背景图会被每个页面加载,再大前端体验反而更差)
        /// </summary>
        private const long MaxBackgroundSize = 5 * 1024 * 1024;

        private static readonly string[] AllowBackgroundExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };

        /// <summary>
        /// 背景图存放目录(相对wwwroot,与框架 files.Save 保持同一套路径规则)
        /// </summary>
        private const string BackgroundDirectory = "Upload/theme";

        /// <summary>
        /// 读我的主题:一次返回"我的"+"当前应用默认"+"是否超管",前端只请求一次就能决定用哪套
        /// </summary>
        public async Task<object> GetMyThemeAsync(int? appId = null)
        {
            int userId = UserContext.Current.UserId;
            int currentAppId = appId ?? 0;

            //一次查两行:我的(UserId=userId)和应用默认(UserId=0)
            var list = await _repository.FindAsIQueryable(x => x.AppId == currentAppId && (x.UserId == userId || x.UserId == 0))
                .ToListAsync();

            var mine = list.FirstOrDefault(x => x.UserId == userId);
            var appDefault = list.FirstOrDefault(x => x.UserId == 0);

            return new
            {
                appId = currentAppId,
                //没存过返回null,前端用内置预设,不在后端造一份默认值(否则前后端两套默认值容易不一致)
                theme = mine?.ThemeJson,
                appDefault = appDefault?.ThemeJson,
                isSuperAdmin = UserContext.Current.IsSuperAdmin
            };
        }

        /// <summary>
        /// 保存我的主题(按 UserId+AppId 覆盖)
        /// </summary>
        public async Task<WebResponseContent> SaveMyThemeAsync(string themeJson, int? appId = null)
        {
            return await SaveThemeAsync(UserContext.Current.UserId, themeJson, appId);
        }

        /// <summary>
        /// 设为当前应用的默认主题(超管专用,存 UserId=0 的行)
        /// </summary>
        public async Task<WebResponseContent> SaveAppDefaultAsync(string themeJson, int? appId = null)
        {
            if (!UserContext.Current.IsSuperAdmin)
            {
                return new WebResponseContent().Error("只有超级管理员可以设置应用默认主题");
            }
            return await SaveThemeAsync(0, themeJson, appId);
        }

        /// <summary>
        /// 重置我的主题(删掉我这行,前端回落到应用默认/内置预设),连带清理我上传的背景图
        /// </summary>
        public async Task<WebResponseContent> ResetMyThemeAsync(int? appId = null)
        {
            var webResponse = new WebResponseContent();
            int userId = UserContext.Current.UserId;
            int currentAppId = appId ?? 0;

            var setting = await GetSettingAsync(userId, currentAppId);
            if (setting == null)
            {
                return webResponse.OK("已是默认主题");
            }

            await _repository.SqlSugarClient.Deleteable<Sys_ThemeSetting>()
                .Where(x => x.ID == setting.ID)
                .ExecuteCommandAsync();

            await DeleteBackgroundFileAsync(setting.BgImage, setting.ID);
            return webResponse.OK("已重置为默认主题");
        }

        /// <summary>
        /// 上传背景图:GUID重命名后存 Upload/theme/{userId}/,同时写回我这行的 BgImage 与 ThemeJson.bgImage
        /// </summary>
        public async Task<WebResponseContent> UploadBackgroundAsync(List<IFormFile> files, int? appId = null)
        {
            var webResponse = new WebResponseContent();
            if ((files?.Count ?? 0) == 0)
            {
                return webResponse.Error("请选择背景图");
            }

            var file = files[0];
            if (file.Length <= 0)
            {
                return webResponse.Error("背景图内容为空");
            }
            if (file.Length > MaxBackgroundSize)
            {
                return webResponse.Error($"背景图不能超过{MaxBackgroundSize / 1024 / 1024}M");
            }

            //只认扩展名白名单:文件会被静态中间件直接吐给浏览器,不能让人传 .html/.js 之类可执行内容
            string extension = Path.GetExtension(file.FileName)?.ToLower() ?? "";
            if (!AllowBackgroundExtensions.Contains(extension))
            {
                return webResponse.Error($"只支持{string.Join("/", AllowBackgroundExtensions)}格式的图片");
            }

            int userId = UserContext.Current.UserId;
            int currentAppId = appId ?? 0;

            //文件名一律GUID重命名:原文件名可能带路径/中文/重名,直接落盘既不安全也会互相覆盖
            string fileName = $"{Guid.NewGuid():N}{extension}";
            string directory = $"{BackgroundDirectory}/{userId}/";
            try
            {
                await new List<IFormFile> { file }.SaveAsync(directory, fileName);
            }
            catch (Exception ex)
            {
                CustomConsole.WriteLine(NlogLoggerType.Error, $"主题背景图保存失败,userId:{userId},{ex.Message}");
                return webResponse.Error("背景图保存失败");
            }

            string url = $"/{directory}{fileName}";
            var setting = await GetSettingAsync(userId, currentAppId);
            string oldImage = setting?.BgImage;

            if (setting == null)
            {
                //还没存过主题就先上传了图:建一行只记图,主题旋钮等用户点保存时再写
                setting = new Sys_ThemeSetting
                {
                    UserId = userId,
                    AppId = currentAppId,
                    BgImage = url,
                    ThemeJson = new JObject { ["bgImage"] = url }.ToString(Newtonsoft.Json.Formatting.None),
                    CreateDate = DateTime.Now
                };
                await _repository.SqlSugarClient.Insertable(setting).ExecuteCommandAsync();
            }
            else
            {
                setting.BgImage = url;
                setting.ThemeJson = SetBackgroundInJson(setting.ThemeJson, url);
                setting.ModifyDate = DateTime.Now;
                await _repository.SqlSugarClient.Updateable(setting)
                    .UpdateColumns(x => new { x.BgImage, x.ThemeJson, x.ModifyDate })
                    .ExecuteCommandAsync();
            }

            //换图后把旧文件删掉,否则用户反复换图会在服务器上越堆越多
            await DeleteBackgroundFileAsync(oldImage, setting.ID);

            return webResponse.OK("背景图已上传", new { url });
        }

        /// <summary>
        /// 删除背景图(清 BgImage 列 + ThemeJson.bgImage + 物理文件)
        /// </summary>
        public async Task<WebResponseContent> RemoveBackgroundAsync(int? appId = null)
        {
            var webResponse = new WebResponseContent();
            int userId = UserContext.Current.UserId;
            int currentAppId = appId ?? 0;

            var setting = await GetSettingAsync(userId, currentAppId);
            if (setting == null || string.IsNullOrEmpty(setting.BgImage))
            {
                return webResponse.OK("当前没有背景图");
            }

            string oldImage = setting.BgImage;
            setting.BgImage = null;
            setting.ThemeJson = SetBackgroundInJson(setting.ThemeJson, null);
            setting.ModifyDate = DateTime.Now;
            await _repository.SqlSugarClient.Updateable(setting)
                .UpdateColumns(x => new { x.BgImage, x.ThemeJson, x.ModifyDate })
                .ExecuteCommandAsync();

            await DeleteBackgroundFileAsync(oldImage, setting.ID);
            return webResponse.OK("背景图已删除");
        }

        /// <summary>
        /// 保存主题(我的与应用默认共用一套逻辑,只差 userId)
        /// </summary>
        private async Task<WebResponseContent> SaveThemeAsync(int userId, string themeJson, int? appId)
        {
            var webResponse = new WebResponseContent();
            int currentAppId = appId ?? 0;

            var (valid, message, normalizedJson, bgImage) = ValidateThemeJson(themeJson);
            if (!valid)
            {
                return webResponse.Error(message);
            }

            var setting = await GetSettingAsync(userId, currentAppId);
            if (setting == null)
            {
                setting = new Sys_ThemeSetting
                {
                    UserId = userId,
                    AppId = currentAppId,
                    ThemeJson = normalizedJson,
                    BgImage = bgImage,
                    CreateDate = DateTime.Now
                };
                await _repository.SqlSugarClient.Insertable(setting).ExecuteCommandAsync();
                return webResponse.OK("主题已保存");
            }

            string oldImage = setting.BgImage;
            setting.ThemeJson = normalizedJson;
            setting.BgImage = bgImage;
            setting.ModifyDate = DateTime.Now;
            await _repository.SqlSugarClient.Updateable(setting)
                .UpdateColumns(x => new { x.ThemeJson, x.BgImage, x.ModifyDate })
                .ExecuteCommandAsync();

            //前端可能在保存时把背景图换成了别的(或清空),旧文件顺手清理
            if (!string.Equals(oldImage, bgImage, StringComparison.OrdinalIgnoreCase))
            {
                await DeleteBackgroundFileAsync(oldImage, setting.ID);
            }
            return webResponse.OK("主题已保存");
        }

        private async Task<Sys_ThemeSetting> GetSettingAsync(int userId, int appId)
        {
            return await _repository.FindAsIQueryable(x => x.UserId == userId && x.AppId == appId)
                .FirstAsync();
        }

        /// <summary>
        /// 校验并规范化前端提交的主题JSON,顺带取出 bgImage 存到独立列
        /// 后端不校验每个旋钮的取值(旋钮是前端定义的,后端跟着改等于两处维护),只把住三条底线:
        /// 长度、必须是JSON对象、bgImage 只能指向本站上传目录或http(s)地址(防止塞 javascript: 之类进 css url())
        /// </summary>
        private (bool valid, string message, string json, string bgImage) ValidateThemeJson(string themeJson)
        {
            if (string.IsNullOrWhiteSpace(themeJson))
            {
                return (false, "主题配置不能为空", null, null);
            }
            if (themeJson.Length > MaxThemeJsonLength)
            {
                return (false, "主题配置内容过长", null, null);
            }

            JObject obj;
            try
            {
                obj = JObject.Parse(themeJson);
            }
            catch
            {
                return (false, "主题配置格式不正确", null, null);
            }

            string bgImage = obj["bgImage"]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(bgImage))
            {
                bgImage = null;
                obj.Remove("bgImage");
            }
            else if (!IsAllowedBackgroundUrl(bgImage))
            {
                return (false, "背景图地址不合法", null, null);
            }

            return (true, null, obj.ToString(Newtonsoft.Json.Formatting.None), bgImage);
        }

        private bool IsAllowedBackgroundUrl(string url)
        {
            return url.StartsWith($"/{BackgroundDirectory}/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 把 ThemeJson 里的 bgImage 换成指定值(传null表示移除),其余旋钮原样保留
        /// </summary>
        private string SetBackgroundInJson(string themeJson, string bgImage)
        {
            JObject obj;
            try
            {
                obj = string.IsNullOrWhiteSpace(themeJson) ? new JObject() : JObject.Parse(themeJson);
            }
            catch
            {
                obj = new JObject();
            }

            if (string.IsNullOrEmpty(bgImage))
            {
                obj.Remove("bgImage");
            }
            else
            {
                obj["bgImage"] = bgImage;
            }
            return obj.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>
        /// 删除背景图物理文件
        /// 删之前必须确认没有别的行还在引用同一个文件:超管上传的图既在自己那行,也可能被设成了应用默认,
        /// 超管重置自己的主题时直接删文件会把应用默认的背景图一起删没
        /// </summary>
        private async Task DeleteBackgroundFileAsync(string url, int excludeId)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith($"/{BackgroundDirectory}/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool stillUsed = await _repository.FindAsIQueryable(x => x.ID != excludeId && x.BgImage == url).AnyAsync();
            if (stillUsed)
            {
                return;
            }

            try
            {
                string fullPath = url.TrimStart('/').MapPath(true);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                //删不掉只是留个垃圾文件,不能因此让主题保存失败
                CustomConsole.WriteLine(NlogLoggerType.Error, $"主题背景图删除失败,{url},{ex.Message}");
            }
        }
    }
}
