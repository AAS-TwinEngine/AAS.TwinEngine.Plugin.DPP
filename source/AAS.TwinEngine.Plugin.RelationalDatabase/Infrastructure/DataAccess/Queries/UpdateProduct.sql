WITH updated_asset AS (
    UPDATE "Asset" SET
        "DateOfManufacture" = @DateOfManufacture,
        "YearOfConstruction" = EXTRACT(YEAR FROM @DateOfManufacture)::INT,
        "AddressInformationStreet_en" = @Street,
        "AddressInformationZipcode_en" = @Zipcode,
        "AddressInformationCityTown_en" = @CityTown,
        "AddressInformationNationalCode_en" = @NationalCode,
        "SerialNumber" = @SerialNumber,
        "ManufacturerProductType" = @ManufacturerProductType,
        "ManufacturerName" = @ManufacturerName,
        "ManufacturerArticleNumber" = @ManufacturerArticleNumber,
        "PcfCO2eq" = @PcfCO2eq,
        "ReferenceImpactUnitForCalculation" = @ReferenceImpactUnitForCalculation,
        "QuantityOfMeasureForCalculation" = @QuantityOfMeasureForCalculation,
        "PublicationDate" = @PublicationDate,
        "ExpirationDate" = @ExpirationDate
    WHERE "ProductId" = @ProductId
    RETURNING "Id", "ProductId", "IdShort", "AasId", "GlobalAssetId"
),
updated_dimensions AS (
    INSERT INTO "TechnicalPropertyArea" ("AssetId", "Index", "Length", "Width", "Height")
    SELECT "Id", 0, @Length, @Width, @Height FROM updated_asset
    ON CONFLICT ("AssetId", "Index") DO UPDATE SET
        "Length" = EXCLUDED."Length", "Width" = EXCLUDED."Width", "Height" = EXCLUDED."Height"
),
updated_specific_ids AS (
    UPDATE "SpecificAssetIds" SET "Value" = CASE
        WHEN "Name" = 'DemoSerialNumber' THEN @SerialNumber
        WHEN "Name" = 'ModelYear' THEN EXTRACT(YEAR FROM @DateOfManufacture)::INT::TEXT
        ELSE "Value" END
    WHERE "ProductId" IN (SELECT "ProductId" FROM updated_asset)
        AND "Name" IN ('DemoSerialNumber', 'ModelYear')
)
SELECT json_build_object(
    'productId', "ProductId", 'idShort', "IdShort", 'aasId', "AasId", 'globalAssetId', "GlobalAssetId"
)::TEXT FROM updated_asset;