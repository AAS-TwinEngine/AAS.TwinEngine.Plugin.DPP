WITH asset_cte AS (
    SELECT "Id", "MaintenanceFreeAsset"
    FROM "Asset"
    WHERE "ProductId" = @ProductId
),
spare_parts_cte AS (
    SELECT
        amsp."AssetId",
        json_agg(
            json_build_object(
                'SparePartID',                         msp."SparePartID",
                'OrderCodeOfManufacturer',             msp."OrderCodeOfManufacturer",
                'AddressOfAdditionalLink',             msp."AddressOfAdditionalLink",
                'SparePartName_en',                    msp."SparePartName_en",
                'SparePartName_de',                    msp."SparePartName_de",
                'CompanyNameSupplierSparePart_en',     msp."CompanyNameSupplierSparePart_en",
                'CompanyNameSupplierSparePart_de',     msp."CompanyNameSupplierSparePart_de",
                'SparePartDescription_en',             msp."SparePartDescription_en",
                'SparePartDescription_de',             msp."SparePartDescription_de",
                'DisposalInstructionsForSparePart_en', msp."DisposalInstructionsForSparePart_en",
                'DisposalInstructionsForSparePart_de', msp."DisposalInstructionsForSparePart_de",
                'QuantityOfSparePart',                 msp."QuantityOfSparePart"
            )
        ) AS "MaintenanceSparePart"
    FROM asset_cte a
    JOIN "AssetMaintenanceSparePart" amsp
        ON amsp."AssetId" = a."Id"
    JOIN "MaintenanceSparePart" msp
        ON msp."Id" = amsp."MaintenanceSparePartId"
    GROUP BY amsp."AssetId"
),
consumables_cte AS (
    SELECT
        amc."AssetId",
        json_agg(
            json_build_object(
                'ConsumableID',                         mc."ConsumableID",
                'UnitMaxQuantityOfConsumable',          mc."UnitMaxQuantityOfConsumable",
                'OrderCodeOfManufacturer',              mc."OrderCodeOfManufacturer",
                'AddressOfAdditionalLink',              mc."AddressOfAdditionalLink",
                'ConsumableName_en',                    mc."ConsumableName_en",
                'ConsumableName_de',                    mc."ConsumableName_de",
                'CompanyNameSupplierConsumable_en',     mc."CompanyNameSupplierConsumable_en",
                'CompanyNameSupplierConsumable_de',     mc."CompanyNameSupplierConsumable_de",
                'ConsumableDescription_en',             mc."ConsumableDescription_en",
                'ConsumableDescription_de',             mc."ConsumableDescription_de",
                'DisposalInstructionsForConsumable_en', mc."DisposalInstructionsForConsumable_en",
                'DisposalInstructionsForConsumable_de', mc."DisposalInstructionsForConsumable_de",
                'QuantityOfConsumable',                 mc."QuantityOfConsumable"
            )
        ) AS "MaintenanceConsumable"
    FROM asset_cte a
    JOIN "AssetMaintenanceConsumable" amc
        ON amc."AssetId" = a."Id"
    JOIN "MaintenanceConsumable" mc
        ON mc."Id" = amc."MaintenanceConsumableId"
    GROUP BY amc."AssetId"
),
tools_cte AS (
    SELECT
        amt."AssetId",
        json_agg(
            json_build_object(
                'ToolID',                     mt."ToolID",
                'OrderCodeOfManufacturer',    mt."OrderCodeOfManufacturer",
                'AddressOfAdditionalLink',    mt."AddressOfAdditionalLink",
                'ToolName_en',                mt."ToolName_en",
                'ToolName_de',                mt."ToolName_de",
                'CompanyNameToolSupplier_en', mt."CompanyNameToolSupplier_en",
                'CompanyNameToolSupplier_de', mt."CompanyNameToolSupplier_de",
                'ToolDescription_en',         mt."ToolDescription_en",
                'ToolDescription_de',         mt."ToolDescription_de",
                'MaxQuantityOfTool',          mt."MaxQuantityOfTool"
            )
        ) AS "MaintenanceTool"
    FROM asset_cte a
    JOIN "AssetMaintenanceTool" amt
        ON amt."AssetId" = a."Id"
    JOIN "MaintenanceTool" mt
        ON mt."Id" = amt."MaintenanceToolId"
    GROUP BY amt."AssetId"
),
instruction_base_cte AS (
    SELECT
        ami."AssetId",
        mi."Id",
        mi."MaintenanceID",
        mi."NameOfMaintenance_en",
        mi."NameOfMaintenance_de",
        mi."SourceOfMaintenanceInstructions_en",
        mi."SourceOfMaintenanceInstructions_de",
        mi."RelatedStandardsLawsRegulations_en",
        mi."RelatedStandardsLawsRegulations_de",
        mi."SafetyRegulationsToBeObserved_en",
        mi."SafetyRegulationsToBeObserved_de",
        mi."MaintenanceIntervalValue",
        mi."MaintenanceIntervalUnit",
        mi."FlowChartOfMaintenanceSteps",
        mi."NumberOfRequiredTechnicians",
        mi."RequiredQualification_en",
        mi."RequiredQualification_de",
        mi."ValueTotalEstimatedWorkingTime",
        mi."UnitValueTotalEstimatedWorkingTime"
    FROM asset_cte a
    JOIN "AssetMaintenanceInstruction" ami
        ON ami."AssetId" = a."Id"
    JOIN "MaintenanceInstructionsForSpecificInterval" mi
        ON mi."Id" = ami."MaintenanceInstructionId"
),
instruction_alarms_cte AS (
    SELECT
        mia."MaintenanceInstructionId",
        json_agg(
            json_build_object(
                'AlarmName_en',              al."AlarmName_en",
                'AlarmName_de',              al."AlarmName_de",
                'WarningLimitRelativeValue', al."WarningLimitRelativeValue",
                'WarningLimitSeverity',      al."WarningLimitSeverity"
            )
        ) AS "Alarm"
    FROM instruction_base_cte ib
    JOIN "MaintenanceInstructionAlarm" mia
        ON mia."MaintenanceInstructionId" = ib."Id"
    JOIN "Alarm" al
        ON al."Id" = mia."AlarmId"
    GROUP BY mia."MaintenanceInstructionId"
),
instruction_contacts_cte AS (
    SELECT
        mic."MaintenanceInstructionId",
        json_agg(
            json_build_object(
                'Company_en',                 c."Company_en",
                'Company_de',                 c."Company_de",
                'Department_en',              c."Department_en",
                'Department_de',              c."Department_de",
                'Title_en',                   c."Title_en",
                'Title_de',                   c."Title_de",
                'AcademicTitle_en',           c."AcademicTitle_en",
                'AcademicTitle_de',           c."AcademicTitle_de",
                'NameOfContact_en',           c."NameOfContact_en",
                'NameOfContact_de',           c."NameOfContact_de",
                'FirstName_en',               c."FirstName_en",
                'FirstName_de',               c."FirstName_de",
                'MiddleNames_en',             c."MiddleNames_en",
                'MiddleNames_de',             c."MiddleNames_de",
                'Street_en',                  c."Street_en",
                'Street_de',                  c."Street_de",
                'Zipcode_en',                 c."Zipcode_en",
                'Zipcode_de',                 c."Zipcode_de",
                'CityTown_en',                c."CityTown_en",
                'CityTown_de',                c."CityTown_de",
                'NationalCode_en',            c."NationalCode_en",
                'NationalCode_de',            c."NationalCode_de",
                'StateCounty_en',             c."StateCounty_en",
                'StateCounty_de',             c."StateCounty_de",
                'FurtherDetailsOfContact_en', c."FurtherDetailsOfContact_en",
                'FurtherDetailsOfContact_de', c."FurtherDetailsOfContact_de",
                'RoleOfContactPerson',        c."RoleOfContactPerson",
                'Email', json_build_object(
                    'EmailAddress',       e."EmailAddress",
                    'TypeOfEmailAddress', e."TypeOfEmailAddress",
                    'PublicKey_en',       e."PublicKey_en",
                    'PublicKey_de',       e."PublicKey_de",
                    'TypeOfPublicKey_en', e."TypeOfPublicKey_en",
                    'TypeOfPublicKey_de', e."TypeOfPublicKey_de"
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
            )
        ) AS "ContactForMaintenanceAuthorization"
    FROM instruction_base_cte ib
    JOIN "MaintenanceInstructionContactForMaintenanceAuthorization" mic
        ON mic."MaintenanceInstructionId" = ib."Id"
    JOIN "ContactForMaintenanceAuthorization" c
        ON c."Id" = mic."ContactForMaintenanceAuthorizationId"
    LEFT JOIN "Email" e
        ON e."ContactForMaintenanceAuthorizationId" = c."Id"
    LEFT JOIN "Phone" p
        ON p."ContactForMaintenanceAuthorizationId" = c."Id"
    LEFT JOIN "Fax" f
        ON f."ContactForMaintenanceAuthorizationId" = c."Id"
    GROUP BY mic."MaintenanceInstructionId"
),
instruction_steps_cte AS (
    SELECT
        mims."MaintenanceInstructionsForSpecificIntervalId",
        json_agg(
            json_build_object(
                'MaintenanceStepID',                              ms."MaintenanceStepID",
                'QuantityOfSparePartForMaintenanceStep',          ms."QuantityOfSparePartForMaintenanceStep",
                'QuantityOfConsumablesForMaintenanceStep',        ms."QuantityOfConsumablesForMaintenanceStep",
                'UnitForQuantityOfConsumablesForMaintenanceStep', ms."UnitForQuantityOfConsumablesForMaintenanceStep",
                'QuantityOfToolsForMaintenanceStep',              ms."QuantityOfToolsForMaintenanceStep",
                'DocumentationSignatureMandatory',                ms."DocumentationSignatureMandatory",
                'EndOfMaintenance',                               ms."EndOfMaintenance",
                'MaintenanceStepName_en',                         ms."MaintenanceStepName_en",
                'MaintenanceStepName_de',                         ms."MaintenanceStepName_de",
                'LocalizationDescription_en',                     ms."LocalizationDescription_en",
                'LocalizationDescription_de',                     ms."LocalizationDescription_de",
                'InstructionMaintenanceStep_en',                  ms."InstructionMaintenanceStep_en",
                'InstructionMaintenanceStep_de',                  ms."InstructionMaintenanceStep_de",
                'ConditionForNextMaintenanceStep_en',             ms."ConditionForNextMaintenanceStep_en",
                'ConditionForNextMaintenanceStep_de',             ms."ConditionForNextMaintenanceStep_de",
                'ConditionForAlternativeNextStep_en',             ms."ConditionForAlternativeNextStep_en",
                'ConditionForAlternativeNextStep_de',             ms."ConditionForAlternativeNextStep_de",
                'RelatedDocumentOrFileMaintenanceStep',           ms."RelatedDocumentOrFileMaintenanceStep",
                'ValueEstimatedDurationTimeMaintenanceStep',      ms."ValueEstimatedDurationTimeMaintenanceStep",
                'UnitEstimatedDurationTimeMaintenanceStep',       ms."UnitEstimatedDurationTimeMaintenanceStep"
            )
        ) AS "MaintenanceStep"
    FROM instruction_base_cte ib
    JOIN "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" mims
        ON mims."MaintenanceInstructionsForSpecificIntervalId" = ib."Id"
    JOIN "MaintenanceStep" ms
        ON ms."Id" = mims."MaintenanceStepId"
    GROUP BY mims."MaintenanceInstructionsForSpecificIntervalId"
),
instructions_cte AS (
    SELECT
        ib."AssetId",
        json_agg(
            json_build_object(
                'MaintenanceID', ib."MaintenanceID",
                'NameOfMaintenance_en', ib."NameOfMaintenance_en",
                'NameOfMaintenance_de', ib."NameOfMaintenance_de",
                'SourceOfMaintenanceInstructions_en', ib."SourceOfMaintenanceInstructions_en",
                'SourceOfMaintenanceInstructions_de', ib."SourceOfMaintenanceInstructions_de",
                'RelatedStandardsLawsRegulations_en', ib."RelatedStandardsLawsRegulations_en",
                'RelatedStandardsLawsRegulations_de', ib."RelatedStandardsLawsRegulations_de",
                'SafetyRegulationsToBeObserved_en', ib."SafetyRegulationsToBeObserved_en",
                'SafetyRegulationsToBeObserved_de', ib."SafetyRegulationsToBeObserved_de",
                'MaintenanceIntervalValue', ib."MaintenanceIntervalValue",
                'MaintenanceIntervalUnit', ib."MaintenanceIntervalUnit",
                'FlowChartOfMaintenanceSteps', ib."FlowChartOfMaintenanceSteps",
                'NumberOfRequiredTechnicians', ib."NumberOfRequiredTechnicians",
                'RequiredQualification_en', ib."RequiredQualification_en",
                'RequiredQualification_de', ib."RequiredQualification_de",
                'ValueTotalEstimatedWorkingTime', ib."ValueTotalEstimatedWorkingTime",
                'UnitValueTotalEstimatedWorkingTime', ib."UnitValueTotalEstimatedWorkingTime",
                'Alarm', COALESCE(ia."Alarm", '[]'::json),
                'ContactForMaintenanceAuthorization', COALESCE(ic."ContactForMaintenanceAuthorization", '[]'::json),
                'MaintenanceStep', COALESCE(istep."MaintenanceStep", '[]'::json)
            )
        ) AS "MaintenanceInstructionsForSpecificInterval"
    FROM instruction_base_cte ib
    LEFT JOIN instruction_alarms_cte ia
        ON ia."MaintenanceInstructionId" = ib."Id"
    LEFT JOIN instruction_contacts_cte ic
        ON ic."MaintenanceInstructionId" = ib."Id"
    LEFT JOIN instruction_steps_cte istep
        ON istep."MaintenanceInstructionsForSpecificIntervalId" = ib."Id"
    GROUP BY ib."AssetId"
)

SELECT
COALESCE(
    json_build_object(
        'MaintenanceInstructions', json_build_object(
            'MaintenanceFreeAsset', a."MaintenanceFreeAsset",
            'MaintenanceSparePartList', json_build_object(
                'MaintenanceSparePart', COALESCE(sp."MaintenanceSparePart", '[]'::json)
            ),
            'MaintenanceConsumablesList', json_build_object(
                'MaintenanceConsumable', COALESCE(c."MaintenanceConsumable", '[]'::json)
            ),
            'MaintenanceToolList', json_build_object(
                'MaintenanceTool', COALESCE(t."MaintenanceTool", '[]'::json)
            ),
            'MaintenanceInstructionsForSpecificInterval', COALESCE(i."MaintenanceInstructionsForSpecificInterval", '[]'::json)
        )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a
LEFT JOIN spare_parts_cte sp
    ON sp."AssetId" = a."Id"
LEFT JOIN consumables_cte c
    ON c."AssetId" = a."Id"
LEFT JOIN tools_cte t
    ON t."AssetId" = a."Id"
LEFT JOIN instructions_cte i
    ON i."AssetId" = a."Id";
