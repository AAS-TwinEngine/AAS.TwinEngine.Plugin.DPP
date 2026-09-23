WITH requested_products AS (
    SELECT unnest(@ProductIds::text[]) AS product_id
),
params AS (
    SELECT a."ProductId" AS product_id, a."Id" AS asset_id
    FROM "Asset" a
    INNER JOIN requested_products rp ON rp.product_id = a."ProductId"
),
asset_docs AS (
    SELECT DISTINCT p.product_id, ad."DocumentId"
    FROM params p
    INNER JOIN "AssetDocument" ad ON ad."AssetId" = p.asset_id
),
doc_ids_agg AS (
    SELECT ad.product_id, ddi."DocumentId",
        json_agg(json_build_object(
            'DocumentDomainId', did."DocumentDomainId",
            'DocumentIdentifier', did."DocumentIdentifier",
            'DocumentIsPrimary', did."DocumentIsPrimary"
        ) ORDER BY did."Index") AS document_ids
    FROM "DocumentDocumentId" ddi
    INNER JOIN asset_docs ad ON ad."DocumentId" = ddi."DocumentId"
    INNER JOIN "DocumentId" did ON did."Id" = ddi."DocumentIdentifierId"
    GROUP BY ad.product_id, ddi."DocumentId"
),
doc_class_agg AS (
    SELECT ad.product_id, ddc."DocumentId",
        json_agg(json_build_object(
            'ClassId', dc."ClassId",
            'ClassificationSystem', dc."ClassificationSystem",
            'ClassName_en', dc."ClassName_en",
            'ClassName_de', dc."ClassName_de"
        ) ORDER BY dc."Index") AS document_classifications
    FROM "DocumentDocumentClassification" ddc
    INNER JOIN asset_docs ad ON ad."DocumentId" = ddc."DocumentId"
    INNER JOIN "DocumentClassification" dc ON dc."Id" = ddc."DocumentClassificationId"
    GROUP BY ad.product_id, ddc."DocumentId"
),
doc_ver_ids AS (
    SELECT DISTINCT ad.product_id, ddv."DocumentVersionId"
    FROM "DocumentDocumentVersion" ddv
    INNER JOIN asset_docs ad ON ad."DocumentId" = ddv."DocumentId"
),
doc_ver_lang_agg AS (
    SELECT dvi.product_id, dvl."DocumentVersionId",
        json_agg(json_build_object('Language', l."Language") ORDER BY l."Index") AS languages
    FROM "DocumentVersionLanguages" dvl
    INNER JOIN doc_ver_ids dvi ON dvi."DocumentVersionId" = dvl."DocumentVersionId"
    INNER JOIN "Languages" l ON l."Id" = dvl."LanguageId"
    GROUP BY dvi.product_id, dvl."DocumentVersionId"
),
doc_ver_agg AS (
    SELECT ad.product_id, ddv."DocumentId",
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
            'Languages', COALESCE(dvla.languages, '[]'::json),
            'PreviewFile', dv."PreviewFile"
        ) ORDER BY dv."Index") AS document_versions
    FROM "DocumentDocumentVersion" ddv
    INNER JOIN asset_docs ad ON ad."DocumentId" = ddv."DocumentId"
    INNER JOIN "DocumentVersion" dv ON dv."Id" = ddv."DocumentVersionId"
    LEFT JOIN doc_ver_lang_agg dvla
        ON dvla.product_id = ad.product_id
       AND dvla."DocumentVersionId" = dv."Id"
    GROUP BY ad.product_id, ddv."DocumentId"
),
documents_agg AS (
    SELECT adx.product_id,
        json_agg(json_build_object(
            'DocumentId', COALESCE(dia.document_ids, '[]'::json),
            'DocumentClassification', COALESCE(dca.document_classifications, '[]'::json),
            'DocumentVersion', COALESCE(dva.document_versions, '[]'::json)
        ) ORDER BY d."Index") AS documents
    FROM asset_docs adx
    INNER JOIN "Document" d ON d."Id" = adx."DocumentId"
    LEFT JOIN doc_ids_agg dia
        ON dia.product_id = adx.product_id AND dia."DocumentId" = d."Id"
    LEFT JOIN doc_class_agg dca
        ON dca.product_id = adx.product_id AND dca."DocumentId" = d."Id"
    LEFT JOIN doc_ver_agg dva
        ON dva.product_id = adx.product_id AND dva."DocumentId" = d."Id"
    GROUP BY adx.product_id
),
product_results AS (
    SELECT p.product_id,
        json_build_object(
            'HandoverDocumentation', json_build_object(
                'Document', COALESCE(da.documents, '[]'::json)
            )
        ) AS result
    FROM params p
    LEFT JOIN documents_agg da ON da.product_id = p.product_id
)
SELECT COALESCE(
    json_object_agg(product_id, result ORDER BY product_id),
    '{}'::json
) AS "Result"
FROM product_results;
