-- =============================================================
-- Sys_Application 多应用/子系统支持 - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-07-31
-- 内容:
--   1. 创建 Sys_Application 应用表
--   2. Sys_Role 增加 AppID 列(角色所属应用，0=未指定)
--   3. 新增数据字典 application(角色管理页"所属应用"下拉数据源，sql取自Sys_Application)
--   4. 种子数据: 默认应用(AppID=1)
--   5. "应用管理"菜单(挂在与Sys_Role菜单相同的父级下)
-- 说明:
--   - 脚本可重复执行(幂等)
--   - 约定: 应用的 AppName 必须与该应用的一级菜单 MenuName 一致，
--     按应用加载菜单时该一级菜单会被隐藏、其子菜单提升为一级
--   - 老角色 AppID=0: 不贡献任何应用，用户登录后如无任何应用权限会提示联系管理员
-- =============================================================

-- 1. Sys_Application 表
IF OBJECT_ID('Sys_Application', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_Application (
        AppID INT PRIMARY KEY IDENTITY(1,1),
        AppCode NVARCHAR(50) NOT NULL,
        AppName NVARCHAR(100) NOT NULL,
        Title NVARCHAR(200) NULL,
        Icon NVARCHAR(100) NULL,
        Theme NVARCHAR(50) NULL,
        PrimaryColor NVARCHAR(20) NULL,
        DataPanel NVARCHAR(100) NULL,
        SortOrder INT NULL DEFAULT 0,
        Enabled BIT NULL DEFAULT 1,
        CreateID INT NULL,
        Creator NVARCHAR(255) NULL,
        CreateDate DATETIME NULL DEFAULT GETDATE(),
        ModifyID INT NULL,
        Modifier NVARCHAR(255) NULL,
        ModifyDate DATETIME NULL
    );
    PRINT 'Sys_Application created';
END
ELSE
    PRINT 'Sys_Application exists, skip';
GO

-- 2. Sys_Role 增加 AppID 列
IF COL_LENGTH('Sys_Role', 'AppID') IS NULL
BEGIN
    ALTER TABLE Sys_Role ADD AppID INT NOT NULL DEFAULT 0;
    PRINT 'Sys_Role.AppID added';
END
ELSE
    PRINT 'Sys_Role.AppID exists, skip';
GO

-- 3. 数据字典 application (所属应用下拉，数据来自Sys_Application)
IF NOT EXISTS (SELECT 1 FROM Sys_Dictionary WHERE DicNo = 'application')
BEGIN
    INSERT INTO Sys_Dictionary (DicNo, DicName, ParentId, Config, DbSql, OrderNo, Remark, Enable, Creator, CreateDate)
    VALUES ('application', N'application', 0, NULL,
            'SELECT AppID AS [key],AppName AS [value] FROM Sys_Application WHERE Enabled=1',
            999, N'app-select-source', 1, 'admin', GETDATE());
    PRINT 'dictionary application created';
END
ELSE
    PRINT 'dictionary application exists, skip';
GO

-- 4. 种子: 默认应用(名称对应现有一级菜单可后续在应用管理中修改)
IF NOT EXISTS (SELECT 1 FROM Sys_Application)
BEGIN
    INSERT INTO Sys_Application (AppCode, AppName, Title, Icon, Theme, PrimaryColor, DataPanel, SortOrder, Enabled, Creator, CreateDate)
    VALUES ('default', N'Default', N'Vol Development Framework', 'el-icon-menu', '', '#409eff', NULL, 1, 1, 'admin', GETDATE());
    PRINT 'default application seeded';
END
ELSE
    PRINT 'Sys_Application has data, skip seed';
GO

-- 5. "应用管理"菜单：挂到与"角色管理"(Sys_Role)相同的父级菜单下
IF NOT EXISTS (SELECT 1 FROM Sys_Menu WHERE TableName = 'Sys_Application')
BEGIN
    DECLARE @parentId INT = (SELECT TOP 1 ParentId FROM Sys_Menu WHERE TableName = 'Sys_Role');
    IF @parentId IS NULL SET @parentId = 0;
    INSERT INTO Sys_Menu (ParentId, MenuName, TableName, Url, Auth, Icon, OrderNo, Enable, MenuType, Creator, CreateDate)
    VALUES (@parentId, N'应用管理', 'Sys_Application', '/Sys_Application',
            '[{"text":"查询","value":"Search"},{"text":"新建","value":"Add"},{"text":"删除","value":"Delete"},{"text":"编辑","value":"Update"},{"text":"导出","value":"Export"}]',
            'el-icon-menu', 900, 1, 0, 'admin', GETDATE());
    PRINT 'menu Sys_Application created';
END
ELSE
    PRINT 'menu Sys_Application exists, skip';
GO

-- 6. (2026-08-03追加) 应用绑定根菜单(多个)：RootMenuIds 列(取代"AppName须与一级菜单同名"的隐式约定)
--    应用的菜单范围 = 多个根菜单子树的并集；公共子树可绑定到多个应用实现菜单共享
--    未绑定时仍回退按 AppName 同名匹配(兼容)
IF COL_LENGTH('Sys_Application', 'RootMenuIds') IS NULL
BEGIN
    ALTER TABLE Sys_Application ADD RootMenuIds NVARCHAR(200) NULL;
    PRINT 'Sys_Application.RootMenuIds added';
END
ELSE
    PRINT 'RootMenuIds exists, skip';
GO

-- 6.1 历史环境如存在单值 RootMenuId 列则迁移并删除(开发库已处理)
IF COL_LENGTH('Sys_Application', 'RootMenuId') IS NOT NULL
BEGIN
    EXEC('UPDATE Sys_Application SET RootMenuIds = CAST(RootMenuId AS NVARCHAR(20)) WHERE RootMenuId IS NOT NULL AND RootMenuId > 0 AND (RootMenuIds IS NULL OR RootMenuIds = '''')');
    EXEC('ALTER TABLE Sys_Application DROP COLUMN RootMenuId');
    PRINT 'migrated RootMenuId -> RootMenuIds';
END
GO

-- 7. (2026-08-03追加) 数据字典 rootmenu：应用管理页"根菜单"多选下拉数据源(所有一级菜单)
IF NOT EXISTS (SELECT 1 FROM Sys_Dictionary WHERE DicNo = 'rootmenu')
BEGIN
    INSERT INTO Sys_Dictionary (DicNo, DicName, ParentId, Config, DbSql, OrderNo, Remark, Enable, Creator, CreateDate)
    VALUES ('rootmenu', N'rootmenu', 0, NULL,
            'SELECT Menu_Id AS [key], MenuName AS [value] FROM Sys_Menu WHERE ParentId=0 AND (Enable=1 OR Enable=2) AND ISNULL(MenuType,0)=0',
            998, N'app-root-menu-source', 1, 'admin', GETDATE());
    PRINT 'dictionary rootmenu created';
END
ELSE
    PRINT 'dictionary rootmenu exists, skip';
GO
