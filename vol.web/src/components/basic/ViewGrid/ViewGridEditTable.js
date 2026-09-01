//表格行内编辑模式(2026.08.10)
//代码生成器[编辑模式]选择[表格行内编辑]后，生成页面table配置editTable:true，
//主表列带edit:{type}配置：新建=行内插行、编辑=点击行/选中行编辑、保存=比对快照后逐行调add/update接口
//扩展钩子(extension js中可重写)：tableAddRowBefore、getDefaultRow、tableEditSaveBefore、tableEditSaveAfter
import { getUrl } from "./ViewGridProvider.jsx";

const NEW_ROW_FLAG = "__editTableNewRow";

export const isEditTable = (props) => {
  return !!(props.table && props.table.editTable);
};

//需要提交/比对的字段：带edit配置的列(排除查看等非数据列)
const getTrackedFields = (props) => {
  return props.columns
    .filter((c) => {
      return c.field && c.edit && !c.view;
    })
    .map((c) => {
      return c.field;
    });
};

const normalizeValue = (val) => {
  if (val === null || val === undefined) return "";
  if (Array.isArray(val)) return val.join(",");
  return val + "";
};

//加载数据后建立快照，保存时比对出被修改过的行
export const snapshotEditTableRows = (props, dataConfig, rows) => {
  if (!isEditTable(props)) return;
  const key = props.table.key;
  const fields = getTrackedFields(props);
  const map = {};
  (rows || []).forEach((row) => {
    const keyValue = row[key];
    if (keyValue === null || keyValue === undefined || keyValue === "") return;
    const snap = {};
    fields.forEach((f) => {
      snap[f] = normalizeValue(row[f]);
    });
    map[keyValue] = snap;
  });
  dataConfig.editTableState = { snapshots: map };
};

//表格编辑开启前权限校验：无编辑权限时，只能编辑行内新建的行
export const canBeginEditRow = (props, dataConfig, row) => {
  if (!isEditTable(props)) return true;
  const perm = dataConfig.editTablePermission;
  if (!perm) return false;
  if (row && row[NEW_ROW_FLAG]) return perm.add;
  return perm.update;
};

//新建：表格顶部插入一行并进入编辑状态
export const editTableAddRow = (proxy, props, dataConfig) => {
  const tableRef = proxy.getTable(true);
  let row = {};
  props.columns.forEach((c) => {
    if (!c.field || c.view) return;
    if (c.edit && c.edit.type === "switch") {
      row[c.field] = c.type === "bool" ? false : 0;
    } else {
      row[c.field] = undefined;
    }
  });
  //行内编辑获取默认编辑行前方法
  if (proxy.tableAddRowBefore.call(proxy, row, 0) === false) {
    return;
  }
  //行内编辑获取默认编辑行(可返回默认值)
  row = proxy.getDefaultRow.call(proxy, row, 0) || row;
  row[NEW_ROW_FLAG] = true;
  row.elementIndex = 0;
  tableRef.rowData.unshift(row);
  //必须setTimeout：新建按钮的click会冒泡到document，VolTable的点击外部处理器
  //会把edit.rowIndex重置为-1，等冒泡结束后再开启编辑
  setTimeout(() => {
    tableRef.edit.rowIndex = 0;
    //只改edit.rowIndex不会让已渲染的单元格重新渲染，
    //用新对象替换该行强制整行重渲染出编辑框
    tableRef.rowData.splice(0, 1, Object.assign({}, tableRef.rowData[0]));
  }, 0);
};

//编辑按钮/快捷编辑字段点击：开启选中行的行内编辑
export const editTableBeginEdit = (proxy, props, rows) => {
  if (rows) {
    if (!Array.isArray(rows)) {
      rows = [rows];
    }
  } else {
    rows = proxy.getSelected();
  }
  if (!rows || rows.length === 0) {
    return proxy.$error(proxy.$ts("请选择要编辑的行!"));
  }
  if (rows.length !== 1) {
    return proxy.$error(proxy.$ts("只能选择一行数据进行编辑!"));
  }
  const tableRef = proxy.getTable(true);
  const index = tableRef.rowData.indexOf(rows[0]);
  if (index === -1) return;
  tableRef.setEdit(index);
};

//删除：行内新建未保存的行直接从表格移除，已保存的行走原删除接口
export const editTableDelete = (proxy, props, rows, serverDelete) => {
  let selected = rows;
  if (selected) {
    if (!Array.isArray(selected)) {
      selected = [selected];
    }
  } else {
    selected = proxy.getSelected();
  }
  if (!selected || selected.length === 0) {
    //保持原删除提示
    return serverDelete(rows);
  }
  const newRows = selected.filter((r) => {
    return r[NEW_ROW_FLAG];
  });
  if (newRows.length) {
    proxy.getTable(true).delRow(newRows);
  }
  const persisted = selected.filter((r) => {
    return !r[NEW_ROW_FLAG];
  });
  if (!persisted.length) return;
  serverDelete(persisted);
};

