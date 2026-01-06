SELECT json_build_object(
    'GlobalAssetId',        A."GlobalAssetId",
    'DefaultThumbnail',     json_build_object(
                                'Path',        A."ThumbnailPath",
                                'ContentType', A."ThumbnailContentType"
                            ),
    'SpecificAssetIds',     (SELECT json_agg(json_build_object(
                                        'Name',  sai."Name",
                                        'Value', sai."Value"
                                    ))
                             FROM "SpecificAssetIds" sai
                             WHERE sai."AssetId" = A."Id")
)
FROM "Asset" A
WHERE A."AasId" = @AasId;
