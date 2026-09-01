<template>
  <div
    class="vol-table"
    ref="refTable"
    :class="[
      textInline ? 'text-inline' : '',
      fxRight ? 'fx-right' : '',
      smallCell ? 'small-table' : '',
    ]"
  >
    <template v-if="dragPosition">
      <div v-show="showDragMask" class="drag-mask"></div>
    </template>
    <!-- <div class="el-drag" ref="dragRef" v-if="dragPosition == 'top'">+</div> -->
    <!-- v-if="loading" -->
    <div class="mask" v-if="loading">
      <vol-loading></vol-loading>
    </div>
    <el-table
      :key="randomTableKey"
      :show-summary="summaryData.length > 0"
      :summary-method="getSummaryData"
      :row-key="rowKey"
      :lazy="lazy"
      :defaultExpandAll="defaultExpandAll"
      :expand-row-keys="rowKey ? expandRowKeys : undefined"
      stripe
      :load="loadTreeChildren"
      @select="userSelect"
      @select-all="userSelect"
      @selection-change="selectionChange"
      @row-dblclick="rowDbClick"
      @row-click="rowClick"
      @headerDragend="headerDragend"
      @header-click="headerClick"
      :highlight-current-row="highlightCurrentRow"
      ref="table"
      class="v-table"
      @sort-change="sortChange"
      tooltip-effect="dark"
      :height="realHeight - extraHeight || null"
      :max-height="realMaxHeight"
      :data="url ? rowData : tableData"
      :border="true"
      :row-class-name="initIndex"
      :cell-style="getCellStyle"
      :cell-class-name="getCellClass"
      style="width: 100%"
      :scrollbar-always-on="true"
      @expand-change="expandChange"
      :span-method="cellSpanMethod"
      @scroll="scrollTable"
    >
      <el-table-column
        v-if="ck"
        type="selection"
        :fixed="fixed"
        :align="'center'"
        :selectable="selectable"
        width="55"
      ></el-table-column>
      <!-- type="index" -->
      <el-table-column
        v-if="columnIndex"
        :fixed="fixed"
        width="55"
        :align="'center'"
        :label="$ts('序号')"
      >
        <template #default="scope">
          {{ scope.$index + 1 + (paginations.page - 1) * paginations.size }}
        </template>
      </el-table-column>
      <!-- 2020.10.10移除table第一行强制排序 -->
      <el-table-column
        v-for="(column, cindex) in tableColumns"
        :prop="column.field"
        :label="column.title"
        :min-width="column.width"
        :formatter="formatter"
        :fixed="column.fixed"
        :key="column.field||('col-' + cindex)"
        :align="column.align"
        :sortable="column.sort ? 'custom' : false"
        :show-overflow-tooltip="
         ( $global.table.showOverflowTooltip&&column.showOverflowTooltip!==false) || column.showOverflowTooltip
        "
        :class-name="getColumnClass(column)"
      >
        <template v-if="column.filterData" #header>
          <div style="display: flex; align-items: center; gap: 5px;">
            <span
              v-if="(column.require || column.required) && column.edit"
              class="column-required"
              >*</span
            ><span :style="column.titleStyle">{{ $ts(column.title) }}</span>

            <el-tooltip placement="top" v-if="column.tip">
              <template #content>
                <div v-html="column.tip.text"></div>
              </template>
              <i
                :style="{ color: column.tip.color || '#7d7979' }"
                @click="column.tip.click"
                :class="column.tip.icon || 'el-icon-warning-outline'"
              ></i>
            </el-tooltip>

            <el-popover
              :visible="filterPopoverVisible[column.field]"
              placement="bottom"
              :width="240"
              trigger="click"
              popper-class="column-filter-popper"
            >
              <template #reference>
                <el-icon
                  class="column-filter-icon"
                  :class="{ 'is-active': columnFilterValues[column.field]?.length > 0 }"
                  @click="toggleFilterPopover(column)"
                >
                  <Filter />
                </el-icon>
              </template>
              <div class="column-filter-popover">
                <!-- 输入框：普通列输入关键字模糊查询；字典列客户端过滤选项；日期列不显示(勾选日期筛选) -->
                <div
                  class="filter-input-wrapper"
                  v-if="column.type !== 'date' && column.type !== 'datetime'"
                >
                  <el-input
                    v-model="columnFilterInputs[column.field]"
                    :placeholder="column.bind ? $ts('搜索选项') : '搜索 ' + $ts(column.title)"
                    size="default"
                    clearable
                    @keyup.enter="applyColumnFilter(column)"
                  >
                    <template #prefix>
                      <el-icon><Search /></el-icon>
                    </template>
                  </el-input>
                </div>

                <!-- 多选框：字典列直接使用绑定数据源；其他列分批加载去重值，滚动到底部自动加载下一批 -->
                <div class="filter-checkbox-wrapper" @scroll="onFilterOptionsScroll($event, column)">
                  <el-checkbox-group v-model="columnFilterCheckboxValues[column.field]">
                    <div
                      v-for="option in getVisibleFilterOptions(column)"
                      :key="option.value"
                      class="filter-checkbox-item"
                      :title="option.label"
                    >
                      <el-checkbox :label="option.value">{{ option.label }}</el-checkbox>
                    </div>
                  </el-checkbox-group>
                  <div v-if="columnFilterLoading[column.field]" class="filter-loading">
                    <el-icon class="is-loading"><Loading /></el-icon>
                    <span>加载中...</span>
                  </div>
                  <div
                    v-else-if="!column.bind && !hasMoreFilterOptions[column.field] && (columnFilterOptions[column.field]?.length || 0) > 30"
                    class="filter-list-end"
                  >
                    已加载全部 (共 {{ columnFilterOptions[column.field]?.length || 0 }} 项)
                  </div>
                  <div
                    v-if="!columnFilterLoading[column.field] && getVisibleFilterOptions(column).length === 0"
                    class="filter-empty"
                  >
                    <el-icon><DocumentDelete /></el-icon>
                    <div>暂无数据</div>
                  </div>
                </div>

                <!-- 操作按钮 -->
                <div class="filter-actions">
                  <span
                    v-if="(columnFilterCheckboxValues[column.field] || []).length > 0"
                    class="filter-selected"
                  >
                    已选 {{ columnFilterCheckboxValues[column.field].length }}
                  </span>
                  <el-button size="small" @click="clearColumnFilter(column)">清空</el-button>
                  <el-button size="small" type="primary" @click="applyColumnFilter(column)">确定</el-button>
                </div>
              </div>
            </el-popover>
          </div>
        </template>
        <template #header>
          <table-render
            v-if="column.renderHeader"
            :column="column"
            :render="column.renderHeader"
          ></table-render>
          <template v-else>
            <span
              v-if="(column.require || column.required) && column.edit"
              class="column-required"
              >*</span
            ><span :style="column.titleStyle">{{ $ts(column.title) }}</span>

            <el-tooltip placement="top" v-if="column.tip">
              <template #content>
                <div v-html="column.tip.text"></div>
              </template>
              <i
                :style="{ color: column.tip.color || '#7d7979' }"
                @click="column.tip.click"
                :class="column.tip.icon || 'el-icon-warning-outline'"
              ></i>
            </el-tooltip>
          </template>
        </template>

        <template #default="scope">
          <!-- 2022.01.08增加多表头，现在只支持常用功能渲染，不支持编辑功能(涉及到组件重写) -->
          <el-table-column
          
            style="border: none"
            v-for="columnChildren in filterChildrenColumn(column.children)"
            :key="columnChildren.field || columnChildren.title"
            :min-width="columnChildren.width"
            :class-name="columnChildren.class"
            :prop="columnChildren.field"
            :align="columnChildren.align"
            :sortable="columnChildren.sort ? 'custom' : false"
            :show-overflow-tooltip="columnChildren.showOverflowTooltip"
            :label="$ts(columnChildren.title)"
          >
            <template #default="scopeChildren">
              <a
                v-if="columnChildren.link"
                href="javascript:void(0)"
                style="text-decoration: none"
                @click="link(scopeChildren.row, columnChildren, $event)"
                v-text="scopeChildren.row[columnChildren.field]"
              ></a>
              <div
                v-else-if="columnChildren.formatter"
                @click="
                  columnChildren.click &&
                    columnChildren.click(
                      scopeChildren.row,
                      columnChildren,
                      scopeChildren.$index
                    )
                "
                v-html="
                  columnChildren.formatter(
                    scopeChildren.row,
                    columnChildren,
                    scopeChildren.$index
                  )
                "
              ></div>
              <table-render
                v-else-if="
                  columnChildren.render &&
                  typeof columnChildren.render == 'function'
                "
                :row="scopeChildren.row"
                key="rd-01"
                :index="scopeChildren.$index"
                :column="columnChildren"
                :render="columnChildren.render"
                :editInfo="edit"
              ></table-render>
               <img
                v-else-if="columnChildren.type == 'img'"
                v-for="(file, imgIndex) in getFilePath(
                  scopeChildren.row[columnChildren.field],
                  columnChildren
                )"
                :key="imgIndex"
                @error="handleImageError"
                @click="viewImg(scopeChildren.row, columnChildren, file.path, $event, imgIndex)"
                class="table-img"
                :style="{
                  height: (columnChildren.imgHeight || 40) + 'px',
                  width: (columnChildren.imgWidth || 40) + 'px',
                }"
                :src="file.path + access_token"
              />
              <div v-else-if="columnChildren.bind">
                {{ formatter(scopeChildren.row, columnChildren, true) }}
              </div>
              <span v-else-if="columnChildren.type == 'date'">{{
                formatDate(scopeChildren.row, columnChildren)
              }}</span>
              <template v-else>
                {{ scopeChildren.row[columnChildren.field] }}
              </template>
            </template>
          </el-table-column>
          <!-- 启用双击编辑功能，带编辑功能的不会渲染下拉框文本背景颜色 -->
          <!-- @click="rowBeginEdit(scope.$index,cindex)" -->
          <template
            v-if="
              column.edit &&
              !column.readonly &&
              ['file', 'img', 'excel'].indexOf(column.edit.type) != -1
            "
          >
            <div style="display: flex; align-items: center" @click.stop>
              <i
                style="
                  padding: 3px;
                  margin-right: 10px;
                  color: #8f9293;
                  cursor: pointer;
                "
                @click="showUpload(scope.row, column)"
                class="el-icon-upload"
              ></i>
              <template v-if="column.edit.type == 'img'">
                <img
                  v-for="(file, imgIndex) in getFilePath(
                    scope.row[column.field],
                    column
                  )"
                  :key="imgIndex"
                  @error="handleImageError"
                  @click="
                    viewImg(scope.row, column, file.path, $event, imgIndex)
                  "
                  class="table-img"
                  :src="file.path + access_token"
                />
              </template>
              <a
                style="margin-right: 8px"
                v-else
                class="t-file"
                v-for="(file, fIndex) in getFilePath(
                  scope.row[column.field],
                  column
                )"
                :key="fIndex"
                @click="dowloadFile(file, column, fIndex, scope.row)"
                >{{ file.name }}</a
              >
            </div>
          </template>
          <!-- 2021.09增加编辑时对readonly属性判断 -->
          <div
            v-else-if="
              column.edit &&
              !column.readonly &&
              (column.edit.keep || edit.rowIndex == scope.$index) &&
              (!column.checkEdit ||
                column.checkEdit(scope.row, column, scope.$index))
            "
            class="edit-el"
          >
            <div @click.stop class="e-item">
              <div>
                <!-- 2020.07.24增加日期onChange事件 -->
                <el-date-picker
                  clearable
                  size="default"
                  style="width: 100%"
                  :ref="column.field + scope.$index"
                  v-if="
                    ['date', 'datetime', 'month'].indexOf(column.edit.type) !=
                    -1
                  "
                  v-model="scope.row[column.field]"
                  @click.prevent
                  @change="
                    (val) => {
                      dateChange(scope.row, column, val);
                    }
                  "
                  :type="column.edit.type"
                  :placeholder="$ts(column.placeholder || column.title)"
                  :disabledDate="(val) => getDateOptions(val, column)"
                  :value-format="getDateFormat(column)"
                  :disabled="initColumnDisabled(scope.row, column)"
                  @visible-change="dateVisibleChang"
                >
                </el-date-picker>
                <!-- time字段：数据库字段要用varhcar类型
             如果使用的是date/datetime类型,需要设置表单配置的字段属性edit.valueFormat='YYYY-MM-DD HH:mm' -->
                <el-time-picker
                  clearable
                  size="default"
                  style="width: 100%"
                  v-else-if="column.edit.type == 'time'"
                  v-model="scope.row[column.field]"
                  @change="
                    (val) => {
                      column.onChange &&
                        column.onChange(scope.row, column, val);
                    }
                  "
                  :placeholder="$ts(column.placeholder || column.title)"
                  :value-format="column.format || 'HH:mm:ss'"
                  :disabled="initColumnDisabled(scope.row, column)"
                >
                </el-time-picker>
                <template v-else-if="column.edit.type == 'color'">
                  {{ scope.row[column.field] }}
                  <el-color-picker
                    @show="isDateChange = true"
                    @hide="isDateChange = false"
                    show-alpha
                    :teleported="true"
                    :predefine="[
                      '#ff4500',
                      '#ff8c00',
                      '#ffd700',
                      '#90ee90',
                      '#00ced1',
                      '#1e90ff',
                      '#c71585',
                    ]"
                    v-model="scope.row[column.field]"
                    @change="
                      (val) => {
                        column.onChange &&
                          column.onChange(scope.row, column, val);
                      }
                    "
                  />
                </template>
                <el-switch
                  v-else-if="column.edit.type == 'switch'"
                  v-model="scope.row[column.field]"
                  active-color="#0f84ff"
                  inactive-color="rgb(194 194 194)"
                  :active-text="$ts(column.activeText)"
                  :inactive-text="$ts(column.inactiveText)"
                  @change="
                    (val) => {
                      switchChange(val, scope.row, column);
                    }
                  "
                  :active-value="
                    typeof scope.row[column.field] == 'boolean'
                      ? true
                      : typeof scope.row[column.field] == 'string'
                      ? '1'
                      : 1
                  "
                  :inactive-value="
                    typeof scope.row[column.field] == 'boolean'
                      ? false
                      : typeof scope.row[column.field] == 'string'
                      ? '0'
                      : 0
                  "
                  :disabled="initColumnDisabled(scope.row, column)"
                >
                </el-switch>

                <template
                  v-else-if="
                    ['select', 'selectList'].indexOf(column.edit.type) != -1
                  "
                >
                  <el-select-v2
                    :ref="column.field + scope.$index"
                    style="width: 100%"
                    size="default"
                    :props="{ label: 'value', value: 'key' }"
                    v-if="column.bind.data.length >= select2Count"
                    v-model="scope.row[column.field]"
                    :filterable="
                      column.filter === undefined ? true : column.filter
                    "
                    :multiple="column.edit.type == 'select' ? false : true"
                    :placeholder="$ts(column.placeholder || column.title)"
                    :allow-create="column.autocomplete"
                    :options="column.bind.data"
                    @change="
                      column.onChange && column.onChange(scope.row, column)
                    "
                    clearable
                    :disabled="initColumnDisabled(scope.row, column)"
                  >
                    <template #default="{ item }">
                      {{ item.label || item.value }}
                    </template>
                  </el-select-v2>

                  <el-select
                    :ref="column.field + scope.$index"
                    size="default"
                    style="width: 100%"
                    v-else
                    v-model="scope.row[column.field]"
                    :filterable="
                      column.filter === undefined ? true : column.filter
                    "
                    :reserve-keyword="false"
                    :multiple="column.edit.type == 'select' ? false : true"
                    :placeholder="$ts(column.placeholder || column.title)"
                    :allow-create="column.autocomplete"
                    @change="
                      (val) => {
                        selectChange(scope.row, column, val);
                      }
                    "
                    @clear="
                      (val) => {
                        selectChange(scope.row, column, val, true);
                      }
                    "
                    clearable
                    :disabled="initColumnDisabled(scope.row, column)"
                  >
                    <el-option
                      v-for="item in column.bind.data"
                      :key="item.key"
                      v-show="!item.hidden"
                      :disabled="item.disabled"
                      :label="$ts(item.value)"
                      :value="item.key"
                    >
                    </el-option>
                  </el-select>
                </template>
                <el-tree-select
                  :ref="column.field + scope.$index"
                  style="width: 100%"
                  v-else-if="
                    column.edit.type == 'treeSelect' ||
                    column.edit.type == 'cascader'
                  "
                  v-model="scope.row[column.field]"
                  :data="column.bind.data"
                  :multiple="
                    column.multiple === undefined ? true : column.multiple
                  "
                  :render-after-expand="false"
                  :show-checkbox="true"
                  :check-strictly="
                    column.checkCtrictly === undefined
                      ? true
                      : column.checkCtrictly
                  "
                  check-on-click-node
                  node-key="key"
                  @change="
                    column.onChange && column.onChange(scope.row, column)
                  "
                  :props="{ label: 'label' }"
                >
                  <template #default="{ data, node }">
                    {{ $ts(data.label) }}</template
                  >
                </el-tree-select>
                <el-input
                  :ref="column.field + scope.$index"
                  v-else-if="column.edit.type == 'textarea'"
                  type="textarea"
                  :placeholder="$ts(column.placeholder || column.title)"
                  v-model="scope.row[column.field]"
                  :disabled="initColumnDisabled(scope.row, column)"
                  :autosize="{
                    minRows: column.minRows || 2,
                    maxRows: column.maxRows || 10,
                  }"
                >
                </el-input>
                <el-input-number
                  :ref="column.field + scope.$index"
                  style="width: 100%"
                  v-else-if="
                    column.edit.type == 'number' ||
                    column.edit.type == 'decimal'
                  "
                  v-model="scope.row[column.field]"
                  :precision="
                    column.edit.type == 'number' ? 0 : column.precision
                  "
                  :min="column.min"
                  :disabled="column.readonly || column.disabled"
                  :max="column.max"
                  controls-position="right"
                  @focus="onFocus(scope.row, column, $event)"
                  @blur="onBlur(scope.row, column, $event)"
                  @keyup.delete="inputKeypress(scope.row, column, $event)"
                  @change="inputKeypress(scope.row, column, $event)"
                  @keypress="
                    ($event) => {
                      inputKeypress(scope.row, column, $event);
                    }
                  "
                />
                <input
                  :ref="column.field + scope.$index"
                  class="table-input"
                  v-else-if="!column.summary && !column.onKeyPress"
                  v-model.lazy="scope.row[column.field]"
                  :placeholder="$ts(column.placeholder || column.title)"
                  :disabled="initColumnDisabled(scope.row, column)"
                  @input="inputKeypress(scope.row, column, $event)"
                  @focus="onFocus(scope.row, column, $event)"
                  @blur="onBlur(scope.row, column, $event)"
                />
                <el-input
                  v-else
                  :ref="column.field + scope.$index"
                  @change="inputKeypress(scope.row, column, $event)"
                  @input="inputKeypress(scope.row, column, $event)"
                  @keyup.enter="inputKeypress(scope.row, column, $event)"
                  size="default"
                  v-model="scope.row[column.field]"
                  :placeholder="$ts(column.placeholder || column.title)"
                  :disabled="initColumnDisabled(scope.row, column)"
                  @blur="onBlur(scope.row, column, $event)"
                ></el-input>
              </div>
              <div
                class="extra"
                v-if="column.extra && edit.rowIndex == scope.$index"
              >
                <a
                  :style="column.extra.style"
                  style="text-decoration: none"
                  @click="extraClick(scope.row, column, getTableData())"
                >
                  <i v-if="column.extra.icon" :class="[column.extra.icon]" />
                  {{ column.extra.text }}
                </a>
              </div>
            </div>
          </div>
          <table-render
            v-else-if="column.render && typeof column.render == 'function'"
            :row="scope.row"
            key="rd-01"
            :index="scope.$index"
            :column="column"
            :render="column.render"
            :editInfo="edit"
          ></table-render>
          <!--没有编辑功能的直接渲染标签-->
          <!-- v-text="scope.row[column.field]" -->
          <template v-else>
            <a
              v-if="column.link"
              href="javascript:void(0)"
              style="text-decoration: none"
              @click="link(scope.row, column, $event)"
              v-text="formatter(scope.row, column, true)"
            ></a>
            <img
              v-else-if="column.type == 'img'"
              v-for="(file, imgIndex) in getFilePath(
                scope.row[column.field],
                column
              )"
              :key="imgIndex"
              @error="handleImageError"
              @click="viewImg(scope.row, column, file.path, $event, imgIndex)"
              class="table-img"
              :style="{
                height: (column.imgHeight || 40) + 'px',
                width: (column.imgWidth || 40) + 'px',
              }"
              :src="file.path + access_token"
            />
            <a
              style="margin-right: 8px"
              v-else-if="column.type == 'file' || column.type == 'excel'"
              class="t-file"
              v-for="(file, fIndex) in getFilePath(
                scope.row[column.field],
                column
              )"
              :key="fIndex"
              @click="dowloadFile(file, column, fIndex, scope.row)"
              >{{ file.name }}</a
            >
            <template v-else-if="column.type == 'date'">{{
              formatDate(scope.row, column)
            }}</template>
            <template v-else-if="column.type == 'month'">{{
              (scope.row[column.field] || "").substr(0, 7)
            }}</template>
            <div
              v-else-if="column.formatter"
              @click.stop="formatterClick(scope.row, column, $event)"
              v-html="column.formatter(scope.row, column)"
            ></div>
            <!-- 2021.11.18修复table数据源设置为normal后点击行$event缺失的问题 -->
            <div
              v-else-if="column.bind && (column.normal || column.edit)"
              @click.stop="formatterClick(scope.row, column, $event)"
            >
              <span
                :style="column.getStyle && column.getStyle(scope.row, column)"
              >
                {{ formatter(scope.row, column, true) }}</span
              >
            </div>
            <div
              v-else-if="column.click && !column.bind"
              @click="formatterClick(scope.row, column)"
            >
              {{ scope.row[column.field] }}
            </div>
            <div
              @click="
                () => {
                  column.click && formatterClick(scope.row, column);
                }
              "
              v-else-if="column.bind"
            >
              <vol-tag 
                v-if="useTag && column.type != 'cascader'"
                class="cell-tag"
                :class="[isEmptyTag(scope.row, column)]"
                :type="getColor(scope.row, column)"
                :effect="column.effect"
                >{{ formatter(scope.row, column, true) }}</vol-tag
              >
              <template v-else>{{
                formatter(scope.row, column, true)
              }}</template>
            </div>

            <template v-else>{{ formatter(scope.row, column, true) }}</template>
            <!-- 快捷复制：代码生成器勾选后值的后面显示复制图标(编辑态与自定义render的列不显示) -->
            <i
              v-if="column.quickCopy && hasQuickCopyText(scope.row, column)"
              class="el-icon-document-copy quick-copy-icon"
              :title="$ts('复制')"
              @click.stop="quickCopyCell(scope.row, column)"
            ></i>
          </template>
        </template>
      </el-table-column>
    </el-table>
    <template v-if="!paginationHide">
      <div
        class="block pagination"
        :class="[tableV2 ? 'v2-pagination' : '']"
        key="pagination-01"
      >
        <div>
          <slot name="pagination"></slot>
        </div>
        <div class="pagination-content">
          <el-pagination
            key="pagination-02"
            @size-change="handleSizeChange"
            @current-change="handleCurrentChange"
            :current-page="paginations.page"
            :page-sizes="paginations.sizes"
            :page-size="paginations.size"
            :pager-count="pagerCount"
            layout="total, sizes, prev, pager, next, jumper"
            :total="paginations.total"
          ></el-pagination>
        </div>
      </div>
    </template>
    <div class="resize-handle" ref="dragRef" v-if="dragPosition == 'bottom'">
      <div></div>
      <div></div>
    </div>
    <vol-table-upload ref="uploadRef"></vol-table-upload>
    <vol-image-viewer ref="viewer"></vol-image-viewer>
  </div>
