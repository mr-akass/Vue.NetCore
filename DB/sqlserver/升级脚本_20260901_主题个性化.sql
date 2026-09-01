-- =============================================================
-- 主题个性化(颜色/效果/布局/字号/背景图) - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-09-01
-- 内容:
--   1. Sys_ThemeSetting  主题配置表(每个用户每个应用一套主题)
-- 设计说明:
--   * 键是 (UserId, AppId): 同一个用户在不同应用下可以是完全不同的主题,
--     切换应用时前端整页刷新会重新拉取(与 Sys_UserShortcut 的 UserId+AppId 同一套约定)
--   * UserId=0 表示"该应用的默认主题"(超管在设置面板里点[设为本应用默认]写入),
--     用户自己没有配置时前端用它渲染,所以新用户进来就是管理员定好的样子
--   * AppId=0 表示不区分应用(超管不带 appId 的全量菜单视角)
--   * 主题项全部塞在 ThemeJson 一列里: 颜色/效果/布局/密度/圆角/字号/遮罩这些开关
--     还会继续加,拆成列的话每加一个开关就要改表+改实体+改脚本,得不偿失
--   * BgImage 单独一列(json 里也有一份): 换图/删图时要按路径清理旧文件,
--     单列能直接查出来,不用把 json 解析一遍
-- 说明: 脚本可重复执行(幂等)
-- =============================================================

IF OBJECT_ID('Sys_ThemeSetting', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_ThemeSetting (
        ID INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        AppId INT NOT NULL DEFAULT 0,
        ThemeJson NVARCHAR(MAX) NULL,
        BgImage NVARCHAR(500) NULL,
        CreateDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifyDate DATETIME NULL
    );
    -- 一个用户在一个应用下只有一条主题记录(UserId=0 为该应用默认主题)
    CREATE UNIQUE INDEX UX_Sys_ThemeSetting_User_App
        ON Sys_ThemeSetting(UserId, AppId);
    PRINT 'Sys_ThemeSetting created';
END
ELSE
    PRINT 'Sys_ThemeSetting exists, skip';
GO

-- 主题设置没有独立菜单: 入口是右上角[基础设置]抽屉(所有登录用户可用),
-- 接口不加 [ApiActionPermission],与首页快捷导航同一套约定,因此无需插入 Sys_Menu 记录。
