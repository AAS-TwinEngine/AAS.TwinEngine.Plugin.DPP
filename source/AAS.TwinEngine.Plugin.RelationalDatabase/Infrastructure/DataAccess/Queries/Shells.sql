WITH filtered_assets AS (
    SELECT
        A."Id",
        A."GlobalAssetId",
        A."IdShort",
        A."AasId"
    FROM "Asset" A
    {{__ASSET_FILTER__}}
    {{__PAGINATION__}}
),
specific_asset_ids AS (
    SELECT
        sai."AssetId",
        json_agg(
            json_build_object(
                'Name',  sai."Name",
                'Value', sai."Value"
            )
            ORDER BY sai."Id"
        ) AS "SpecificAssetIds"
    FROM "SpecificAssetIds" sai
    INNER JOIN filtered_assets fa ON fa."Id" = sai."AssetId"
    GROUP BY sai."AssetId"
)
SELECT json_agg(
    json_build_object(
        'GlobalAssetId',    fa."GlobalAssetId",
        'IdShort',          fa."IdShort",
        'Id',               fa."AasId",
        'SpecificAssetIds', COALESCE(sai."SpecificAssetIds", '[]'::json)
    )
    ORDER BY fa."AasId"
)
FROM filtered_assets fa
LEFT JOIN specific_asset_ids sai ON sai."AssetId" = fa."Id";
