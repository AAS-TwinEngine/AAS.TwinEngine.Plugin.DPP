namespace AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

public sealed record NewProduct(
    string IdShort,
    DateTime DateOfManufacture,
    string ManufacturerName,
    string ManufacturerArticleNumber,
    decimal PcfCO2eq,
    string ReferenceImpactUnitForCalculation);

public sealed record CreatedProduct(
    string ProductId,
    string IdShort,
    string AasId,
    string GlobalAssetId);

public sealed record UpdatedProduct(
    string ProductId,
    DateTime DateOfManufacture,
    string ManufacturerName,
    string ManufacturerArticleNumber,
    decimal PcfCO2eq,
    string ReferenceImpactUnitForCalculation,
    string Street,
    string Zipcode,
    string CityTown,
    string NationalCode,
    string SerialNumber,
    string ManufacturerProductType,
    decimal QuantityOfMeasureForCalculation,
    DateTime PublicationDate,
    DateTime ExpirationDate,
    decimal Length,
    decimal Width,
    decimal Height);