</template>
<script lang="jsx" setup>
import {
  ref,
  getCurrentInstance,
  defineAsyncComponent,
  computed,
  reactive,
  nextTick,
  onMounted,
  onUnmounted,
  watch,
  watchEffect,
} from "vue";
import { Filter, Search, Loading, DocumentDelete } from '@element-plus/icons-vue';
import VolTableProps from "./VolTable/VolTableProps.js";
import TableRender from "./VolTable/VolTableRender";
import { initDataSource } from "./VolTable/VolTableDicData.js";
import {
  initCellStyleSummary,
  initSummaryData,
  initColumnSummaryData,
  loadDataSummaries,
} from "./VolTable/VolTableSummary.js";
import {
  selectChange,
  switchChange,
  inputChange,
  extraClick,
  selectionRowChange,
  expandTreeChange,
  onBlur,
  onFocus,
} from "./VolTable/VolTableEvent.js";
import { getPath, previewImg } from "./VolTable/VolTableFile.js";
import { resetPage, loadData } from "./VolTable/VolTableLoadData.js";
import { getDateOptions, getDateFormat } from "./VolTable/VolTableDate.js";
import {
  isEmptyTag,
  filterChildrenColumn,
  initColumnDisabled,
  initDrag,
  initSortable,
  toNextTableCell,
  getColumnFilters,
  addTableRow,
  delTableRow,
  resetTable,
  initPaginations,
  hasQuickCopyText as hasQuickCopyTextValue,
  quickCopyCell as copyCellToClipboard,
} from "./VolTable/VolTableProvider.js";
import {
  getCellColor,
  formatDate,
  cellFormatter,
} from "./VolTable/VolTableFormat.js";
import {
  tableRowClick,
  rowEndEdit,
  tableValidate,
} from "./VolTable/VolTableEdit.js";
import { regTableEventNext,getNextTableCell } from "./VolTable/VolTableEventNext.js";
const VolTableUpload = defineAsyncComponent(() =>
  import("./VolTable/VolTableUpload.vue")
);
import VolLoading from "@/components/basic/VolLoading";
const VolImageViewer = defineAsyncComponent(() =>
  import("@/components/basic/VolImageViewer.vue")
);
import VolTag from "@/components/basic/VolTag/VolTag.vue";
const emit = defineEmits([
  "dicInited",
  "loadBefore",
  "loadAfter",
  "rowChange",
  "selectionChange",
  "onSortEnd",
  "rowDbClick",
  "rowClick",
  "paginationChange",
  "headerDragend",
]);
const props = defineProps(VolTableProps());
const { proxy, vnode } = getCurrentInstance();

