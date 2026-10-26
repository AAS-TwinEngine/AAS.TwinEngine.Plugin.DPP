WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = '000-001'
)
SELECT COALESCE(
    json_build_object(
        'DppMetadata',      json_build_object(
                                'GlobalAssetId',            a."GlobalAssetId",
                                'AasId',                    a."AasId",
                                'dppSchemaVersion',         a."dppSchemaVersion",
                                'dppStatus',                a."dppStatus",
                                'facilityId',               a."facilityId",
                                'economicOperatorId',       a."economicOperatorId",
                                'granularity',              a."granularity",
                                'lastUpdate',               a."lastUpdate"
                            )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a;
