SELECT json_build_object(
    'GlobalAssetId',        A."GlobalAssetId",
    'IdShort',              A."IdShort",
    'Id',                   A."AasId",
    'ProductId',            A."ProductId",
    'AssetKind',            A."AssetKind",
    'AssetType',            A."AssetType",
    'SpecificAssetIds',     COALESCE(
                                (SELECT json_agg(json_build_object(
                                            'Name',  sai."Name",
                                            'Value', sai."Value"
                                        ))
                                 FROM "SpecificAssetIds" sai
                                 WHERE sai."ProductId" = A."ProductId"),
                                '[]'::json
                            )
)
FROM "Asset" A
WHERE A."AasId" = @AasId;
