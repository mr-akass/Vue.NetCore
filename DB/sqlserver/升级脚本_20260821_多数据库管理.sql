-- =============================================================
-- Sys_DbConnection 多数据库管理 - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-08-21
-- 内容:
--   1. 创建 Sys_DbConnection 数据库连接表(界面上新增连接后落库,启动时自动注册到SqlSugar)
--   2. 字典 dbServer 改为从 Sys_DbConnection 取数(代码生成器/字典的"所在数据库"下拉自动同步)
--   3. "数据库管理"菜单(只给查询/新建/编辑权限,不给删除)
-- 说明:
--   - 脚本可重复执行(幂等)
--   - ConnName 即 SqlSugar 的 ConfigId,也是 Sys_TableInfo.DBServer / Sys_Dictionary.DBServer 中保存的值
--   - ConnectionString 落库时按 Secret.DB 做 DES 加密,界面上回显时密码会被掩码
--   - 有意不提供删除:连接被实体/字典/代码生成器引用,删掉会导致这些功能直接报错
--     (需要停用某个库时把 Enabled 置0,已有引用仍会回退到默认库而不是崩溃)
-- =============================================================

-- 1. Sys_DbConnection 表
IF OBJECT_ID('Sys_DbConnection', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_DbConnection (
        ID INT PRIMARY KEY IDENTITY(1,1),
        ConnName NVARCHAR(50) NOT NULL,
        DBType NVARCHAR(20) NOT NULL,
        ConnectionString NVARCHAR(1000) NOT NULL,
        Remark NVARCHAR(200) NULL,
        Enabled BIT NOT NULL DEFAULT 1,
        CreateID INT NULL,
        Creator NVARCHAR(255) NULL,
        CreateDate DATETIME NULL DEFAULT GETDATE(),
        ModifyID INT NULL,
        Modifier NVARCHAR(255) NULL,
        ModifyDate DATETIME NULL
    );
    CREATE UNIQUE INDEX UX_Sys_DbConnection_Name ON Sys_DbConnection(ConnName);
    PRINT 'Sys_DbConnection created';
END
ELSE
    PRINT 'Sys_DbConnection exists, skip';
GO

-- 2. 字典 dbServer 改为 sql 取数(原来是手工维护的字典明细,新增库后还要手动加一行)
IF EXISTS (SELECT 1 FROM Sys_Dictionary WHERE DicNo = 'dbServer')
BEGIN
    UPDATE Sys_Dictionary
    SET DbSql = 'SELECT ConnName AS [key],ConnName AS [value] FROM Sys_DbConnection WHERE Enabled=1 ORDER BY ConnName'
    WHERE DicNo = 'dbServer' AND ISNULL(DbSql, '') = '';
    PRINT 'dictionary dbServer DbSql updated';
END
ELSE
BEGIN
    INSERT INTO Sys_Dictionary (DicNo, DicName, ParentId, Config, DbSql, OrderNo, Remark, Enable, Creator, CreateDate)
    VALUES ('dbServer', N'dbServer', 0, NULL,
            'SELECT ConnName AS [key],ConnName AS [value] FROM Sys_DbConnection WHERE Enabled=1 ORDER BY ConnName',
            997, N'db-connection-source', 1, 'admin', GETDATE());
    PRINT 'dictionary dbServer created';
END
GO

-- 3. "数据库管理"菜单：挂到与"数据字典"(Sys_Dictionary)相同的父级下，Auth不含Delete
--    Auth里有中文，字符串字面量必须加N前缀，否则会被写成"??"
IF NOT EXISTS (SELECT 1 FROM Sys_Menu WHERE TableName = 'Sys_DbConnection')
BEGIN
    DECLARE @parentId INT = (SELECT TOP 1 ParentId FROM Sys_Menu WHERE TableName = 'Sys_Dictionary');
    IF @parentId IS NULL SET @parentId = 0;
    INSERT INTO Sys_Menu (ParentId, MenuName, TableName, Url, Auth, Icon, OrderNo, Enable, MenuType, Creator, CreateDate)
    VALUES (@parentId, N'数据库管理', 'Sys_DbConnection', '/Sys_DbConnection',
            N'[{"text":"查询","value":"Search"},{"text":"新建","value":"Add"},{"text":"编辑","value":"Update"}]',
            'el-icon-coin', 890, 1, 0, 'admin', GETDATE());
    PRINT 'menu Sys_DbConnection created';
END
ELSE
BEGIN
    --修正早期脚本(Auth缺N前缀)写坏的中文按钮名
    UPDATE Sys_Menu
    SET Auth = N'[{"text":"查询","value":"Search"},{"text":"新建","value":"Add"},{"text":"编辑","value":"Update"}]'
    WHERE TableName = 'Sys_DbConnection' AND Auth LIKE '%?%';
    PRINT 'menu Sys_DbConnection exists, auth fixed if needed';
END
GO
