-- =============================================
-- 2026-08-10 Code generator edit mode support
-- Add EditType column to Sys_TableInfo:
--   0/NULL = dialog edit (default)
--   1      = new tab page edit (newTabEdit)
--   2      = inline table edit (editTable)
-- Executed on DEV(vol_v3): 2026-08-10
-- STG/PRD: NOT executed yet
-- =============================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Sys_TableInfo') AND name = N'EditType'
)
BEGIN
    ALTER TABLE dbo.Sys_TableInfo ADD EditType INT NULL;
END
GO
