<template>
  <div>
    <VolBox v-model="model" :lazy="true" title="设置角色(多角色)" :height="480" :width="420" :padding="10">
      <div class="user-role-box">
        <el-alert type="success" :closable="false" show-icon>
          <template #title>
            <div style="font-size: 12px">
              用户【{{ userName }}】可勾选多个角色，权限为所有角色的并集；
              主角色在用户编辑表单中修改，此处不可取消
            </div>
          </template>
        </el-alert>
        <div class="role-tree" v-loading="loading">
          <el-tree
            ref="roleTree"
            :data="treeData"
            node-key="id"
            show-checkbox
            check-strictly
            default-expand-all
            :props="{ label: 'name', children: 'children', disabled: 'disabled' }"
          >
            <template #default="{ data }">
              <span>
                {{ data.name }}
                <el-tag v-if="data.id === mainRoleId" size="small" type="warning" style="margin-left: 6px">主角色</el-tag>
              </span>
            </template>
          </el-tree>
        </div>
      </div>
      <template #footer>
        <div>
          <el-button type="primary" size="small" :loading="saving" @click="save">
            <i class="el-icon-check"></i>保存
          </el-button>
          <el-button type="default" size="small" @click="model = false">关闭</el-button>
        </div>
      </template>
    </VolBox>
  </div>
</template>
<script>
export default {
  data() {
    return {
      model: false,
      loading: false,
      saving: false,
      userId: 0,
      userName: "",
      mainRoleId: 0,
      treeData: [],
    };
  },
  methods: {
    open(row) {
      this.userId = row.User_Id;
      this.userName = row.UserTrueName || row.UserName;
      this.treeData = [];
      this.model = true;
      this.loadRoles();
    },
    loadRoles() {
      this.loading = true;
      this.http
        .post("api/User/getUserRoles?userId=" + this.userId, {}, false)
        .then((x) => {
          this.loading = false;
          if (!x.status) {
            this.$message.error(x.message);
            return;
          }
          this.mainRoleId = x.data.mainRoleId;
          const userRoleIds = x.data.userRoleIds || [];
          //主角色不可取消勾选(由用户编辑表单维护)
          const list = (x.data.roles || []).map((r) => {
            return { ...r, disabled: r.id === this.mainRoleId };
          });
          this.treeData = this.convertTree(list);
          this.$nextTick(() => {
            this.$refs.roleTree && this.$refs.roleTree.setCheckedKeys(userRoleIds);
          });
        })
        .catch(() => {
          this.loading = false;
        });
    },
    //平铺角色列表转树形结构(父级不在可选范围内时作为一级节点)
    convertTree(list) {
      const map = {};
      list.forEach((x) => {
        map[x.id] = { ...x, children: [] };
      });
      const tree = [];
      list.forEach((x) => {
        if (x.parentId && map[x.parentId] && x.parentId !== x.id) {
          map[x.parentId].children.push(map[x.id]);
        } else {
          tree.push(map[x.id]);
        }
      });
      return tree;
    },
    save() {
      const roleIds = this.$refs.roleTree.getCheckedKeys();
      this.saving = true;
      this.http
        .post("api/User/saveRole?userId=" + this.userId, roleIds, true)
        .then((x) => {
          this.saving = false;
          if (!x.status) {
            this.$message.error(x.message);
            return;
          }
          this.$message.success(x.message || "角色设置成功");
          this.model = false;
        })
        .catch(() => {
          this.saving = false;
        });
    },
  },
};
</script>
<style scoped>
.user-role-box {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.role-tree {
  flex: 1;
  margin-top: 10px;
  overflow: auto;
  border: 1px solid #eee;
  border-radius: 4px;
  padding: 8px;
}
</style>
