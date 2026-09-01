<template>
  <!-- 回车登录：原来绑的是 keypress，输入法状态下部分浏览器不触发，改用 keyup.enter -->
  <div class="login-page" @keyup.enter="login">
    <!-- 背景：光斑 + 极光光带 + 网格 + 上升光点 + 装饰光环 + 四周压暗 -->
    <div class="bg-glow glow-1"></div>
    <div class="bg-glow glow-2"></div>
    <div class="bg-glow glow-3"></div>
    <div class="bg-aurora">
      <span class="aurora-band band-1"></span>
      <span class="aurora-band band-2"></span>
      <span class="aurora-band band-3"></span>
    </div>
    <div class="bg-grid"></div>
    <!-- 上升光点：位置/大小/时长写死在数组里，不用随机数——每次刷新都一样才不会显得"乱" -->
    <div class="bg-particles">
      <span v-for="(p, i) in particles" :key="i" class="particle" :style="p"></span>
    </div>
    <div class="bg-rings">
      <span class="ring ring-1"></span>
      <span class="ring ring-2"></span>
    </div>
    <div class="bg-vignette"></div>

    <div v-if="$global.lang" class="app-lang">
      <lang color="#ffffff"></lang>
    </div>

    <div class="login-box">
      <!-- 登录卡片 -->
      <div class="login-card">
        <!-- 边框流星：一圈旋转的 conic 渐变(亮头 + 拖尾)被遮罩成只剩边框那一圈。
             两层同步转——beam-glow 是模糊的光晕、beam-core 是锐利的白色内核，
             分层才有"亮到发光"的观感，单层要么细得看不见要么糊成一团 -->
        <span class="card-beam beam-glow"><i></i></span>
        <span class="card-beam beam-core"><i></i></span>

        <!-- 品牌标识收进卡片内、与标题同行：去掉卡片上方的大号品牌区后整体才能真正居中 -->
        <div class="card-head">
          <div class="brand-mark">
            <i class="el-icon-s-platform"></i>
          </div>
          <div class="card-head-text">
            <div class="card-title">{{ $ts("账号登录") }}</div>
            <div class="card-subtitle">WELCOME BACK</div>
          </div>
        </div>

        <div class="form-input">
          <i class="el-icon-user"></i>
          <input ref="userRef" type="text" v-model="userInfo.userName" :placeholder="$ts(['请输入', '账号'])" />
        </div>
        <div class="form-input">
          <i class="el-icon-lock"></i>
          <input :type="showPwd ? 'text' : 'password'" v-model="userInfo.password"
            :placeholder="$ts(['请输入', '密码'])" />
          <!-- 眼睛用 Element-Plus 的 svg 图标：老图标字体里只有 el-icon-view 没有 el-icon-hide，
               闭眼状态会渲染成零宽的空 i 而点不到(改版前就有这个问题) -->
          <el-icon class="pwd-eye" :title="$ts(showPwd ? '隐藏密码' : '显示密码')" @click="showPwd = !showPwd">
            <View v-if="showPwd" />
            <Hide v-else />
          </el-icon>
        </div>

        <el-button class="login-btn" :loading="loading" type="primary" @click="login">
          <span v-if="!loading">{{ $ts("登录") }}</span>
          <span v-else>{{ $ts("登录中") }}...</span>
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, getCurrentInstance, onMounted } from "vue";
import { useRouter } from "vue-router";
import store from "../store/index";
import http from "@/../src/api/http.js";
import lang from "@/components/lang/lang";
import { saveAppId, removeSavedAppId } from "@/config/appConfig";
const loading = ref(false);
const showPwd = ref(false);
const userRef = ref(null);
const userInfo = reactive({
  userName: "",
  password: "",
});

//打开页面自动聚焦账号框：回车绑在 .login-page 上，焦点落在 body 时事件不冒泡过来，
//不聚焦的话"打开页面直接敲回车"没有任何反应
onMounted(() => userRef.value && userRef.value.focus());

