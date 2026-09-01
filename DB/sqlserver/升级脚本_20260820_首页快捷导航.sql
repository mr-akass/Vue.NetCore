-- =============================================================
-- 首页快捷导航(用户自定义快捷菜单) - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-08-20
-- 内容:
--   1. Sys_UserShortcut  用户快捷菜单表(每个用户每个应用一组快捷项)
-- 设计说明:
--   * 只存 MenuId,不存 Url/Icon: 菜单改名/改地址后快捷项自动跟随;
--     用户被取消该菜单权限后前端按权限过滤,快捷项自动消失
--   * MenuName 为冗余列,仅便于直接查库排查,渲染以菜单权限为准
--     (与 Sys_MessageUser 同时存 UserId/UserName 的做法一致)
--   * AppId=0 表示不区分应用(超管不带 appId 的全量菜单视角)
-- 说明: 脚本可重复执行(幂等)
-- =============================================================

IF OBJECT_ID('Sys_UserShortcut', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_UserShortcut (
        ID INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        MenuId INT NOT NULL,
        MenuName NVARCHAR(100) NULL,
        AppId INT NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        CreateDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    -- 同一用户同一应用下不允许重复收藏同一菜单
    CREATE UNIQUE INDEX UX_Sys_UserShortcut_User_App_Menu
        ON Sys_UserShortcut(UserId, AppId, MenuId);
    -- 列表查询: 按用户+应用取,按排序号升序
    CREATE INDEX IX_Sys_UserShortcut_User_App_Sort
        ON Sys_UserShortcut(UserId, AppId, SortOrder);
    PRINT 'Sys_UserShortcut created';
END
ELSE
    PRINT 'Sys_UserShortcut exists, skip';
GO
