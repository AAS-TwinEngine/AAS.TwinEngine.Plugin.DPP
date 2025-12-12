SELECT json_build_object(
    'GlobalAssetId', A."GlobalAssetId",
	'DefaultThumbnail',
        COALESCE(
            (
                    json_build_object(
                        'Path', A."ThumbnailPath",
                        'ContentType', A."ThumbnailContentType"
                    )   
            )
        ),
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
