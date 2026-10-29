using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public record UpdateProductRequest(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("dateOfManufacture")] DateOnly? DateOfManufacture,
    [property: JsonPropertyName("addressInformation")] ProductAddressRequest? AddressInformation,
    [property: JsonPropertyName("serialNumber")] string? SerialNumber,
    [property: JsonPropertyName("manufacturerProductType")] string? ManufacturerProductType,
    [property: JsonPropertyName("manufacturerName")] string? ManufacturerName,
    [property: JsonPropertyName("manufacturerArticleNumber")] string? ManufacturerArticleNumber,
    [property: JsonPropertyName("pcfCO2eq")] decimal? PcfCO2eq,
    [property: JsonPropertyName("referenceImpactUnitForCalculation")] string? ReferenceImpactUnitForCalculation,
    [property: JsonPropertyName("quantityOfMeasureForCalculation")] decimal? QuantityOfMeasureForCalculation,
    [property: JsonPropertyName("publicationDate")] DateOnly? PublicationDate,
    [property: JsonPropertyName("expirationDate")] DateOnly? ExpirationDate,
    [property: JsonPropertyName("length")] decimal? Length,
    [property: JsonPropertyName("width")] decimal? Width,
    [property: JsonPropertyName("height")] decimal? Height);

public record ProductAddressRequest(
    [property: JsonPropertyName("street")] string? Street,
    [property: JsonPropertyName("zipcode")] string? Zipcode,
    [property: JsonPropertyName("cityTown")] string? CityTown,
    [property: JsonPropertyName("nationalCode")] string? NationalCode);