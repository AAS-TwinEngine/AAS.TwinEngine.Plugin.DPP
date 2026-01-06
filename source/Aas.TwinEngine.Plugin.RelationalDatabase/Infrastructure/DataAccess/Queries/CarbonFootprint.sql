WITH asset_cte AS (
    SELECT *
    FROM "Asset"
    WHERE "ProductId" = @ProductId
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
                                                                                                                                                                    (SELECT json_build_object(
                                                                                                                                                                                'PcfCalculationMethod',         ps."PcfCalculationMethod",
                                                                                                                                                                                'PcfRuleOperator',              ps."PcfRuleOperator",
                                                                                                                                                                                'PcfRuleName',                  ps."PcfRuleName",
                                                                                                                                                                                'PcfRuleVersion',               ps."PcfRuleVersion",
                                                                                                                                                                                'PcfRuleOnlineReference',       ps."PcfRuleOnlineReference",
                                                                                                                                                                                'PcfApiEndpoint',               ps."PcfApiEndpoint",
                                                                                                                                                                                'PcfApiQuery',                  ps."PcfApiQuery"
                                                                                                                                                                            )
                                                                                                                                                                     FROM "ProductOrSectorSpecificCarbonFootprint" ps
                                                                                                                                                                     WHERE ps."AssetId" = a."Id"),
                                                                                                                                                                    '{}'::json
                                                                                                                                                                )
                                                                                                                )
                                                            )
    ),
    '{}'::json
) AS "Result"
FROM asset_cte a;