//上升光点：[左边距%, 大小px, 时长s, 延迟s, 透明度]，固定值而非随机——
//登录页每天都看，随机分布会时密时疏，固定值调一次就永远是这个观感
const particles = [
  [6, 4, 15, 0, 0.9], [13, 3, 19, 4, 0.6], [21, 5, 13, 2, 0.85],
  [28, 3, 22, 7, 0.55], [35, 4, 17, 1, 0.75], [43, 3, 20, 9, 0.6],
  [57, 4, 16, 3, 0.85], [65, 3, 21, 6, 0.6], [72, 5, 14, 8, 0.75],
  [79, 3, 18, 2, 0.65], [86, 4, 20, 5, 0.85], [94, 3, 16, 10, 0.6],
].map(([left, size, dur, delay, opacity]) => ({
  left: left + "%",
  width: size + "px",
  height: size + "px",
  animationDuration: dur + "s",
  animationDelay: delay + "s",
  "--p-opacity": opacity,
}));

const { proxy } = getCurrentInstance();
let $message = proxy.$message;
let router = useRouter();
let $ts = proxy.$ts;
const login = () => {
  //回车与按钮同一个入口，请求中再次触发直接忽略，避免连按提交两次
  if (loading.value) return;
  if (!userInfo.userName) return $message.error($ts(["请输入", "账号"]));
  if (!userInfo.password) return $message.error($ts(["请输入", "密码"]));
  loading.value = true;
  http.post("/api/user/login", userInfo, $ts("正在登录") + "....").then((result) => {
    if (!result.status) {
      loading.value = false;
      return $message.error(result.message);
    }
    store.commit("setUserInfo", result.data);

    //多应用支持：按用户有权限的应用决定跳转
    const { isSuperAdmin, appIds } = result.data || {};
    store.commit("setUserAppIds", appIds || []);
    if (isSuperAdmin) {
      //超级管理员直接进入首页(不按应用过滤)；清除上次选择的应用视角，避免菜单被按应用过滤
      removeSavedAppId(result.data.userId);
      router.push({ path: "/" });
    } else if (appIds && appIds.length === 1) {
      //只有一个应用，自动选中并进入首页
      saveAppId(appIds[0]);
      router.push({ path: "/" });
    } else if (appIds && appIds.length > 1) {
      //有多个应用，进入应用选择页
      router.push({ path: "/guide" });
    } else {
      //没有任何应用权限
      $message.warning($ts("当前账号没有任何应用权限，请联系管理员分配"));
      loading.value = false;
    }
  });
};
</script>
<style lang="less" scoped>
//主色：与框架一致的 indigo→violet
@c1: #6366f1;
@c2: #8b5cf6;

.login-page {
  position: relative;
  width: 100%;
  height: 100vh;
  overflow: hidden;
  //底色带一点径向提亮，纯平的 #0b1022 会显得发闷
  background:
    radial-gradient(ellipse 80% 60% at 50% 40%, #141b3d 0%, transparent 70%),
    #0a0f20;
  display: flex;
  align-items: center;
  justify-content: center;
}

//背景光斑
.bg-glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(120px);
  pointer-events: none;
  animation: drift 22s ease-in-out infinite;
}

.glow-1 {
  width: 620px;
  height: 620px;
  background: rgba(79, 70, 229, 0.42);
  top: -180px;
  left: -140px;
}

.glow-2 {
  width: 520px;
  height: 520px;
  background: rgba(14, 165, 233, 0.26);
  bottom: -180px;
  right: -120px;
  animation-delay: 7s;
}

.glow-3 {
  width: 400px;
  height: 400px;
  background: rgba(168, 85, 247, 0.26);
  top: 26%;
  left: 56%;
  animation-delay: 13s;
}