proxy.errMsg = "";
proxy.errorFiled = "";
const table = ref(null);
const randomTableKey = ref(1);
const realHeight = ref(0);
const realMaxHeight = ref(0);
const isPageLoad = ref(false);
const defaultImg = new URL("@/assets/imgs/error-img.png", import.meta.url).href;
const loading = ref(false);
const formatConfig = reactive({});
//外部调用rowData只能使用splice+push
// const rowData = reactive(props.tableData);
const rowData = ref(props.tableData);

//分页选择的数据
const reserveSelectionRows = []; // reactive([]);
const paginations = reactive(props.pagination);
initPaginations(paginations);

const edit = reactive({ columnIndex: -1, rowIndex: -1 }); // 当前双击编辑的行与列坐标
// const summary = ref(false); // 是否显示合计
// 目前只支持从后台返回的summaryData数据
const summaryData = reactive([]);
const summaryDataV2=ref({});
const summaryIndex = reactive({});
const cellStyleColumns = reactive({});
const remoteColumns = reactive([]); // 需要每次刷新或分页后从后台加载字典数据源的列配置
//-table带数据源的单元格是否启用tag标签(下拉框等单元格以tag标签显示)
//2023.04.02更新voltable与main.js
const useTag = ref(true);

const smallCell = ref(true);
const showDragMask = ref(false);

