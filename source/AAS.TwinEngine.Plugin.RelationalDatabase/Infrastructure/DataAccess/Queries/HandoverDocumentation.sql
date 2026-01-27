WITH asset_cte AS (
    SELECT "Id"
    FROM "Asset"
    WHERE "ProductId" = @ProductId
)
SELECT COALESCE(
    json_build_object(
        'HandoverDocumentation',    json_build_object(
                                        'Document',     COALESCE(
                                                            (SELECT json_agg(json_build_object(
                                                                        'DocumentId',               COALESCE(
                                                                                                        (SELECT json_agg(json_build_object(
                                                                                                                    'DocumentDomainId',     did."DocumentDomainId",
                                                                                                                    'DocumentIdentifier',   did."DocumentIdentifier",
                                                                                                                    'DocumentIsPrimary',    did."DocumentIsPrimary"
                                                                                                                ) ORDER BY did."Index")
                                                                                                         FROM "DocumentDocumentId" ddi
                                                                                                         JOIN "DocumentId" did ON did."Id" = ddi."DocumentIdentifierId"
                                                                                                         WHERE ddi."DocumentId" = d."Id"),
                                                                                                        '[]'::json
                                                                                                    ),
                                                                        'DocumentClassification',  COALESCE(
                                                                                                        (SELECT json_agg(json_build_object(
                                                                                                                    'ClassId',                  dc."ClassId",
                                                                                                                    'ClassificationSystem',     dc."ClassificationSystem",
                                                                                                                    'ClassName_en',             dc."ClassName_en",
                                                                                                                    'ClassName_de',             dc."ClassName_de"
                                                                                                                ) ORDER BY dc."Index")
                                                                                                         FROM "DocumentDocumentClassification" ddc
                                                                                                         JOIN "DocumentClassification" dc ON dc."Id" = ddc."DocumentClassificationId"
                                                                                                         WHERE ddc."DocumentId" = d."Id"),
                                                                                                        '[]'::json
                                                                                                    ),
                                                                        'DocumentVersion',          COALESCE(
                                                                                                        (SELECT json_agg(json_build_object(
                                                                                                                    'en',                           dv."en",
                                                                                                                    'DigitalFile',                  dv."DigitalFile",
                                                                                                                    'Version',                      dv."Version",
                                                                                                                    'StatusSetDate',                dv."StatusSetDate",
                                                                                                                    'StatusValue',                  dv."StatusValue",
                                                                                                                    'OrganizationShortName',        dv."OrganizationShortName",
                                                                                                                    'OrganizationOfficialName',     dv."OrganizationOfficialName",
                                                                                                                    'Title_en',                     dv."Title_en",
                                                                                                                    'Title_de',                     dv."Title_de",
                                                                                                                    'Subtitle_en',                  dv."Subtitle_en",
                                                                                                                    'Subtitle_de',                  dv."Subtitle_de",
                                                                                                                    'Description_en',               dv."Description_en",
                                                                                                                    'Description_de',               dv."Description_de",
                                                                                                                    'KeyWords_en',                  dv."KeyWords_en",
                                                                                                                    'KeyWords_de',                  dv."KeyWords_de",
                                                                                                                    'PreviewFile',                  dv."PreviewFile"
                                                                                                                ) ORDER BY dv."Index")
                                                                                                         FROM "DocumentDocumentVersion" ddv
                                                                                                         JOIN "DocumentVersion" dv ON dv."Id" = ddv."DocumentVersionId"
                                                                                                         WHERE ddv."DocumentId" = d."Id"),
                                                                                                        '[]'::json
                                                                                                    )
                                                                    ) ORDER BY d."Index")
                                                             FROM "AssetDocument" ad
                                                             JOIN "Document" d ON d."Id" = ad."DocumentId"
                                                             WHERE ad."AssetId" = (SELECT "Id" FROM asset_cte)),
                                                            '[]'::json
                                                        )
                                    )
    ),
    '{}'::json
) AS "Result";
