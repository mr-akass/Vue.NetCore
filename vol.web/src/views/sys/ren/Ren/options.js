// *Email：283591387@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'ID',
        footer: "Foots",
        cnName: 'Ren',
        name: 'Ren',
        newTabEdit: false,
        editTable: true,
        url: "/Ren/",
        sortName: '{"ID":"asc"}',
        fixedSearch:true,
        showFooterDetail:false,
        quickQueryFields:'Name,Sex,Birth'
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {"Name":"","Sex":"","Birth":""};
    const editFormOptions = [[{"title":"Name","required":true,"field":"Name","type":"text"},
                               {"title":"Sex","required":true,"field":"Sex","type":"text"},
                               {"title":"Birth","field":"Birth","type":"date"}]];
    const searchFormFields = {"Name":"","Sex":"","Birth":""};
    const searchFormOptions = [[{"title":"Name","field":"Name","type":"like"},{"title":"Sex","field":"Sex","type":"like"},{"title":"Birth","field":"Birth","type":"datetime"}]];
    const columns = [{field:'ID',title:'ID',type:'int',width:110,hidden:true,readonly:true,require:true},
                       {field:'Name',title:'Name',type:'string',link:true,filterData:true,quickCopy:true,showOverflowTooltip:true,width:110,edit:{type:'text'},require:true},
                       {field:'Sex',title:'Sex',type:'string',filterData:true,showOverflowTooltip:true,width:110,edit:{type:'text'},require:true},
                       {field:'Birth',title:'Birth',type:'datetime',filterData:true,showOverflowTooltip:true,width:110,edit:{type:'date'}}];
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