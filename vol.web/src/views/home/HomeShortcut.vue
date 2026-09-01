<template>
  <div class="home-shortcut">
    <div class="hs-header">
      <div class="hs-title">
        <i class="el-icon-star-on"></i>
        <span>{{ $ts('快捷导航') }}</span>
        <span class="hs-count" v-if="shortcuts.length">{{ shortcuts.length }}</span>
      </div>
      <div class="hs-actions">
        <el-button link size="small" @click="openSelect">
          <i class="el-icon-plus"></i>
          <span class="hs-btn-txt">{{ $ts('添加') }}</span>
        </el-button>
        <el-button link size="small" v-if="shortcuts.length" @click="toggleEdit">
          <i :class="editing ? 'el-icon-check' : 'el-icon-delete'"></i>
          <span class="hs-btn-txt">{{ editing ? $ts('完成') : $ts('删除') }}</span>
        </el-button>
      </div>
    </div>

    <!-- 空状态 -->
    <div class="hs-empty" v-if="!loading && !shortcuts.length" @click="openSelect">
      <i class="el-icon-plus"></i>
      <span>{{ $ts('还没有快捷菜单，点击添加常用功能') }}</span>
    </div>

    <!-- 快捷项：直接拖动即排序并自动保存；编辑态只是额外显示删除角标 -->
    <draggable
      v-else
      class="hs-list"
      :list="shortcuts"
      v-bind="dragOptions"
      item-key="id"
      @start="onDragStart"
      @end="onDragEnd"
    >
      <div
        class="hs-item"
        :class="{ 'hs-item--editing': editing }"
        v-for="item in shortcuts"
        :key="item.id"
        @click="itemClick(item)"
      >
        <i class="hs-remove el-icon-close" v-if="editing" @click.stop="remove(item)"></i>
        <div class="hs-icon"><i :class="item.icon || 'el-icon-document'"></i></div>
        <div class="hs-name" :title="$ts(item.name)">{{ $ts(item.name) }}</div>
      </div>
    </draggable>

    <!-- 添加弹窗：可折叠展开的权限菜单树，勾选后批量添加 -->
    <el-dialog
      v-model="selectModel"
      width="460px"
      align-center
      draggable
      :close-on-click-modal="false"
      :title="$ts('添加快捷菜单')"
    >
      <el-input
        v-model="filterText"
        clearable
        :placeholder="$ts('搜索菜单') + '...'"
        :prefix-icon="Search"
      />
      <el-scrollbar max-height="360px" class="hs-tree-box">
        <el-tree
          ref="treeRef"
          :data="menuTree"
          show-checkbox
          node-key="id"
          :props="{ label: 'name', children: 'children' }"
          :filter-node-method="filterNode"
          :default-checked-keys="checkedKeys"
          :expand-on-click-node="true"
          :check-strictly="true"
        >
          <template #default="{ data }">
            <span class="hs-tree-node">
              <i :class="data.icon || 'el-icon-document'"></i>
              <span class="hs-tree-name">{{ $ts(data.name) }}</span>
              <span class="hs-tree-tag" v-if="data.children && data.children.length">
                {{ $ts('目录') }}
              </span>
            </span>
          </template>
        </el-tree>
      </el-scrollbar>
      <template #footer>
        <span class="hs-tip">{{ $ts('只能选择具体页面，目录不可添加') }}</span>
        <el-button @click="selectModel = false" icon="Close">{{ $ts('取消') }}</el-button>
        <el-button type="primary" icon="Check" plain :loading="saving" @click="saveSelect">
          {{ $ts('确定') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, getCurrentInstance, onMounted } from 'vue'
import { VueDraggableNext as draggable } from 'vue-draggable-next'
import { Search } from '@element-plus/icons-vue'
import store from '@/store/index'
import { getSavedAppId } from '@/config/appConfig'

const { proxy } = getCurrentInstance()

//当前应用ID(多应用隔离)：未选择应用时用0，与后端 AppId=0 对应
const appId = computed(() => getSavedAppId() || 0)

const loading = ref(false)
const saving = ref(false)
const editing = ref(false)
const shortcuts = ref([])
const selectModel = ref(false)
const filterText = ref('')
const treeRef = ref(null)
const checkedKeys = ref([])

const dragOptions = reactive({
  animation: 200,
  ghostClass: 'hs-ghost'
})

//拖动中标记：click在Sortable的end之后才触发，拖完要拦掉这一下点击，否则松手就跳页面了
let dragging = false

/** 当前用户有权限的菜单(登录时已存入store，无需重复请求接口) */
const getPermissionMenus = () => {
  return (store.state.permission || []).filter((x) => {
    //首页那条是IndexMethods里塞进去的辅助数据(id=0)，排除掉
    return x && x.id !== '0' && x.id !== 0 && (x.enable === undefined || x.enable == 1)
  })
}

/** 菜单是不是可打开的页面(有地址且不是纯目录) */
const isPageMenu = (menu, all) => {
  const url = menu.url || menu.path
  if (!url || url === '#') return false
  return !all.some((c) => c.parentId == menu.id)
}

/** 用权限菜单补全快捷项的名称/地址/图标：菜单改名或权限被收回会自动跟随 */
const fillShortcuts = (list) => {
  const menus = getPermissionMenus()
  return list
    .map((x) => {
      const menu = menus.find((m) => m.id == x.menuId)
      if (!menu) return null //已无该菜单权限，不显示
      return {
        id: x.id,
        menuId: x.menuId,
        name: menu.name,
        path: menu.path || menu.url,
        query: menu.query,
        icon: menu.icon,
        linkType: menu.linkType
      }
    })
    .filter((x) => x)
}

const loadShortcuts = () => {
  loading.value = true
  proxy.http
    .get(`api/Sys_UserShortcut/GetMyShortcuts?appId=${appId.value}`, {}, false)
    .then((result) => {
      shortcuts.value = fillShortcuts(result || [])
      loading.value = false
    })
    .catch(() => {
      loading.value = false
    })
}

/** 打开弹窗：把权限菜单拼成树 */
const menuTree = ref([])
const buildMenuTree = () => {
  const all = getPermissionMenus()
  const nodes = all.map((x) => {
    return {
      id: x.id,
      parentId: x.parentId,
      name: x.name,
      icon: x.icon,
      url: x.url,
      path: x.path,
      query: x.query,
      isPage: isPageMenu(x, all),
      children: []
    }
  })
  const map = {}
  nodes.forEach((n) => (map[n.id] = n))
  const roots = []
  nodes.forEach((n) => {
    const parent = map[n.parentId]
    if (parent) {
      parent.children.push(n)
    } else {
      roots.push(n)
    }
  })
  //目录节点不允许勾选(只有具体页面能加入快捷导航)
  const setDisabled = (list) => {
    list.forEach((n) => {
      n.disabled = !n.isPage
      if (n.children.length) setDisabled(n.children)
      else delete n.children //没有子节点时去掉空数组，避免出现空的展开箭头
    })
  }
  setDisabled(roots)
  return roots
}

const openSelect = () => {
  menuTree.value = buildMenuTree()
  //已收藏的默认勾上
  checkedKeys.value = shortcuts.value.map((x) => x.menuId)
  filterText.value = ''
  selectModel.value = true
}

const filterNode = (value, data) => {
  if (!value) return true
  return (proxy.$ts(data.name) || '').indexOf(value) !== -1
}

watch(filterText, (val) => {
  treeRef.value?.filter(val)
})

const saveSelect = () => {
  //只取叶子页面节点(check-strictly=true，勾选父节点不会连带子节点)
  const checked = (treeRef.value?.getCheckedNodes() || []).filter((x) => x.isPage)
  const existIds = shortcuts.value.map((x) => x.menuId)
  const menuIds = checked.map((x) => x.id).filter((id) => !existIds.includes(id))
  //取消勾选的已收藏项 => 删除
  const checkedIds = checked.map((x) => x.id)
  const toRemove = shortcuts.value.filter((x) => !checkedIds.includes(x.menuId))

  if (!menuIds.length && !toRemove.length) {
    selectModel.value = false
    return
  }

  saving.value = true
  const tasks = []
  if (menuIds.length) {
    tasks.push(
      proxy.http.post('api/Sys_UserShortcut/AddShortcut', { menuIds, appId: appId.value }, false)
    )
  }
  toRemove.forEach((x) => {
    tasks.push(proxy.http.post(`api/Sys_UserShortcut/RemoveShortcut/${x.id}`, {}, false))
  })

  Promise.all(tasks)
    .then((results) => {
      saving.value = false
      const failed = results.find((r) => r && r.status === false)
      if (failed) {
        proxy.$message.error(proxy.$ts(failed.message || '保存失败'))
      } else {
        proxy.$message.success(proxy.$ts('保存成功'))
      }
      selectModel.value = false
      loadShortcuts()
    })
    .catch(() => {
      saving.value = false
      proxy.$message.error(proxy.$ts('保存失败'))
    })
}

const toggleEdit = () => {
  editing.value = !editing.value
  //vue-draggable-next只在挂载时读一次Sortable配置(内部没有监听attrs变化)，
  //好在拖动一直开启、不需要动态改配置，这里只切删除角标的显示
}

const itemClick = (item) => {
  if (dragging || editing.value) return //刚拖完/删除态不跳转，避免误触
  if (item.linkType == 1) {
    window.open(item.path, '_blank')
    return
  }
  //用text而不是name：$tabs.open最终会调router.push(item)，带name会被当成命名路由解析
  proxy.$tabs.open({
    id: item.menuId,
    text: proxy.$ts(item.name),
    path: item.path,
    query: item.query
  })
}

const remove = (item) => {
  proxy.http.post(`api/Sys_UserShortcut/RemoveShortcut/${item.id}`, {}, false).then((result) => {
    if (result.status) {
      shortcuts.value = shortcuts.value.filter((x) => x.id !== item.id)
      if (!shortcuts.value.length) editing.value = false
    } else {
      proxy.$message.error(proxy.$ts(result.message || '移除失败'))
    }
  })
}

const onDragStart = () => {
  dragging = true
}

const onDragEnd = (evt) => {
  //Sortable的end早于click触发，延迟复位标记，拦掉松手那一下点击
  setTimeout(() => (dragging = false), 50)
  //原地放回不用保存
  if (evt && evt.oldIndex === evt.newIndex) return
  const ids = shortcuts.value.map((x) => x.id)
  proxy.http.post('api/Sys_UserShortcut/SaveSort', { ids }, false).then((result) => {
    if (!result.status) {
      proxy.$message.error(proxy.$ts(result.message || '排序保存失败'))
      loadShortcuts()
    }
  })
}

onMounted(() => {
  //Home.vue可能比Index.vue的菜单请求先渲染完，等权限数据就绪再取
  if (store.state.permission && store.state.permission.length) {
    loadShortcuts()
  } else {
    const stop = watch(
      () => store.state.permission.length,
      (len) => {
        if (len) {
          loadShortcuts()
          stop()
        }
      }
    )
  }
})
</script>

<style lang="less" scoped>
.home-shortcut {
  background: #fff;
  border-radius: 5px;
  padding: 12px 15px 15px 15px;
  margin-bottom: 15px;
}

.hs-header {
  display: flex;
  align-items: center;
  margin-bottom: 10px;

  .hs-title {
    flex: 1;
    font-size: 14px;
    font-weight: bolder;
    color: #1d252f;
    display: flex;
    align-items: center;

    i {
      color: #f7ba2a;
      margin-right: 5px;
      font-size: 16px;
    }
  }

  .hs-count {
    margin-left: 6px;
    font-size: 12px;
    font-weight: normal;
    color: #909399;
  }

  .hs-actions {
    display: flex;
    align-items: center;

    .hs-btn-txt {
      margin-left: 3px;
    }
  }
}

.hs-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 66px;
  border: 1px dashed #dcdfe6;
  border-radius: 5px;
  color: #909399;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;

  i {
    margin-right: 6px;
  }

  &:hover {
    border-color: var(--el-color-primary);
    color: var(--el-color-primary);
  }
}

