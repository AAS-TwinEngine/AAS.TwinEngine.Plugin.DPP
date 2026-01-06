using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.MetaData.Helper;

public class PaginatorTests
{
    private static List<TestItem> CreateItems(int count)
        => Enumerable.Range(1, count)
            .Select(i => new TestItem { Id = $"id-{i}" })
            .ToList();

    #region No Cursor / No Limit

    [Fact]
    public void GetPagedResult_NoCursor_NoLimit_ReturnsAllItems_AndNullCursor()
    {
        var items = CreateItems(5);

        var (pagedItems, paging) = Paginator.GetPagedResult(
            items,
            x => x.Id,
            limit: null,
            cursor: null);

        Assert.Equal(5, pagedItems.Count);
        Assert.Null(paging.Cursor);
    }

    #endregion

    #region Limit Only

    [Fact]
    public void GetPagedResult_LimitOnly_ReturnsLimitedItems_WithNextCursor()
    {
        var items = CreateItems(10);

        var (pagedItems, paging) = Paginator.GetPagedResult(
            items,
            x => x.Id,
            limit: 3,
            cursor: null);

        Assert.Equal(3, pagedItems.Count);
        Assert.Equal("id-3", pagedItems.Last().Id);
        Assert.Equal("id-3".EncodeBase64(), paging.Cursor);
    }

    #endregion

    #region Cursor Only

    [Fact]
    public void GetPagedResult_CursorOnly_StartsAfterCursor()
    {
        var items = CreateItems(5);
        var cursor = "id-2".EncodeBase64();

        var (pagedItems, paging) = Paginator.GetPagedResult(
            items,
            x => x.Id,
            limit: null,
            cursor: cursor);

        Assert.Equal(3, pagedItems.Count);
        Assert.Equal("id-3", pagedItems.First().Id);
        Assert.Equal("id-5".EncodeBase64(), paging.Cursor);
    }

    #endregion

    #region Cursor + Limit

    [Fact]
    public void GetPagedResult_CursorAndLimit_ReturnsCorrectPage()
    {
        var items = CreateItems(10);
        var cursor = "id-4".EncodeBase64();

        var (pagedItems, paging) = Paginator.GetPagedResult(
            items,
            x => x.Id,
            limit: 3,
            cursor: cursor);

        Assert.Equal(3, pagedItems.Count);
        Assert.Equal("id-5", pagedItems.First().Id);
        Assert.Equal("id-7".EncodeBase64(), paging.Cursor);
    }

    #endregion

    #region Cursor Not Found

    [Fact]
    public void GetPagedResult_CursorNotFound_StartsFromBeginning()
    {
        var items = CreateItems(5);
        var cursor = "non-existing-id".EncodeBase64();

        var (pagedItems, paging) = Paginator.GetPagedResult(
            items,
            x => x.Id,
            limit: 2,
            cursor: cursor);

        Assert.Equal(2, pagedItems.Count);
        Assert.Equal("id-1", pagedItems.First().Id);
        Assert.Equal("id-2".EncodeBase64(), paging.Cursor);
    }

    #endregion

    #region Cursor At Last Item

    [Fact]
    public void GetPagedResult_CursorAtLastItem_ReturnsEmptyPage_AndNullCursor()
    {
        var items = CreateItems(3);
        var cursor = "id-3".EncodeBase64();

        var (pagedItems, paging) = Paginator.GetPagedResult(
            items,
            x => x.Id,
            limit: 5,
            cursor: cursor);

        Assert.Empty(pagedItems);
        Assert.Null(paging.Cursor);
    }

    #endregion

    private class TestItem
    {
        public string Id { get; set; } = string.Empty;
    }
}
