-- =============================================
-- 2026-08-24 Grid quick copy support
-- Add QuickCopy column to Sys_TableColumn:
--   0/NULL = no copy icon (default)
--   1      = show copy icon after the cell value in grid
-- Executed on DEV(vol_v3): 2026-08-24
-- STG/PRD: NOT executed yet
-- =============================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Sys_TableColumn') AND name = N'QuickCopy'
)
BEGIN
    ALTER TABLE dbo.Sys_TableColumn ADD QuickCopy INT NULL;
END
GO