//文件权限token
const access_token = ref(proxy.base.getAccessToken());

if (proxy.$global && proxy.$global.table) {
  useTag.value = proxy.$global.table && proxy.$global.table.useTag;
  smallCell.value = proxy.$global.table && proxy.$global.table.smallCell;
}

// 没有定义高度与最大高度，使用table默认值 // 定义了最大高度则不使用高度
realHeight.value =
  (!props.height && !props.maxHeight) || props.maxHeight
    ? null
    : props.height || null;
// 筛选相关的响应式变量
const filterPopoverVisible = reactive({});
const columnFilterInputs = reactive({});
const columnFilterValues = reactive({});
const columnFilterCheckboxValues = reactive({});
const columnFilterOptions = reactive({});
const columnFilterLoading = reactive({});
const hasMoreFilterOptions = reactive({});
const columnFilterPageIndexes = reactive({}); // 记录每列已加载的页数

// 没有定义高度与最大高度，使用table默认值
if (props.dragPosition) {
  realMaxHeight.value = 500;
} else {
  realMaxHeight.value = props.maxHeight || props.height || null;
}

const setHeight = (value) => {
  realHeight.value = value;
  realMaxHeight.value=value;
};

const getTableData = () => {
  // return rowData.value;
  return props.url ? rowData.value : props.tableData;
};
const getTable = () => {
  return table.value;
};
const tableColumns = computed(() => {
  return proxy.columns.filter((x) => {
    return !x.hidden;
  });
});

