-- =============================================================
-- 多角色支持 - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-07-31
-- 内容:
--   1. 创建用户角色中间表 Sys_UserRole (一个用户可分配多个角色)
--   2. 将现有用户的主角色(Sys_User.Role_Id)初始化到 Sys_UserRole
-- 说明:
--   - 脚本可重复执行(幂等)
--   - 权限 = 用户所有启用角色的权限并集(Sys_UserRole.Enable=1 的角色 ∪ 主角色 Role_Id)
--   - 移除角色时 Enable 置 0 软删除
-- =============================================================

-- 1. 创建 Sys_UserRole 表
IF OBJECT_ID('Sys_UserRole', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_UserRole (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        Enable TINYINT NOT NULL DEFAULT 1,
        CreateID INT NULL,
        Creator NVARCHAR(255) NULL,
        CreateDate DATETIME NULL DEFAULT GETDATE(),
        ModifyID INT NULL,
        Modifier NVARCHAR(255) NULL,
        ModifyDate DATETIME NULL
    );
    CREATE INDEX IX_Sys_UserRole_UserId ON Sys_UserRole(UserId);
    CREATE INDEX IX_Sys_UserRole_RoleId ON Sys_UserRole(RoleId);
    PRINT 'Sys_UserRole 表已创建';
END
ELSE
    PRINT 'Sys_UserRole 表已存在，跳过';
GO

-- 2. 现有用户的主角色初始化到 Sys_UserRole (只补缺失的)
INSERT INTO Sys_UserRole (UserId, RoleId, Enable, Creator, CreateDate)
SELECT u.User_Id, u.Role_Id, 1, 'system-migrate', GETDATE()
FROM Sys_User u
WHERE u.Role_Id > 0
  AND NOT EXISTS (SELECT 1 FROM Sys_UserRole ur WHERE ur.UserId = u.User_Id AND ur.RoleId = u.Role_Id);
PRINT '已将现有用户主角色初始化到 Sys_UserRole';
GO
