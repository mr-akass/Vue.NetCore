<!--
 *数据库管理(多数据库支持)：新增连接后立即注册到SqlSugar,不用重启
 *只支持查看/新增/编辑，不支持删除(连接名被实体、字典、代码生成器引用，删掉会导致这些功能报错)
 *业务请在@/extension/sys/system/Sys_DbConnection.jsx编写
 -->
<template>
    <view-grid ref="grid"
               :columns="columns"
               :detail="detail"
               :editFormFields="editFormFields"
               :editFormOptions="editFormOptions"
               :searchFormFields="searchFormFields"
               :searchFormOptions="searchFormOptions"
               :table="table"
               :extend="extend">
    </view-grid>
</template>
<script>
    import extend from "@/extension/sys/system/Sys_DbConnection.jsx";
    import { ref, defineComponent } from "vue";
    export default defineComponent({
        setup() {
            const table = ref({
                key: 'ID',
                footer: "Foots",
                cnName: '数据库管理',
                name: 'Sys_DbConnection',
                url: "/Sys_DbConnection/",
                sortName: "ConnName"
            });
            //数据库类型选项与后端SqlSugarDbType.GetType()支持的取值一一对应，写死在这里不用再建字典
            const dbTypeData = [
                { key: 'MsSql', value: 'SqlServer' },
                { key: 'MySql', value: 'MySql' },
                { key: 'PgSql', value: 'PostgreSQL' },
                { key: 'Oracle', value: 'Oracle' },
                { key: 'DM', value: '达梦' },
                { key: 'Kdbndp', value: '人大金仓' },
                { key: 'GaussDB', value: 'GaussDB' },
                { key: 'OceanBase', value: 'OceanBase' }
            ];
            const editFormFields = ref({"ConnName":"","DBType":"","ConnectionString":"","Remark":"","Enabled":""});
            const editFormOptions = ref([[{"title":"连接名称","required":true,"field":"ConnName","readonlyUpdate":true,"placeholder":"字母开头,只能字母数字下划线(保存后不可修改)"},
                               {"data":dbTypeData,"title":"数据库类型","required":true,"field":"DBType","type":"select"},
                               {"dataKey":"enable","data":[],"title":"是否启用","field":"Enabled","type":"switch"}],
                              [{"title":"连接字符串","required":true,"field":"ConnectionString","colSize":12,"type":"textarea","placeholder":"SqlServer示例：Data Source=服务器;Initial Catalog=库名;User ID=账号;Password=密码;TrustServerCertificate=True;  (自签证书必须加TrustServerCertificate=True,否则报SSL证书错误)"}],
                              [{"title":"备注","field":"Remark","colSize":12,"type":"text"}]]);
            const searchFormFields = ref({"ConnName":"","DBType":"","Enabled":""});
            const searchFormOptions = ref([[{"title":"连接名称","field":"ConnName","type":"like"},{"data":dbTypeData,"title":"数据库类型","field":"DBType","type":"select"},{"dataKey":"enable","data":[],"title":"是否启用","field":"Enabled","type":"select"}]]);
            const columns = ref([{field:'ID',title:'ID',type:'int',width:70,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'ConnName',title:'连接名称',type:'string',link:true,width:130,require:true,align:'left'},
                       {field:'DBType',title:'数据库类型',type:'string',bind:{ key:'dbConnType',data:dbTypeData},width:100,require:true,align:'left'},
                       {field:'ConnectionString',title:'连接字符串',type:'string',width:340,require:true,align:'left'},
                       {field:'Remark',title:'备注',type:'string',width:150,align:'left'},
                       {field:'Enabled',title:'是否启用',type:'bool',bind:{ key:'enable',data:[]},width:80,align:'left'},
                       {field:'Creator',title:'创建人',type:'string',width:100,readonly:true,align:'left'},
                       {field:'CreateDate',title:'创建时间',type:'datetime',width:150,readonly:true,align:'left'},
                       {field:'Modifier',title:'修改人',type:'string',width:100,hidden:true,readonly:true,align:'left'},
                       {field:'ModifyDate',title:'修改时间',type:'datetime',width:150,hidden:true,readonly:true,align:'left'}]);
            const detail = ref({
                cnName: "#detailCnName",
                table: "#detailTable",
                columns: [],
                sortName: "",
                key: ""
            });
            return {
                table,
                extend,
                editFormFields,
                editFormOptions,
                searchFormFields,
                searchFormOptions,
                columns,
                detail,
            };
        },
    });
</script>
