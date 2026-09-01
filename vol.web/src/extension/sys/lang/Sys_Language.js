// 语言设置扩展：生成语言包按钮(调用后端api/Sys_Language/createLanguagePack
// 生成wwwroot/lang/{lang}.js，前端切换语言后启动时加载)
let extension = {
  components: {
      // 动态扩充组件或组件路径
      // 表单header、content、footer对应位置扩充的组件
      gridHeader: '', // { template: "<div>扩展组xx件</div>" },
      gridBody: '',
      gridFooter: '',
      // 弹出框(修改、编辑、查看)header、content、footer对应位置扩充的组件
      modelHeader: '',
      modelBody: '',
      modelFooter: ''
  },
  text: '',
  buttons: {
      view: [
          {
              name: '生成语言包',
              icon: 'el-icon-plus',
              index: 1,
              type: 'primary',
              plain: true,
              onClick: function () {
                  this.createLanguagePack()
              }
          }
      ],
      box: [],
      detail: []
  }, // 扩展的按钮
  methods: {
      destroyed () {
      },
      // 事件扩展
      onInit () {
          this.labelWidth = 140
          this.maxBtnLength = 10
          this.boxOptions.height = 300
          this.textInline = false
          this.continueAdd = true;
      },
      onInited () {

      },
      createLanguagePack () {
          this.http
              .get('/api/Sys_Language/createLanguagePack', {}, true)
              .then(x => {
                  this.$Message[x.status ? 'info' : 'error'](this.$ts(x.message))
              })
      },
      searchAfter (result) {
          return true
      },
      modelOpenAfter (row) {
          if (this.currentAction == this.const.ADD) {
              this.editFormFields.IsPackageContent = "1";
          }
      }
  }
}
export default extension
