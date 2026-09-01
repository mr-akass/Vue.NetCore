/*****************************************************************************************
 **  Author:jxx 2022
 **  QQ:283591387
 **完整文档见：http://v2.volcore.xyz/document/api 【代码生成页面ViewGrid】
 **常用示例见：http://v2.volcore.xyz/document/vueDev
 **后台操作见：http://v2.volcore.xyz/document/netCoreDev
 *****************************************************************************************/
//此js文件是用来自定义扩展业务代码，可以扩展一些自定义页面或者重新配置生成的代码
import gridBody from './Sys_QuartzOptionsGridBody';
import cronBuilder from './CronBuilderDialog.vue';
let extension = {
  components: {
    //查询界面扩展组件
    gridHeader: '',
    gridBody: gridBody,
    gridFooter: '',
    //新建、编辑弹出框扩展组件
    modelHeader: '',
    modelBody: cronBuilder, //设置频率(生成cron表达式)弹窗
    modelFooter: ''
  },
  tableAction: '', //指定某张表的权限(这里填写表名,默认不用填写)
  buttons: { view: [], box: [], detail: [] }, //扩展的按钮
  methods: {
    //下面这些方法可以保留也可以删除
    onInit() {
      this.textInline = false;
      this.columns.push({
        field: '操作',
        title: '操作',
        width: 180,
        fixed: 'right',
        align: 'center',
        render: (h, { row, column, index }) => {
          return (
            <div>
              <el-buton
                style="color:#2882df;font-size:12px;"
                onClick={() => {
                  this.edit(row);
                }}
                type="text"
              >
                编辑
              </el-buton>
              <el-buton
                style="color:#2882df;font-size:12px;margin-left:10px;"
                onClick={() => {
                  this.request('Run', row, 'once');
                }}
                type="text"
              >
                执行一次
              </el-buton>
              <el-buton
                style="color:#2882df;font-size:12px;margin-left:10px;"
                onClick={() => {
                  if (row.Status == 1) {
                    //开启任务
                    this.request('start', row);
                  } else {
                    this.request('pause', row);
                  }
                }}
                type="text"
              >
                {row.Status == 1 ? '恢复' : '暂停'}
              </el-buton>
              <el-buton
                style="color:#2882df;font-size:12px;margin-left:10px;"
                onClick={() => {
                  this.$store.getters.data().quartzId = row.Id;
                  this.$refs.gridBody.open();
                }}
                type="text"
              >
                日志
              </el-buton>
            </div>
          );
        }
      });
      //示例：设置修改新建、编辑弹出框字段标签的长度
      // this.boxOptions.labelWidth = 150;
    },
    request(action, row,val) {
      let url = `api/Sys_QuartzOptions/${action}?val=${val}`;
      this.http.post(url, row, true).then((result) => {
        this.$message.success('执行成功');
        this.search();
      });
    },
    onInited() {
      this.height = this.height - 50;
      this.columns.forEach((col) => {
        if (col.field == 'Status') {
          col.align = 'center';
          col.formatter = (row) => {
            //  return row.Status;
            if (row.Status == 1) {
              return '<a style="color:red;"><span style="display: inline-block;padding: 4px;background: red; border-radius: 50%; margin-right: 5px;"></span>暂停</a>';
            }
            return '<a style="color:#04b348;"><span style="display: inline-block;padding: 4px;background: #20c423; border-radius: 50%; margin-right: 5px;"></span>正常</a>';
          };
        }
      });
      //框架初始化配置后
      //如果要配置明细表,在此方法操作
      //this.detailOptions.columns.forEach(column=>{ });
      this.editFormOptions.forEach((options) => {
        options.forEach((option) => {
          if (option.field == 'CronExpression') {
            //点击[设置频率]弹窗按选择的时间段生成cron表达式
            option.placeholder = '点击右侧[设置频率]生成，也可手动输入';
            option.extra = {
              style: 'color: #2196F3;cursor: pointer;font-size:13px;',
              text: '设置频率',
              click: () => {
                const currentCron = this.editFormFields.CronExpression;
                this.$refs.modelBody.open(currentCron, (cronExpr, cronDescr) => {
                  //回填表单cron字段，extra文字换成中文描述
                  this.editFormFields.CronExpression = cronExpr;
                  option.extra = {
                    text: cronDescr || '设置频率',
                    style: 'color: #2196F3;cursor: pointer;font-size:13px;',
                    click: option.extra.click
                  };
                });
              }
            };
          }else if(option.field=='PostData'){
              option.placeholder=`post参数格式如：{name:"1",value:"2"}`;
          }
        });
      });
    },
    searchBefore(param) {
      //界面查询前,可以给param.wheres添加查询参数
      //返回false，则不会执行查询
      return true;
    },
    searchAfter(result) {
      //查询后，result返回的查询数据,可以在显示到表格前处理表格的值
      return true;
    },
    addBefore(formData) {
      //新建保存前formData为对象，包括明细表，可以给给表单设置值，自己输出看formData的值
      return true;
    },
    updateBefore(formData) {
      //编辑保存前formData为对象，包括明细表、删除行的Id
      return true;
    },
    rowClick({ row, column, event }) {
      //查询界面点击行事件
      // this.$refs.table.$refs.table.toggleRowSelection(row); //单击行时选中当前行;
    },
    modelOpenAfter(row) {
      if (this.currentAction == 'Add' || !this.editFormFields.TimeOut) {
        this.editFormFields.TimeOut = 180;
      }
      this.editFormOptions.forEach((options) => {
        options.forEach((option) => {
          if (option.field == 'GroupName') {
            option.readonly = this.currentAction != 'Add';
          }
        });
      });
      //编辑时[设置频率]链接显示当前任务的频率中文描述，新建时恢复默认文字
      const cronOpt = this.getFormOption('CronExpression');
      const clickFn = cronOpt && cronOpt.extra ? cronOpt.extra.click : null;
      if (clickFn) {
        const descr = this.currentAction != 'Add' && row ? row.CronDescr : '';
        cronOpt.extra = {
          text: descr || '设置频率',
          style: 'color: #2196F3;cursor: pointer;font-size:13px;',
          click: clickFn
        };
      }
    },
    getFormOption(field) {
      let option;
      this.editFormOptions.forEach((x) => {
        x.forEach((item) => {
          if (item.field == field) {
            option = item;
          }
        });
      });
      return option;
    }
  }
};
export default extension;
