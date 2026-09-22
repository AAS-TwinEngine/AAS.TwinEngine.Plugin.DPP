WITH requested_products AS (
    SELECT unnest(@ProductIds::text[]) AS product_id
),
raw_data AS (
    SELECT
        a."ProductId" AS product_id,
        a.*,
        m."Id" AS "MarkingId",
        m."Index" AS "MarkingIndex",
        m."MarkingName",
        m."DesignationOfCertificateOrApproval",
        m."IssueDate",
        m."ExpiryDate",
        m."MarkingAdditionalText",
        m."MarkingFile"
    FROM "Asset" a
    LEFT JOIN "AssetMarking" am ON am."AssetId" = a."Id"
    LEFT JOIN "Marking" m ON m."Id" = am."MarkingId"
    INNER JOIN requested_products rp ON rp.product_id = a."ProductId"
),
product_results AS (
    SELECT DISTINCT ON ("ProductId")
        "ProductId" AS product_id,
        json_build_object(
            'nameplate', json_build_object(
                'UriOfTheProduct', d."UriOfTheProduct",
                'ManufacturerProductType', d."ManufacturerProductType",
                'OrderCodeOfManufacturer', d."OrderCodeOfManufacturer",
                'ProductArticleNumberOfManufacturer', d."ProductArticleNumberOfManufacturer",
                'SerialNumber', d."SerialNumber",
                'YearOfConstruction', d."YearOfConstruction",
                'DateOfManufacture', d."DateOfManufacture",
                'HardwareVersion', d."HardwareVersion",
                'FirmwareVersion', d."FirmwareVersion",
                'SoftwareVersion', d."SoftwareVersion",
                'CountryOfOrigin', d."CountryOfOrigin",
                'UniqueFacilityIdentifier', d."UniqueFacilityIdentifier",
                'ManufacturerName', d."ManufacturerName",
                'ManufacturerProductDesignation_en', d."ManufacturerProductDesignation_en",
                'ManufacturerProductDesignation_de', d."ManufacturerProductDesignation_de",
                'ManufacturerProductRoot_en', d."ManufacturerProductRoot_en",
                'ManufacturerProductRoot_de', d."ManufacturerProductRoot_de",
                'ManufacturerProductFamily_en', d."ManufacturerProductFamily_en",
                'ManufacturerProductFamily_de', d."ManufacturerProductFamily_de",
                'CompanyLogo', d."CompanyLogo",
                'Markings', json_build_object(
                    'Marking', COALESCE((
                        SELECT json_agg(json_build_object(
                            'MarkingName', x."MarkingName",
                            'DesignationOfCertificateOrApproval', x."DesignationOfCertificateOrApproval",
                            'IssueDate', x."IssueDate",
                            'ExpiryDate', x."ExpiryDate",
                            'MarkingAdditionalText', x."MarkingAdditionalText",
                            'MarkingFile', x."MarkingFile"
                        ) ORDER BY x."MarkingIndex")
                        FROM (
                            SELECT DISTINCT "MarkingId", "MarkingIndex", "MarkingName", "DesignationOfCertificateOrApproval",
                                            "IssueDate", "ExpiryDate", "MarkingAdditionalText", "MarkingFile"
                            FROM raw_data
                            WHERE "ProductId" = d."ProductId" AND "MarkingId" IS NOT NULL
                        ) x
                    ), '[]'::json)
                )
            )
        ) AS result
    FROM (SELECT DISTINCT ON ("ProductId") * FROM raw_data ORDER BY "ProductId", "Id") d
)
SELECT COALESCE(
    json_object_agg(product_id, result ORDER BY product_id),
    '{}'::json
) AS "Result"
FROM product_results;
