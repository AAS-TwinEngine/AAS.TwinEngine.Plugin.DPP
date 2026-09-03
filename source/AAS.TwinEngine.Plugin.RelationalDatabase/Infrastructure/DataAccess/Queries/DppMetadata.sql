WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = '000-001'
)
SELECT COALESCE(
    json_build_object(
        'DppMetadata',      json_build_object(
                                'ContentSpecificationIds', json_build_object('ContentSpecificationId', 'https://admin-shell-io/idta/digitalproductpassport/Nameplate/1',
                                                                             'ContentSpecificationId', 'https://admin-shell.io/idta/SubmodelTemplate/MaintenanceInstructions/1/0',
                                                                             'ContentSpecificationId', '0173-1#01-AHF578#003',
                                                                             'ContentSpecificationId', '0173-1#01-AHX837#002',
                                                                            'ContentSpecificationId', 'https://admin-shell.io/idta/CarbonFootprint/CarbonFootprint/1/0'
                            ),
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
