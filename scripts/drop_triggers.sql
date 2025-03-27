-- Save the current consumption data before dropping triggers
CREATE TABLE IF NOT EXISTS `backup_consumptions` AS SELECT * FROM `consumptions`;

-- Drop the triggers
DROP TRIGGER IF EXISTS `trg_documents_ai`;
DROP TRIGGER IF EXISTS `trg_documents_au`;
DROP TRIGGER IF EXISTS `trg_documents_ad`;

-- Keep the stored procedure for reference
-- It can be useful for ad-hoc updates or troubleshooting
SELECT 'Triggers removed successfully.' as message;
SELECT 'Stored procedure "update_company_consumption" kept for reference.' as message;
