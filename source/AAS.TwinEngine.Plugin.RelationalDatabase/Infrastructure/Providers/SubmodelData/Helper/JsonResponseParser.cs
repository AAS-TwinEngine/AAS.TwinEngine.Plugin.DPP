using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.SubmodelData.Helper;

public class JsonResponseParser(IOptions<Semantics> semanticsOptions, ILogger<JsonResponseParser> logger) : IJsonResponseParser
{
    private readonly string _indexPrefix = semanticsOptions.Value.IndexContextPrefix;

    public SemanticTreeNode ParseJson(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.String)
            {
                return ConvertJsonElement(root);
            }

            var jsonString = root.GetString();
            using var nestedDoc = JsonDocument.Parse(jsonString!);
            return ConvertJsonElement(nestedDoc.RootElement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid JSON received from database");
            throw new ResponseParsingException();
        }
    }

    private SemanticTreeNode ConvertJsonElement(JsonElement element)
    {
        var properties = element.EnumerateObject().ToList();

        if (properties.Count == 0)
        {
            return new SemanticBranchNode(string.Empty, DataType.Unknown);
        }

        if (properties is [{ Value.ValueKind: JsonValueKind.String } _])
        {
            return new SemanticLeafNode(properties[0].Name, DataType.String, properties[0].Value.ToString());
        }

        var rootProperty = element.EnumerateObject().First();

        var rootBranch = new SemanticBranchNode(rootProperty.Name, GetDataType(rootProperty.Value.ValueKind));

        ProcessJsonValue(rootProperty.Value, rootBranch);

        return rootBranch;
    }

    private void ProcessJsonValue(JsonElement valueElement, SemanticBranchNode parentBranch)
    {
        switch (valueElement.ValueKind)
        {
            case JsonValueKind.Object:
                ProcessJsonObject(valueElement, parentBranch);
                break;

            case JsonValueKind.Array:
                ProcessJsonArray(valueElement, parentBranch);
                break;

            default:
                parentBranch.AddChild(new SemanticLeafNode(
                    parentBranch.SemanticId,
                    GetDataType(valueElement.ValueKind),
                    valueElement.ToString()
                ));
                break;
        }
    }

    private void ProcessJsonObject(JsonElement objectElement, SemanticBranchNode parentBranch)
    {
        foreach (var property in objectElement.EnumerateObject())
        {
            if (IsPrimitiveValue(property.Value))
            {
                parentBranch.AddChild(new SemanticLeafNode(
                    property.Name,
                    GetDataType(property.Value.ValueKind),
                    property.Value.ToString()
                ));
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var baseSemanticId = property.Name;
                ProcessJsonArray(property.Value, parentBranch, baseSemanticId);
            }
            else
            {
                var branchNode = new SemanticBranchNode(property.Name, GetDataType(property.Value.ValueKind));
                ProcessJsonValue(property.Value, branchNode);
                parentBranch.AddChild(branchNode);
            }
        }
    }

    private void ProcessJsonArray(JsonElement arrayElement, SemanticBranchNode parentBranch, string? baseSemanticId = null)
    {
        var arrayLength = arrayElement.GetArrayLength();
        var semanticIdBase = baseSemanticId ?? parentBranch.SemanticId;
        var elementDataType = GetDataType(arrayElement.ValueKind);

        if (arrayLength > 1)
        {
            for (var i = 0; i < arrayLength; i++)
            {
                var indexedSemanticId = $"{semanticIdBase}{_indexPrefix}{i}";
                var arrayItemBranch = new SemanticBranchNode(indexedSemanticId, elementDataType);
                ProcessJsonValue(arrayElement[i], arrayItemBranch);
                parentBranch.AddChild(arrayItemBranch);
            }

            return;
        }

        foreach (var item in arrayElement.EnumerateArray())
        {
            var arrayItemBranch = new SemanticBranchNode(semanticIdBase, elementDataType);
            ProcessJsonValue(item, arrayItemBranch);
            parentBranch.AddChild(arrayItemBranch);
        }
    }

    private static bool IsPrimitiveValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String or
            JsonValueKind.Number or
            JsonValueKind.True or
            JsonValueKind.False or
            JsonValueKind.Null => true,
            _ => false
        };
    }

    private static DataType GetDataType(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.String => DataType.String,
            JsonValueKind.Number => DataType.Number,
            JsonValueKind.True or JsonValueKind.False => DataType.Boolean,
            JsonValueKind.Object => DataType.Object,
            JsonValueKind.Array => DataType.Array,
            JsonValueKind.Null => DataType.Null,
            _ => DataType.Unknown
        };
    }
}
