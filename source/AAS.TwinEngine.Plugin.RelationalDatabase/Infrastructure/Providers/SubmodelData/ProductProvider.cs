using System.Data.Common;
using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.QueryExecutor;

using Npgsql;

using NpgsqlTypes;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData;

public class ProductProvider(IQueryExecutor queryExecutor) : IProductProvider
{
    private const string CreateProductLockName = "dpp_create_product";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> DeleteProductAsync(string sqlQuery, string productId, CancellationToken cancellationToken)
    {
        var parameters = new List<DbParameter>
        {
            new NpgsqlParameter("@ProductId", NpgsqlDbType.Varchar) { Value = productId }
        };

        var deletedProductId = await queryExecutor
            .ExecuteInTransactionAsync(sqlQuery, parameters, CreateProductLockName, cancellationToken)
            .ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(deletedProductId);
    }

    public async Task<CreatedProduct?> UpdateProductAsync(string sqlQuery, UpdatedProduct product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var parameters = new List<DbParameter>
        {
            new NpgsqlParameter("@ProductId", NpgsqlDbType.Varchar) { Value = product.ProductId },
            new NpgsqlParameter("@DateOfManufacture", NpgsqlDbType.Date) { Value = product.DateOfManufacture },
            new NpgsqlParameter("@ManufacturerName", NpgsqlDbType.Text) { Value = product.ManufacturerName },
            new NpgsqlParameter("@ManufacturerArticleNumber", NpgsqlDbType.Text) { Value = product.ManufacturerArticleNumber },
            new NpgsqlParameter("@PcfCO2eq", NpgsqlDbType.Numeric) { Value = product.PcfCO2eq },
            new NpgsqlParameter("@ReferenceImpactUnitForCalculation", NpgsqlDbType.Text) { Value = product.ReferenceImpactUnitForCalculation },
            new NpgsqlParameter("@Street", NpgsqlDbType.Text) { Value = product.Street },
            new NpgsqlParameter("@Zipcode", NpgsqlDbType.Text) { Value = product.Zipcode },
            new NpgsqlParameter("@CityTown", NpgsqlDbType.Text) { Value = product.CityTown },
            new NpgsqlParameter("@NationalCode", NpgsqlDbType.Text) { Value = product.NationalCode },
            new NpgsqlParameter("@SerialNumber", NpgsqlDbType.Text) { Value = product.SerialNumber },
            new NpgsqlParameter("@ManufacturerProductType", NpgsqlDbType.Text) { Value = product.ManufacturerProductType },
            new NpgsqlParameter("@QuantityOfMeasureForCalculation", NpgsqlDbType.Numeric) { Value = product.QuantityOfMeasureForCalculation },
            new NpgsqlParameter("@PublicationDate", NpgsqlDbType.Timestamp) { Value = product.PublicationDate },
            new NpgsqlParameter("@ExpirationDate", NpgsqlDbType.Timestamp) { Value = product.ExpirationDate },
            new NpgsqlParameter("@Length", NpgsqlDbType.Numeric) { Value = product.Length },
            new NpgsqlParameter("@Width", NpgsqlDbType.Numeric) { Value = product.Width },
            new NpgsqlParameter("@Height", NpgsqlDbType.Numeric) { Value = product.Height }
        };

        var jsonResult = await queryExecutor
            .ExecuteInTransactionAsync(sqlQuery, parameters, CreateProductLockName, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CreatedProduct>(jsonResult, SerializerOptions);
    }

    public async Task<CreatedProduct?> CreateProductAsync(string sqlQuery, NewProduct product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var parameters = new List<DbParameter>
        {
            new NpgsqlParameter("@IdShort", NpgsqlDbType.Varchar) { Value = product.IdShort },
            new NpgsqlParameter("@DateOfManufacture", NpgsqlDbType.Date) { Value = product.DateOfManufacture },
            new NpgsqlParameter("@ManufacturerName", NpgsqlDbType.Text) { Value = product.ManufacturerName },
            new NpgsqlParameter("@ManufacturerArticleNumber", NpgsqlDbType.Text) { Value = product.ManufacturerArticleNumber },
            new NpgsqlParameter("@PcfCO2eq", NpgsqlDbType.Numeric) { Value = product.PcfCO2eq },
            new NpgsqlParameter("@ReferenceImpactUnitForCalculation", NpgsqlDbType.Text) { Value = product.ReferenceImpactUnitForCalculation }
        };

        var jsonResult = await queryExecutor
            .ExecuteInTransactionAsync(sqlQuery, parameters, CreateProductLockName, cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(jsonResult)
            ? null
            : JsonSerializer.Deserialize<CreatedProduct>(jsonResult, SerializerOptions);
    }
}
