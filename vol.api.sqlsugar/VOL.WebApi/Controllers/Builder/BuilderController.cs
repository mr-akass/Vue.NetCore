using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Builder.IRepositories;
using VOL.Builder.IServices;
using VOL.Core.Filters;
using VOL.Entity.DomainModels;
using VOL.Sys.IServices;

namespace VOL.WebApi.Controllers.Builder
{
    [JWTAuthorize]
    [Route("api/Builder")]
    public class BuilderController : Controller
    {
        /// <summary>
        /// 代码生成器路径配置在Sys_ConfigSetting中的键(原来存前端localStorage,换机器/复制框架会串)
        /// </summary>
        private const string VuePathKey = "builder.vuePath";
        private const string AppPathKey = "builder.appPath";

        private ISys_TableInfoService Service;
        private ISys_TableInfoRepository _repository;
        private readonly ISys_ConfigSettingService _configService;
        public BuilderController(ISys_TableInfoService service, ISys_TableInfoRepository repository, ISys_ConfigSettingService configService)
        {
            Service = service;
            _repository = repository;
            _configService = configService;
        }

        /// <summary>
        /// 读取代码生成器的Vue/App生成路径(所有登录用户可读,页面初始化时回填表单)
        /// </summary>
        [HttpPost, Route("GetBuilderPaths")]
        public async Task<ActionResult> GetBuilderPaths()
        {
            var values = await _configService.GetValuesAsync(VuePathKey, AppPathKey);
            return Json(new
            {
                vuePath = values.TryGetValue(VuePathKey, out string vuePath) ? vuePath : "",
                appPath = values.TryGetValue(AppPathKey, out string appPath) ? appPath : ""
            });
        }

        /// <summary>
        /// 保存代码生成器的Vue/App生成路径(路径会决定文件写到哪,限超级管理员)
        /// </summary>
        [HttpPost, Route("SaveBuilderPaths")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<ActionResult> SaveBuilderPaths([FromBody] BuilderPathModel model)
        {
            var values = new Dictionary<string, string>
            {
                { VuePathKey, model?.VuePath ?? "" },
                { AppPathKey, model?.AppPath ?? "" }
            };
            return Json(await _configService.SetValuesAsync(values));
        }

        public class BuilderPathModel
        {
            /// <summary>
            /// Vue项目views目录绝对路径
            /// </summary>
            public string VuePath { get; set; }
            /// <summary>
            /// uniapp pages目录绝对路径
            /// </summary>
            public string AppPath { get; set; }
        }

        [HttpPost]
        [Route("GetTableTree")]
        public async Task<ActionResult> GetTableTree()
        {
            try
            {
                (string, string) builderInfo = await Service.GetTableTree();
                return Json(new { list = builderInfo.Item1, nameSpace = builderInfo.Item2 });
            }
            catch (Exception ex)
            {

                return Json(new { list = ex.Message + ex.StackTrace + ex.Source, nameSpace = ex.InnerException?.Message });
            }
        }

        [Route("CreateVuePage")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult CreateVuePage([FromBody] Sys_TableInfo sysTableInfo, string vuePath, int tableId, string table)
        {
            return Content(Service.CreateVuePage(sysTableInfo, vuePath, tableId, table));
        }

        [Route("loadOptions")]

        [HttpPost]
        public ActionResult CreateVuePage(int tableId, string table)
        {
            var res = Service.CreateVuePage(null, null, tableId, table);

            return Json(new { status = res.Contains("fun"), content = res });
        }
        [Route("CreateModel")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult CreateEntityModel([FromBody] Sys_TableInfo tableInfo)
        {
            return Content(Service.CreateEntityModel(tableInfo));
        }
        [Route("Save")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult SaveEidt([FromBody] Sys_TableInfo tableInfo)
        {
            return Json(Service.SaveEidt(tableInfo));
        }
        [Route("CreateServices")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult CreateServices(string tableName, string nameSpace, string foldername, bool? partial, bool? api)
        {
            return Content(Service.CreateServices(tableName, nameSpace, foldername, false, true));
        }
        [Route("LoadTableInfo")]
        [HttpPost]
        public ActionResult LoadTable([FromBody] Sys_TableInfo sysTableInfo, int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int table_Id, bool isTreeLoad, string dbServer, bool gengeneric)
        {
            return Json(Service.LoadTable(sysTableInfo, parentId, tableName, columnCNName, nameSpace, foldername, table_Id, isTreeLoad, dbServer));
        }
        [Route("delTree")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public async Task<ActionResult> DelTree(int table_Id)
        {
            return Json(await Service.DelTree(table_Id));
        }
        [Route("syncTable")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public async Task<ActionResult> SyncTable(string tableName)
        {
            return Json(await Service.SyncTable(tableName));
        }

        [Route("getDyTable")]
        [HttpPost]
        public async Task<ActionResult> GetDyTable()
        {
            await Task.CompletedTask;
            return Json(Array.Empty<object>());
        }
    }
}
