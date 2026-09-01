/*
*多语言业务实现：将Sys_Language表数据生成语言包文件到wwwroot/lang目录
*文件内容为纯JSON({"简体中文key":"对应语言翻译"})，前端src/uitils/translator启动时
*按当前语言GET lang/{lang}.js加载后commit到store(setLocal)，$ts()按key取翻译
*/
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VOL.Core.Extensions;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;

namespace VOL.Sys.Services
{
    public partial class Sys_LanguageService
    {
        private readonly ISys_LanguageRepository _repository;//访问数据库
        private readonly IWebHostEnvironment _env;

        [ActivatorUtilitiesConstructor]
        public Sys_LanguageService(
            ISys_LanguageRepository dbRepository,
            IWebHostEnvironment env
            )
        : base(dbRepository)
        {
            _repository = dbRepository;
            _env = env;
        }

        /// <summary>
        /// 生成语言包：每种语言一个文件wwwroot/lang/{lang}.js，内容为JSON
        /// </summary>
        public WebResponseContent CreateLanguagePack()
        {
            //语言文件名与实体翻译列的对应关系(前端lang.vue下拉的value与文件名一致)
            var languages = new Dictionary<string, Func<Sys_Language, string>>
            {
                ["zh-tw"] = x => x.ZHTW,
                ["en"] = x => x.English,
                ["fr"] = x => x.French,
                ["es"] = x => x.Spanish,
                ["ru"] = x => x.Russian,
                ["ar"] = x => x.Arabic
            };

            var list = _repository.FindAsIQueryable(x => true).ToList();

            string webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = (_env.ContentRootPath + "/wwwroot").ReplacePath();
            }
            string langPath = Path.Combine(webRootPath, "lang").ReplacePath();
            if (!Directory.Exists(langPath))
            {
                Directory.CreateDirectory(langPath);
            }

            foreach (var lang in languages)
            {
                var dic = new Dictionary<string, string>();
                foreach (var item in list)
                {
                    string key = item.ZHCN?.Trim();
                    string value = lang.Value(item)?.Trim();
                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    {
                        continue;
                    }
                    dic[key] = value;//重复key取最后一条
                }
                //utf-8无BOM，前端直接JSON.parse文件内容
                File.WriteAllText(Path.Combine(langPath, $"{lang.Key}.js"),
                    dic.Serialize(), new UTF8Encoding(false));
            }

            return WebResponseContent.Instance.OK($"语言包生成成功,共{list.Count}条翻译数据");
        }
    }
}
