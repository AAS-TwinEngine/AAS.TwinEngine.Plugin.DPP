-- Optimized version: same JSON output shape as the original, including the
-- Email/Phone/Fax cross-multiplication behavior on Contact (preserved as-is
-- since it's a plain LEFT JOIN, not an aggregate, in the original).
-- Each json_agg below is explicitly ordered by the child table's "Index"
-- column so array order is deterministic and consistent across the batch
-- (/data) and single-submodel (/data/{id}) endpoints -- without an ORDER BY,
-- Postgres row order for an aggregate is plan-dependent and can differ
-- between query shapes even for identical data.
--
-- If no requested Asset row is found, this returns an empty JSON object,
-- matching the original's no-data behavior (the original's
-- outer COALESCE to '{}'::json was effectively unreachable dead code).

WITH requested_products AS (
    SELECT unnest(@ProductIds::text[]) AS product_id
),
params AS (
    SELECT "ProductId" AS product_id, "Id" AS asset_id, "MaintenanceFreeAsset" AS maintenance_free_asset
    FROM "Asset" a
    INNER JOIN requested_products rp ON rp.product_id = a."ProductId"
),

-- MaintenanceSparePart[]
spare_part_agg AS (
    SELECT
        p.product_id,
        json_agg(
            json_build_object(
                'SparePartID',                         msp."SparePartID",
                'OrderCodeOfManufacturer',              msp."OrderCodeOfManufacturer",
                'AddressOfAdditionalLink',               msp."AddressOfAdditionalLink",
                'SparePartName_en',                     msp."SparePartName_en",
                'SparePartName_de',                     msp."SparePartName_de",
                'CompanyNameSupplierSparePart_en',      msp."CompanyNameSupplierSparePart_en",
                'CompanyNameSupplierSparePart_de',      msp."CompanyNameSupplierSparePart_de",
                'SparePartDescription_en',              msp."SparePartDescription_en",
                'SparePartDescription_de',              msp."SparePartDescription_de",
                'DisposalInstructionsForSparePart_en',  msp."DisposalInstructionsForSparePart_en",
                'DisposalInstructionsForSparePart_de',  msp."DisposalInstructionsForSparePart_de",
                'QuantityOfSparePart',                  msp."QuantityOfSparePart"
            ) ORDER BY msp."Index"
        ) AS spare_parts
    FROM params p
    INNER JOIN "AssetMaintenanceSparePart" amsp ON amsp."AssetId" = p.asset_id
    JOIN "MaintenanceSparePart" msp ON msp."Id" = amsp."MaintenanceSparePartId"
    GROUP BY p.product_id
),

-- MaintenanceConsumable[]
consumable_agg AS (
    SELECT
        p.product_id,
        json_agg(
            json_build_object(
                'ConsumableID',                          mc."ConsumableID",
                'UnitMaxQuantityOfConsumable',           mc."UnitMaxQuantityOfConsumable",
                'OrderCodeOfManufacturer',                mc."OrderCodeOfManufacturer",
                'AddressOfAdditionalLink',                 mc."AddressOfAdditionalLink",
                'ConsumableName_en',                      mc."ConsumableName_en",
                'ConsumableName_de',                      mc."ConsumableName_de",
                'CompanyNameSupplierConsumable_en',       mc."CompanyNameSupplierConsumable_en",
                'CompanyNameSupplierConsumable_de',       mc."CompanyNameSupplierConsumable_de",
                'ConsumableDescription_en',               mc."ConsumableDescription_en",
                'ConsumableDescription_de',               mc."ConsumableDescription_de",
                'DisposalInstructionsForConsumable_en',   mc."DisposalInstructionsForConsumable_en",
                'DisposalInstructionsForConsumable_de',   mc."DisposalInstructionsForConsumable_de",
                'QuantityOfConsumable',                   mc."QuantityOfConsumable"
            ) ORDER BY mc."Index"
        ) AS consumables
    FROM params p
    INNER JOIN "AssetMaintenanceConsumable" amc ON amc."AssetId" = p.asset_id
    JOIN "MaintenanceConsumable" mc ON mc."Id" = amc."MaintenanceConsumableId"
    GROUP BY p.product_id
),

