SELECT json_build_object(
    'GlobalAssetId', A."GlobalAssetId",
    'IdShort', A."IdShort",
    'Id', A."AasId",
    'SpecificAssetIds',
        COALESCE(
            (
                SELECT json_agg(
                    json_build_object(
                        'Name', sai."KeyName",
                        'Value', sai."KeyValue"
                    )
                )
                FROM "SpecificAssetIds" sai
                WHERE sai."AssetID" = A."AssetID"
            ),
            '[]'::json
        )
)
FROM "Asset" A
WHERE A."AasId" = @aasId;
