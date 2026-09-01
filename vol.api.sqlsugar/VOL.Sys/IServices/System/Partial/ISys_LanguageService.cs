/*
*多语言业务接口
*/
using VOL.Core.Utilities;

namespace VOL.Sys.IServices
{
    public partial interface ISys_LanguageService
    {
        /// <summary>
        /// 生成语言包文件wwwroot/lang/{en,zh-tw,fr,es,ru,ar}.js
        /// </summary>
        WebResponseContent CreateLanguagePack();
    }
}
