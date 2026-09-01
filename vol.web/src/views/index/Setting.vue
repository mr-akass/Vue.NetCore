<!--
  主题个性化面板(顶栏"基础设置"抽屉里)
  说明：
   1) 所有旋钮都是"改一下立刻预览",点[保存]才写服务端;直接关掉抽屉会还原成打开时的样子
   2) 每个应用一套配置:面板顶部标出当前应用,切应用后进来看到的是那个应用自己的配置
   3) 变量计算与落库都在 uitils/themeManager.js,这里只负责界面
-->
<template>
  <div class="vol-theme-setting">
    <div class="ts-tip">
      <i class="el-icon-info"></i>
      <span>{{ $ts('以下设置只对当前应用') }}【{{ appName }}】{{ $ts('生效') }}</span>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('整套方案') }}</div>
      <div class="ts-schemes">
        <div v-for="s in schemes" :key="s.name" class="ts-scheme" :class="{ 'ts-actived': theme.scheme === s.name }"
          @click="applyScheme(s)">
          <span class="ts-scheme-dot" :style="{ background: presetColor(s.config.preset) }"></span>
          {{ $ts(s.label) }}
        </div>
      </div>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('主题色') }}</div>
      <div class="ts-colors">
        <div v-for="p in presets" :key="p.name" class="ts-color" :class="{ 'ts-actived': theme.preset === p.name }"
          :style="{ background: p.primary }" :title="$ts(p.label)" @click="selectPreset(p)">
          <i v-show="theme.preset === p.name" class="el-icon-check"></i>
        </div>
        <el-color-picker v-model="theme.primary" size="small" @change="onChange" />
      </div>
      <div class="ts-hint">{{ $ts('最后一个是自定义主色，清空后用预设色') }}</div>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('视觉效果') }}</div>
      <el-radio-group v-model="theme.effect" size="small" @change="onChange">
        <el-radio-button value="flat">{{ $ts('纯色') }}</el-radio-button>
        <el-radio-button value="gradient">{{ $ts('渐变') }}</el-radio-button>
        <el-radio-button value="glass">{{ $ts('玻璃') }}</el-radio-button>
      </el-radio-group>
      <!-- 通透度不再只服务于玻璃效果:设了背景图后,界面半透明才能让整页都透出图片,所以两种情况都要露出这个滑块 -->
      <div v-show="theme.effect === 'glass' || theme.bgImage" class="ts-row">
        <span class="ts-label">{{ $ts('界面通透度') }}（{{ Math.round(theme.surfaceAlpha * 100) }}%）</span>
        <el-slider v-model="theme.surfaceAlpha" :min="0.1" :max="1" :step="0.02" size="small" @input="onChange" />
      </div>
      <div v-show="theme.effect === 'glass' || theme.bgImage" class="ts-hint">
        {{ $ts('调越小界面越透，背景图露出得越多') }}
      </div>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('菜单布局') }}</div>
      <el-radio-group v-model="theme.layout" size="small" @change="onChange">
        <el-radio-button value="classics">{{ $ts('经典导航') }}</el-radio-button>
        <el-radio-button value="top">{{ $ts('顶部导航') }}</el-radio-button>
        <el-radio-button value="left">{{ $ts('双栏导航') }}</el-radio-button>
      </el-radio-group>
      <div class="ts-hint">{{ $ts('双栏=一级菜单单独一栏，右边只显示它下面的子菜单') }}</div>
      <div class="ts-row ts-row-switch">
        <span class="ts-label">{{ $ts('侧边菜单默认折叠') }}</span>
        <el-switch v-model="theme.menuCollapsed" size="small" @change="onChange" />
      </div>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('页签样式') }}</div>
      <el-radio-group v-model="theme.navStyle" size="small" @change="onChange">
        <el-radio-button value="card">{{ $ts('卡片') }}</el-radio-button>
        <el-radio-button value="line">{{ $ts('下划线') }}</el-radio-button>
        <el-radio-button value="pill">{{ $ts('胶囊') }}</el-radio-button>
      </el-radio-group>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('内容排版') }}</div>
      <el-radio-group v-model="theme.pageStyle" size="small" @change="onChange">
        <el-radio-button value="plain">{{ $ts('平铺') }}</el-radio-button>
        <el-radio-button value="card">{{ $ts('卡片') }}</el-radio-button>
      </el-radio-group>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('排版密度') }}</div>
      <el-radio-group v-model="theme.density" size="small" @change="onChange">
        <el-radio-button value="compact">{{ $ts('紧凑') }}</el-radio-button>
        <el-radio-button value="default">{{ $ts('标准') }}</el-radio-button>
        <el-radio-button value="loose">{{ $ts('宽松') }}</el-radio-button>
      </el-radio-group>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('圆角') }}</div>
      <el-radio-group v-model="theme.radius" size="small" @change="onChange">
        <el-radio-button value="square">{{ $ts('直角') }}</el-radio-button>
        <el-radio-button value="round">{{ $ts('圆角') }}</el-radio-button>
        <el-radio-button value="pill">{{ $ts('大圆角') }}</el-radio-button>
      </el-radio-group>
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('全局字体大小') }}（{{ theme.fontSize }}px）</div>
      <el-slider v-model="theme.fontSize" :min="12" :max="20" :step="1" show-stops size="small" @input="onChange" />
    </div>

    <div class="ts-block">
      <div class="ts-title">{{ $ts('背景图') }}</div>
      <div class="ts-bg">
        <div class="ts-bg-preview" :style="{ backgroundImage: bgPreview ? `url(${bgPreview})` : 'none' }">
          <span v-show="!bgPreview" class="ts-bg-empty">{{ $ts('未设置') }}</span>
        </div>
        <div class="ts-bg-btns">
          <el-button size="small" type="primary" plain @click="selectFile">{{ $ts('上传') }}</el-button>
          <el-button size="small" :disabled="!theme.bgImage" @click="clearBackground">{{ $ts('清除') }}</el-button>
        </div>
        <input ref="fileRef" type="file" accept="image/*" style="display: none" @change="onFileChange" />
      </div>
      <div v-show="theme.bgImage" class="ts-row">
        <span class="ts-label">{{ $ts('遮罩') }}</span>
        <el-slider v-model="theme.bgMask" :min="0" :max="0.85" :step="0.05" size="small" @input="onChange" />
      </div>
      <div class="ts-hint">{{ $ts('支持jpg/png/webp/gif，不超过5M') }}</div>
      <div v-show="theme.bgImage" class="ts-hint">
        {{ $ts('背景图铺满整个页面；觉得图被界面挡住了，就把上面【视觉效果】里的界面通透度调小') }}
      </div>
    </div>

    <div class="ts-footer">
      <el-button type="primary" size="small" :loading="saving" @click="save">{{ $ts('保存') }}</el-button>
      <el-button size="small" @click="reset">{{ $ts('重置') }}</el-button>
      <el-button v-if="isSuperAdmin" size="small" type="warning" plain @click="saveDefault">
        {{ $ts('设为本应用默认') }}
      </el-button>
    </div>
  </div>
