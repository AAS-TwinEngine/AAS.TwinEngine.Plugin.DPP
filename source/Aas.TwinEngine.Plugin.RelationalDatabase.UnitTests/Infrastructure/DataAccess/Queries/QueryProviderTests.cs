using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Queries;

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
        // Arrange
        var serviceName = "shells";
        var expectedSql = "SELECT * FROM shells;";

        var queryPath = Path.Combine(
            _tempRoot,
            "Infrastructure",
            "DataAccess",
            "Queries",
            $"{serviceName}.sql");

        File.WriteAllText(queryPath, expectedSql);

        // Act
        var result = _sut.GetQuery(serviceName);

        // Assert
        Assert.Equal(expectedSql, result);
    }

    #endregion

    #region File not found

    [Fact]
    public void GetQuery_WhenFileDoesNotExist_ReturnsNull()
    {
        // Act
        var result = _sut.GetQuery("missing-query");

        // Assert
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
    public void GetQuery_WhenServiceNameIsInvalid_ThrowsArgumentException(string? serviceName)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _sut.GetQuery(serviceName!));
        Assert.Equal("serviceName", ex.ParamName);
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
