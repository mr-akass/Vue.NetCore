// *Email：283591387@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'ID',
        footer: "Foots",
        cnName: 'Sys_Area',
        name: 'Sys_Area',
        newTabEdit: false,
        editTable: true,
        url: "/Sys_Area/",
        sortName: '{"ID":"asc"}',
        fixedSearch:false,
        showFooterDetail:false,
        quickQueryFields:''
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {};
    const editFormOptions = [];
    const searchFormFields = {};
    const searchFormOptions = [];
    const columns = [{field:'ID',title:'ID',type:'int',width:110,hidden:true,readonly:true,require:true},
                       {field:'CnName',title:'中文名称',type:'string',link:true,sort:true,filterData:true,quickCopy:true,showOverflowTooltip:true,width:110,readonly:true,require:true},
                       {field:'Code',title:'编号',type:'string',sort:true,filterData:true,quickCopy:true,showOverflowTooltip:true,width:110,require:true},
                       {field:'CreateID',title:'CreateID',type:'int',width:80,hidden:true},
                       {field:'Creator',title:'Creator',type:'string',width:100,hidden:true},
                       {field:'CreateDate',title:'CreateDate',type:'datetime',width:110,hidden:true},
                       {field:'ModifyID',title:'ModifyID',type:'int',width:80,hidden:true},
                       {field:'Modifier',title:'Modifier',type:'string',width:100,hidden:true},
                       {field:'ModifyDate',title:'ModifyDate',type:'datetime',width:110,hidden:true}];
    const detail ={columns:[]};
    const details = [];

    return {
        table,
        key,
        tableName,
        tableCNName,
        newTabEdit,
        editFormFields,
        editFormOptions,
        searchFormFields,
        searchFormOptions,
        columns,
        detail,
        details
    };
}