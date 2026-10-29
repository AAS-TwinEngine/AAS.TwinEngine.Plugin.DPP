using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;

public interface IProductHandler
{
    Task DeleteProductAsync(string productId, CancellationToken cancellationToken);

    Task<CreateProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<UpdateProductResponse> UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken cancellationToken);
}