-- MaintenanceTool[]
tool_agg AS (
    SELECT
        p.product_id,
        json_agg(
            json_build_object(
                'ToolID',                          mt."ToolID",
                'OrderCodeOfManufacturer',         mt."OrderCodeOfManufacturer",
                'AddressOfAdditionalLink',         mt."AddressOfAdditionalLink",
                'ToolName_en',                     mt."ToolName_en",
                'ToolName_de',                     mt."ToolName_de",
                'CompanyNameToolSupplier_en',      mt."CompanyNameToolSupplier_en",
                'CompanyNameToolSupplier_de',      mt."CompanyNameToolSupplier_de",
                'ToolDescription_en',              mt."ToolDescription_en",
                'ToolDescription_de',              mt."ToolDescription_de",
                'MaxQuantityOfTool',               mt."MaxQuantityOfTool"
            ) ORDER BY mt."Index"
        ) AS tools
    FROM params p
    INNER JOIN "AssetMaintenanceTool" amt ON amt."AssetId" = p.asset_id
    JOIN "MaintenanceTool" mt ON mt."Id" = amt."MaintenanceToolId"
    GROUP BY p.product_id
),

-- Maintenance instructions linked to this asset. Referenced multiple times
-- below, so Postgres will materialize it (computed once).
asset_mi AS (
    SELECT DISTINCT p.product_id, ami."MaintenanceInstructionId" AS mi_id
    FROM params p
    INNER JOIN "AssetMaintenanceInstruction" ami ON ami."AssetId" = p.asset_id
),

-- Alarm[] per maintenance instruction
alarm_agg AS (
    SELECT
        asset_mi.product_id,
        mia."MaintenanceInstructionId" AS mi_id,
        json_agg(
            json_build_object(
                'AlarmName_en',              al."AlarmName_en",
                'AlarmName_de',              al."AlarmName_de",
                'WarningLimitRelativeValue', al."WarningLimitRelativeValue",
                'WarningLimitSeverity',      al."WarningLimitSeverity"
            ) ORDER BY al."Index"
        ) AS alarms
    FROM "MaintenanceInstructionAlarm" mia
    JOIN asset_mi ON asset_mi.mi_id = mia."MaintenanceInstructionId"
    JOIN "Alarm" al ON al."Id" = mia."AlarmId"
    GROUP BY asset_mi.product_id, mia."MaintenanceInstructionId"
),

-- ContactForMaintenanceAuthorization[] per maintenance instruction.
-- NOTE: Email/Phone/Fax are LEFT JOINed (not aggregated), matching the
-- original -- a contact with multiple emails/phones/faxes will still
-- produce multiple array entries, same cross-product as before.
contact_agg AS (
    SELECT
        asset_mi.product_id,
        mic."MaintenanceInstructionId" AS mi_id,
        json_agg(
            json_build_object(
                'Company_en',                     c."Company_en",
                'Company_de',                     c."Company_de",
                'Department_en',                  c."Department_en",
                'Department_de',                  c."Department_de",
                'Title_en',                       c."Title_en",
                'Title_de',                       c."Title_de",
                'AcademicTitle_en',               c."AcademicTitle_en",
                'AcademicTitle_de',               c."AcademicTitle_de",
                'NameOfContact_en',               c."NameOfContact_en",
                'NameOfContact_de',               c."NameOfContact_de",
                'FirstName_en',                   c."FirstName_en",
                'FirstName_de',                   c."FirstName_de",
                'MiddleNames_en',                 c."MiddleNames_en",
                'MiddleNames_de',                 c."MiddleNames_de",
                'Street_en',                      c."Street_en",
                'Street_de',                      c."Street_de",
                'Zipcode_en',                     c."Zipcode_en",
                'Zipcode_de',                     c."Zipcode_de",
                'CityTown_en',                    c."CityTown_en",
                'CityTown_de',                    c."CityTown_de",
                'NationalCode_en',                c."NationalCode_en",
                'NationalCode_de',                c."NationalCode_de",
                'StateCounty_en',                 c."StateCounty_en",
                'StateCounty_de',                 c."StateCounty_de",
                'FurtherDetailsOfContact_en',     c."FurtherDetailsOfContact_en",
                'FurtherDetailsOfContact_de',     c."FurtherDetailsOfContact_de",
                'RoleOfContactPerson',            c."RoleOfContactPerson",
                'Email', json_build_object(
                    'EmailAddress',        e."EmailAddress",
                    'TypeOfEmailAddress',  e."TypeOfEmailAddress",
                    'PublicKey_en',        e."PublicKey_en",
                    'PublicKey_de',        e."PublicKey_de",
                    'TypeOfPublicKey_en',  e."TypeOfPublicKey_en",
                    'TypeOfPublicKey_de',  e."TypeOfPublicKey_de"
                ),
                'Phone', json_build_object(
                    'TelephoneNumber_en', p."TelephoneNumber_en",
                    'TelephoneNumber_de', p."TelephoneNumber_de",
                    'AvailableTime_en',   p."AvailableTime_en",
                    'AvailableTime_de',   p."AvailableTime_de",
                    'TypeOfTelephone',    p."TypeOfTelephone"
                ),
                'Fax', json_build_object(
                    'FaxNumber_en',    f."FaxNumber_en",
                    'FaxNumber_de',    f."FaxNumber_de",
                    'TypeOfFaxNumber', f."TypeOfFaxNumber"
                )
            ) ORDER BY c."Index"
        ) AS contacts
    FROM "MaintenanceInstructionContactForMaintenanceAuthorization" mic
    JOIN asset_mi ON asset_mi.mi_id = mic."MaintenanceInstructionId"
    JOIN "ContactForMaintenanceAuthorization" c ON c."Id" = mic."ContactForMaintenanceAuthorizationId"
    LEFT JOIN "Email" e ON e."ContactForMaintenanceAuthorizationId" = c."Id"
    LEFT JOIN "Phone" p ON p."ContactForMaintenanceAuthorizationId" = c."Id"
    LEFT JOIN "Fax" f ON f."ContactForMaintenanceAuthorizationId" = c."Id"
    GROUP BY asset_mi.product_id, mic."MaintenanceInstructionId"
),