//右侧固定
const fxRight = computed(() => {
  return proxy.columns.some((x) => {
    return x.fixed == "right" && !x.hidden;
  });
});
//左边固定
const fixed = computed(() => {
  return props.columns.some((x) => {
    return x.fixed && x.fixed != "right" && !x.hidden;
  });
});

const initIndex = ({ row, rowIndex }) => {
  //if (props.ck) {
  row.elementIndex = rowIndex;
  //}
  return;
};
// 背景颜色、合计
const initConfig = () => {
  initCellStyleSummary(
    props,
    proxy,
    cellStyleColumns,
    summaryData,
    summaryIndex,
    summaryDataV2
  );
};
const getSummaryData = () => {
  return summaryData;
};
//列的class：快捷复制的列加标记类，样式里把值的div改成行内(否则复制图标会掉到下一行)
const getColumnClass = (column) => {
  if (!column.quickCopy) {
    return column.class;
  }
  return (column.class ? column.class + " " : "") + "quick-copy-cell";
};
const getCellClass = ({ row, column, rowIndex, columnIndex }) => {  const b = props.columns.some((x) => {
    return (
      x.field === column.property &&
      x.edit &&
      (x.edit.keep || edit.rowIndex === rowIndex)
    );
  });
  if (b) return "current-edit-cell";
  if (props.columns[columnIndex]) {
    return props.columns[columnIndex].class;
  }
};
const getCellStyle = (option) => {  // 2020.12.13增加设置单元格颜色
  if (!option.column.property || !cellStyleColumns[option.column.property])
    return;
  return cellStyleColumns[option.column.property](
    option.row,
    option.rowIndex,
    option.columnIndex,
    getTableData()
  );
};
initConfig();

//reset=是否重置
const initDicKeys = (reset = true) => {
  initDataSource(proxy, props, reset, (dicData) => {
    emit("dicInited", dicData);
  });
};
//初始化字典
initDicKeys(false);

// 全局绑定编辑输入跳转到下一个字段
regTableEventNext(proxy, props, getTableData(), edit, nextTick);
//获取选中行
const getSelectionRows = () => {
  if (props.tableV2) {
    return getTableData().filter((x) => {
      return x.elChecked;
    });
  }
  if (props.reserveSelection && props.rowKey) {
    const rows = table.value.getSelectionRows();
    if (!reserveSelectionRows.length) {
      return rows;
    }
    const rows2 = reserveSelectionRows.filter((x) => {
      return !rows.some((c) => {
        return c[props.rowKey] == x[props.rowKey];
      });
    });
    //如果有删除行操作，这里可能不准会误删
    return [...rows, ...rows2];
  }

  return table.value ? table.value.getSelectionRows() : [];
};
//获取当前正在编辑的行
const getCurrentEditRow=()=>{
      if (edit.rowIndex==-1) {
        return null;
      }
      return getTableData()[edit.rowIndex]
}
//获取选中行
const getSelected = () => {
  return getSelectionRows();
};
const getSelectedIndex = (rows) => {
  // if (!props.index) {
  //   // 只有设置了属性index才有索引行
  //   return [];
  // }
  return (rows || getSelectionRows()).map((x) => {
    return x.elementIndex;
  });
};
//合计
const initSummary = () => {
  initSummaryData(props, getTableData(), summaryData, summaryIndex,summaryDataV2);
};
const getInputSummaries = (scope, val, event, column) => {
  if (!column) return;
  initColumnSummaryData(column);
};
//设置字段配置合计
const setColumnSummary = (column) => {
  initColumnSummaryData(column, getTableData(), summaryData, summaryIndex,summaryDataV2);
};
if (props.tableData.length) {
  initSummary();
}

const clearSelection = () => {
  reserveSelectionRows.splice(0);
  if (props.tableV2) {
    getTableData().forEach((x) => {
      x.elChecked = false;
    });
    return;
  } else {
    table.value.clearSelection();
  }
};

const toggleRowSelection = (row) => {
  if (props.tableV2) {
    row.elChecked = true;
    return;
  }
  table.value.toggleRowSelection(row);
};

const watchRowSelectChange = (newLen, oldLen) => {
  if (props.tableV2) {
    return;
  }
  if (!props.reserveSelection && newLen < oldLen && getSelectionRows().length) {
    //reserveSelectionRows.splice(0);
    clearSelection();
  }
  if (!props.reserveSelection && isPageLoad.value) {
    isPageLoad.value = false;
    return;
  }
  initSummary();
};
//刷新指定字段合计
const updateSummary = (fields, reset) => {
  if (!fields) {
    fields = props.columns
      .filter((c) => {
        return c.summary;
      })
      .map((c) => {
        return c.field;
      });
  } else if (!Array.isArray(fields)) {
    fields = [fields];
  }
  if (reset) {
    initConfig();
  }
  for (let index = 0; index < fields.length; index++) {
    const field = fields[index];
    //这里可能有多级表头
    const column = proxy.base.getColumn(props.columns, field);
    if (column) {
      setColumnSummary(column);
    }
  }
};

//输入事件
const inputKeypress = (row, column, $event) => {
  inputChange(row, column, $event);
  setColumnSummary(column);
};
const link = (row, column, $e) => {
  $e && $e.stopPropagation && $e.stopPropagation();
  props.linkView(row, column);
};

const headerClick = (column, event) => {
  if (edit.rowIndex != -1) {
    const b = rowEndEdit(proxy, props, getTableData(), column, edit);
    if (b) {
      edit.rowIndex = -1;
    }
  }
};

const headerDragend = (newWidth, oldWidth, column, event) => {
  emit("headerDragend", { newWidth, oldWidth, column, event });
};

//行双击事件
const rowDbClick = (row, column, event) => {
  //2021.05.23增加双击行事件
  emit("rowDbClick", { row, column, event, index: row.elementIndex });
};

const rowClickV2 = (param) => {
  // console.log(param)
  rowClick(param.rowData, null, param.event);
  // console.log(edit);
};

