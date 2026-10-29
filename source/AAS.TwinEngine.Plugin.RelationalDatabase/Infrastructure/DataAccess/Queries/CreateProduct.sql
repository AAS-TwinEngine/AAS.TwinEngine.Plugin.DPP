-- Creates a product with user values in "Asset" and dummy rows in every related table.
-- Every insert selects FROM new_asset so nothing is written when @IdShort already exists.
WITH next_product AS (
    SELECT '000-' || lpad((COALESCE(MAX(substring("ProductId" FROM 5)::INT), 0) + 1)::TEXT, 3, '0') AS "ProductId"
    FROM "Asset"
    WHERE "ProductId" ~ '^000-[0-9]{3}$'
),
new_asset AS (
    INSERT INTO "Asset" (
        "ProductId","IdShort","GlobalAssetId","AasId","AssetKind","AssetType",
        "ThumbnailContentType","ThumbnailPath","MaintenanceFreeAsset",
        "UriOfTheProduct","ManufacturerProductType","OrderCodeOfManufacturer","ProductArticleNumberOfManufacturer",
        "SerialNumber","YearOfConstruction","DateOfManufacture","HardwareVersion","FirmwareVersion","SoftwareVersion",
        "CountryOfOrigin","UniqueFacilityIdentifier",
        "AddressInformationStreet_en","AddressInformationStreet_de","AddressInformationZipcode_en",
        "AddressInformationZipcode_de","AddressInformationCityTown_en","AddressInformationCityTown_de",
        "AddressInformationNationalCode_en","AddressInformationNationalCode_de","ManufacturerName","ManufacturerProductDesignation_en",
        "ManufacturerProductDesignation_de","ManufacturerProductRoot_en","ManufacturerProductRoot_de",
        "ManufacturerProductFamily_en","ManufacturerProductFamily_de","CompanyLogo","ManufacturerArticleNumber",
        "ManufacturerOrderCode","ProductImage","ManufacturerLogo","TextStatement_en","TextStatement_de",
        "ValidDate","PcfCalculationMethod","LifeCyclePhase","PcfCO2eq","ReferenceImpactUnitForCalculation",
        "QuantityOfMeasureForCalculation","PublicationDate","ExpirationDate","ExplanatoryStatement"
    )
    SELECT
        np."ProductId",
        @IdShort,
        'https://mm-software.com/ids/assets/' || np."ProductId",
        'https://mm-software.com/ids/aas/' || np."ProductId",
        'Instance','Product','image/jpeg',
        'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=85',
        FALSE,'https://mm-software.com/products/' || np."ProductId",'Demo product type',
        'DEMO-ORDER-' || np."ProductId",'DEMO-ARTICLE-' || np."ProductId",'DEMO-SN-' || np."ProductId",
        EXTRACT(YEAR FROM @DateOfManufacture)::INT,
        @DateOfManufacture,
        'demo-1.0','demo-1.0','demo-1.0','DE','DEMO-FACILITY-01',
        'Demo Street 1','Demostrasse 1','10115','10115','Berlin','Berlin','DE','DE',
        @ManufacturerName,
        'Demo product','Demoprodukt','Demo product root','Demoprodukt-Stamm','Demo family','Demofamilie',
        'https://upload.wikimedia.org/wikipedia/commons/4/44/BMW.svg',
        @ManufacturerArticleNumber,
        'DEMO-ORDER-' || np."ProductId",
        'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=85',
        'https://upload.wikimedia.org/wikipedia/commons/4/44/BMW.svg',
        'Illustrative demonstration record created via API; values other than the provided fields are synthetic.',
        'Beispieldatensatz, per API erstellt; Werte ausser den angegebenen Feldern sind synthetisch.',
        (@DateOfManufacture + INTERVAL '10 years')::DATE,
        'ISO 14067 (illustrative estimate)','Cradle-to-gate (A1-A3)',
        @PcfCO2eq,
        @ReferenceImpactUnitForCalculation,
        1, now(), now() + INTERVAL '8 years',
        'Illustrative footprint estimate; not a verified manufacturer declaration.'
    FROM next_product np
    WHERE NOT EXISTS (SELECT 1 FROM "Asset" WHERE "IdShort" = @IdShort)
    RETURNING "Id", "ProductId", "IdShort", "AasId", "GlobalAssetId"
),
ins_specific_asset_ids AS (
    INSERT INTO "SpecificAssetIds" ("ProductId","Name","Value")
    SELECT na."ProductId", v."Name", v."Value"
    FROM new_asset na
    CROSS JOIN LATERAL (VALUES
        ('DemoSerialNumber', 'DEMO-SN-' || na."ProductId"),
        ('ModelYear', EXTRACT(YEAR FROM @DateOfManufacture)::INT::TEXT),
        ('Origin', 'API')
    ) AS v("Name", "Value")
),
ins_technical_property_area AS (
    INSERT INTO "TechnicalPropertyArea" ("AssetId","Index","Length","Width","Height")
    SELECT na."Id", 0, 1000, 500, 400 FROM new_asset na
),
ins_marking AS (
    INSERT INTO "Marking" ("Index","MarkingName","DesignationOfCertificateOrApproval","IssueDate","ExpiryDate","MarkingAdditionalText","MarkingFile")
    SELECT 0, 'CE marking - ' || na."IdShort", 'DEMO-CE-' || na."ProductId", CURRENT_DATE, (CURRENT_DATE + INTERVAL '10 years')::DATE,
           'Synthetic demonstration reference; not an actual certificate.', 'https://mm-software.com/'
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_marking AS (
    INSERT INTO "AssetMarking" ("AssetId","MarkingId")
    SELECT na."Id", m."Id" FROM new_asset na CROSS JOIN ins_marking m
),
ins_product_image AS (
    INSERT INTO "ProductImage" ("Id","Index","ImageFile","ImageNote_en","ImageNote_de")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM "ProductImage"), 0,
           'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&w=1200&q=85',
           'Representative product image; illustrative only.', 'Repraesentatives Produktbild; nur illustrativ.'
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_product_image AS (
    INSERT INTO "AssetProductImage" ("Id","AssetId","ProductImageId")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM "AssetProductImage"), na."Id", pi."Id"
    FROM new_asset na CROSS JOIN ins_product_image pi
),
ins_product_classification AS (
    INSERT INTO "ProductClassifications" (
        "Index","ClassificationSystem","ClassificationSystemVersion","ClassificationSystemUrl",
        "ProductClassId","ProductClassCodedName","ProductClassName_en","ProductClassName_de"
    )
    SELECT 0, 'UNSPSC', '26.0601', 'https://www.unspsc.org', '25101500', 'DEMO-PRODUCT-' || na."ProductId",
           'Demo product', 'Demoprodukt'
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_product_classification AS (
    INSERT INTO "AssetProductClassifications" ("AssetId","ProductClassificationsId")
    SELECT na."Id", pc."Id" FROM new_asset na CROSS JOIN ins_product_classification pc
),
ins_carbon_footprint AS (
    INSERT INTO "ProductOrSectorSpecificCarbonFootprint" (
        "AssetId","PcfCalculationMethod","PcfRuleOperator","PcfRuleName","PcfRuleVersion",
        "PcfRuleOnlineReference","PcfApiEndpoint","PcfApiQuery"
    )
    SELECT na."Id", 'ISO 14067 (illustrative estimate)', 'GHG Protocol Product Standard',
           'Product manufacturing footprint estimate', 'Demo 1.0', 'https://ghgprotocol.org/product-standard',
           NULL, '?scope=cradle-to-gate&unit=kgCO2e&status=illustrative'
    FROM new_asset na
),
ins_maintenance_instruction AS (
    INSERT INTO "MaintenanceInstructionsForSpecificInterval" (
        "Index","MaintenanceID","NameOfMaintenance_en","NameOfMaintenance_de",
        "SourceOfMaintenanceInstructions_en","SourceOfMaintenanceInstructions_de",
        "RelatedStandardsLawsRegulations_en","RelatedStandardsLawsRegulations_de",
        "SafetyRegulationsToBeObserved_en","SafetyRegulationsToBeObserved_de",
        "MaintenanceIntervalValue","MaintenanceIntervalUnit","FlowChartOfMaintenanceSteps",
        "NumberOfRequiredTechnicians","RequiredQualification_en","RequiredQualification_de",
        "ValueTotalEstimatedWorkingTime","UnitValueTotalEstimatedWorkingTime"
    )
    SELECT 0, 'MNT-' || na."ProductId", 'Annual inspection', 'Jaehrliche Inspektion',
           'Manufacturer service schedule', 'Wartungsplan des Herstellers',
           'Follow the product-specific service schedule', 'Produktspezifischen Wartungsplan beachten',
           'Secure the product before maintenance', 'Produkt vor der Wartung sichern',
           12, 'months', 'https://mm-software.com/', 1, 'Qualified technician', 'Qualifizierte Fachkraft', 1.5, 'hour'
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_maintenance_instruction AS (
    INSERT INTO "AssetMaintenanceInstruction" ("AssetId","MaintenanceInstructionId")
    SELECT na."Id", mi."Id" FROM new_asset na CROSS JOIN ins_maintenance_instruction mi
),
ins_alarm AS (
    INSERT INTO "Alarm" ("Index","AlarmName_en","AlarmName_de","WarningLimitRelativeValue","WarningLimitSeverity")
    SELECT 0, 'Scheduled service due', 'Faelliger planmaessiger Service', 90, 'Warning'
    FROM new_asset na
    RETURNING "Id"
),
ins_maintenance_instruction_alarm AS (
    INSERT INTO "MaintenanceInstructionAlarm" ("MaintenanceInstructionId","AlarmId")
    SELECT mi."Id", a."Id" FROM ins_maintenance_instruction mi CROSS JOIN ins_alarm a
),
ins_contact AS (
    INSERT INTO "ContactForMaintenanceAuthorization" (
        "Index","Company_en","Company_de","Department_en","Department_de","Title_en","Title_de",
        "AcademicTitle_en","AcademicTitle_de","NameOfContact_en","NameOfContact_de","FirstName_en","FirstName_de",
        "MiddleNames_en","MiddleNames_de","Street_en","Street_de","Zipcode_en","Zipcode_de",
        "CityTown_en","CityTown_de","NationalCode_en","NationalCode_de","StateCounty_en","StateCounty_de",
        "FurtherDetailsOfContact_en","FurtherDetailsOfContact_de","RoleOfContactPerson"
    )
    SELECT 0, @ManufacturerName, @ManufacturerName, 'Service', 'Service', 'Mr', 'Herr', 'Dr', 'Dr',
           'Doe', 'Doe', 'John', 'John', '', '', 'Demo Street 1', 'Demostrasse 1', '10115', '10115',
           'Berlin', 'Berlin', 'DE', 'DE', 'Berlin', 'Berlin',
           'Synthetic demonstration contact', 'Synthetischer Beispielkontakt', 'Service technician'
    FROM new_asset na
    RETURNING "Id"
),
ins_email AS (
    INSERT INTO "Email" ("ContactForMaintenanceAuthorizationId","EmailAddress","TypeOfEmailAddress","PublicKey_en","PublicKey_de","TypeOfPublicKey_en","TypeOfPublicKey_de")
    SELECT c."Id", 'service@example.com', 'Office', NULL, NULL, NULL, NULL FROM ins_contact c
),
ins_phone AS (
    INSERT INTO "Phone" ("ContactForMaintenanceAuthorizationId","TelephoneNumber_en","TelephoneNumber_de","AvailableTime_en","AvailableTime_de","TypeOfTelephone")
    SELECT c."Id", '+49 30 000000', '+49 30 000000', 'Mon-Fri 08:00-17:00', 'Mo-Fr 08:00-17:00', 'Office' FROM ins_contact c
),
ins_fax AS (
    INSERT INTO "Fax" ("ContactForMaintenanceAuthorizationId","FaxNumber_en","FaxNumber_de","TypeOfFaxNumber")
    SELECT c."Id", '+49 30 000001', '+49 30 000001', 'Office' FROM ins_contact c
),
ins_maintenance_instruction_contact AS (
    INSERT INTO "MaintenanceInstructionContactForMaintenanceAuthorization" ("MaintenanceInstructionId","ContactForMaintenanceAuthorizationId")
    SELECT mi."Id", c."Id" FROM ins_maintenance_instruction mi CROSS JOIN ins_contact c
),
ins_maintenance_step AS (
    INSERT INTO "MaintenanceStep" (
        "Index","MaintenanceStepID","QuantityOfSparePartForMaintenanceStep",
        "QuantityOfConsumablesForMaintenanceStep","UnitForQuantityOfConsumablesForMaintenanceStep",
        "QuantityOfToolsForMaintenanceStep","DocumentationSignatureMandatory","EndOfMaintenance",
        "MaintenanceStepName_en","MaintenanceStepName_de","LocalizationDescription_en","LocalizationDescription_de",
        "InstructionMaintenanceStep_en","InstructionMaintenanceStep_de","ConditionForNextMaintenanceStep_en",
        "ConditionForNextMaintenanceStep_de","ConditionForAlternativeNextStep_en","ConditionForAlternativeNextStep_de",
        "RelatedDocumentOrFileMaintenanceStep","ValueEstimatedDurationTimeMaintenanceStep",
        "UnitEstimatedDurationTimeMaintenanceStep"
    )
    SELECT 0, 'STEP-' || na."ProductId", 1, 1, 'piece', 1, TRUE, TRUE,
           'Perform inspection', 'Inspektion durchfuehren', 'Service area', 'Servicebereich',
           'Inspect the product using the manufacturer procedure.', 'Produkt gemaess Herstellervorgabe pruefen.',
           'Inspection results are within specification.', 'Pruefergebnisse liegen innerhalb der Vorgaben.',
           'Stop and escalate any unsafe condition.', 'Bei unsicherem Zustand Arbeit stoppen und eskalieren.',
           'https://mm-software.com/', 30, 'minutes'
    FROM new_asset na
    RETURNING "Id"
),
ins_maintenance_instruction_step AS (
    INSERT INTO "MaintenanceInstructionsForSpecificIntervalMaintenanceStep" ("MaintenanceInstructionsForSpecificIntervalId","MaintenanceStepId")
    SELECT mi."Id", s."Id" FROM ins_maintenance_instruction mi CROSS JOIN ins_maintenance_step s
),
ins_maintenance_tool AS (
    INSERT INTO "MaintenanceTool" (
        "Index","ToolID","OrderCodeToolOfManufacturer","AddressOfAdditionalLinkTool","ToolName_en","ToolName_de",
        "CompanyNameToolSupplier_en","CompanyNameToolSupplier_de","ToolDescription_en","ToolDescription_de","MaxQuantityOfTool"
    )
    SELECT 0, 'TOOL-' || na."ProductId", 'DEMO-TOOL-001', 'https://mm-software.com/', 'Torque wrench', 'Drehmomentschluessel',
           'Demo Tools GmbH', 'Demo Tools GmbH', 'Standard torque wrench', 'Standard-Drehmomentschluessel', 1
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_maintenance_tool AS (
    INSERT INTO "AssetMaintenanceTool" ("AssetId","MaintenanceToolId")
    SELECT na."Id", t."Id" FROM new_asset na CROSS JOIN ins_maintenance_tool t
),
ins_maintenance_consumable AS (
    INSERT INTO "MaintenanceConsumable" (
        "Index","ConsumableID","UnitMaxQuantityOfConsumable","OrderCodeConsumableOfManufacturer","AddressOfAdditionalLinkConsumable",
        "ConsumableName_en","ConsumableName_de","CompanyNameSupplierConsumable_en","CompanyNameSupplierConsumable_de",
        "ConsumableDescription_en","ConsumableDescription_de","DisposalInstructionsForConsumable_en","DisposalInstructionsForConsumable_de",
        "QuantityOfConsumable"
    )
    SELECT 0, 'CONS-' || na."ProductId", 'litre', 'DEMO-CONS-001', 'https://mm-software.com/',
           'Cleaning agent', 'Reinigungsmittel', 'Demo Supplies GmbH', 'Demo Supplies GmbH',
           'General purpose cleaning agent', 'Allzweck-Reinigungsmittel',
           'Dispose of according to local regulations', 'Gemaess oertlichen Vorschriften entsorgen', 1
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_maintenance_consumable AS (
    INSERT INTO "AssetMaintenanceConsumable" ("AssetId","MaintenanceConsumableId")
    SELECT na."Id", c."Id" FROM new_asset na CROSS JOIN ins_maintenance_consumable c
),
ins_maintenance_spare_part AS (
    INSERT INTO "MaintenanceSparePart" (
        "Index","SparePartID","OrderCodeSparePartOfManufacturer","AddressOfAdditionalLinkSparePart",
        "SparePartName_en","SparePartName_de","CompanyNameSupplierSparePart_en","CompanyNameSupplierSparePart_de",
        "SparePartDescription_en","SparePartDescription_de","DisposalInstructionsForSparePart_en","DisposalInstructionsForSparePart_de",
        "QuantityOfSparePart"
    )
    SELECT 0, 'SPARE-' || na."ProductId", 'DEMO-SPARE-001', 'https://mm-software.com/',
           'Filter element', 'Filterelement', 'Demo Parts GmbH', 'Demo Parts GmbH',
           'Replacement filter element', 'Ersatz-Filterelement',
           'Dispose of according to local regulations', 'Gemaess oertlichen Vorschriften entsorgen', 1
    FROM new_asset na
    RETURNING "Id"
),
ins_asset_maintenance_spare_part AS (
    INSERT INTO "AssetMaintenanceSparePart" ("AssetId","MaintenanceSparePartId")
    SELECT na."Id", sp."Id" FROM new_asset na CROSS JOIN ins_maintenance_spare_part sp
),
ins_document AS (
    INSERT INTO "Document" ("Index")
    SELECT 0 FROM new_asset na
    RETURNING "Id"
),
ins_asset_document AS (
    INSERT INTO "AssetDocument" ("AssetId","DocumentId")
    SELECT na."Id", d."Id" FROM new_asset na CROSS JOIN ins_document d
),
ins_document_identifier AS (
    INSERT INTO "DocumentId" ("Index","DocumentDomainId","DocumentIdentifier","DocumentIsPrimary")
    SELECT 0, gen_random_uuid(), 'OWNER-GUIDE-' || na."ProductId", TRUE FROM new_asset na
    RETURNING "Id"
),
ins_document_document_id AS (
    INSERT INTO "DocumentDocumentId" ("DocumentId","DocumentIdentifierId")
    SELECT d."Id", di."Id" FROM ins_document d CROSS JOIN ins_document_identifier di
),
ins_document_classification AS (
    INSERT INTO "DocumentClassification" ("Index","ClassId","ClassificationSystem","ClassName_en","ClassName_de")
    SELECT 0, 'PRODUCT-OWNER-MANUAL', 'DEMO', 'Product owner information', 'Produkt-Besitzerinformationen' FROM new_asset na
    RETURNING "Id"
),
ins_document_document_classification AS (
    INSERT INTO "DocumentDocumentClassification" ("DocumentId","DocumentClassificationId")
    SELECT d."Id", dc."Id" FROM ins_document d CROSS JOIN ins_document_classification dc
),
ins_document_version AS (
    INSERT INTO "DocumentVersion" (
        "Index","DigitalFile","Version","StatusSetDate","StatusValue","OrganizationShortName",
        "OrganizationOfficialName","Title_en","Title_de","Subtitle_en","Subtitle_de","Description_en","Description_de",
        "KeyWords_en","KeyWords_de","PreviewFile"
    )
    SELECT 0, 'https://mm-software.com/', 'Demo 1.0', CURRENT_DATE, 'Illustrative',
           @ManufacturerName, @ManufacturerName,
           na."IdShort" || ' - owner guide reference (demo)', na."IdShort" || ' - Referenz zur Betriebsanleitung (Demo)',
           'Synthetic demonstration metadata', 'Synthetische Beispieldaten',
           'Demonstration reference for owner information.', 'Beispielreferenz fuer Besitzerinformationen.',
           'owner guide, demo', 'Betriebsanleitung, Demo', 'https://mm-software.com/'
    FROM new_asset na
    RETURNING "Id"
),
ins_document_document_version AS (
    INSERT INTO "DocumentDocumentVersion" ("DocumentId","DocumentVersionId")
    SELECT d."Id", dv."Id" FROM ins_document d CROSS JOIN ins_document_version dv
),
ins_language AS (
    INSERT INTO "Languages" ("Index","Language")
    SELECT 0, 'en' FROM new_asset na
    RETURNING "Id"
),
ins_document_version_language AS (
    INSERT INTO "DocumentVersionLanguages" ("DocumentVersionId","LanguageId")
    SELECT dv."Id", l."Id" FROM ins_document_version dv CROSS JOIN ins_language l
)
SELECT json_build_object(
    'productId', na."ProductId",
    'idShort', na."IdShort",
    'aasId', na."AasId",
    'globalAssetId', na."GlobalAssetId"
)::TEXT
FROM new_asset na;
