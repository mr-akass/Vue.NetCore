
import gridHeader from './Sys_UserGridHeader.vue'
let extension = {
  components: {
    //动态扩充组件或组件路径
    //表单header、content、footer对应位置扩充的组件
    gridHeader: gridHeader, //设置角色(多角色)弹窗
    gridBody: '',
    gridFooter: '',
    //弹出框(修改、编辑、查看)header、content、footer对应位置扩充的组件
    modelHeader: '',
    modelBody: '',
    modelFooter: ''
  },
  text: '',
  buttons: {
    //查询界面工具栏扩展按钮
    view: [
      {
        name: '设置角色',
        icon: 'el-icon-s-custom',
        type: 'primary',
        plain: true,
        onClick() {
          const rows = this.getSelectRows();
          if (!rows || rows.length !== 1) {
            return this.$message.error('请选择一行用户数据');
          }
          this.$refs.gridHeader.open(rows[0]);
        }
      }
    ],
    box: [],
    detail: []
  },
  methods: {
  }
}
export default extension
