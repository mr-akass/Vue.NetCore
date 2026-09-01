/*
 *所有关于Sys_QuartzOptions类的业务代码应在此处编写
*可使用repository.调用常用方法，获取EF/Dapper等信息
*如果需要事务请使用repository.DbContextBeginTransaction
*也可使用DBServerProvider.手动获取数据库相关信息
*用户信息、权限、角色等使用UserContext.Current操作
*Sys_QuartzOptionsService对增、删、改查、导入、导出、审核业务代码扩展参照ServiceFunFilter
*/
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;
using System.Linq;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using VOL.Sys.IRepositories;
using VOL.Core.Quartz;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace VOL.Sys.Services
{
    public partial class Sys_QuartzOptionsService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_QuartzOptionsRepository _repository;//访问数据库
        private readonly ISchedulerFactory _schedulerFactory;
        [ActivatorUtilitiesConstructor]
        public Sys_QuartzOptionsService(
            ISys_QuartzOptionsRepository dbRepository,
            IHttpContextAccessor httpContextAccessor,
            ISchedulerFactory schedulerFactory
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            _schedulerFactory = schedulerFactory;
            //多租户会用到这init代码，其他情况可以不用
            //base.Init(dbRepository);
        }

        public override PageGridData<Sys_QuartzOptions> GetPageData(PageDataOptions options)
        {
            var result = base.GetPageData(options);
            return result;
        }

        WebResponseContent webResponse = new WebResponseContent();

        /// <summary>
        /// 根据Cron表达式生成中文描述(与前端CronBuilderDialog生成的四种频率对应)
        /// Cron格式: 秒 分 时 日 月 周
        /// </summary>
        private string GenerateCronDescr(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression)) return "";
            var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6) return cronExpression;

            string min = parts[1], hour = parts[2], day = parts[3], week = parts[5];

            if (hour == "*")
            {
                return $"每小时第{min}分钟执行";
            }
            if (week != "?" && week != "*")
            {
                var dayMap = new Dictionary<string, string>
                {
                    {"MON","星期一"},{"TUE","星期二"},{"WED","星期三"},{"THU","星期四"},
                    {"FRI","星期五"},{"SAT","星期六"},{"SUN","星期日"}
                };
                string dayName = dayMap.ContainsKey(week) ? dayMap[week] : week;
                return $"每周{dayName} {hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}执行";
            }
            if (day != "*" && day != "?")
            {
                return $"每月{day}日 {hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}执行";
            }
            return $"每天{hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}执行";
        }

        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            AddOnExecuting = (Sys_QuartzOptions options, object list) =>
            {
                //根据cron表达式自动生成中文描述
                options.CronDescr = GenerateCronDescr(options.CronExpression);
                options.CronStr = options.CronExpression;
                options.Status = (int)TriggerState.Paused;
                return webResponse.OK();
            };
            Sys_QuartzOptions ops = null;
            AddOnExecuted = (Sys_QuartzOptions options, object list) =>
            {
                ops = options;
                return webResponse.OK();
            };
            var result = base.Add(saveDataModel);
            if (result.Status)
            {
                ops.AddJob(_schedulerFactory).GetAwaiter().GetResult();
            }
            return result;
        }

        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            var ids = keys.Select(s => (Guid)(s.GetGuid())).ToArray();

            repository.FindAsIQueryable(x => ids.Contains(x.Id)).ToList().ForEach(options =>
            {
                _schedulerFactory.Remove(options).GetAwaiter().GetResult();
            });

            return base.Del(keys, delList);
        }

        public override WebResponseContent Update(SaveModel saveModel)
        {
            //根据cron表达式自动生成中文描述，加入MainData确保框架更新这些字段
            string cronExpr = saveModel.MainData.ContainsKey("CronExpression") ? saveModel.MainData["CronExpression"]?.ToString() : "";
            if (!string.IsNullOrWhiteSpace(cronExpr))
            {
                saveModel.MainData["CronDescr"] = GenerateCronDescr(cronExpr);
                saveModel.MainData["CronStr"] = cronExpr;
            }

            UpdateOnExecuted = (Sys_QuartzOptions options, object addList, object updateList, List<object> delKeys) =>
            {
                _schedulerFactory.Update(options).GetAwaiter().GetResult();
                return webResponse.OK();
            };
            return base.Update(saveModel);
        }

        /// <summary>
        /// 手动执行一次
        /// </summary>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public async Task<object> Run(Sys_QuartzOptions taskOptions)
        {
            return await _schedulerFactory.Run(taskOptions);
        }
        /// <summary>
        /// 开启任务
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public async Task<object> Start(Sys_QuartzOptions taskOptions)
        {
            var result = await _schedulerFactory.Start(taskOptions);
            if (taskOptions.Status != (int)TriggerState.Normal)
            {
                taskOptions.Status = (int)TriggerState.Normal;
                _repository.Update(taskOptions, x => new { x.Status }, true);
            }
            return result;
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public async Task<object> Pause(Sys_QuartzOptions taskOptions)
        {
            //  var result = await _schedulerFactory.Remove(taskOptions);
            var result = await _schedulerFactory.Pause(taskOptions);
            taskOptions.Status = (int)TriggerState.Paused;
            _repository.Update(taskOptions, x => new { x.Status }, true);
            return result;
        }
    }
}