-- MaintenanceStep[] per maintenance instruction
step_agg AS (
    SELECT
        asset_mi.product_id,
        mims."MaintenanceInstructionsForSpecificIntervalId" AS mi_id,
        json_agg(
            json_build_object(
                'MaintenanceStepID',                                ms."MaintenanceStepID",
                'QuantityOfSparePartForMaintenanceStep',            ms."QuantityOfSparePartForMaintenanceStep",
                'QuantityOfConsumablesForMaintenanceStep',          ms."QuantityOfConsumablesForMaintenanceStep",
                'UnitForQuantityOfConsumablesForMaintenanceStep',   ms."UnitForQuantityOfConsumablesForMaintenanceStep",
                'QuantityOfToolsForMaintenanceStep',                ms."QuantityOfToolsForMaintenanceStep",
                'DocumentationSignatureMandatory',                  ms."DocumentationSignatureMandatory",
                'EndOfMaintenance',                                 ms."EndOfMaintenance",
                'MaintenanceStepName_en',                           ms."MaintenanceStepName_en",
                'MaintenanceStepName_de',                           ms."MaintenanceStepName_de",
                'LocalizationDescription_en',                       ms."LocalizationDescription_en",
                'LocalizationDescription_de',                       ms."LocalizationDescription_de",
                'InstructionMaintenanceStep_en',                    ms."InstructionMaintenanceStep_en",
                'InstructionMaintenanceStep_de',                    ms."InstructionMaintenanceStep_de",
                'ConditionForNextMaintenanceStep_en',               ms."ConditionForNextMaintenanceStep_en",
                'ConditionForNextMaintenanceStep_de',               ms."ConditionForNextMaintenanceStep_de",
                'ConditionForAlternativeNextStep_en',               ms."ConditionForAlternativeNextStep_en",
                'ConditionForAlternativeNextStep_de',               ms."ConditionForAlternativeNextStep_de",
                'RelatedDocumentOrFileMaintenanceStep',             ms."RelatedDocumentOrFileMaintenanceStep",
                'ValueEstimatedDurationTimeMaintenanceStep',        ms."ValueEstimatedDurationTimeMaintenanceStep",
                'UnitEstimatedDurationTimeMaintenanceStep',         ms."UnitEstimatedDurationTimeMaintenanceStep"
            ) ORDER BY ms."Index"
        ) AS steps
    FROM "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" mims
    JOIN asset_mi ON asset_mi.mi_id = mims."MaintenanceInstructionsForSpecificIntervalId"
    JOIN "MaintenanceStep" ms ON ms."Id" = mims."MaintenanceStepId"
    GROUP BY asset_mi.product_id, mims."MaintenanceInstructionsForSpecificIntervalId"
),