//必填校验(仅校验新建/修改的行；隐藏列不校验，可在tableAddRowBefore/getDefaultRow中设置默认值)
const validateRows = (proxy, props, rows) => {
  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    for (let j = 0; j < props.columns.length; j++) {
      const c = props.columns[j];
      if (!c.edit || c.hidden || !(c.require || c.required)) continue;
      const val = row[c.field];
      if (
        val === null ||
        val === undefined ||
        val === "" ||
        (Array.isArray(val) && !val.length)
      ) {
        proxy.$error(proxy.$ts(c.title) + " " + proxy.$ts("不能为空"));
        return false;
      }
    }
  }
  return true;
};

//构造提交数据：主键 + 带edit配置的列
const buildMainData = (props, row, isAdd) => {
  const data = {};
  getTrackedFields(props).forEach((field) => {
    let val = row[field];
    if (Array.isArray(val)) val = val.join(",");
    if (val !== undefined) data[field] = val;
  });
  const key = props.table.key;
  if (isAdd) {
    delete data[key];
  } else {
    data[key] = row[key];
  }
  return data;
};

//保存：新建的行调add接口，快照比对有变化的行调update接口
export const editTableSave = async (proxy, props, dataConfig) => {
  const tableRef = proxy.getTable(true);
  const key = props.table.key;
  const fields = getTrackedFields(props);
  //结束当前正在编辑的行(数据已实时绑定到行上，保存前统一做必填校验)
  if (tableRef.edit.rowIndex !== -1) {
    tableRef.edit.rowIndex = -1;
  }
  const allRows = tableRef.rowData || [];
  const snapshots = (dataConfig.editTableState || {}).snapshots || {};
  const perm = dataConfig.editTablePermission || {};
  const addRows = allRows.filter((r) => {
    return r[NEW_ROW_FLAG];
  });
  const updateRows = allRows.filter((r) => {
    if (r[NEW_ROW_FLAG]) return false;
    if (!perm.update) return false;
    const snap = snapshots[r[key]];
    if (!snap) return false;
    return fields.some((f) => {
      return normalizeValue(r[f]) !== snap[f];
    });
  });
  if (!addRows.length && !updateRows.length) {
    return proxy.$message.info(proxy.$ts("没有需要保存的数据"));
  }
  if (!validateRows(proxy, props, addRows.concat(updateRows))) {
    return;
  }
  //保存前钩子，返回false停止保存
  if (proxy.tableEditSaveBefore.call(proxy, addRows, updateRows) === false) {
    return;
  }
  const asyncApi = dataConfig.asyncApi.value;
  let success = 0,
    failed = 0,
    firstError = "";
  for (let i = 0; i < addRows.length; i++) {
    const res = await proxy.http.post(
      getUrl("add", null, props.table, null, props, asyncApi),
      { mainData: buildMainData(props, addRows[i], true), detailData: null, delKeys: null },
      true
    );
    if (res.status) {
      success++;
    } else {
      failed++;
      firstError = firstError || res.message;
    }
  }
  for (let i = 0; i < updateRows.length; i++) {
    const res = await proxy.http.post(
      getUrl("update", null, props.table, null, props, asyncApi),
      { mainData: buildMainData(props, updateRows[i], false), detailData: null, delKeys: null },
      true
    );
    if (res.status) {
      success++;
    } else {
      failed++;
      firstError = firstError || res.message;
    }
  }
  proxy.tableEditSaveAfter.call(proxy, { success, failed, message: firstError });
  if (failed) {
    proxy.$error(
      proxy.$ts("保存成功") + ":" + success + "，" + proxy.$ts("失败") + ":" + failed + (firstError ? "，" + firstError : "")
    );
  } else {
    proxy.$success(proxy.$ts("保存成功"));
  }
  if (success) {
    //刷新数据并重建快照(未保存成功的新行会被刷新移除)
    proxy.search();
  }
};

//初始化：按权限生成保存按钮/剥离编辑配置
export const initEditTable = (proxy, props, dataConfig) => {
  if (!isEditTable(props)) return;
  const buttons = dataConfig.buttons.value;
  const hasAdd = buttons.some((x) => {
    return x.value === "Add";
  });
  const hasUpdate = buttons.some((x) => {
    return x.value === "Update";
  });
  dataConfig.editTablePermission = { add: hasAdd, update: hasUpdate };
  //无新建也无编辑权限：列全部只读
  if (!hasAdd && !hasUpdate) {
    props.columns.forEach((c) => {
      if (c.edit) {
        delete c.edit;
      }
    });
    return;
  }
  proxy.saveEditTable = () => {
    editTableSave(proxy, props, dataConfig);
  };
  const saveButton = {
    name: "保存",
    icon: "el-icon-check",
    type: "primary",
    plain: true,
    value: "saveEditTable",
    onClick() {
      proxy.saveEditTable();
    },
  };
  let idx = buttons.findIndex((x) => {
    return x.value === "Update";
  });
  if (idx === -1) {
    idx = buttons.findIndex((x) => {
      return x.value === "Add";
    });
  }
  buttons.splice(idx + 1, 0, saveButton);
  dataConfig.maxBtnLength.value += 1;
};
