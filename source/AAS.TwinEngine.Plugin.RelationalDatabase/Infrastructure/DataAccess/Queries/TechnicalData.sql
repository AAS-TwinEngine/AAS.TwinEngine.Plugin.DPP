WITH asset_cte AS (

    SELECT *
    FROM "Asset"
    WHERE "ProductId" = @ProductId
)
SELECT
COALESCE(
    json_build_object(
        'TechnicalData',            json_build_object(

                                        'GeneralInformation',           json_build_object(

                                                                            'ManufacturerName',                     a."ManufacturerName",
                                                                            'ManufacturerArticleNumber',            a."ManufacturerArticleNumber",
                                                                            'ManufacturerOrderCode',                a."ManufacturerOrderCode",
                                                                            'CompanyLogo',                          a."CompanyLogo",
                                                                            'ManufacturerProductDesignation_en',    a."ManufacturerProductDesignation_en",
                                                                            'ManufacturerProductDesignation_de',    a."ManufacturerProductDesignation_de",
                                                                            'ProductImage',                         COALESCE(
                                                                                                                                (
                                                                                                                                                SELECT json_agg(
                                                                                                                                                           json_build_object(
                                                                                                                                                               'ImageFile',      pi."ImageFile",
                                                                                                                                                               'ImageNote_en',   pi."ImageNote_en",
                                                                                                                                                               'ImageNote_de',   pi."ImageNote_de"
                                                                                                                                                           )
                                                                                                                                                           ORDER BY pi."Index"
                                                                                                                                                       )
                                                                                                                                                FROM "AssetProductImage" api
                                                                                                                                                JOIN "ProductImage" pi
                                                                                                                                                    ON pi."Id" = api."ProductImageId"
                                                                                                                                                WHERE api."AssetId" = a."Id"
                                                                                                                                            ),
                                                                                                                                            '[]'::json
                                                                                                                                        )
                                                                        ),

                                        'ProductClassifications',    COALESCE(

                                                                            (SELECT json_agg(json_build_object(
                                                                                        'ClassificationSystem',             pci."ClassificationSystem",
                                                                                        'ClassificationSystemVersion',      pci."ClassificationSystemVersion",
                                                                                        'ClassificationSystemUrl',          pci."ClassificationSystemUrl",
                                                                                        'ProductClassId',                    pci."ProductClassId",
                                                                                        'ProductClassCodedName',             pci."ProductClassCodedName",
                                                                                        'ProductClassName_en',              pci."ProductClassName_en",
                                                                                        'ProductClassName_de',              pci."ProductClassName_de"
                                                                                    ) ORDER BY pci."Index")
                                                                             FROM "AssetProductClassifications" apci
                                                                             JOIN "ProductClassifications" pci ON pci."Id" = apci."ProductClassificationsId"
                                                                             WHERE apci."AssetId" = a."Id"),
                                                                            '[]'::json
                                                                        ),

                                        'FurtherInformation',           json_build_object(
                                                                            'TextStatement_en',    a."TextStatement_en",
                                                                            'TextStatement_de',    a."TextStatement_de",
                                                                            'ValidDate',        a."ValidDate"
                                                                        )
                                    )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a;