.hs-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
  gap: 10px;
}

.hs-item {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px 6px 10px 6px;
  border: 1px solid #eef1f6;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  background: #fff;
  //直接拖动即排序，不需要先进编辑态，鼠标按下时给出可拖动反馈
  &:active {
    cursor: grabbing;
  }

  &:hover {
    border-color: var(--el-color-primary);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
    transform: translateY(-2px);
  }

  .hs-icon {
    width: 36px;
    height: 36px;
    border-radius: 8px;
    background: #f4fcff;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 6px;

    i {
      font-size: 18px;
      color: #339aed;
    }
  }

  .hs-name {
    font-size: 12px;
    color: #3d3c3c;
    text-align: center;
    width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.hs-item--editing {
  &:hover {
    transform: none;
  }
}

.hs-remove {
  position: absolute;
  right: -6px;
  top: -6px;
  width: 18px;
  height: 18px;
  line-height: 18px;
  text-align: center;
  font-size: 12px;
  color: #fff;
  background: #f56c6c;
  border-radius: 50%;
  z-index: 2;
  cursor: pointer;

  &:hover {
    background: #f78989;
  }
}

.hs-ghost {
  opacity: 0.5;
  background: #f4fcff;
  border: 1px dashed var(--el-color-primary);
}

.hs-tree-box {
  margin-top: 10px;
}

.hs-tree-node {
  display: flex;
  align-items: center;

  i {
    margin-right: 5px;
    color: #909399;
  }

  .hs-tree-tag {
    margin-left: 6px;
    font-size: 11px;
    color: #c0c4cc;
  }
}

.hs-tip {
  float: left;
  line-height: 32px;
  font-size: 12px;
  color: #909399;
}
</style>
