using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

public static class Paginator
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
            var foundIndex = allItems.ToList().FindIndex(item => getId(item) == lastId);
            startIndex = foundIndex >= 0 ? foundIndex + 1 : 0;
        }

        var pageSize = limit ?? 100;
        var pagedItems = allItems.Skip(startIndex).Take(pageSize).ToList();

        string? nextCursor = null;

        var isFirstPageWithNoMoreResults = limit == null && cursor == null && pagedItems.Count < pageSize;
        if (isFirstPageWithNoMoreResults)
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
