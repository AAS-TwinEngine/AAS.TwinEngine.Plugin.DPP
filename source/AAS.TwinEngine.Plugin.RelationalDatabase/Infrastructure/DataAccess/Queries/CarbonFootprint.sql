WITH raw_data AS (
    SELECT
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
    WHERE a."ProductId" = @ProductId
)
SELECT COALESCE(
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
                    WHEN d."SpecificPcfId" IS NULL
                    THEN '{}'::json
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
    ),
    '{}'::json
) AS "Result"
FROM (SELECT DISTINCT ON ("Id") * FROM raw_data ORDER BY "Id") d;