//行点击事件
const rowClick = (row, column, event) => {
  if (!column) {
    column = props.columns.find((x) => {
      return x.edit && !x.hidden && !x.readonly;
    })||{};
  } else if (!column.field && column.property) {
      column = props.columns.find((x) => {
          return x.field == column.property;
      });
  }

  tableRowClick(
    proxy,
    props,
    getTableData(),
    edit,
    nextTick,
    emit,
    row,
    column,
    event
  );
  // console.log(edit);
};

//图片预览
const viewer = ref(null);
const viewImg = (row, column, path, $event, index) => {
  $event && $event.stopPropagation();
  previewImg(proxy, row, column, index, viewer.value);
};

const getFilePath = (url, column) => {
  return getPath(url, column, proxy);
};

const dowloadFile = (file, column, index, row) => {
  //file,column,fIndex
  if (
    column &&
    column.fileClick &&
    column.fileClick(index, file, [file], row) === false
  ) {
    return;
  }

  if (
    file.path.toLowerCase().endsWith(".jpg") ||
    file.path.toLowerCase().endsWith(".jpeg") ||
    file.path.toLowerCase().endsWith(".png")
  ) {
    previewImg(proxy, { img: file.path }, { field: "img" }, 0, viewer.value);
    return;
  }
  proxy.base.dowloadFile(
    file.path + access_token.value,
    file.name,
    {
      Authorization: proxy.$store.getters.getToken(),
    },
    proxy.$global.oss?.url || proxy.http.ipAddress
  );
};
const reset = () => {
  resetTable(proxy, props, getTableData(), paginations, edit);
};

//加数据
const load = async (query, isResetPage) => {
  if (!props.url) {
    return;
  }
  let data = await loadData(
    props,
    proxy,
    vnode,
    getTableData(),
    emit,
    loading,
    paginations,
    query,
    isResetPage,
    isPageLoad,
    randomTableKey
  );
  if (!data) {
    return;
  }
  // data.summary = null;
  //没有返回合计，但有合计字段的，前端默认对当前页面的数据进行合计处理

  if (!data.summary && summaryData?.length) {
    //initConfig()
    updateSummary();
    //console.log(summaryData)
  } else {
    //重置合计
    loadDataSummaries(proxy, props, data, summaryData,summaryDataV2);
  }

  //设置分页后记录默认选中行2024.09.10
  if (!(props.reserveSelection && props.rowKey)) return;
  // isPageLoad.value = false;
  nextTick(() => {
    isPageLoad.value = true;
    const selectRows = reserveSelectionRows;
    getTableData().forEach((row) => {
      const b = selectRows.some((c) => {
        return c[proxy.rowKey] === row[proxy.rowKey];
      });
      if (b) toggleRowSelection(row);
    });
    isPageLoad.value = false;
  });
};
if (props.defaultLoadPage) {
  load();
}

const handleSizeChange = (val) => {
  paginations.size = val;
  paginations.rows = val;
  load();
  emit("paginationChange", paginations);
};
const handleCurrentChange = (val) => {
  paginations.page = val;
  load();
  emit("paginationChange", paginations);
};
const sortChange = (sort) => {
  if (props.url) {
    paginations.sort = sort.prop;
    paginations.order = sort.order == "ascending" ? "asc" : "desc";
    load();
    return;
  }
  const rows = getTableData();
  rows.sort(function (a, b) {
    if (sort.order == "ascending") {
      return a[sort.prop] - b[sort.prop];
    }
    return b[sort.prop] - a[sort.prop];
  });
};
//复选框选中事件
const selectionChange = (selection) => {
  selectionRowChange(
    props,
    emit,
    getTableData(),
    selection,
    table.value,
    reserveSelectionRows,
    isPageLoad
  );
};
const userSelect = (selection, row) => {
  //   this.$emit("rowChange", { row, selection });
};
const isDateChange = ref(false);
const dateVisibleChang = (show) => {
  isDateChange.value = show;
};
const dateChange = (row, column, val) => {
  isDateChange.value = true;
  column.onChange && column.onChange(row, column, val);
};
//树形结构展开事件
const expandChange = (row, expandedRows) => {
  expandTreeChange(props, row, expandedRows);
};

//单元格颜色
const getColor = (row, column) => {
  return getCellColor(row, column, formatConfig);
};
//格式化
const formatter = (row, column, template) => {
  return cellFormatter(proxy, row, column, template);
};
//快捷复制(代码生成器勾选快捷复制的列)
const hasQuickCopyText = (row, column) => {
  return hasQuickCopyTextValue(proxy, row, column);
};
const quickCopyCell = (row, column) => {
  copyCellToClipboard(proxy, row, column);
};
const formatterClick = (row, column, event) => {  if (column.click) {
    column.click(row, column, event);
    event && event.stopPropagation && event.stopPropagation();
  } else {
    rowClick(row, column, event);
  }
};
//合并单元格
const cellSpanMethod = ({ row, column, rowIndex, columnIndex }) => {
  return props.spanMethod(
    { row, column, rowIndex, columnIndex },
    getTableData()
  );
};
//表头过滤
const getFilters = (column) => {
  return getColumnFilters(proxy, column, getTableData());
};

const filterHandler = (value, row, column) => {
  return row[column.property] === value;
};

const handleImageError = ($e) => {
  $e.target.src = defaultImg;
};
//按回车跳转到下一行
const toNextCell = (row,column) => {
  getNextTableCell(proxy, props, getTableData(), edit, nextTick, row, column);
  // toNextTableCell(proxy, props, getTableData(), row, edit, nextField, newRow);
};
//添加行
const addRow = (row) => {
  addTableRow(proxy, props, getTableData(), row);
  return row;
};

const delRow = (rows) => {
  if (rows) {
    if (!Array.isArray(rows)) {
      rows = [rows];
    }
  } else {
    rows = getSelected();
  }

  delTableRow(proxy, edit, getTableData(), rows, getSelectedIndex(rows));
  return rows;
};

//上传图片、文件
const uploadRef = ref(null);
const showUpload = (row, column) => {
  uploadRef.value.showUpload(row, column, props.url);
};

const refTable = ref();

const setEdit = (index) => {
  //结束编辑
  if (index == -1) {
    if (edit.rowIndex == -1) {
      return;
    }
    let row = getTableData[edit.rowIndex];
    rowEndEdit(proxy, props, getTableData(), row, edit);
    return;
  }
  //开启编辑
  rowClick(getTableData()[index], null, {});
};
const scrollInfo = ref({ scrollLeft: 0, scrollTop: 0 });
const scrollTable = (data) => {
  if (!props.sortable) {
    return;
  }
  scrollInfo.value = data;
};

const columnsV2 = computed(() => {
  return [];
});

const tableDataV2 = computed(() => {
  const _rows = getTableData();
  _rows.forEach((x, index) => {
    x.elementIndex = index;
  });
  return _rows;
});

