SELECT json_build_object(
    'GlobalAssetId',        A."GlobalAssetId",
    'IdShort',              A."IdShort",
    'Id',                   A."AasId",
    'SpecificAssetIds',     COALESCE(
                                (SELECT json_agg(json_build_object(
                                            'Name',  sai."Name",
                                            'Value', sai."Value"
                                        ))
                                 FROM "SpecificAssetIds" sai
                                 WHERE sai."AssetId" = A."Id"),
                                '[]'::json
                            )
)
FROM "Asset" A
WHERE A."AasId" = @AasId;
