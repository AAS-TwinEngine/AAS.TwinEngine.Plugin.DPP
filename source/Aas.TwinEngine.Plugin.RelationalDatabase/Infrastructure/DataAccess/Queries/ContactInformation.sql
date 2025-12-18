DECLARE @AssetId INT;

SELECT @AssetId = AssetID
FROM Asset
WHERE ProductId = @ProductId;

IF @AssetId IS NULL
BEGIN
    SELECT '{}' AS Result;
    RETURN;
END;

SELECT
(
    SELECT
        JSON_QUERY
        (
            (
                SELECT
                    JSON_QUERY
                    (
                        (
                            SELECT
                                ci.RoleOfContactPerson,
                                ci.[Language],
                                ci.TimeZone,
                                ci.AddressOfAdditionalLink,
                                ci.NationalCode_en,
                                ci.NationalCode_de,
                                ci.CityTown_en,
                                ci.CityTown_de,
                                ci.Company_en,
                                ci.Company_de,
                                ci.Department_en,
                                ci.Department_de,
                                ci.Street_en,
                                ci.Street_de,
                                ci.Zipcode_en,
                                ci.Zipcode_de,
                                ci.POBox_en,
                                ci.POBox_de,
                                ci.ZipCodeOfPOBox_en,
                                ci.ZipCodeOfPOBox_de,
                                ci.StateCounty_en,
                                ci.StateCounty_de,
                                ci.NameOfContact_en,
                                ci.NameOfContact_de,
                                ci.FirstName_en,
                                ci.FirstName_de,
                                ci.MiddleNames_en,
                                ci.MiddleNames_de,
                                ci.Title_en,
                                ci.Title_de,
                                ci.AcademicTitle_en,
                                ci.AcademicTitle_de,
                                ci.FurtherDetailsOfContact_en,
                                ci.FurtherDetailsOfContact_de,

                                JSON_QUERY
                                (
                                    (
                                        SELECT
                                            p.TelephoneNumber_en,
                                            p.TelephoneNumber_de,
                                            p.AvailableTime_en,
                                            p.AvailableTime_de,
                                            p.TypeOfTelephone
                                        FROM ContactInformationPhone cip
                                        JOIN Phone p ON p.PhoneID = cip.PhoneID
                                        WHERE cip.ContactInformationID = ci.ContactInformationID
                                        FOR JSON PATH
                                    )
                                ) AS Phone,

                                JSON_QUERY
                                (
                                    (
                                        SELECT
                                            f.FaxNumber_en,
                                            f.FaxNumber_de,
                                            f.TypeOfFaxNumber
                                        FROM ContactInformationFax cif
                                        JOIN Fax f ON f.FaxID = cif.FaxID
                                        WHERE cif.ContactInformationID = ci.ContactInformationID
                                        FOR JSON PATH
                                    )
                                ) AS Fax,

                                JSON_QUERY
                                (
                                    (
                                        SELECT
                                            e.EmailAddress,
                                            e.TypeOfEmailAddress,
                                            e.PublicKey_en,
                                            e.PublicKey_de,
                                            e.TypeOfPublicKey_en,
                                            e.TypeOfPublicKey_de
                                        FROM ContactInformationEmail cie
                                        JOIN Email e ON e.EmailID = cie.EmailID
                                        WHERE cie.ContactInformationID = ci.ContactInformationID
                                        FOR JSON PATH
                                    )
                                ) AS Email,

                                JSON_QUERY
                                (
                                    (
                                        SELECT
                                            ip.AddressOfAdditionalLink,
                                            ip.TypeOfCommunication,
                                            ip.AvailableTime_en,
                                            ip.AvailableTime_de
                                        FROM ContactInformationIPCommunication ciip
                                        JOIN IPCommunication ip
                                            ON ip.IPCommunicationID = ciip.IPCommunicationID
                                        WHERE ciip.ContactInformationID = ci.ContactInformationID
                                        FOR JSON PATH
                                    )
                                ) AS IPCommunication

                            FROM AssetContactInformation aci
                            JOIN ContactInformation ci
                                ON ci.ContactInformationID = aci.ContactInformationID
                            WHERE aci.AssetID = @AssetId
                            FOR JSON PATH
                        )
                    ) AS ContactInformation
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        ) AS ContactInformations
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
) AS Result;
