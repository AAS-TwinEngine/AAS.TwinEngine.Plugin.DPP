using System.Text.RegularExpressions;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public partial class ProductHandler(ILogger<ProductHandler> logger, IProductService productService) : IProductHandler
{
    private const int MaxIdShortLength = 100;
    private const int MaxTextLength = 255;

    // AAS idShort constraint (AASd-002).
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_-]*[a-zA-Z0-9_]$|^[a-zA-Z]$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex IdShortPattern();

    public async Task DeleteProductAsync(string productId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId) || productId.Length > 50 || productId.Any(char.IsControl))
        {
            throw new InvalidUserInputException("A productId of at most 50 characters is required in the route.");
        }

        await productService.DeleteProductAsync(productId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CreateProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new InvalidUserInputException("Request body is required.");
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > MaxIdShortLength || !IdShortPattern().IsMatch(name))
        {
            throw new InvalidUserInputException($"'name' is required, must be at most {MaxIdShortLength} characters and a valid AAS idShort.");
        }

        if (request.DateOfManufacture is null)
        {
            throw new InvalidUserInputException("'dateOfManufacture' is required (yyyy-MM-dd).");
        }

        if (request.PcfCO2eq is null or < 0)
        {
            throw new InvalidUserInputException("'pcfCO2eq' is required and must not be negative.");
        }

        var manufacturerName = ValidateText(request.ManufacturerName, "manufacturerName");
        var manufacturerArticleNumber = ValidateText(request.ManufacturerArticleNumber, "manufacturerArticleNumber");
        var referenceImpactUnit = ValidateText(request.ReferenceImpactUnitForCalculation, "referenceImpactUnitForCalculation");

        logger.LogInformation("Start executing create product request for IdShort: {IdShort}", name);

        var product = new NewProduct(
            name,
            request.DateOfManufacture.Value.ToDateTime(TimeOnly.MinValue),
            manufacturerName,
            manufacturerArticleNumber,
            request.PcfCO2eq.Value,
            referenceImpactUnit);

        var created = await productService.CreateProductAsync(product, cancellationToken).ConfigureAwait(false);

        return new CreateProductResponse
        {
            ProductId = created.ProductId,
            IdShort = created.IdShort,
            AasId = created.AasId,
            GlobalAssetId = created.GlobalAssetId
        };
    }

    public async Task<UpdateProductResponse> UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new InvalidUserInputException("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(productId) || productId.Length > 50 || productId.Any(char.IsControl))
        {
            throw new InvalidUserInputException("A productId of at most 50 characters is required in the route.");
        }

        if (request.Id is not null && !string.Equals(request.Id, productId, StringComparison.Ordinal))
        {
            throw new InvalidUserInputException("Body 'id' must match the route productId, or be omitted.");
        }

        if (request.DateOfManufacture is null || request.PublicationDate is null || request.ExpirationDate is null)
        {
            throw new InvalidUserInputException("'dateOfManufacture', 'publicationDate' and 'expirationDate' are required (yyyy-MM-dd).");
        }

        if (request.ExpirationDate < request.PublicationDate)
        {
            throw new InvalidUserInputException("'expirationDate' must not precede 'publicationDate'.");
        }

        var address = request.AddressInformation ?? throw new InvalidUserInputException("'addressInformation' is required.");
        var product = new UpdatedProduct(
            productId,
            request.DateOfManufacture.Value.ToDateTime(TimeOnly.MinValue),
            ValidateText(request.ManufacturerName, "manufacturerName"),
            ValidateText(request.ManufacturerArticleNumber, "manufacturerArticleNumber"),
            ValidateNumber(request.PcfCO2eq, "pcfCO2eq"),
            ValidateText(request.ReferenceImpactUnitForCalculation, "referenceImpactUnitForCalculation"),
            ValidateText(address.Street, "addressInformation.street"),
            ValidateText(address.Zipcode, "addressInformation.zipcode"),
            ValidateText(address.CityTown, "addressInformation.cityTown"),
            ValidateText(address.NationalCode, "addressInformation.nationalCode"),
            ValidateText(request.SerialNumber, "serialNumber"),
            ValidateText(request.ManufacturerProductType, "manufacturerProductType"),
            ValidateNumber(request.QuantityOfMeasureForCalculation, "quantityOfMeasureForCalculation"),
            request.PublicationDate.Value.ToDateTime(TimeOnly.MinValue),
            request.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue),
            ValidateNumber(request.Length, "length"),
            ValidateNumber(request.Width, "width"),
            ValidateNumber(request.Height, "height"));

        var updated = await productService.UpdateProductAsync(product, cancellationToken).ConfigureAwait(false);
        return new UpdateProductResponse
        {
            ProductId = updated.ProductId,
            IdShort = updated.IdShort,
            AasId = updated.AasId,
            GlobalAssetId = updated.GlobalAssetId
        };
    }

    private static decimal ValidateNumber(decimal? value, string fieldName)
    {
        if (value is null or < 0)
        {
            throw new InvalidUserInputException($"'{fieldName}' is required and must not be negative.");
        }

        return value.Value;
    }

    private static string ValidateText(string? value, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxTextLength || trimmed.Any(char.IsControl))
        {
            throw new InvalidUserInputException($"'{fieldName}' is required and must be at most {MaxTextLength} characters.");
        }

        return trimmed;
    }
}
