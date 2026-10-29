WITH target AS MATERIALIZED (
    SELECT "Id" FROM "Asset" WHERE "ProductId" = @ProductId FOR UPDATE
),
instructions AS MATERIALIZED (
    SELECT DISTINCT link."MaintenanceInstructionId" AS "Id"
    FROM "AssetMaintenanceInstruction" link
    WHERE link."AssetId" IN (SELECT "Id" FROM target)
      AND NOT EXISTS (
          SELECT 1 FROM "AssetMaintenanceInstruction" other
          WHERE other."MaintenanceInstructionId" = link."MaintenanceInstructionId"
            AND other."AssetId" NOT IN (SELECT "Id" FROM target)
      )
),
documents AS MATERIALIZED (
    SELECT DISTINCT link."DocumentId" AS "Id" FROM "AssetDocument" link
    WHERE link."AssetId" IN (SELECT "Id" FROM target)
      AND NOT EXISTS (
          SELECT 1 FROM "AssetDocument" other
          WHERE other."DocumentId" = link."DocumentId"
            AND other."AssetId" NOT IN (SELECT "Id" FROM target)
      )
),
versions AS MATERIALIZED (
    SELECT DISTINCT link."DocumentVersionId" AS "Id" FROM "DocumentDocumentVersion" link
    WHERE link."DocumentId" IN (SELECT "Id" FROM documents)
      AND NOT EXISTS (
          SELECT 1 FROM "DocumentDocumentVersion" other
          WHERE other."DocumentVersionId" = link."DocumentVersionId"
            AND other."DocumentId" NOT IN (SELECT "Id" FROM documents)
      )
),
deleted_markings AS (
    DELETE FROM "Marking" item
    WHERE EXISTS (SELECT 1 FROM "AssetMarking" link WHERE link."MarkingId" = item."Id" AND link."AssetId" IN (SELECT "Id" FROM target))
      AND NOT EXISTS (SELECT 1 FROM "AssetMarking" link WHERE link."MarkingId" = item."Id" AND link."AssetId" NOT IN (SELECT "Id" FROM target))
    RETURNING "Id"
),
deleted_images AS (
    DELETE FROM "ProductImage" item
    WHERE EXISTS (SELECT 1 FROM "AssetProductImage" link WHERE link."ProductImageId" = item."Id" AND link."AssetId" IN (SELECT "Id" FROM target))
      AND NOT EXISTS (SELECT 1 FROM "AssetProductImage" link WHERE link."ProductImageId" = item."Id" AND link."AssetId" NOT IN (SELECT "Id" FROM target))
    RETURNING "Id"
),
deleted_classifications AS (
    DELETE FROM "ProductClassifications" item
    WHERE EXISTS (SELECT 1 FROM "AssetProductClassifications" link WHERE link."ProductClassificationsId" = item."Id" AND link."AssetId" IN (SELECT "Id" FROM target))
      AND NOT EXISTS (SELECT 1 FROM "AssetProductClassifications" link WHERE link."ProductClassificationsId" = item."Id" AND link."AssetId" NOT IN (SELECT "Id" FROM target))
    RETURNING "Id"
),
deleted_tools AS (
    DELETE FROM "MaintenanceTool" item
    WHERE EXISTS (SELECT 1 FROM "AssetMaintenanceTool" link WHERE link."MaintenanceToolId" = item."Id" AND link."AssetId" IN (SELECT "Id" FROM target))
      AND NOT EXISTS (SELECT 1 FROM "AssetMaintenanceTool" link WHERE link."MaintenanceToolId" = item."Id" AND link."AssetId" NOT IN (SELECT "Id" FROM target))
    RETURNING "Id"
),
deleted_consumables AS (
    DELETE FROM "MaintenanceConsumable" item
    WHERE EXISTS (SELECT 1 FROM "AssetMaintenanceConsumable" link WHERE link."MaintenanceConsumableId" = item."Id" AND link."AssetId" IN (SELECT "Id" FROM target))
      AND NOT EXISTS (SELECT 1 FROM "AssetMaintenanceConsumable" link WHERE link."MaintenanceConsumableId" = item."Id" AND link."AssetId" NOT IN (SELECT "Id" FROM target))
    RETURNING "Id"
),
deleted_spare_parts AS (
    DELETE FROM "MaintenanceSparePart" item
    WHERE EXISTS (SELECT 1 FROM "AssetMaintenanceSparePart" link WHERE link."MaintenanceSparePartId" = item."Id" AND link."AssetId" IN (SELECT "Id" FROM target))
      AND NOT EXISTS (SELECT 1 FROM "AssetMaintenanceSparePart" link WHERE link."MaintenanceSparePartId" = item."Id" AND link."AssetId" NOT IN (SELECT "Id" FROM target))
    RETURNING "Id"
),
deleted_alarms AS (
    DELETE FROM "Alarm" item
    WHERE EXISTS (SELECT 1 FROM "MaintenanceInstructionAlarm" link WHERE link."AlarmId" = item."Id" AND link."MaintenanceInstructionId" IN (SELECT "Id" FROM instructions))
      AND NOT EXISTS (SELECT 1 FROM "MaintenanceInstructionAlarm" link WHERE link."AlarmId" = item."Id" AND link."MaintenanceInstructionId" NOT IN (SELECT "Id" FROM instructions))
    RETURNING "Id"
),
deleted_contacts AS (
    DELETE FROM "ContactForMaintenanceAuthorization" item
    WHERE EXISTS (SELECT 1 FROM "MaintenanceInstructionContactForMaintenanceAuthorization" link WHERE link."ContactForMaintenanceAuthorizationId" = item."Id" AND link."MaintenanceInstructionId" IN (SELECT "Id" FROM instructions))
      AND NOT EXISTS (SELECT 1 FROM "MaintenanceInstructionContactForMaintenanceAuthorization" link WHERE link."ContactForMaintenanceAuthorizationId" = item."Id" AND link."MaintenanceInstructionId" NOT IN (SELECT "Id" FROM instructions))
    RETURNING "Id"
),
deleted_steps AS (
    DELETE FROM "MaintenanceStep" item
    WHERE EXISTS (SELECT 1 FROM "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" link WHERE link."MaintenanceStepId" = item."Id" AND link."MaintenanceInstructionsForSpecificIntervalId" IN (SELECT "Id" FROM instructions))
      AND NOT EXISTS (SELECT 1 FROM "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" link WHERE link."MaintenanceStepId" = item."Id" AND link."MaintenanceInstructionsForSpecificIntervalId" NOT IN (SELECT "Id" FROM instructions))
    RETURNING "Id"
),
deleted_instructions AS (
    DELETE FROM "MaintenanceInstructionsForSpecificInterval"
    WHERE "Id" IN (SELECT "Id" FROM instructions)
      AND (SELECT count(*) FROM deleted_alarms) >= 0
      AND (SELECT count(*) FROM deleted_contacts) >= 0
      AND (SELECT count(*) FROM deleted_steps) >= 0
    RETURNING "Id"
),
deleted_document_ids AS (
    DELETE FROM "DocumentId" item
    WHERE EXISTS (SELECT 1 FROM "DocumentDocumentId" link WHERE link."DocumentIdentifierId" = item."Id" AND link."DocumentId" IN (SELECT "Id" FROM documents))
      AND NOT EXISTS (SELECT 1 FROM "DocumentDocumentId" link WHERE link."DocumentIdentifierId" = item."Id" AND link."DocumentId" NOT IN (SELECT "Id" FROM documents))
    RETURNING "Id"
),
deleted_document_classifications AS (
    DELETE FROM "DocumentClassification" item
    WHERE EXISTS (SELECT 1 FROM "DocumentDocumentClassification" link WHERE link."DocumentClassificationId" = item."Id" AND link."DocumentId" IN (SELECT "Id" FROM documents))
      AND NOT EXISTS (SELECT 1 FROM "DocumentDocumentClassification" link WHERE link."DocumentClassificationId" = item."Id" AND link."DocumentId" NOT IN (SELECT "Id" FROM documents))
    RETURNING "Id"
),
deleted_languages AS (
    DELETE FROM "Languages" item
    WHERE EXISTS (SELECT 1 FROM "DocumentVersionLanguages" link WHERE link."LanguageId" = item."Id" AND link."DocumentVersionId" IN (SELECT "Id" FROM versions))
      AND NOT EXISTS (SELECT 1 FROM "DocumentVersionLanguages" link WHERE link."LanguageId" = item."Id" AND link."DocumentVersionId" NOT IN (SELECT "Id" FROM versions))
    RETURNING "Id"
),
deleted_versions AS (
    DELETE FROM "DocumentVersion" WHERE "Id" IN (SELECT "Id" FROM versions)
      AND (SELECT count(*) FROM deleted_languages) >= 0
    RETURNING "Id"
),
deleted_documents AS (
    DELETE FROM "Document" WHERE "Id" IN (SELECT "Id" FROM documents)
      AND (SELECT count(*) FROM deleted_document_ids) >= 0
      AND (SELECT count(*) FROM deleted_document_classifications) >= 0
      AND (SELECT count(*) FROM deleted_versions) >= 0
    RETURNING "Id"
),
deleted_asset AS (
    DELETE FROM "Asset" WHERE "Id" IN (SELECT "Id" FROM target)
      AND (SELECT count(*) FROM deleted_markings) >= 0
      AND (SELECT count(*) FROM deleted_images) >= 0
      AND (SELECT count(*) FROM deleted_classifications) >= 0
      AND (SELECT count(*) FROM deleted_tools) >= 0
      AND (SELECT count(*) FROM deleted_consumables) >= 0
      AND (SELECT count(*) FROM deleted_spare_parts) >= 0
      AND (SELECT count(*) FROM deleted_instructions) >= 0
      AND (SELECT count(*) FROM deleted_documents) >= 0
    RETURNING "ProductId"
)
SELECT "ProductId"::TEXT FROM deleted_asset;