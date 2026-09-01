/*
*主题个性化业务接口：读取/保存我的主题、重置、设为应用默认、背景图上传与删除
*/
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Core.Utilities;

namespace VOL.Sys.IServices
{
    public partial interface ISys_ThemeSettingService
    {
        Task<object> GetMyThemeAsync(int? appId = null);
        Task<WebResponseContent> SaveMyThemeAsync(string themeJson, int? appId = null);
        Task<WebResponseContent> ResetMyThemeAsync(int? appId = null);
        Task<WebResponseContent> SaveAppDefaultAsync(string themeJson, int? appId = null);
        Task<WebResponseContent> UploadBackgroundAsync(List<IFormFile> files, int? appId = null);
        Task<WebResponseContent> RemoveBackgroundAsync(int? appId = null);
    }
}
