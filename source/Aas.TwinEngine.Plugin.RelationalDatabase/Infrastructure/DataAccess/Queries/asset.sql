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
                        'Name', sai."Name",
                        'Value', sai."Value"
                    )
                )
                FROM "SpecificAssetIds" sai
                WHERE sai."AssetId" = A."Id"
            ),
            '[]'::json
        )
)
FROM "Asset" A
WHERE A."AasId" = @AasId;
