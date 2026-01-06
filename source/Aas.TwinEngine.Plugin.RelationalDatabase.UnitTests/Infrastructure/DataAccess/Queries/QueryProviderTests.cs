using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.DataAccess.Queries;

public class QueryProviderTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<QueryProvider> _logger;
    private readonly QueryProvider _sut;

    public QueryProviderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            Path.Combine(_tempRoot, "Infrastructure", "DataAccess", "Queries"));

        _env = Substitute.For<IWebHostEnvironment>();
        _env.ContentRootPath.Returns(_tempRoot);

        _logger = Substitute.For<ILogger<QueryProvider>>();

        _sut = new QueryProvider(_logger, _env);
    }

    #region Happy path

    [Fact]
    public void GetQuery_WhenSqlFileExists_ReturnsFileContent()
    {
        var serviceName = "shells";
        var expectedSql = "SELECT * FROM shells;";

        var queryPath = Path.Combine(
            _tempRoot,
            "Infrastructure",
            "DataAccess",
            "Queries",
            $"{serviceName}.sql");

        File.WriteAllText(queryPath, expectedSql);

        var result = _sut.GetQuery(serviceName);

        Assert.Equal(expectedSql, result);
    }

    #endregion

    #region File not found

    [Fact]
    public void GetQuery_WhenFileDoesNotExist_ReturnsNull()
    {
        var result = _sut.GetQuery("missing-query");

        Assert.Null(result);
    }

    #endregion

    #region Invalid service names

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("../shells")]
    [InlineData("shells/")]
    [InlineData("shells\\")]
    [InlineData("shells*")]
    [InlineData("shells?")]
    public void GetQuery_WhenServiceNameIsInvalid_ThrowsInvalidUserInputException(string? serviceName)
    {
        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.GetQuery(serviceName!));
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
