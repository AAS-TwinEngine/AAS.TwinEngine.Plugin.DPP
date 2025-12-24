using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

using Json.Schema;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.SubmodelData.Services;

public class JsonSchemaValidatorTests
{
    private readonly JsonSchemaValidator _sut;
    private readonly ILogger<JsonSchemaValidator> _logger;

    public static IEnumerable<object[]> InvalidPrimitives => [
        [SchemaValueType.String,  "name",  123],
        [SchemaValueType.Integer, "count", 12.34],
        [SchemaValueType.Number,  "price", "19.99a"],
        [SchemaValueType.Boolean, "flag",  "flase"],
        [SchemaValueType.Number,  "age",   "8o5"],
        [SchemaValueType.Number,  "age",   "-10n5"],
        [SchemaValueType.Integer, "name",  "10o"],
        [SchemaValueType.Boolean, "flag",  "\"true\""]
    ];

    public JsonSchemaValidatorTests()
    {
        var semantics = Substitute.For<IOptions<Semantics>>();
        semantics.Value.Returns(new Semantics
        {
            IndexContextPrefix = "_aastwinengine_"
        });
        _logger = Substitute.For<ILogger<JsonSchemaValidator>>();
        _sut = new JsonSchemaValidator(semantics, _logger);
    }

    [Fact]
    public void ValidateRequestSchema_NullSchema_ThrowsNotFoundException()
    {
        Assert.Throws<NotFoundException>(() => _sut.ValidateRequestSchema(null!));
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ValidateRequestSchema_WithInvalidJson_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["invalid"] = null!
            })
            .Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ValidSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Schema("http://json-schema.org/draft-07/schema#")
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_EmptySchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_ComplexNestedSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["person"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                        ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
                    })
                    .Build()
            })
            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_SchemaWithDefinitions_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Defs(new Dictionary<string, JsonSchema>
            {
                ["address"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["street"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                    })
                    .Build()
            })
            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateResponseContent_EmptyResponse_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("", schema));
    }

    [Fact]
    public void ValidateResponseContent_NullResponse_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(null!, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhitespaceResponse_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("   ", schema));
    }

    [Fact]
    public void ValidateResponseContent_ValidateJsonSchemaRemovePrefix_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["ContactInformation_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build()
            })
            .Required("ContactInformation_aastwinengine_00")
            .Build();

        const string Json = "{\"ContactInformation\": {}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ValidJsonAndSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Required("name")
            .Build();

        const string Json = "{\"name\": \"Test\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Theory]
    [MemberData(nameof(InvalidPrimitives))]
    public void ValidateResponseContent_InvalidValueType_ThrowsNotFoundException(
        SchemaValueType expectedType,
        string property,
        object rawValue)
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                [property] = new JsonSchemaBuilder().Type(expectedType).Build()
            })
            .Required(property)
            .Build();

        var json = $"{{\"{property}\": {rawValue} }}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(json, schema));
    }

    [Fact]
    public void ValidateResponseContent_SchemaMismatch_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Required("name")
            .Build();

        const string Json = "{}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_InvalidJson_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        const string BadJson = "{ not valid json }";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(BadJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_MalformedJson_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        const string MalformedJson = "{\"key\": }";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(MalformedJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_JsonWithExtraProperties_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Required("name")
            .Build();

        const string Json = "{\"name\": \"Test\", \"extra\": \"value\"} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_NestedObjectValidation_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["person"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchema>
                    {
                        ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                        ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
                    })
                    .Required("name", "age")
                    .Build()
            })
            .Required("person")
            .Build();

        const string Json = "{\"person\": {\"name\": \"John\", \"age\": 30}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ArrayValidation_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["items"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                    .Build()
            })
            .Required("items")
            .Build();

        const string Json = "{\"items\": [\"item1\", \"item2\", \"item3\"]} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithContextPrefixInMultipleProperties_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["Property1_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                ["Property2_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Required("Property1_aastwinengine_00", "Property2_aastwinengine_01")
            .Build();

        const string Json = "{\"Property1\": \"value1\", \"Property2\": \"value2\"} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ComplexSchemaWithNestedArrays_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["users"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(new Dictionary<string, JsonSchema>
                        {
                            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                            ["emails"] = new JsonSchemaBuilder()
                                .Type(SchemaValueType.Array)
                                .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                                .Build()
                        })
                        .Build())
                    .Build()
            })
            .Build();

        const string Json = "{\"users\": [{\"name\": \"Alice\", \"emails\": [\"alice@test.com\"]}, {\"name\": \"Bob\", \"emails\": [\"bob@test.com\"]}]} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithSpecialCharactersInPropertyNames_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["property/with~special_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Required("property/with~special_aastwinengine_00")
            .Build();

        const string Json = "{\"property/with~special\": \"value\"} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_EmptyArray_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["items"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                    .Build()
            })
            .Required("items")
            .Build();

        const string Json = "{\"items\": []} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_LogsError_WhenValidationFails()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Required("name")
            .Build();

        const string Json = "{}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));

        _logger.Received(2).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ValidateResponseContent_WithNullableProperties_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["optionalField"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Null).Build()
            })
            .Build();

        const string Json = "{\"optionalField\": null}";

        _sut.ValidateResponseContent(Json, schema);
    }
}

