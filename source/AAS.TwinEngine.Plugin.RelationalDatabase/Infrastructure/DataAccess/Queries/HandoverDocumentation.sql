-- Optimized version: same JSON output shape and ordering as the original,
-- but nested aggregates are pre-computed via GROUP BY + json_agg in CTEs
-- and joined back in, instead of running a correlated subquery per row.

WITH params AS (
    SELECT "Id" AS asset_id
    FROM "Asset"
    WHERE "ProductId" = @ProductId
),

-- All documents linked to this asset. Referenced multiple times below,
-- so Postgres will materialize it (computed once).
asset_docs AS (
    SELECT DISTINCT ad."DocumentId"
    FROM "AssetDocument" ad
    WHERE ad."AssetId" = (SELECT asset_id FROM params)
),

-- DocumentId[] per document
doc_ids_agg AS (
    SELECT
        ddi."DocumentId",
        json_agg(
            json_build_object(
                'DocumentDomainId',   did."DocumentDomainId",
                'DocumentIdentifier', did."DocumentIdentifier",
                'DocumentIsPrimary',  did."DocumentIsPrimary"
            ) ORDER BY did."Index"
        ) AS document_ids
    FROM "DocumentDocumentId" ddi
    JOIN asset_docs USING ("DocumentId")
    JOIN "DocumentId" did ON did."Id" = ddi."DocumentIdentifierId"
    GROUP BY ddi."DocumentId"
),

-- DocumentClassification[] per document
doc_class_agg AS (
    SELECT
        ddc."DocumentId",
        json_agg(
            json_build_object(
                'ClassId',              dc."ClassId",
                'ClassificationSystem', dc."ClassificationSystem",
                'ClassName_en',         dc."ClassName_en",
                'ClassName_de',         dc."ClassName_de"
            ) ORDER BY dc."Index"
        ) AS document_classifications
    FROM "DocumentDocumentClassification" ddc
    JOIN asset_docs USING ("DocumentId")
    JOIN "DocumentClassification" dc ON dc."Id" = ddc."DocumentClassificationId"
    GROUP BY ddc."DocumentId"
),

-- DocumentVersionIds relevant to this asset (used to scope the languages CTE)
doc_ver_ids AS (
    SELECT DISTINCT ddv."DocumentVersionId"
    FROM "DocumentDocumentVersion" ddv
    JOIN asset_docs USING ("DocumentId")
),

-- Languages[] per document version
doc_ver_lang_agg AS (
    SELECT
        dvl."DocumentVersionId",
        json_agg(
            json_build_object('Language', l."Language") ORDER BY l."Index"
        ) AS languages
    FROM "DocumentVersionLanguages" dvl
    JOIN doc_ver_ids USING ("DocumentVersionId")
    JOIN "Languages" l ON l."Id" = dvl."LanguageId"
    GROUP BY dvl."DocumentVersionId"
),

-- DocumentVersion[] per document (embeds Languages[])
doc_ver_agg AS (
    SELECT
        ddv."DocumentId",
        json_agg(
            json_build_object(
                'DigitalFile',              dv."DigitalFile",
                'Version',                  dv."Version",
                'StatusSetDate',            dv."StatusSetDate",
                'StatusValue',              dv."StatusValue",
                'OrganizationShortName',    dv."OrganizationShortName",
                'OrganizationOfficialName', dv."OrganizationOfficialName",
                'Title_en',                 dv."Title_en",
                'Title_de',                 dv."Title_de",
                'Subtitle_en',              dv."Subtitle_en",
                'Subtitle_de',              dv."Subtitle_de",
                'Description_en',           dv."Description_en",
                'Description_de',           dv."Description_de",
                'KeyWords_en',              dv."KeyWords_en",
                'KeyWords_de',              dv."KeyWords_de",
                'Languages',                COALESCE(dvla.languages, '[]'::json),
                'PreviewFile',              dv."PreviewFile"
            ) ORDER BY dv."Index"
        ) AS document_versions
    FROM "DocumentDocumentVersion" ddv
    JOIN asset_docs USING ("DocumentId")
    JOIN "DocumentVersion" dv ON dv."Id" = ddv."DocumentVersionId"
    LEFT JOIN doc_ver_lang_agg dvla ON dvla."DocumentVersionId" = dv."Id"
    GROUP BY ddv."DocumentId"
),

-- Document[] — top-level array, assembling all three nested pieces per document
documents_agg AS (
    SELECT
        json_agg(
            json_build_object(
                'DocumentId',             COALESCE(dia.document_ids, '[]'::json),
                'DocumentClassification', COALESCE(dca.document_classifications, '[]'::json),
                'DocumentVersion',        COALESCE(dva.document_versions, '[]'::json)
            ) ORDER BY d."Index"
        ) AS documents
    FROM asset_docs adx
    JOIN "Document" d ON d."Id" = adx."DocumentId"
    LEFT JOIN doc_ids_agg  dia ON dia."DocumentId" = d."Id"
    LEFT JOIN doc_class_agg dca ON dca."DocumentId" = d."Id"
    LEFT JOIN doc_ver_agg   dva ON dva."DocumentId" = d."Id"
)

SELECT COALESCE(
    json_build_object(
        'HandoverDocumentation', json_build_object(
            'Document', COALESCE((SELECT documents FROM documents_agg), '[]'::json)
        )
    ),
    '{}'::json
) AS "Result";