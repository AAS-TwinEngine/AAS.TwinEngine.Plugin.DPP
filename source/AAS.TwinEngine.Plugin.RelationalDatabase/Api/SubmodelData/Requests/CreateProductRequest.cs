using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

public sealed record CreateProductRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("dateOfManufacture")] DateOnly? DateOfManufacture,
    [property: JsonPropertyName("manufacturerName")] string? ManufacturerName,
    [property: JsonPropertyName("manufacturerArticleNumber")] string? ManufacturerArticleNumber,
    [property: JsonPropertyName("pcfCO2eq")] decimal? PcfCO2eq,
    [property: JsonPropertyName("referenceImpactUnitForCalculation")] string? ReferenceImpactUnitForCalculation);
