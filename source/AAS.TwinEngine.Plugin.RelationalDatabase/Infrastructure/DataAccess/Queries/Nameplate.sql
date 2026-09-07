WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = @ProductId
), markings_cte AS (
    SELECT
        am."AssetId",
        json_agg(json_build_object(
            'MarkingName', m."MarkingName",
            'DesignationOfCertificateOrApproval', m."DesignationOfCertificateOrApproval",
            'IssueDate', m."IssueDate",
            'ExpiryDate', m."ExpiryDate",
            'MarkingAdditionalText', m."MarkingAdditionalText",
            'MarkingFile', m."MarkingFile"
        )) AS "Marking"
    FROM "AssetMarking" am
    JOIN "Marking" m ON m."Id" = am."MarkingId"
    JOIN asset_cte a ON a."Id" = am."AssetId"
    GROUP BY am."AssetId"
)
SELECT COALESCE(
    (SELECT json_build_object(
        'nameplate',     json_build_object(
                                            'UriOfTheProduct',                          a."UriOfTheProduct",
                                            'ManufacturerProductType',                  a."ManufacturerProductType",
                                            'OrderCodeOfManufacturer',                  a."OrderCodeOfManufacturer",
                                            'ProductArticleNumberOfManufacturer',       a."ProductArticleNumberOfManufacturer",
                                            'SerialNumber',                             a."SerialNumber",
                                            'YearOfConstruction',                       a."YearOfConstruction",
                                            'DateOfManufacture',                        a."DateOfManufacture",
                                            'HardwareVersion',                          a."HardwareVersion",
                                            'FirmwareVersion',                          a."FirmwareVersion",
                                            'SoftwareVersion',                          a."SoftwareVersion",
                                            'CountryOfOrigin',                          a."CountryOfOrigin",
                                            'UniqueFacilityIdentifier',                 a."UniqueFacilityIdentifier",
                                            'ManufacturerName',                         a."ManufacturerName",
                                            'ManufacturerProductDesignation_en',        a."ManufacturerProductDesignation_en",
                                            'ManufacturerProductDesignation_de',        a."ManufacturerProductDesignation_de",
                                            'ManufacturerProductRoot_en',               a."ManufacturerProductRoot_en",
                                            'ManufacturerProductRoot_de',               a."ManufacturerProductRoot_de",
                                            'ManufacturerProductFamily_en',             a."ManufacturerProductFamily_en",
                                            'ManufacturerProductFamily_de',             a."ManufacturerProductFamily_de",
                                            'CompanyLogo',                              a."CompanyLogo",
                                            'Markings',                                 json_build_object(
                                                                                                        'Marking',  COALESCE(mc."Marking", '[]'::json)
                                                                                                    )
                                                    )
    )
    FROM asset_cte a
    LEFT JOIN markings_cte mc ON mc."AssetId" = a."Id"),
    '{}'::json
) AS "Result";
