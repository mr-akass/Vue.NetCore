<!--
 *Author：jxx
 *Contact：283591387@qq.com
 *应用/子系统管理页面(多应用支持)
 *业务请在@/extension/sys/system/Sys_Application.jsx编写
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
    import extend from "@/extension/sys/system/Sys_Application.jsx";
    import { ref, defineComponent } from "vue";
    export default defineComponent({
        setup() {
            const table = ref({
                key: 'AppID',
                footer: "Foots",
                cnName: '应用管理',
                name: 'Sys_Application',
                url: "/Sys_Application/",
                sortName: "SortOrder"
            });
            const editFormFields = ref({"AppName":"","AppCode":"","RootMenuIds":[],"Title":"","Icon":"","Theme":"","PrimaryColor":"","DataPanel":"","SortOrder":"","Enabled":""});
            const editFormOptions = ref([[{"title":"应用名称","required":true,"field":"AppName"},
                               {"title":"应用代码","required":true,"field":"AppCode"},
                               {"dataKey":"rootmenu","data":[],"title":"根菜单","field":"RootMenuIds","type":"selectList","placeholder":"可选多个一级菜单(菜单范围=子树并集，公共子树可绑多个应用)"}],
                              [{"title":"标题","field":"Title","placeholder":"进入应用后浏览器标题"},
                               {"title":"图标","field":"Icon","placeholder":"如el-icon-menu"},
                               {"title":"主色调","field":"PrimaryColor","placeholder":"如#409eff"}],
                              [{"title":"首页面板","field":"DataPanel","placeholder":"前端src/views/home/下的组件名，空=默认首页"},
                               {"title":"排序","field":"SortOrder","type":"number"},
                               {"dataKey":"enable","data":[],"title":"是否启用","field":"Enabled","type":"switch"}]]);
            const searchFormFields = ref({"AppName":"","AppCode":"","Enabled":""});
            const searchFormOptions = ref([[{"title":"应用名称","field":"AppName","type":"like"},{"title":"应用代码","field":"AppCode","type":"like"},{"dataKey":"enable","data":[],"title":"是否启用","field":"Enabled","type":"select"}]]);
            const columns = ref([{field:'AppID',title:'AppID',type:'int',width:70,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'AppName',title:'应用名称',type:'string',link:true,width:120,require:true,align:'left'},
                       {field:'AppCode',title:'应用代码',type:'string',width:100,require:true,align:'left'},
                       {field:'RootMenuIds',title:'根菜单',type:'string',bind:{ key:'rootmenu',data:[]},width:150,align:'left'},
                       {field:'Title',title:'标题',type:'string',width:150,align:'left'},
                       {field:'Icon',title:'图标',type:'string',width:110,align:'left'},
                       {field:'Theme',title:'默认主题',type:'string',width:90,hidden:true,align:'left'},
                       {field:'PrimaryColor',title:'主色调',type:'string',width:90,align:'left'},
                       {field:'DataPanel',title:'首页面板',type:'string',width:130,align:'left'},
                       {field:'SortOrder',title:'排序',type:'int',width:70,align:'left'},
                       {field:'Enabled',title:'是否启用',type:'bool',bind:{ key:'enable',data:[]},width:80,align:'left'},
                       {field:'Creator',title:'创建人',type:'string',width:100,hidden:true,readonly:true,align:'left'},
                       {field:'CreateDate',title:'创建时间',type:'datetime',width:150,hidden:true,readonly:true,align:'left'},
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