// 筛选相关方法
const toggleFilterPopover = (column) => {
  const field = column.field;
  const willOpen = !filterPopoverVisible[field];
  // 打开时关闭其他列已打开的筛选弹窗
  if (willOpen) {
    Object.keys(filterPopoverVisible).forEach((key) => {
      if (key !== field) {
        filterPopoverVisible[key] = false;
      }
    });
  }
  filterPopoverVisible[field] = willOpen;

  // 首次打开时初始化选项
  if (willOpen && !columnFilterOptions[field]) {
    // 字典列(下拉框/单选等)直接使用绑定的数据源作为选项，无需从后端加载去重值
    if (column.bind && Array.isArray(column.bind.data) && column.bind.data.length) {
      columnFilterOptions[field] = column.bind.data
        .filter((x) => !x.hidden)
        .map((x) => ({
          label: x.value === undefined || x.value === null ? String(x.key) : String(x.value),
          value: x.key
        }));
      if (!columnFilterCheckboxValues[field]) {
        columnFilterCheckboxValues[field] = [];
      }
      hasMoreFilterOptions[field] = false;
      return;
    }
    columnFilterOptions[field] = [];
    hasMoreFilterOptions[field] = true;
    loadMoreFilterOptions(column);
  }
};

// 点击筛选弹窗与筛选图标以外的区域时关闭弹窗
const closeFilterPopoverOnOutsideClick = (event) => {
  const hasOpen = Object.keys(filterPopoverVisible).some((key) => filterPopoverVisible[key]);
  if (!hasOpen) return;
  const target = event.target;
  if (
    target &&
    target.closest &&
    (target.closest('.column-filter-popper') || target.closest('.column-filter-icon'))
  ) {
    return;
  }
  Object.keys(filterPopoverVisible).forEach((key) => {
    filterPopoverVisible[key] = false;
  });
};
onMounted(() => {
  document.addEventListener('click', closeFilterPopoverOnOutsideClick, true);
});
onUnmounted(() => {
  document.removeEventListener('click', closeFilterPopoverOnOutsideClick, true);
});

// 选项列表滚动到底部时自动加载下一批(字典列数据全在本地，无需加载)
const onFilterOptionsScroll = (event, column) => {
  const field = column.field;
  if (column.bind || !hasMoreFilterOptions[field] || columnFilterLoading[field]) {
    return;
  }
  const el = event.target;
  if (el.scrollTop + el.clientHeight >= el.scrollHeight - 30) {
    loadMoreFilterOptions(column);
  }
};

// 弹窗中显示的选项：字典列输入框做客户端过滤，其他列显示全部
const getVisibleFilterOptions = (column) => {
  const options = columnFilterOptions[column.field] || [];
  if (column.bind) {
    const keyword = String(columnFilterInputs[column.field] || '').trim().toLowerCase();
    if (keyword) {
      return options.filter(
        (x) =>
          String(x.label).toLowerCase().includes(keyword) ||
          String(x.value).toLowerCase().includes(keyword)
      );
    }
  }
  return options;
};

const applyColumnFilter = (column) => {
  const field = column.field;
  // 字典列输入框只做选项过滤，日期列无输入框：两者仅按勾选值in查询
  const isDict = !!column.bind;
  const isDate = column.type === 'date' || column.type === 'datetime';
  const inputValue = String(columnFilterInputs[field] || '').trim();
  const checkboxValues = columnFilterCheckboxValues[field] || [];

  // 确保 Filter 是数组
  if (!Array.isArray(paginations.Filter)) {
    paginations.Filter = [];
  }

  // 移除该字段的旧筛选条件
  paginations.Filter = paginations.Filter.filter(w => w.Name !== field);

  // 普通列：合并输入框和多选框的值
  const useInput = !isDict && !isDate && inputValue;
  const allValues = [];
  if (useInput) {
    allValues.push(inputValue);
  }
  if (checkboxValues.length > 0) {
    allValues.push(...checkboxValues);
  }

  if (allValues.length > 0) {
    columnFilterValues[field] = allValues;
    // 仅输入框有值时模糊匹配，否则in精确匹配(日期列后端按天区间匹配)
    if (useInput && checkboxValues.length === 0) {
      paginations.Filter.push({
        Name: field,
        Value: inputValue,
        DisplayType: 'like'
      });
    } else {
      paginations.Filter.push({
        Name: field,
        Value: allValues.join(','),
        // 值本身带逗号时(如地区名"北京市,新疆")Value会被后端拆坏，同时传数组由后端优先取用
        Values: allValues.map((v) => (v === null || v === undefined ? '' : String(v))),
        DisplayType: 'in'
      });
    }
  } else {
    delete columnFilterValues[field];
  }

  filterPopoverVisible[field] = false;
  // 重新加载数据
  load();
};

const clearColumnFilter = (column) => {
  columnFilterInputs[column.field] = '';
  columnFilterCheckboxValues[column.field] = [];
  delete columnFilterValues[column.field];
  // 确保 Filter 是数组
  if (!Array.isArray(paginations.Filter)) {
    paginations.Filter = [];
  }
  // 移除查询条件
  const whereIndex = paginations.Filter.findIndex(w => w.Name === column.field);
  if (whereIndex >= 0) {
    paginations.Filter.splice(whereIndex, 1);
  }
  filterPopoverVisible[column.field] = false;
  // 重新加载数据
  load();
};

// 一键清除所有表头筛选条件，返回清除的列数(工具栏"清除筛选"按钮调用)
const clearAllColumnFilters = () => {
  const count = Object.keys(columnFilterValues).filter((key) => columnFilterValues[key]?.length > 0).length;
  Object.keys(filterPopoverVisible).forEach((key) => {
    filterPopoverVisible[key] = false;
  });
  Object.keys(columnFilterInputs).forEach((key) => {
    columnFilterInputs[key] = '';
  });
  Object.keys(columnFilterCheckboxValues).forEach((key) => {
    columnFilterCheckboxValues[key] = [];
  });
  Object.keys(columnFilterValues).forEach((key) => {
    delete columnFilterValues[key];
  });
  const hasFilter = Array.isArray(paginations.Filter) && paginations.Filter.length > 0;
  paginations.Filter = [];
  if (hasFilter) {
    load();
  }
  return count;
};

// 加载更多筛选选项
const loadMoreFilterOptions = async (column) => {
  const field = column.field;
  // 初始化
  if (!columnFilterOptions[field]) {
    columnFilterOptions[field] = [];
  }
  if (!columnFilterCheckboxValues[field]) {
    columnFilterCheckboxValues[field] = [];
  }

  columnFilterLoading[field] = true;

  try {
    // 初始化页码
    if (!columnFilterPageIndexes[field]) {
      columnFilterPageIndexes[field] = 1;
      columnFilterOptions[field] = [];
    }

    // 表头筛选为框架级接口，所有表的查询地址getPageData替换为getColumnDistinctValues
    let url = props.url;
    if (!url || !url.includes('getPageData')) {
      hasMoreFilterOptions[field] = false;
      proxy.$message.error('当前页面查询地址不支持表头筛选');
      return;
    }
    url = url.replace(/getPageData(Async)?/, 'getColumnDistinctValues');

    const response = await proxy.http.post(url, {
      ColumnName: field,
      Page: columnFilterPageIndexes[field],
      PageSize: 30
    });

    if (response.status) {
      const newOptions = (response.rows || [])
        .map(item => {
          // 兼容返回原始值或{字段:值}对象两种格式
          const value = item !== null && typeof item === 'object' ? item[field] : item;
          // 过滤掉 null、undefined、空字符串
          if (value === null || value === undefined || value === '') {
            return null;
          }
          return {
            label: getFilterOptionLabel(column, value),
            value: value
          };
        })
        .filter(option => option !== null); // 过滤掉空值

      // 追加到已有选项
      columnFilterOptions[field].push(...newOptions);

      // 更新页码
      columnFilterPageIndexes[field]++;

      // 判断是否还有更多
      hasMoreFilterOptions[field] = columnFilterOptions[field].length < response.total;
    } else {
      proxy.$message.error(response.message || '加载失败');
    }

  } catch (error) {
    proxy.$message.error('加载筛选选项失败: ' + (error.message || error));
  } finally {
    columnFilterLoading[field] = false;
  }
};

