-- =============================================================
-- 多数据库连接支持 - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-07-30
-- 内容:
--   1. Sys_TableInfo 增加 DBServer 列(代码生成器选择表所在数据库)
--   2. Sys_Dictionary 增加 DBServer 列(字典数据源所在数据库,通常已存在)
--   3. 新增字典 dbServer(字典管理/代码生成器中数据库下拉数据源)
-- 说明: 脚本可重复执行(幂等)
-- =============================================================

-- 1. Sys_TableInfo 增加 DBServer 列
IF COL_LENGTH('Sys_TableInfo', 'DBServer') IS NULL
BEGIN
    ALTER TABLE Sys_TableInfo ADD DBServer NVARCHAR(100) NULL;
    PRINT 'Sys_TableInfo.DBServer 列已添加';
END
ELSE
    PRINT 'Sys_TableInfo.DBServer 列已存在，跳过';
GO

-- 2. Sys_Dictionary 增加 DBServer 列(框架实体已有该字段，老库可能缺列)
IF COL_LENGTH('Sys_Dictionary', 'DBServer') IS NULL
BEGIN
    ALTER TABLE Sys_Dictionary ADD DBServer NVARCHAR(100) NULL;
    PRINT 'Sys_Dictionary.DBServer 列已添加';
END
ELSE
    PRINT 'Sys_Dictionary.DBServer 列已存在，跳过';
GO

-- 3. 新增字典 dbServer: 数据库连接名下拉数据源
--    字典项的"数据源key"必须与 appsettings.json 中 Connections 节点下的连接名一致
--    默认库不需要配置(留空即默认库)，这里的 Default 项仅作显式选择用
IF NOT EXISTS (SELECT 1 FROM Sys_Dictionary WHERE DicNo = 'dbServer')
BEGIN
    INSERT INTO Sys_Dictionary (DicNo, DicName, ParentId, Config, DbSql, OrderNo, Remark, Enable, Creator, CreateDate)
    VALUES ('dbServer', '数据库连接', 0, NULL, NULL, 1000, '多数据库连接名(与appsettings.json中Connections节点的连接名一致)', 1, 'admin', GETDATE());

    DECLARE @dicId INT = (SELECT Dic_ID FROM Sys_Dictionary WHERE DicNo = 'dbServer');

    INSERT INTO Sys_DictionaryList (Dic_ID, DicValue, DicName, OrderNo, Enable, Creator, CreateDate)
    VALUES (@dicId, 'Default', '默认数据库', 100, 1, 'admin', GETDATE());

    -- 按实际配置的 Connections 节点增加字典项，示例:
    -- INSERT INTO Sys_DictionaryList (Dic_ID, DicValue, DicName, OrderNo, Enable, Creator, CreateDate)
    -- VALUES (@dicId, 'ReportDB', '报表数据库', 90, 1, 'admin', GETDATE());
    -- INSERT INTO Sys_DictionaryList (Dic_ID, DicValue, DicName, OrderNo, Enable, Creator, CreateDate)
    -- VALUES (@dicId, 'ServiceDB', '业务数据库', 80, 1, 'admin', GETDATE());

    PRINT '字典 dbServer 已创建';
END
ELSE
    PRINT '字典 dbServer 已存在，跳过';
GO
