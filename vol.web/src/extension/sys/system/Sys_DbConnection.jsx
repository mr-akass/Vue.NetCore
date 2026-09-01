/*****************************************************************************************
 **  数据库管理(多数据库支持)扩展业务
 **  只有查看/新增/编辑，没有删除：连接名(ConfigId)被实体[Entity(DBServer)]、字典DBServer、
 **  代码生成器Sys_TableInfo.DBServer引用，删掉会让这些功能直接报错，需要停用请把[是否启用]关掉
 *****************************************************************************************/
let extension = {
  components: {
    //查询界面扩展组件
    gridHeader: '',
    gridBody: '',
    gridFooter: '',
    //新建、编辑弹出框扩展组件
    modelHeader: '',
    modelBody: '',
    modelFooter: ''
  },
  text: '新增连接保存后会立即注册到运行中的程序，不用重启；连接名称即代码生成器/字典中[所在数据库]的选项值，保存后不可修改，也不能删除(停用请关闭[是否启用])',
  buttons: {
    view: [
      {
        name: '已注册连接',
        icon: 'el-icon-view',
        type: 'primary',
        plain: true,
        onClick() {
          this.http.post('api/Sys_DbConnection/GetRegistered', {}, true).then((result) => {
            const rows = (result && result.data) || [];
            const html = rows
              .map((x) => {
                const state = x.registered ? '<span style="color:#04b348">已注册</span>' : '<span style="color:red">未注册</span>';
                const enabled = x.enabled ? '' : '<span style="color:#e6a23c">(已停用)</span>';
                return `<div style="line-height:22px">${x.connName} [${x.dbType || '默认'}] ${state} ${enabled} <span style="color:#909399">来源:${x.source}</span></div>`;
              })
              .join('');
            this.$alert(html || '无', '当前已注册到SqlSugar的连接', { dangerouslyUseHTMLString: true });
          });
        }
      }
    ],
    box: [
      {
        name: '测试连接',
        icon: 'el-icon-link',
        type: 'primary',
        plain: true,
        onClick() {
          const form = this.editFormFields;
          if (!form.ConnectionString) {
            return this.$message.error('请先填写连接字符串');
          }
          if (!form.DBType) {
            //类型为空时后端会回退成默认库类型,测出来的"成功"和实际要连的库可能不是一回事
            return this.$message.error('请先选择数据库类型');
          }
          this.http
            .post(
              'api/Sys_DbConnection/TestConnection',
              { connName: form.ConnName, dbType: form.DBType, connectionString: form.ConnectionString },
              '正在测试连接...'
            )
            .then((result) => {
              if (result.status) {
                this.$message.success(result.message || '连接成功');
              } else {
                //连接串/证书类错误信息很长,用message会被截断,改用可关闭的弹框方便复制
                this.$alert(result.message || '连接失败', '测试连接失败', { type: 'error' });
              }
            });
        }
      }
    ],
    detail: []
  },
  methods: {
    onInited() {
      //连接字符串太长，表格里只显示掩码后的内容，鼠标悬停看全文
      this.columns.forEach((col) => {
        if (col.field == 'ConnectionString') {
          col.showOverflowTooltip = true;
        }
      });
    },
    modelOpenAfter(row) {
      //连接名只读由字段配置readonlyUpdate实现(框架在打开弹窗时统一处理)，这里只补默认值
      if (this.currentAction == 'Add') {
        this.editFormFields.Enabled = true;
      }
      //编辑历史数据时类型可能是空的,不补上会因为"没选类型"被后端拦下
      this.editFormFields.DBType = this.editFormFields.DBType || 'MsSql';
    }
  }
};
export default extension;