// 筛选选项显示文本：数据字典转换、日期格式化
const getFilterOptionLabel = (column, value) => {
  if (column.bind && Array.isArray(column.bind.data) && column.bind.data.length) {
    const item = column.bind.data.find((x) => x.key == value);
    if (item && item.value !== undefined && item.value !== null) {
      return String(item.value);
    }
  }
  let label = String(value);
  if ((column.type === 'date' || column.type === 'datetime') && label.includes('T')) {
    label = column.type === 'date' ? label.substring(0, 10) : label.substring(0, 19).replace('T', ' ');
  }
  return label;
};


const tableV2SummaryHeight = computed(() => {
  return props.columns.some((x) => {
    return x.summary && !x.hidden;
  })
    ? 37
    : 0;
});

const tableV2FooterColumns = computed(() => {
  return props.columns.filter((x) => {
    return !x.hidden
  })
});


let tableDataChange = false;
watch(
  () => props.tableData.length,
  (newLen, oldLen) => {
    tableDataChange = true;
    // console.log('tableData')
    watchRowSelectChange(newLen, oldLen);
  }
);
watch(
  () => rowData.value.length,
  (newLen, oldLen) => {
    if (tableDataChange) {
      tableDataChange = false;
      return;
    }
    // console.log('rowData')
    watchRowSelectChange(newLen, oldLen);
  }
);

const handleTableClickOutside = async (event) => {
  const target = event.target;
  // 日期/时间选择器面板 teleport 到 body，点击面板不应结束行编辑
  if (
    target &&
    typeof target.closest === "function" &&
    target.closest('[class*="picker__popper"]')
  ) {
    return;
  }
  if (!refTable.value?.contains(target)) {
    if (isDateChange.value) return;
    if (edit.rowIndex != -1) {
      // let row = getTableData[edit.rowIndex];
      //if ((await rowEndEdit(proxy, props, getTableData(), row, edit)) !== false) {
      edit.rowIndex = -1;
      //}
    }
  }
};

const dragRef = ref(null);
const hasEdit = () => {
  return props.columns.some((x) => {
    return x.edit;
  });
};

const validate = (callBack) => {
  const res = tableValidate(proxy, props, getTableData());
  callBack?.(res);
  return res;
};

onMounted(() => {
  if (hasEdit()) {
    document.addEventListener("click", handleTableClickOutside);
  }
  if (props.tableV2) return;
  nextTick(() => {
    if (props.dragPosition) {
      nextTick(() => {
        initDrag(
          props,
          dragRef.value,
          refTable.value,
          showDragMask,
          realHeight
        );
      });
    }
  });
  initSortable(props, emit, nextTick, refTable.value, () => {
    return {
      rows: getTableData(),
      elTableRef: getTable(),
      scrollInfo: scrollInfo.value,
    };
  });
});
onUnmounted(() => {
  if (hasEdit()) {
    document.removeEventListener("click", handleTableClickOutside);
  }
});

const focus=(row,field)=>{
    const $el=  proxy.$refs[field + row.elementIndex];
    if (Array.isArray($el) && $el.length) {
       $el[0].focus();
    }
}

defineExpose({
  table,
  getTable,
  getTableData,
  paginations,
  rowData,
  edit,
  realHeight,
  realMaxHeight,
  setHeight,
  initConfig,
  initDicKeys,
  summaryData,
  summaryIndex,
  initSummary,
  getInputSummaries,
  setColumnSummary,
  getSelectionRows,
  getSelected,
  getSelectedIndex,
  getCurrentEditRow,
  load,
  updateSummary,
  toNextCell,
  clearSelection,
  toggleRowSelection,
  addRow,
  delRow,
  remoteColumns,
  reset,
  setEdit,
  viewImg,
  tableData: props.tableData,
  validate,
  focus,
  clearAllColumnFilters
});
</script>
<style lang="less" scoped>
@import "./VolTable/VolTable.less";
</style>
<style lang="less">
// 表头筛选：弹窗teleport到body，需使用全局样式；颜色使用element变量以适配暗色主题
.column-filter-icon {
  cursor: pointer;
  outline: none;
  color: var(--el-text-color-placeholder);
  transition: color 0.2s;

  &:hover,
  &.is-active {
    color: var(--el-color-primary);
  }
}

.el-popover.el-popper.column-filter-popper {
  padding: 0;
  border-radius: 8px;
}

.column-filter-popover {
  .filter-input-wrapper {
    padding: 10px 10px 6px;

    .el-input__wrapper {
      background: var(--el-fill-color-light);
      border-radius: 6px;
      box-shadow: none;
      transition: background 0.2s, box-shadow 0.2s;

      &:hover {
        box-shadow: none;
      }

      &.is-focus {
        background: var(--el-bg-color);
        box-shadow: 0 0 0 1px var(--el-color-primary) inset;
      }
    }
  }

  .filter-checkbox-wrapper {
    max-height: 180px;
    overflow-y: auto;
    padding: 2px 6px 6px;

    // 日期列无输入框时顶部留白
    &:first-child {
      padding-top: 8px;
    }

    &::-webkit-scrollbar {
      width: 6px;
    }

    &::-webkit-scrollbar-thumb {
      background: var(--el-border-color-lighter);
      border-radius: 3px;

      &:hover {
        background: var(--el-border-color);
      }
    }

    .filter-checkbox-item {
      border-radius: 6px;
      transition: background 0.15s;

      &:hover {
        background: var(--el-fill-color-light);
      }

      .el-checkbox {
        display: flex;
        align-items: center;
        width: 100%;
        height: 30px;
        padding: 0 8px;
        margin-right: 0;

        .el-checkbox__label {
          flex: 1;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          font-size: 13px;
          color: var(--el-text-color-regular);
        }

        &.is-checked .el-checkbox__label {
          color: var(--el-color-primary);
        }
      }
    }
  }

  .filter-loading,
  .filter-list-end {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 4px;
    padding: 6px 0 4px;
    font-size: 12px;
    color: var(--el-text-color-placeholder);
  }

  .filter-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 26px 0;
    color: var(--el-text-color-placeholder);
    font-size: 12px;

    .el-icon {
      font-size: 24px;
    }

    > div {
      margin-top: 6px;
    }
  }

  .filter-actions {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    padding: 8px 10px;
    border-top: 1px solid var(--el-border-color-extra-light);

    .filter-selected {
      margin-right: auto;
      font-size: 12px;
      color: var(--el-text-color-secondary);
    }

    .el-button + .el-button {
      margin-left: 8px;
    }
  }
}
</style>

