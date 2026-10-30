WITH requested_products AS (
    SELECT unnest(@ProductIds::text[]) AS product_id
),
asset_cte AS (
    SELECT
        a."ProductId" AS product_id,
        a."GlobalAssetId",
        a."AasId",
        a."dppSchemaVersion",
        a."dppStatus",
        a."facilityId",
        a."economicOperatorId",
        a."granularity",
        a."lastUpdate"
    FROM "Asset" a
    INNER JOIN requested_products rp ON rp.product_id = a."ProductId"
),
product_results AS (
    SELECT
        product_id,
        json_build_object(
            'DppMetadata', json_build_object(
                'GlobalAssetId', a."GlobalAssetId",
                'AasId', a."AasId",
                'dppSchemaVersion', a."dppSchemaVersion",
                'dppStatus', a."dppStatus",
                'facilityId', a."facilityId",
                'economicOperatorId', a."economicOperatorId",
                'granularity', a."granularity",
                'lastUpdate', a."lastUpdate"
            )
        ) AS result
    FROM asset_cte a
)
SELECT COALESCE(
    json_object_agg(product_id, result ORDER BY product_id),
    '{}'::json
) AS "Result"
FROM product_results;
