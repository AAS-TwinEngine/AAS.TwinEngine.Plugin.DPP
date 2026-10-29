using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;

public interface IProductService
{
    Task DeleteProductAsync(string productId, CancellationToken cancellationToken);

    Task<CreatedProduct> CreateProductAsync(NewProduct product, CancellationToken cancellationToken);

    Task<CreatedProduct> UpdateProductAsync(UpdatedProduct product, CancellationToken cancellationToken);
}
