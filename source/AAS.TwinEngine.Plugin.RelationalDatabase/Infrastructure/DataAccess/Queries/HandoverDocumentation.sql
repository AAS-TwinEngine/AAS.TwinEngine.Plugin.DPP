WITH asset_cte AS (
    SELECT "Id"
    FROM "Asset"
    WHERE "ProductId" = @ProductId
),
asset_documents_cte AS (
    SELECT
        ad."AssetId",
        d."Id" AS "DocumentPk",
        d."Index" AS "DocumentIndex"
    FROM asset_cte a
    JOIN "AssetDocument" ad
        ON ad."AssetId" = a."Id"
    JOIN "Document" d
        ON d."Id" = ad."DocumentId"
),
document_ids_cte AS (
    SELECT
        ddi."DocumentId" AS "DocumentPk",
        json_agg(
            json_build_object(
                'DocumentDomainId',   did."DocumentDomainId",
                'DocumentIdentifier', did."DocumentIdentifier",
                'DocumentIsPrimary',  did."DocumentIsPrimary"
            )
            ORDER BY did."Index"
        ) AS "DocumentId"
    FROM asset_documents_cte ad
    JOIN "DocumentDocumentId" ddi
        ON ddi."DocumentId" = ad."DocumentPk"
    JOIN "DocumentId" did
        ON did."Id" = ddi."DocumentIdentifierId"
    GROUP BY ddi."DocumentId"
),
document_classifications_cte AS (
    SELECT
        ddc."DocumentId",
        json_agg(
            json_build_object(
                'ClassId',              dc."ClassId",
                'ClassificationSystem', dc."ClassificationSystem",
                'ClassName_en',         dc."ClassName_en",
                'ClassName_de',         dc."ClassName_de"
            )
            ORDER BY dc."Index"
        ) AS "DocumentClassification"
    FROM asset_documents_cte ad
    JOIN "DocumentDocumentClassification" ddc
        ON ddc."DocumentId" = ad."DocumentPk"
    JOIN "DocumentClassification" dc
        ON dc."Id" = ddc."DocumentClassificationId"
    GROUP BY ddc."DocumentId"
),
relevant_versions_cte AS (
    SELECT
        ddv."DocumentId",
        dv."Id" AS "DocumentVersionPk",
        dv."Index",
        dv."DigitalFile",
        dv."Version",
        dv."StatusSetDate",
        dv."StatusValue",
        dv."OrganizationShortName",
        dv."OrganizationOfficialName",
        dv."Title_en",
        dv."Title_de",
        dv."Subtitle_en",
        dv."Subtitle_de",
        dv."Description_en",
        dv."Description_de",
        dv."KeyWords_en",
        dv."KeyWords_de",
        dv."PreviewFile"
    FROM asset_documents_cte ad
    JOIN "DocumentDocumentVersion" ddv
        ON ddv."DocumentId" = ad."DocumentPk"
    JOIN "DocumentVersion" dv
        ON dv."Id" = ddv."DocumentVersionId"
),
version_languages_cte AS (
    SELECT
        dvl."DocumentVersionId",
        json_agg(
            json_build_object(
                'Language', l."Language"
            )
            ORDER BY l."Index"
        ) AS "Languages"
    FROM relevant_versions_cte rv
    JOIN "DocumentVersionLanguages" dvl
        ON dvl."DocumentVersionId" = rv."DocumentVersionPk"
    JOIN "Languages" l
        ON l."Id" = dvl."LanguageId"
    GROUP BY dvl."DocumentVersionId"
),
document_versions_cte AS (
    SELECT
        rv."DocumentId",
        json_agg(
            json_build_object(
                'DigitalFile',              rv."DigitalFile",
                'Version',                  rv."Version",
                'StatusSetDate',            rv."StatusSetDate",
                'StatusValue',              rv."StatusValue",
                'OrganizationShortName',    rv."OrganizationShortName",
                'OrganizationOfficialName', rv."OrganizationOfficialName",
                'Title_en',                 rv."Title_en",
                'Title_de',                 rv."Title_de",
                'Subtitle_en',              rv."Subtitle_en",
                'Subtitle_de',              rv."Subtitle_de",
                'Description_en',           rv."Description_en",
                'Description_de',           rv."Description_de",
                'KeyWords_en',              rv."KeyWords_en",
                'KeyWords_de',              rv."KeyWords_de",
                'Languages',                COALESCE(vl."Languages", '[]'::json),
                'PreviewFile',              rv."PreviewFile"
            )
            ORDER BY rv."Index"
        ) AS "DocumentVersion"
    FROM relevant_versions_cte rv
    LEFT JOIN version_languages_cte vl
        ON vl."DocumentVersionId" = rv."DocumentVersionPk"
    GROUP BY rv."DocumentId"
),
documents_cte AS (
    SELECT
        ad."AssetId",
        json_agg(
            json_build_object(
                'DocumentId', COALESCE(di."DocumentId", '[]'::json),
                'DocumentClassification', COALESCE(dc."DocumentClassification", '[]'::json),
                'DocumentVersion', COALESCE(dv."DocumentVersion", '[]'::json)
            )
            ORDER BY ad."DocumentIndex"
        ) AS "Document"
    FROM asset_documents_cte ad
    LEFT JOIN document_ids_cte di
        ON di."DocumentPk" = ad."DocumentPk"
    LEFT JOIN document_classifications_cte dc
        ON dc."DocumentId" = ad."DocumentPk"
    LEFT JOIN document_versions_cte dv
        ON dv."DocumentId" = ad."DocumentPk"
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
LEFT JOIN documents_cte d
    ON d."AssetId" = a."Id";
