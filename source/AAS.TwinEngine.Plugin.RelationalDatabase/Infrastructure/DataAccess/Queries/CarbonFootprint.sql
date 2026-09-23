WITH requested_products AS (
    SELECT unnest(@ProductIds::text[]) AS product_id
),
raw_data AS (
    SELECT
        a."ProductId" AS product_id,
        a.*,
        ps."Id" AS "SpecificPcfId",
        ps."PcfCalculationMethod" AS "SpecificPcfCalculationMethod",
        ps."PcfRuleOperator",
        ps."PcfRuleName",
        ps."PcfRuleVersion",
        ps."PcfRuleOnlineReference",
        ps."PcfApiEndpoint",
        ps."PcfApiQuery"
    FROM "Asset" a
    LEFT JOIN "ProductOrSectorSpecificCarbonFootprint" ps ON ps."AssetId" = a."Id"
    INNER JOIN requested_products rp ON rp.product_id = a."ProductId"
),
product_results AS (
    SELECT DISTINCT ON ("ProductId")
        "ProductId" AS product_id,
        json_build_object(
            'CarbonFootprint', json_build_object(
                'ProductCarbonFootprints', json_build_object(
                    'ProductCarbonFootprint', json_build_object(
                        'PcfCalculationMethod', d."PcfCalculationMethod",
                        'LifeCyclePhase', d."LifeCyclePhase",
                        'PcfCO2eq', d."PcfCO2eq",
                        'ReferenceImpactUnitForCalculation', d."ReferenceImpactUnitForCalculation",
                        'QuantityOfMeasureForCalculation', d."QuantityOfMeasureForCalculation",
                        'PublicationDate', d."PublicationDate",
                        'ExpirationDate', d."ExpirationDate",
                        'ExplanatoryStatement', d."ExplanatoryStatement"
                    )
                ),
                'ProductOrSectorSpecificCarbonFootprints', json_build_object(
                    'ProductOrSectorSpecificCarbonFootprint', CASE
                        WHEN d."SpecificPcfId" IS NULL THEN '{}'::json
                        ELSE json_build_object(
                            'PcfCalculationMethod', d."SpecificPcfCalculationMethod",
                            'PcfRuleOperator', d."PcfRuleOperator",
                            'PcfRuleName', d."PcfRuleName",
                            'PcfRuleVersion', d."PcfRuleVersion",
                            'PcfRuleOnlineReference', d."PcfRuleOnlineReference",
                            'PcfApiEndpoint', d."PcfApiEndpoint",
                            'PcfApiQuery', d."PcfApiQuery"
                        )
                    END
                )
            )
        ) AS result
    FROM (SELECT DISTINCT ON ("ProductId") * FROM raw_data ORDER BY "ProductId", "Id") d
)
SELECT COALESCE(
    json_object_agg(product_id, result ORDER BY product_id),
    '{}'::json
) AS "Result"
FROM product_results;
