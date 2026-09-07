WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = @ProductId
), product_images_cte AS (
    SELECT
        api."AssetId",
        json_agg(
            json_build_object(
                'ImageFile', pi."ImageFile",
                'ImageNote_en', pi."ImageNote_en",
                'ImageNote_de', pi."ImageNote_de"
            ) ORDER BY pi."Index"
        ) AS "ProductImage"
    FROM "AssetProductImage" api
    JOIN "ProductImage" pi ON pi."Id" = api."ProductImageId"
    JOIN asset_cte a ON a."Id" = api."AssetId"
    GROUP BY api."AssetId"
), product_classifications_cte AS (
    SELECT
        apci."AssetId",
        json_agg(
            json_build_object(
                'ClassificationSystem', pci."ClassificationSystem",
                'ClassificationSystemVersion', pci."ClassificationSystemVersion",
                'ClassificationSystemUrl', pci."ClassificationSystemUrl",
                'ProductClassId', pci."ProductClassId",
                'ProductClassCodedName', pci."ProductClassCodedName",
                'ProductClassName_en', pci."ProductClassName_en",
                'ProductClassName_de', pci."ProductClassName_de"
            ) ORDER BY pci."Index"
        ) AS "ProductClassifications"
    FROM "AssetProductClassifications" apci
    JOIN "ProductClassifications" pci ON pci."Id" = apci."ProductClassificationsId"
    JOIN asset_cte a ON a."Id" = apci."AssetId"
    GROUP BY apci."AssetId"
)
SELECT COALESCE(
    json_build_object(
        'TechnicalData', json_build_object(
            'GeneralInformation', json_build_object(
                'ManufacturerName', a."ManufacturerName",
                'ManufacturerArticleNumber', a."ManufacturerArticleNumber",
                'ManufacturerOrderCode', a."ManufacturerOrderCode",
                'CompanyLogo', a."CompanyLogo",
                'ManufacturerProductDesignation_en', a."ManufacturerProductDesignation_en",
                'ManufacturerProductDesignation_de', a."ManufacturerProductDesignation_de",
                'ProductImage', COALESCE(pi."ProductImage", '[]'::json)
            ),
            'ProductClassifications', COALESCE(pc."ProductClassifications", '[]'::json),
            'FurtherInformation', json_build_object(
                'TextStatement_en', a."TextStatement_en",
                'TextStatement_de', a."TextStatement_de",
                'ValidDate', a."ValidDate"
            )
        )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a
LEFT JOIN product_images_cte pi ON pi."AssetId" = a."Id"
LEFT JOIN product_classifications_cte pc ON pc."AssetId" = a."Id";
