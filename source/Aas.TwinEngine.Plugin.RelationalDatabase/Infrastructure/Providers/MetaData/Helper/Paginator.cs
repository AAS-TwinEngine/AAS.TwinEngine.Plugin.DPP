using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

public class Paginator
{
    public static (IList<T> Items, PagingMetaData PagingMetaData) GetPagedResult<T>(
        IList<T> allItems,
        Func<T, string> getId,
        int? limit,
        string? cursor)
    {
        var startIndex = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            var lastId = cursor.DecodeBase64();
            startIndex = allItems.ToList().FindIndex(item => getId(item) == lastId) + 1;
        }

        var pageSize = limit ?? 100;
        var pagedItems = allItems.Skip(startIndex).Take(pageSize).ToList();

        string? nextCursor = null;

        if (limit == null && cursor == null && pagedItems.Count < pageSize)
        {
            return (pagedItems, new PagingMetaData { Cursor = nextCursor });
        }

        var lastItem = pagedItems.LastOrDefault();
        if (lastItem != null)
        {
            nextCursor = getId(lastItem).EncodeBase64();
        }

        return (pagedItems, new PagingMetaData { Cursor = nextCursor });
    }
}
