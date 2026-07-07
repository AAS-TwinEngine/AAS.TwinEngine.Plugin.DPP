WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = @ProductId
)

SELECT
COALESCE(

    json_build_object(

        'MaintenanceInstructions',         json_build_object(

                                                'MaintenanceFreeAsset',                     a."MaintenanceFreeAsset",

                                                'MaintenanceSparePartList',                 json_build_object(
                                                                                                 'MaintenanceSparePart', COALESCE(
                                                                                                                              (
                                                                                                                                  SELECT json_agg(
                                                                                                                                             json_build_object(

                                                                                                                                                 'SparePartID',                             msp."SparePartID",
                                                                                                                                                 'OrderCodeOfManufacturer',                 msp."OrderCodeOfManufacturer",
                                                                                                                                                 'AddressOfAdditionalLink',        msp."AddressOfAdditionalLink",

                                                                                                                                                 'SparePartName_en',                        msp."SparePartName_en",
                                                                                                                                                 'SparePartName_de',                        msp."SparePartName_de",

                                                                                                                                                 'CompanyNameSupplierSparePart_en',         msp."CompanyNameSupplierSparePart_en",
                                                                                                                                                 'CompanyNameSupplierSparePart_de',         msp."CompanyNameSupplierSparePart_de",

                                                                                                                                                 'SparePartDescription_en',                 msp."SparePartDescription_en",
                                                                                                                                                 'SparePartDescription_de',                 msp."SparePartDescription_de",

                                                                                                                                                 'DisposalInstructionsForSparePart_en',     msp."DisposalInstructionsForSparePart_en",
                                                                                                                                                 'DisposalInstructionsForSparePart_de',     msp."DisposalInstructionsForSparePart_de",

                                                                                                                                                 'QuantityOfSparePart',                     msp."QuantityOfSparePart"

                                                                                                                                             )
                                                                                                                                         )
                                                                                                                                  FROM "AssetMaintenanceSparePart" amsp
                                                                                                                                  JOIN "MaintenanceSparePart" msp
                                                                                                                                      ON msp."Id" = amsp."MaintenanceSparePartId"
                                                                                                                                  WHERE amsp."AssetId" = a."Id"
                                                                                                                              ),
                                                                                                                              '[]'::json
                                                                                                                          )
                                                                                             ),

                                                'MaintenanceConsumablesList',               json_build_object(
                                                                                                 'MaintenanceConsumable', COALESCE(
                                                                                                                               (
                                                                                                                                   SELECT json_agg(
                                                                                                                                              json_build_object(

                                                                                                                                                  'ConsumableID',                              mc."ConsumableID",
                                                                                                                                                  'UnitMaxQuantityOfConsumable',               mc."UnitMaxQuantityOfConsumable",
                                                                                                                                                  'OrderCodeOfManufacturer',                    mc."OrderCodeOfManufacturer",
                                                                                                                                                  'AddressOfAdditionalLink',                     mc."AddressOfAdditionalLink",

                                                                                                                                                  'ConsumableName_en',                         mc."ConsumableName_en",
                                                                                                                                                  'ConsumableName_de',                         mc."ConsumableName_de",

                                                                                                                                                  'CompanyNameSupplierConsumable_en',          mc."CompanyNameSupplierConsumable_en",
                                                                                                                                                  'CompanyNameSupplierConsumable_de',          mc."CompanyNameSupplierConsumable_de",

                                                                                                                                                  'ConsumableDescription_en',                  mc."ConsumableDescription_en",
                                                                                                                                                  'ConsumableDescription_de',                  mc."ConsumableDescription_de",

                                                                                                                                                  'DisposalInstructionsForConsumable_en',      mc."DisposalInstructionsForConsumable_en",
                                                                                                                                                  'DisposalInstructionsForConsumable_de',      mc."DisposalInstructionsForConsumable_de",

                                                                                                                                                  'QuantityOfConsumable',                      mc."QuantityOfConsumable"

                                                                                                                                              )
                                                                                                                                          )
                                                                                                                                   FROM "AssetMaintenanceConsumable" amc
                                                                                                                                   JOIN "MaintenanceConsumable" mc
                                                                                                                                       ON mc."Id" = amc."MaintenanceConsumableId"
                                                                                                                                   WHERE amc."AssetId" = a."Id"
                                                                                                                               ),
                                                                                                                               '[]'::json
                                                                                                                           )
                                                                                             ),

                                                'MaintenanceToolList',                      json_build_object(
                                                                                                 'MaintenanceTool', COALESCE(
                                                                                                                        (
                                                                                                                            SELECT json_agg(
                                                                                                                                       json_build_object(

                                                                                                                                           'ToolID',                             mt."ToolID",
                                                                                                                                           'OrderCodeOfManufacturer',            mt."OrderCodeOfManufacturer",
                                                                                                                                           'AddressOfAdditionalLink',            mt."AddressOfAdditionalLink",
                                                                                                                                           'ToolName_en',                        mt."ToolName_en",
                                                                                                                                           'ToolName_de',                        mt."ToolName_de",
                                                                                                                                           'CompanyNameToolSupplier_en',         mt."CompanyNameToolSupplier_en",
                                                                                                                                           'CompanyNameToolSupplier_de',         mt."CompanyNameToolSupplier_de",
                                                                                                                                           'ToolDescription_en',                 mt."ToolDescription_en",
                                                                                                                                           'ToolDescription_de',                 mt."ToolDescription_de",
                                                                                                                                           'MaxQuantityOfTool',                  mt."MaxQuantityOfTool"

                                                                                                                                       )
                                                                                                                                   )
                                                                                                                            FROM "AssetMaintenanceTool" amt
                                                                                                                            JOIN "MaintenanceTool" mt
                                                                                                                                ON mt."Id" = amt."MaintenanceToolId"
                                                                                                                            WHERE amt."AssetId" = a."Id"
                                                                                                                        ),
                                                                                                                        '[]'::json
                                                                                                                    )
                                                                                             ),

                                                'MaintenanceInstructionsForSpecificInterval', COALESCE (
                                                                                                                                      (
                                                                                                                                          SELECT json_agg(

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

                                                                                                                                                         'FlowChartOfMaintenanceSteps',              mi."FlowChartOfMaintenanceSteps",

                                                                                                                                                         'NumberOfRequiredTechnicians',              mi."NumberOfRequiredTechnicians",

                                                                                                                                                         'RequiredQualification_en',                 mi."RequiredQualification_en",
                                                                                                                                                         'RequiredQualification_de',                 mi."RequiredQualification_de",

                                                                                                                                                         'ValueTotalEstimatedWorkingTime',           mi."ValueTotalEstimatedWorkingTime",

                                                                                                                                                         'UnitValueTotalEstimatedWorkingTime',       mi."UnitValueTotalEstimatedWorkingTime",

                                                                                                                                                         'Alarm', COALESCE(
                                                                                                                                                                      (
                                                                                                                                                                          SELECT json_agg(
                                                                                                                                                                                     json_build_object(

                                                                                                                                                                                         'AlarmName_en',                al."AlarmName_en",
                                                                                                                                                                                         'AlarmName_de',                al."AlarmName_de",
                                                                                                                                                                                         'WarningLimitRelativeValue',   al."WarningLimitRelativeValue",
                                                                                                                                                                                         'WarningLimitSeverity',        al."WarningLimitSeverity"

                                                                                                                                                                                     )
                                                                                                                                                                                 )
                                                                                                                                                                          FROM "MaintenanceInstructionAlarm" mia
                                                                                                                                                                          JOIN "Alarm" al
                                                                                                                                                                              ON al."Id" = mia."AlarmId"
                                                                                                                                                                          WHERE mia."MaintenanceInstructionId" = mi."Id"
                                                                                                                                                                      ),
                                                                                                                                                                      '[]'::json
                                                                                                                                                                  ),

                                                                                                                                                         'ContactForMaintenanceAuthorization', COALESCE(
                                                                                                                                                                                                        (
                                                                                                                                                                                                            SELECT json_agg(

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
                                                                                                                                                                                                                                        'EmailAddress',            e."EmailAddress",
                                                                                                                                                                                                                                        'TypeOfEmailAddress',      e."TypeOfEmailAddress",
                                                                                                                                                                                                                                        'PublicKey_en',            e."PublicKey_en",
                                                                                                                                                                                                                                        'PublicKey_de',            e."PublicKey_de",
                                                                                                                                                                                                                                        'TypeOfPublicKey_en',      e."TypeOfPublicKey_en",
                                                                                                                                                                                                                                        'TypeOfPublicKey_de',      e."TypeOfPublicKey_de"
                                                                                                                                                                                                                                    ),

                                                                                                                                                                                                                           'Phone', json_build_object(
                                                                                                                                                                                                                                        'TelephoneNumber_en',      p."TelephoneNumber_en",
                                                                                                                                                                                                                                        'TelephoneNumber_de',      p."TelephoneNumber_de",
                                                                                                                                                                                                                                        'AvailableTime_en',        p."AvailableTime_en",
                                                                                                                                                                                                                                        'AvailableTime_de',        p."AvailableTime_de",
                                                                                                                                                                                                                                        'TypeOfTelephone',         p."TypeOfTelephone"
                                                                                                                                                                                                                                    ),

                                                                                                                                                                                                                           'Fax', json_build_object(
                                                                                                                                                                                                                                      'FaxNumber_en',            f."FaxNumber_en",
                                                                                                                                                                                                                                      'FaxNumber_de',            f."FaxNumber_de",
                                                                                                                                                                                                                                      'TypeOfFaxNumber',         f."TypeOfFaxNumber"
                                                                                                                                                                                                                                  )

                                                                                                                                                                                                                       )
                                                                                                                                                                                                                   )

                                                                                                                                                                                                            FROM "MaintenanceInstructionContactForMaintenanceAuthorization" mic
                                                                                                                                                                                                            JOIN "ContactForMaintenanceAuthorization" c
                                                                                                                                                                                                                ON c."Id" = mic."ContactForMaintenanceAuthorizationId"

                                                                                                                                                                                                            LEFT JOIN "Email" e
                                                                                                                                                                                                                ON e."ContactForMaintenanceAuthorizationId" = c."Id"

                                                                                                                                                                                                            LEFT JOIN "Phone" p
                                                                                                                                                                                                                ON p."ContactForMaintenanceAuthorizationId" = c."Id"

                                                                                                                                                                                                            LEFT JOIN "Fax" f
                                                                                                                                                                                                                ON f."ContactForMaintenanceAuthorizationId" = c."Id"

                                                                                                                                                                                                            WHERE mic."MaintenanceInstructionId" = mi."Id"
                                                                                                                                                                                                        ),
                                                                                                                                                                                                        '[]'::json
                                                                                                                                                                                                    ),

                                                                                                                                                         'MaintenanceStep', COALESCE(
                                                                                                                                                                                        (
                                                                                                                                                                                            SELECT json_agg(

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

                                                                                                                                                                                                       )

                                                                                                                                                                                                   )

                                                                                                                                                                                            FROM "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" mims
                                                                                                                                                                                            JOIN "MaintenanceStep" ms
                                                                                                                                                                                                ON ms."Id" = mims."MaintenanceStepId"

                                                                                                                                                                                            WHERE mims."MaintenanceInstructionsForSpecificIntervalId" = mi."Id"

                                                                                                                                                                                        ),
                                                                                                                                                                                        '[]'::json
                                                                                                                                                                                    )

                                                                                                                                                     )

                                                                                                                                                 )

                                                                                                                                          FROM "AssetMaintenanceInstruction" ami
                                                                                                                                          JOIN "MaintenanceInstructionsForSpecificInterval" mi
                                                                                                                                              ON mi."Id" = ami."MaintenanceInstructionId"

                                                                                                                                          WHERE ami."AssetId" = a."Id"

                                                                                                                                      ),
                                                                                                                                      '[]'::json
                                                                                                                                  
                                                                                               )
                                            )
    ),
    '{}'::json
) AS "Result"

FROM asset_cte a;
