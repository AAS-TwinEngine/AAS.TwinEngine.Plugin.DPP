WITH asset_cte AS (
    SELECT "Id"
    FROM "Asset"
    WHERE "ProductId" = @ProductId
), document_ids_cte AS (
    SELECT
        ddi."DocumentId",
        json_agg(json_build_object(
            'DocumentDomainId', did."DocumentDomainId",
            'DocumentIdentifier', did."DocumentIdentifier",
            'DocumentIsPrimary', did."DocumentIsPrimary"
        ) ORDER BY did."Index") AS "DocumentId"
    FROM "DocumentDocumentId" ddi
    JOIN "DocumentId" did ON did."Id" = ddi."DocumentIdentifierId"
    GROUP BY ddi."DocumentId"
), document_classifications_cte AS (
    SELECT
        ddc."DocumentId",
        json_agg(json_build_object(
            'ClassId', dc."ClassId",
            'ClassificationSystem', dc."ClassificationSystem",
            'ClassName_en', dc."ClassName_en",
            'ClassName_de', dc."ClassName_de"
        ) ORDER BY dc."Index") AS "DocumentClassification"
    FROM "DocumentDocumentClassification" ddc
    JOIN "DocumentClassification" dc ON dc."Id" = ddc."DocumentClassificationId"
    GROUP BY ddc."DocumentId"
), document_languages_cte AS (
    SELECT
        dvl."DocumentVersionId",
        json_agg(json_build_object('Language', l."Language") ORDER BY l."Index") AS "Languages"
    FROM "DocumentVersionLanguages" dvl
    JOIN "Languages" l ON l."Id" = dvl."LanguageId"
    GROUP BY dvl."DocumentVersionId"
), document_versions_cte AS (
    SELECT
        ddv."DocumentId",
        json_agg(json_build_object(
            'DigitalFile', dv."DigitalFile",
            'Version', dv."Version",
            'StatusSetDate', dv."StatusSetDate",
            'StatusValue', dv."StatusValue",
            'OrganizationShortName', dv."OrganizationShortName",
            'OrganizationOfficialName', dv."OrganizationOfficialName",
            'Title_en', dv."Title_en",
            'Title_de', dv."Title_de",
            'Subtitle_en', dv."Subtitle_en",
            'Subtitle_de', dv."Subtitle_de",
            'Description_en', dv."Description_en",
            'Description_de', dv."Description_de",
            'KeyWords_en', dv."KeyWords_en",
            'KeyWords_de', dv."KeyWords_de",
            'Languages', COALESCE(dl."Languages", '[]'::json),
            'PreviewFile', dv."PreviewFile"
        ) ORDER BY dv."Index") AS "DocumentVersion"
    FROM "DocumentDocumentVersion" ddv
    JOIN "DocumentVersion" dv ON dv."Id" = ddv."DocumentVersionId"
    LEFT JOIN document_languages_cte dl ON dl."DocumentVersionId" = dv."Id"
    GROUP BY ddv."DocumentId"
), documents_cte AS (
    SELECT
        ad."AssetId",
        json_agg(json_build_object(
            'DocumentId', COALESCE(di."DocumentId", '[]'::json),
            'DocumentClassification', COALESCE(dc."DocumentClassification", '[]'::json),
            'DocumentVersion', COALESCE(dv."DocumentVersion", '[]'::json)
        ) ORDER BY d."Index") AS "Document"
    FROM "AssetDocument" ad
    JOIN "Document" d ON d."Id" = ad."DocumentId"
    LEFT JOIN document_ids_cte di ON di."DocumentId" = d."Id"
    LEFT JOIN document_classifications_cte dc ON dc."DocumentId" = d."Id"
    LEFT JOIN document_versions_cte dv ON dv."DocumentId" = d."Id"
    JOIN asset_cte a ON a."Id" = ad."AssetId"
    GROUP BY ad."AssetId"
)
SELECT COALESCE(
    json_build_object(
        'HandoverDocumentation', json_build_object(
            'Document', COALESCE(d."Document", '[]'::json)
        )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a
LEFT JOIN documents_cte d ON d."AssetId" = a."Id";
