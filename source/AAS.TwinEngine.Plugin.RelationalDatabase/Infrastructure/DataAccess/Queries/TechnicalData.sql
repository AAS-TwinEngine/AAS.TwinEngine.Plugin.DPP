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
                                                                            'ManufacturerLogo',                     a."ManufacturerLogo",
                                                                            'ProductImage',                         a."ProductImage",
                                                                            'ManufacturerProductDesignation_en',    a."ManufacturerProductDesignation_en",
                                                                            'ManufacturerProductDesignation_de',    a."ManufacturerProductDesignation_de"
                                                                        ),

                                        'ProductClassificationItem',    COALESCE(

                                                                            (SELECT json_agg(json_build_object(
                                                                                        'ProductClassificationSystem',  pci."ProductClassificationSystem",
                                                                                        'ClassificationSystemVersion',   pci."ClassificationSystemVersion",
                                                                                        'ProductClassId',                pci."ProductClassId"
                                                                                    ) ORDER BY pci."Index")
                                                                             FROM "AssetProductClassificationItem" apci
                                                                             JOIN "ProductClassificationItem" pci ON pci."Id" = apci."ProductClassificationItemId"
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