</template>
<script>
import { defineComponent, reactive, ref, computed, onUnmounted, getCurrentInstance } from 'vue'
import store from '@/store/index'
import themeManager, { themeState, COLOR_PRESETS, THEME_SCHEMES, DEFAULT_THEME } from '@/uitils/themeManager'

export default defineComponent({
  setup() {
    const { proxy } = getCurrentInstance()
    const presets = COLOR_PRESETS
    const schemes = THEME_SCHEMES
    const saving = ref(false)
    const fileRef = ref()
    let saved = false //关闭面板时:没保存过就把预览还原回来

    //打开面板时的状态:已启用自定义主题就接着改,否则用默认值(布局沿用当前实际布局,避免一进来界面就跳)
    const origin = themeState.custom ? themeManager.normalizeTheme(themeState) : null
    const theme = reactive(
      themeManager.normalizeTheme(
        origin || Object.assign({}, DEFAULT_THEME, { layout: localStorage.getItem('vol-layout') || proxy.$global.layout || 'top' })
      )
    )

    const appId = themeManager.currentAppId()
    const appName = computed(() => {
      const app = store.getters.getAppConfig && store.getters.getAppConfig()
      if (appId && app && app.appId === appId) return app.appName || app.title
      return appId ? `#${appId}` : proxy.$ts('默认')
    })
    const isSuperAdmin = computed(() => themeState.isSuperAdmin)
    const bgPreview = computed(() => themeManager.getImageUrl(theme.bgImage))

    /** 取纯对象:reactive 直接丢给 JSON.stringify 会把 Proxy 的东西也带上 */
    const toPlain = () => themeManager.normalizeTheme(theme)
    const apply = () => themeManager.applyTheme(toPlain())
    /** 手动改了任意一项就不再算"某套方案",取消方案高亮 */
    const onChange = () => {
      theme.scheme = ''
      apply()
    }
    const presetColor = (name) => themeManager.getPreset(name).primary

    const applyScheme = (scheme) => {
      Object.assign(theme, scheme.config, { scheme: scheme.name })
      apply()
    }
    const selectPreset = (preset) => {
      theme.preset = preset.name
      theme.primary = '' //选了预设就清掉自定义色,否则看不出变化
      onChange()
    }

    /* ---------------------------- 背景图 ---------------------------- */
    const selectFile = () => {
      fileRef.value.value = '' //清掉上次的值:选同一个文件不会触发 change
      fileRef.value.click()
    }
    const onFileChange = (e) => {
      const file = e.target.files && e.target.files[0]
      if (!file) return
      if (file.size > 5 * 1024 * 1024) {
        return proxy.$message.error(proxy.$ts('图片不能超过5M'))
      }
      themeManager.uploadBackground(file, appId).then((x) => {
        if (!x.status) return
        //后端把地址同时写进了库里的 BgImage 与 ThemeJson,这里只需要更新预览
        theme.bgImage = x.data && x.data.url
        apply()
      })
    }
    const clearBackground = () => {
      themeManager.removeBackground(appId).then((x) => {
        if (!x.status) return
        theme.bgImage = ''
        apply()
      })
    }

    /* ---------------------------- 保存/重置 ---------------------------- */
    const save = () => {
      saving.value = true
      themeManager
        .saveTheme(toPlain(), appId)
        .then((x) => {
          if (x.status) {
            saved = true
            proxy.$message.success(proxy.$ts('保存成功'))
          }
        })
        .finally(() => {
          saving.value = false
        })
    }
    const saveDefault = () => {
      proxy
        .$confirm(proxy.$ts('确定把当前配置设为本应用所有用户的默认主题吗?'), proxy.$ts('提示'), { type: 'warning' })
        .then(() => {
          themeManager.saveAppDefault(toPlain(), appId).then((x) => {
            if (x.status) proxy.$message.success(proxy.$ts('已设为本应用默认主题'))
          })
        })
        .catch(() => {})
    }
    /** 重置:服务端删记录+清缓存+撤掉所有变量,回到框架原生样式;布局变了必须刷新页面才能重新渲染 */
    const reset = () => {
      proxy
        .$confirm(proxy.$ts('确定还原成系统默认主题吗?'), proxy.$ts('提示'), { type: 'warning' })
        .then(() => {
          themeManager.resetTheme(appId).then((x) => {
            if (!x.status) return
            saved = true
            window.location.reload()
          })
        })
        .catch(() => {})
    }

    //改一下就实时预览,所以直接关掉面板(不保存)时要把预览撤回去,否则界面留着没保存的样子
    onUnmounted(() => {
      if (saved) return
      if (origin) {
        themeManager.applyTheme(origin)
      } else if (themeState.custom) {
        themeManager.clearTheme()
      }
    })

    return {
      presets,
      schemes,
      theme,
      saving,
      fileRef,
      appName,
      isSuperAdmin,
      bgPreview,
      presetColor,
      applyScheme,
      selectPreset,
      onChange,
      selectFile,
      onFileChange,
      clearBackground,
      save,
      saveDefault,
      reset
    }
  }
})
</script>
<style lang="less" scoped>
.vol-theme-setting {
  padding: 0 4px 10px;

  .ts-tip {
    display: flex;
    align-items: center;
    padding: 8px 10px;
    margin-bottom: 14px;
    font-size: 12px;
    color: #606266;
    background: var(--vol-primary-soft, rgba(45, 140, 240, 0.1));
    border-radius: 4px;

    i {
      margin-right: 6px;
      color: var(--el-color-primary);
    }
  }

  .ts-block {
    padding-bottom: 14px;
    margin-bottom: 14px;
    border-bottom: 1px dashed #ebeef5;

    &:last-of-type {
      border-bottom: 0;
    }
  }

  .ts-title {
    margin-bottom: 10px;
    font-size: 13px;
    font-weight: 500;
    color: #303133;
  }

  .ts-hint {
    margin-top: 6px;
    font-size: 12px;
    color: #909399;
  }

  /* 整套方案:两列,选中的描一圈主色 */
  .ts-schemes {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
  }

  .ts-scheme {
    display: flex;
    align-items: center;
    width: calc(50% - 4px);
    padding: 7px 9px;
    font-size: 12px;
    color: #606266;
    cursor: pointer;
    border: 1px solid #dcdfe6;
    border-radius: 4px;
    transition: all 0.2s;

    &:hover {
      border-color: var(--el-color-primary);
    }

    &.ts-actived {
      color: var(--el-color-primary);
      border-color: var(--el-color-primary);
      background: var(--vol-primary-soft, rgba(45, 140, 240, 0.1));
    }
  }

  .ts-scheme-dot {
    width: 10px;
    height: 10px;
    margin-right: 6px;
    border-radius: 50%;
    flex: none;
  }

  /* 主题色色块 */
  .ts-colors {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
  }

  .ts-color {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    color: #fff;
    font-size: 12px;
    cursor: pointer;
    border-radius: 4px;
    box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.06) inset;

    &.ts-actived {
      box-shadow: 0 0 0 2px #fff inset, 0 0 0 3px currentColor;
    }
  }

  .ts-row {
    display: flex;
    align-items: center;
    margin-top: 10px;

    .ts-label {
      flex: none;
      width: 84px;
      font-size: 12px;
      color: #606266;
    }

    :deep(.el-slider) {
      margin-right: 8px;
    }
  }

  .ts-row-switch {
    justify-content: space-between;

    .ts-label {
      width: auto;
    }
  }

  /* 背景图预览 */
  .ts-bg {
    display: flex;
    align-items: center;
  }

  .ts-bg-preview {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 96px;
    height: 58px;
    margin-right: 10px;
    background-color: #f5f7fa;
    background-size: cover;
    background-position: center center;
    border: 1px solid #dcdfe6;
    border-radius: 4px;
  }

  .ts-bg-empty {
    font-size: 12px;
    color: #c0c4cc;
  }

  .ts-bg-btns {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .ts-footer {
    padding-top: 6px;
    text-align: right;
    border-top: 1px solid #ebeef5;
  }

  :deep(.el-radio-button__inner) {
    padding: 6px 11px;
    font-size: 12px;
  }
}
</style>

