/*
 *系统配置(键值对)读写接口
 */
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Core.Enums;
using VOL.Core.Utilities;

namespace VOL.Sys.IServices
{
    public partial interface ISys_ConfigSettingService
    {
        /// <summary>
        /// 按键取值(不存在返回null)
        /// </summary>
        Task<string> GetValueAsync(string key);

        /// <summary>
        /// 批量取值,返回 key=>value
        /// </summary>
        Task<Dictionary<string, string>> GetValuesAsync(params string[] keys);

        /// <summary>
        /// 保存(存在则更新,不存在则新增)
        /// </summary>
        Task<WebResponseContent> SetValueAsync(string key, string value, string remark = null);

        /// <summary>
        /// 批量保存
        /// </summary>
        Task<WebResponseContent> SetValuesAsync(Dictionary<string, string> values);
    }
}