-- MaintenanceInstructionsForSpecificInterval[] -- assembles Alarm/Contact/Step per instruction
mi_agg AS (
    SELECT
        am.product_id,
        json_agg(
            json_build_object(
                'MaintenanceID',                            mi."MaintenanceID",
                'NameOfMaintenance_en',                     mi."NameOfMaintenance_en",
                'NameOfMaintenance_de',                     mi."NameOfMaintenance_de",
                'SourceOfMaintenanceInstructions_en',       mi."SourceOfMaintenanceInstructions_en",
                'SourceOfMaintenanceInstructions_de',       mi."SourceOfMaintenanceInstructions_de",
                'RelatedStandardsLawsRegulations_en',       mi."RelatedStandardsLawsRegulations_en",
                'RelatedStandardsLawsRegulations_de',       mi."RelatedStandardsLawsRegulations_de",
                'SafetyRegulationsToBeObserved_en',         mi."SafetyRegulationsToBeObserved_en",
                'SafetyRegulationsToBeObserved_de',         mi."SafetyRegulationsToBeObserved_de",
                'MaintenanceIntervalValue',                 mi."MaintenanceIntervalValue",
                'MaintenanceIntervalUnit',                  mi."MaintenanceIntervalUnit",
                'FlowChartOfMaintenanceSteps',               mi."FlowChartOfMaintenanceSteps",
                'NumberOfRequiredTechnicians',               mi."NumberOfRequiredTechnicians",
                'RequiredQualification_en',                 mi."RequiredQualification_en",
                'RequiredQualification_de',                 mi."RequiredQualification_de",
                'ValueTotalEstimatedWorkingTime',           mi."ValueTotalEstimatedWorkingTime",
                'UnitValueTotalEstimatedWorkingTime',       mi."UnitValueTotalEstimatedWorkingTime",
                'Alarm',                                    COALESCE(aa.alarms, '[]'::json),
                'ContactForMaintenanceAuthorization',       COALESCE(ca.contacts, '[]'::json),
                'MaintenanceStep',                          COALESCE(sa.steps, '[]'::json)
            ) ORDER BY mi."Index"
        ) AS instructions
    FROM asset_mi am
    JOIN "MaintenanceInstructionsForSpecificInterval" mi ON mi."Id" = am.mi_id
    LEFT JOIN alarm_agg   aa ON aa.product_id = am.product_id AND aa.mi_id = mi."Id"
    LEFT JOIN contact_agg ca ON ca.product_id = am.product_id AND ca.mi_id = mi."Id"
    LEFT JOIN step_agg    sa ON sa.product_id = am.product_id AND sa.mi_id = mi."Id"
    GROUP BY am.product_id
)

SELECT COALESCE(json_object_agg(p.product_id, json_build_object(
        'MaintenanceInstructions', json_build_object(
            'MaintenanceFreeAsset',       p.maintenance_free_asset,
            'MaintenanceSparePartList',   json_build_object(
                                               'MaintenanceSparePart', COALESCE(spa.spare_parts, '[]'::json)
                                           ),
            'MaintenanceConsumablesList', json_build_object(
                                               'MaintenanceConsumable', COALESCE(cma.consumables, '[]'::json)
                                           ),
            'MaintenanceToolList',        json_build_object(
                                               'MaintenanceTool', COALESCE(tla.tools, '[]'::json)
                                           ),
            'MaintenanceInstructionsForSpecificInterval', COALESCE(mia.instructions, '[]'::json)
        )
    ) ORDER BY p.product_id),
    '{}'::json
) AS "Result"
FROM params p
LEFT JOIN spare_part_agg   spa ON spa.product_id = p.product_id
LEFT JOIN consumable_agg   cma ON cma.product_id = p.product_id
LEFT JOIN tool_agg         tla ON tla.product_id = p.product_id
LEFT JOIN mi_agg           mia ON mia.product_id = p.product_id;
