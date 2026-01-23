WITH asset_cte AS (
    SELECT "Id"
    FROM "Asset"
    WHERE "ProductId" = @ProductId
)
SELECT COALESCE(
    (SELECT json_build_object(
        'ContactInformations',      json_build_object(
                                        'ContactInformation',   COALESCE(
                                                                    json_agg(json_build_object(
                                                                        'RoleOfContactPerson',              ci."RoleOfContactPerson",
                                                                        'Language',                         ci."Language",
                                                                        'TimeZone',                         ci."TimeZone",
                                                                        'AddressOfAdditionalLink',          ci."AddressOfAdditionalLink",
                                                                        'NationalCode_en',                  ci."NationalCode_en",
                                                                        'NationalCode_de',                  ci."NationalCode_de",
                                                                        'CityTown_en',                      ci."CityTown_en",
                                                                        'CityTown_de',                      ci."CityTown_de",
                                                                        'Company_en',                       ci."Company_en",
                                                                        'Company_de',                       ci."Company_de",
                                                                        'Department_en',                    ci."Department_en",
                                                                        'Department_de',                    ci."Department_de",
                                                                        'Street_en',                        ci."Street_en",
                                                                        'Street_de',                        ci."Street_de",
                                                                        'Zipcode_en',                       ci."Zipcode_en",
                                                                        'Zipcode_de',                       ci."Zipcode_de",
                                                                        'POBox_en',                         ci."POBox_en",
                                                                        'POBox_de',                         ci."POBox_de",
                                                                        'ZipCodeOfPOBox_en',                ci."ZipCodeOfPOBox_en",
                                                                        'ZipCodeOfPOBox_de',                ci."ZipCodeOfPOBox_de",
                                                                        'StateCounty_en',                   ci."StateCounty_en",
                                                                        'StateCounty_de',                   ci."StateCounty_de",
                                                                        'NameOfContact_en',                 ci."NameOfContact_en",
                                                                        'NameOfContact_de',                 ci."NameOfContact_de",
                                                                        'FirstName_en',                     ci."FirstName_en",
                                                                        'FirstName_de',                     ci."FirstName_de",
                                                                        'MiddleNames_en',                   ci."MiddleNames_en",
                                                                        'MiddleNames_de',                   ci."MiddleNames_de",
                                                                        'Title_en',                         ci."Title_en",
                                                                        'Title_de',                         ci."Title_de",
                                                                        'AcademicTitle_en',                 ci."AcademicTitle_en",
                                                                        'AcademicTitle_de',                 ci."AcademicTitle_de",
                                                                        'FurtherDetailsOfContact_en',       ci."FurtherDetailsOfContact_en",
                                                                        'FurtherDetailsOfContact_de',       ci."FurtherDetailsOfContact_de",
                                                                        'Phone',                            COALESCE(
                                                                                                                (SELECT json_build_object(
                                                                                                                            'TelephoneNumber_en',   p."TelephoneNumber_en",
                                                                                                                            'TelephoneNumber_de',   p."TelephoneNumber_de",
                                                                                                                            'AvailableTime_en',     p."AvailableTime_en",
                                                                                                                            'AvailableTime_de',     p."AvailableTime_de",
                                                                                                                            'TypeOfTelephone',      p."TypeOfTelephone"
                                                                                                                        )
                                                                                                                 FROM "Phone" p
                                                                                                                 WHERE p."ContactInformationId" = ci."Id"
                                                                                                                 LIMIT 1),
                                                                                                                '{}'::json
                                                                                                            ),
                                                                        'Fax',                              COALESCE(
                                                                                                                (SELECT json_build_object(
                                                                                                                            'FaxNumber_en',         f."FaxNumber_en",
                                                                                                                            'FaxNumber_de',         f."FaxNumber_de",
                                                                                                                            'TypeOfFaxNumber',      f."TypeOfFaxNumber"
                                                                                                                        )
                                                                                                                 FROM "Fax" f
                                                                                                                 WHERE f."ContactInformationId" = ci."Id"
                                                                                                                 LIMIT 1),
                                                                                                                '{}'::json
                                                                                                            ),
                                                                        'Email',                            COALESCE(
                                                                                                                (SELECT json_build_object(
                                                                                                                            'EmailAddress',         e."EmailAddress",
                                                                                                                            'TypeOfEmailAddress',   e."TypeOfEmailAddress",
                                                                                                                            'PublicKey_en',         e."PublicKey_en",
                                                                                                                            'PublicKey_de',         e."PublicKey_de",
                                                                                                                            'TypeOfPublicKey_en',   e."TypeOfPublicKey_en",
                                                                                                                            'TypeOfPublicKey_de',   e."TypeOfPublicKey_de"
                                                                                                                        )
                                                                                                                 FROM "Email" e
                                                                                                                 WHERE e."ContactInformationId" = ci."Id"
                                                                                                                 LIMIT 1),
                                                                                                                '{}'::json
                                                                                                            ),
                                                                        'IPCommunication',                  COALESCE(
                                                                                                                (SELECT json_agg(json_build_object(
                                                                                                                            'AddressOfAdditionalLink',  ip."AddressOfAdditionalLink",
                                                                                                                            'TypeOfCommunication',      ip."TypeOfCommunication",
                                                                                                                            'AvailableTime_en',         ip."AvailableTime_en",
                                                                                                                            'AvailableTime_de',         ip."AvailableTime_de"
                                                                                                                        ))
                                                                                                                 FROM "ContactInformationIPCommunication" ciip
                                                                                                                 JOIN "IPCommunication" ip ON ip."Id" = ciip."IPCommunicationId"
                                                                                                                 WHERE ciip."ContactInformationId" = ci."Id"),
                                                                                                                '[]'::json
                                                                                                            )
                                                                    )),
                                                                    '[]'::json
                                                                )
                                    )
    )
     FROM "AssetContactInformation" aci
     JOIN "ContactInformation" ci ON ci."Id" = aci."ContactInformationId"
     WHERE aci."AssetId" = (SELECT "Id" FROM asset_cte)),
    '{}'::json
) AS "Result";