//位移幅度比原来小、周期更长：登录页是每天都要看的页面，动效要"察觉不到在动"
@keyframes drift {

  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  50% {
    transform: translate(24px, -22px) scale(1.05);
  }
}
//极光光带：斜向的细长渐变条，缓慢横向漂移 + 明暗呼吸
.bg-aurora {
  position: absolute;
  inset: -10%;
  pointer-events: none;
  overflow: hidden;
  //边缘淡出，不然能看到光带的头尾切边
  -webkit-mask-image: radial-gradient(ellipse 80% 80% at 50% 50%, #000 30%, transparent 90%);
  mask-image: radial-gradient(ellipse 80% 80% at 50% 50%, #000 30%, transparent 90%);
}

.aurora-band {
  position: absolute;
  left: -30%;
  width: 160%;
  height: 200px;
  border-radius: 50%;
  filter: blur(60px);
  opacity: 0.5;
  //倾角用变量传给关键帧：关键帧里的 transform 是整条覆盖的，
  //写死 rotate 会把三条光带的角度都抹成同一个
  transform: rotate(var(--rot));
  animation: auroraFlow 28s ease-in-out infinite;
}

.band-1 {
  --rot: -8deg;
  top: 12%;
  background: linear-gradient(90deg, transparent, rgba(99, 102, 241, 0.5), rgba(139, 92, 246, 0.35), transparent);
}

.band-2 {
  --rot: 6deg;
  top: 46%;
  background: linear-gradient(90deg, transparent, rgba(56, 189, 248, 0.3), rgba(99, 102, 241, 0.4), transparent);
  animation-duration: 34s;
  animation-delay: 6s;
}

.band-3 {
  --rot: -5deg;
  bottom: 10%;
  background: linear-gradient(90deg, transparent, rgba(168, 85, 247, 0.35), rgba(56, 189, 248, 0.25), transparent);
  animation-duration: 40s;
  animation-delay: 12s;
}

//只动 translateX + opacity：这两个属性走合成层，不触发重排
@keyframes auroraFlow {

  0%,
  100% {
    transform: translateX(-8%) rotate(var(--rot));
    opacity: 0.45;
  }

  50% {
    transform: translateX(8%) rotate(var(--rot));
    opacity: 0.85;
  }
}

//背景网格(中心可见、四周淡出)
.bg-grid {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.045) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.045) 1px, transparent 1px);
  background-size: 60px 60px;
  -webkit-mask-image: radial-gradient(ellipse 65% 55% at 50% 45%, #000 15%, transparent 100%);
  mask-image: radial-gradient(ellipse 65% 55% at 50% 45%, #000 15%, transparent 100%);
}

//上升光点
.bg-particles {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}

.particle {
  position: absolute;
  bottom: -20px;
  border-radius: 50%;
  background: #dbe3ff;
  box-shadow: 0 0 10px 2px rgba(165, 180, 252, 0.9);
  opacity: 0;
  animation-name: rise;
  animation-timing-function: linear;
  animation-iteration-count: infinite;
}

//从底部升到顶部，中途最亮、首尾淡出，看不到"凭空出现/消失"
@keyframes rise {
  0% {
    transform: translateY(0);
    opacity: 0;
  }

  12% {
    opacity: var(--p-opacity, 0.4);
  }

  85% {
    opacity: var(--p-opacity, 0.4);
  }

  100% {
    transform: translateY(-102vh);
    opacity: 0;
  }
}

//两个装饰光环：给大片空白处一点结构感，不参与交互
.bg-rings {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.ring {
  position: absolute;
  border-radius: 50%;
  border: 1px solid rgba(165, 180, 252, 0.12);
  animation: ringPulse 12s ease-in-out infinite;
}

.ring-1 {
  width: 420px;
  height: 420px;
  top: 8%;
  left: 7%;
  //内圈虚线环，单实线环显得太"几何"
  box-shadow: inset 0 0 0 1px rgba(165, 180, 252, 0.05);
}

.ring-2 {
  width: 300px;
  height: 300px;
  bottom: 10%;
  right: 9%;
  animation-delay: 6s;
}

@keyframes ringPulse {

  0%,
  100% {
    transform: scale(1);
    opacity: 0.5;
  }

  50% {
    transform: scale(1.06);
    opacity: 1;
  }
}

//四周压暗：把视觉重心收到中间的卡片上
.bg-vignette {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: radial-gradient(ellipse 75% 70% at 50% 50%, transparent 40%, rgba(4, 7, 18, 0.55) 100%);
}

.app-lang {
  position: absolute;
  z-index: 9;
  right: 40px;
  top: 24px;
}

.login-box {
  position: relative;
  z-index: 2;
  width: 440px;
  max-width: 92%;
  animation: fadeUp 0.55s cubic-bezier(0.22, 1, 0.36, 1) both;
}

@keyframes fadeUp {
  from {
    opacity: 0;
    transform: translateY(22px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}
//卡片头部：品牌图标 + 标题同行
.card-head {
  display: flex;
  align-items: center;
  margin-bottom: 26px;

  .brand-mark {
    position: relative;
    flex-shrink: 0;
    width: 46px;
    height: 46px;
    margin-right: 14px;
    border-radius: 13px;
    background: linear-gradient(135deg, @c1, @c2);
    //外发光 + 内层高光：只有纯渐变的圆角方块看着像"没加载出来的图片"
    box-shadow:
      0 8px 20px rgba(99, 102, 241, 0.42),
      inset 0 1px 0 rgba(255, 255, 255, 0.35);
    display: flex;
    align-items: center;
    justify-content: center;

    i {
      font-size: 22px;
      color: #fff;
      //图标本身给一点投影，避免在亮渐变上发虚
      text-shadow: 0 2px 6px rgba(30, 20, 90, 0.4);
    }
  }
}
//登录卡片
.login-card {
  position: relative;
  padding: 40px 36px;
  border-radius: 20px;
  //上亮下暗的玻璃：单一半透明色在深色背景上会"糊"在一起、看不出卡片边界
  background: linear-gradient(160deg, rgba(255, 255, 255, 0.11) 0%, rgba(255, 255, 255, 0.04) 100%);
  border: 1px solid rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(24px);
  box-shadow:
    0 28px 70px rgba(3, 7, 24, 0.62),
    inset 0 1px 0 rgba(255, 255, 255, 0.16);

  //顶部一条高光细线，玻璃质感主要靠它
  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 12%;
    right: 12%;
    height: 1px;
    background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.6), transparent);
  }

  .card-title {
    font-size: 20px;
    font-weight: 600;
    color: #fff;
    letter-spacing: 1px;
  }

  //副标题跟在标题下方，间距由 .card-head 统一控制(不再自己撑 margin)
  .card-subtitle {
    font-size: 11px;
    color: rgba(255, 255, 255, 0.32);
    letter-spacing: 3px;
    margin-top: 5px;
  }
}

//边框流星：一圈旋转的 conic 渐变(大半圈透明 + 末端冲到纯白 = 亮头拖长尾)，
//用 mask 把中间镂空、只留边框那一圈。
//为什么不用"四条边各一个移动光条"：那种做法光条被跑道的 overflow 切掉朝外那半边光晕，
//再怎么加 box-shadow/drop-shadow 也只是一条细白线，亮不起来；conic + 镂空 mask 没有这个限制
.card-beam {
  position: absolute;
  inset: -1px;
  border-radius: 21px;
  pointer-events: none;

  //只保留 padding 那一圈：整块 - 内容区 = 空心边框(不镂空的话旋转的渐变会糊满整张卡片)
  -webkit-mask:
    linear-gradient(#000 0 0) content-box,
    linear-gradient(#000 0 0);
  -webkit-mask-composite: xor;
  mask:
    linear-gradient(#000 0 0) content-box,
    linear-gradient(#000 0 0);
  mask-composite: exclude;

  i {
    position: absolute;
    top: 50%;
    left: 50%;
    //正方形且大于卡片对角线，旋转时四角才不会转出空白
    width: 160%;
    aspect-ratio: 1;
    //亮头只占 conic 的最后一小段：前面 0.8 圈全透明(边框此处不亮)，之后由暗到亮冲到纯白。
    //这一段留得越窄越像"一颗流星"、越宽越像"半条边在发光"
    background: conic-gradient(from 0turn,
        transparent 0turn,
        transparent 0.8turn,
        rgba(139, 92, 246, 0.45) 0.9turn,
        rgba(224, 231, 255, 1) 0.98turn,
        #fff 1turn);
    animation: beamSpin 4s linear infinite;
  }
}

//外层光晕：只比内核粗一点点 + 小半径模糊，负责"发光"而不是"发一条宽带子"。
//blur 会把亮度摊薄，brightness 补回来，不然模糊完只剩一层淡紫；
//padding 和 blur 都要小——大了就变成边框上贴了一条粗糊的光带
.beam-glow {
  padding: 3px;

  i {
    filter: blur(4px) brightness(2.2);
  }
}

//内层核心：1px 不模糊，负责"锐利的亮线"，粗细跟卡片自身的 1px 边框一致
.beam-core {
  padding: 1px;

  i {
    filter: brightness(1.4);
  }
}

//转一圈就是流星绕卡片一周；transform-origin 用默认中心 + translate 回正
@keyframes beamSpin {
  from {
    transform: translate(-50%, -50%) rotate(0deg);
  }

  to {
    transform: translate(-50%, -50%) rotate(360deg);
  }
}
.form-input {
  display: flex;
  align-items: center;
  height: 50px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  padding: 0 14px;
  background: rgba(10, 15, 34, 0.35);
  margin-bottom: 20px;
  transition: border-color 0.2s, background 0.2s, box-shadow 0.2s;

  &:hover {
    border-color: rgba(255, 255, 255, 0.2);
  }

  &:focus-within {
    border-color: #818cf8;
    background: rgba(10, 15, 34, 0.5);
    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.22);

    //聚焦时左侧图标跟着亮起来，给一个明确的"焦点在这"的反馈
    >i:first-child {
      color: #a5b4fc;
    }
  }

  >i {
    color: rgba(255, 255, 255, 0.4);
    font-size: 16px;
    margin-right: 10px;
    transition: color 0.2s;
  }

  input {
    flex: 1;
    min-width: 0;
    height: 100%;
    border: 0;
    outline: none;
    background: transparent;
    font-size: 15px;
    color: #eef1f8;
    letter-spacing: 0.5px;

    &::placeholder {
      color: rgba(255, 255, 255, 0.3);
    }
  }

  //浏览器自动填充会把背景刷成亮黄色，用大面积 inset 阴影盖回深色
  input:-webkit-autofill {
    -webkit-box-shadow: 0 0 0px 1000px #141b3a inset !important;
    -webkit-text-fill-color: #eef1f8 !important;
    caret-color: #eef1f8;
    border-radius: 0;
  }

  .pwd-eye {
    cursor: pointer;
    margin-right: 0;
    font-size: 16px;
    color: rgba(255, 255, 255, 0.4);
    transition: color 0.2s;

    &:hover {
      color: #a5b4fc;
    }
  }
}
.login-btn {
  position: relative;
  width: 100%;
  height: 50px;
  font-size: 15px;
  font-weight: 500;
  letter-spacing: 6px;
  border-radius: 12px;
  margin-top: 10px;
  border: 0;
  overflow: hidden;
  //三段渐变让按钮中间偏亮，比两端渐变更有体积感
  background: linear-gradient(135deg, @c1 0%, #7c6cf3 50%, @c2 100%);
  box-shadow: 0 12px 26px rgba(99, 102, 241, 0.38);
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover,
  &:focus {
    background: linear-gradient(135deg, @c1 0%, #7c6cf3 50%, @c2 100%);
    transform: translateY(-2px);
    box-shadow: 0 16px 32px rgba(99, 102, 241, 0.5);
  }

  &:active {
    transform: translateY(0);
    box-shadow: 0 8px 18px rgba(99, 102, 241, 0.35);
  }

  //hover 时一束高光扫过，纯 css 无额外元素
  &::after {
    content: "";
    position: absolute;
    top: 0;
    left: -60%;
    width: 40%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.28), transparent);
    transform: skewX(-20deg);
    transition: left 0.55s ease;
  }

  &:hover::after {
    left: 120%;
  }
}
//窄屏：缩小卡片留白；装饰元素与持续动画一并砍掉(小屏没空间，且移动端持续动画费电)
@media screen and (max-width: 900px) {
  .app-lang {
    right: 20px;
    top: 16px;
  }

  .card-head {
    margin-bottom: 22px;

    .brand-mark {
      width: 42px;
      height: 42px;
      margin-right: 12px;

      i {
        font-size: 20px;
      }
    }
  }

  .login-card {
    padding: 28px 22px;
  }

  .bg-glow,
  .aurora-band,
  .ring {
    animation: none;
  }

  //光点和光环在窄屏上只会挤在卡片边上碍眼
  .bg-particles,
  .bg-rings {
    display: none;
  }
}

//系统设置了"减少动态效果"时关掉所有动画(无障碍)
@media (prefers-reduced-motion: reduce) {

  .bg-glow,
  .aurora-band,
  .ring,
  .login-box {
    animation: none !important;
  }

  //光点靠动画才可见(初始 opacity:0)；边框流星停下来会变成"边上挂着一段白光"，
  //两者都不适合静态呈现，索性不渲染
  .bg-particles,
  .card-beam {
    display: none;
  }
}
</style>
