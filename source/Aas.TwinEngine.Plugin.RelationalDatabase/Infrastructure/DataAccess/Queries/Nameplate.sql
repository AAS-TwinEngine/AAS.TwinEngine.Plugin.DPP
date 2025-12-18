DECLARE @AssetId INT;

SELECT @AssetId = AssetId
FROM Asset
WHERE ProductId = @ProductId;

SELECT
    (
        SELECT
            JSON_QUERY(
                (
                    SELECT
                        a.UriOfTheProduct,
                        a.ManufacturerProductType,
                        a.OrderCodeOfManufacturer,
                        a.ProductArticleNumberOfManufacturer,
                        a.SerialNumber,
                        a.YearOfConstruction,
                        a.DateOfManufacture,
                        a.HardwareVersion,
                        a.FirmwareVersion,
                        a.SoftwareVersion,
                        a.CountryOfOrigin,
                        a.UniqueFacilityIdentifier,
                        a.ManufacturerName_en,
                        a.ManufacturerName_de,
                        a.ManufacturerProductDesignation_en,
                        a.ManufacturerProductDesignation_de,
                        a.ManufacturerProductRoot_en,
                        a.ManufacturerProductRoot_de,
                        a.ManufacturerProductFamily_en,
                        a.ManufacturerProductFamily_de,
                        JSON_QUERY(
                            (
                                SELECT 
                                    m.MarkingID,
                                    m.DesignationOfCertificateOrApproval,
                                    m.IssueDate,
                                    m.ExpiryDate,
                                    m.MarkingAdditionalText,
                                    m.MarkingFile
                                FROM AssetMarking am
                                JOIN Marking m ON m.MarkingID = am.MarkingID
                                WHERE am.AssetID = a.AssetID
                                FOR JSON PATH
                            )
                        ) AS Markings,
                        a.CompanyLogo
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                )
            ) AS nameplate
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS Result
FROM Asset a
WHERE a.AssetID = @AssetId;
