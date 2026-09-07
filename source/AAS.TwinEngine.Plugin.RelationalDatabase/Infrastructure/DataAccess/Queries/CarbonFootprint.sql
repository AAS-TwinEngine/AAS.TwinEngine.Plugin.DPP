WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = @ProductId
), product_specific_footprint_cte AS (
    SELECT
        ps."AssetId",
        json_build_object(
            'PcfCalculationMethod', ps."PcfCalculationMethod",
            'PcfRuleOperator', ps."PcfRuleOperator",
            'PcfRuleName', ps."PcfRuleName",
            'PcfRuleVersion', ps."PcfRuleVersion",
            'PcfRuleOnlineReference', ps."PcfRuleOnlineReference",
            'PcfApiEndpoint', ps."PcfApiEndpoint",
            'PcfApiQuery', ps."PcfApiQuery"
        ) AS "ProductOrSectorSpecificCarbonFootprint"
    FROM "ProductOrSectorSpecificCarbonFootprint" ps
    JOIN asset_cte a ON a."Id" = ps."AssetId"
)
SELECT COALESCE(
    json_build_object(
        'CarbonFootprint',                                  json_build_object(
                                                                'ProductCarbonFootprints',                      json_build_object(
                                                                                                                    'ProductCarbonFootprint',   json_build_object(
                                                                                                                                                    'PcfCalculationMethod',                 a."PcfCalculationMethod",
                                                                                                                                                    'LifeCyclePhase',                       a."LifeCyclePhase",
                                                                                                                                                    'PcfCO2eq',                             a."PcfCO2eq",
                                                                                                                                                    'ReferenceImpactUnitForCalculation',    a."ReferenceImpactUnitForCalculation",
                                                                                                                                                    'QuantityOfMeasureForCalculation',      a."QuantityOfMeasureForCalculation",
                                                                                                                                                    'PublicationDate',                      a."PublicationDate",
                                                                                                                                                    'ExpirationDate',                       a."ExpirationDate",
                                                                                                                                                    'ExplanatoryStatement',                 a."ExplanatoryStatement"
                                                                                                                                                )
                                                                                                                ),
                                                                'ProductOrSectorSpecificCarbonFootprints',      json_build_object(
                                                                                                                    'ProductOrSectorSpecificCarbonFootprint',   COALESCE(
                                                                                                                                                                    ps."ProductOrSectorSpecificCarbonFootprint",
                                                                                                                                                                    '{}'::json
                                                                                                                                                                )
                                                                                                                )
                                                            )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a
LEFT JOIN product_specific_footprint_cte ps ON ps."AssetId" = a."Id";
