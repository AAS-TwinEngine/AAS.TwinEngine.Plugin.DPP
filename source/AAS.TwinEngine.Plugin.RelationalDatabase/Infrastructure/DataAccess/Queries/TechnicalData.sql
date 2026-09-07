WITH raw_data AS (
    SELECT
        a.*,
        pi."Id" AS "ProductImageId",
        pi."ImageFile",
        pi."ImageNote_en",
        pi."ImageNote_de",
        pi."Index" AS "ProductImageIndex",
        pci."Id" AS "ProductClassificationId",
        pci."ClassificationSystem",
        pci."ClassificationSystemVersion",
        pci."ClassificationSystemUrl",
        pci."ProductClassId",
        pci."ProductClassCodedName",
        pci."ProductClassName_en",
        pci."ProductClassName_de",
        pci."Index" AS "ProductClassificationIndex"
    FROM "Asset" a
    LEFT JOIN "AssetProductImage" api ON api."AssetId" = a."Id"
    LEFT JOIN "ProductImage" pi ON pi."Id" = api."ProductImageId"
    LEFT JOIN "AssetProductClassifications" apci ON apci."AssetId" = a."Id"
    LEFT JOIN "ProductClassifications" pci ON pci."Id" = apci."ProductClassificationsId"
    WHERE a."ProductId" = @ProductId
)
SELECT COALESCE(
    json_build_object(
        'TechnicalData', json_build_object(
            'GeneralInformation', json_build_object(
                'ManufacturerName', d."ManufacturerName",
                'ManufacturerArticleNumber', d."ManufacturerArticleNumber",
                'ManufacturerOrderCode', d."ManufacturerOrderCode",
                'CompanyLogo', d."CompanyLogo",
                'ManufacturerProductDesignation_en', d."ManufacturerProductDesignation_en",
                'ManufacturerProductDesignation_de', d."ManufacturerProductDesignation_de",
                'ProductImage', COALESCE((
                    SELECT json_agg(json_build_object(
                        'ImageFile', x."ImageFile",
                        'ImageNote_en', x."ImageNote_en",
                        'ImageNote_de', x."ImageNote_de"
                    ) ORDER BY x."ProductImageIndex")
                    FROM (
                        SELECT DISTINCT "ProductImageId", "ImageFile", "ImageNote_en", "ImageNote_de", "ProductImageIndex"
                        FROM raw_data
                        WHERE "ProductImageId" IS NOT NULL
                    ) x
                ), '[]'::json)
            ),
            'ProductClassifications', COALESCE((
                SELECT json_agg(json_build_object(
                    'ClassificationSystem', x."ClassificationSystem",
                    'ClassificationSystemVersion', x."ClassificationSystemVersion",
                    'ClassificationSystemUrl', x."ClassificationSystemUrl",
                    'ProductClassId', x."ProductClassId",
                    'ProductClassCodedName', x."ProductClassCodedName",
                    'ProductClassName_en', x."ProductClassName_en",
                    'ProductClassName_de', x."ProductClassName_de"
                ) ORDER BY x."ProductClassificationIndex")
                FROM (
                    SELECT DISTINCT "ProductClassificationId", "ClassificationSystem", "ClassificationSystemVersion",
                                    "ClassificationSystemUrl", "ProductClassId", "ProductClassCodedName",
                                    "ProductClassName_en", "ProductClassName_de", "ProductClassificationIndex"
                    FROM raw_data
                    WHERE "ProductClassificationId" IS NOT NULL
                ) x
            ), '[]'::json),
            'FurtherInformation', json_build_object(
                'TextStatement_en', d."TextStatement_en",
                'TextStatement_de', d."TextStatement_de",
                'ValidDate', d."ValidDate"
            )
        )
    ),
    '{}'::json
) AS "Result"
FROM (SELECT DISTINCT ON ("Id") * FROM raw_data ORDER BY "Id") d;
