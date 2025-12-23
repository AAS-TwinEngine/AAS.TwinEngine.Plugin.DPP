using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public class SemanticTreeHandler(IJsonSchemaValidator jsonSchemaValidator) : ISemanticTreeHandler
{
    public JsonObject GetJson(SemanticTreeNode semanticTreeNodeWithValues, JsonSchema dataQuery)
    {
        var jsonNode = ConvertTreeNodeToJson(semanticTreeNodeWithValues);

        var wrappedJsonObject = new JsonObject { [semanticTreeNodeWithValues?.SemanticId!] = jsonNode };

        var serializedJson = JsonSerializer.Serialize(wrappedJsonObject);

        jsonSchemaValidator.ValidateResponseContent(serializedJson, dataQuery);

        return wrappedJsonObject;
    }

    private static JsonNode ConvertTreeNodeToJson(SemanticTreeNode treeNode)
    {
        return treeNode switch
        {
            SemanticLeafNode leafNode => ConvertLeafToJsonValue(leafNode),
            SemanticBranchNode branchNode => ConvertBranchToJsonStructure(branchNode),
            _ => throw new NotImplementedException($"Unsupported node type: {treeNode.GetType()}")
        };
    }

    private static JsonValue ConvertLeafToJsonValue(SemanticLeafNode leafNode)
    {
        return leafNode.DataType switch
        {
            DataType.Boolean => ParseAsBooleanOrString(leafNode.Value),
            DataType.Integer => ParseAsIntegerOrString(leafNode.Value),
            DataType.Number => ParseAsNumberOrString(leafNode.Value),
            DataType.String => JsonValue.Create(leafNode.Value),
            _ => JsonValue.Create(leafNode.Value)
        };
    }

    private static JsonValue ParseAsBooleanOrString(string textValue)
    {
        return bool.TryParse(textValue, out var booleanValue)
            ? JsonValue.Create(booleanValue)
            : JsonValue.Create(textValue);
    }

    private static JsonValue ParseAsIntegerOrString(string textValue)
    {
        return int.TryParse(textValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue)
            ? JsonValue.Create(integerValue)
            : JsonValue.Create(textValue);
    }

    private static JsonValue ParseAsNumberOrString(string textValue)
    {
        return double.TryParse(textValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numberValue)
            ? JsonValue.Create(numberValue)
            : JsonValue.Create(textValue);
    }

    private static JsonNode ConvertBranchToJsonStructure(SemanticBranchNode branchNode)
    {
        var isArrayType = branchNode.DataType == DataType.Array;
        var hasOnlyBranchChildren = branchNode.Children.All(child => child is SemanticBranchNode);
        var hasOnlyLeafChildren = branchNode.Children.All(child => child is SemanticLeafNode);
        var childrenShareSameSemanticId = branchNode.Children.Select(child => child.SemanticId).Distinct().Count() == 1;
        var childrenMatchParentSemanticId = branchNode.Children.All(child => child is SemanticLeafNode && child.SemanticId == branchNode.SemanticId);
        var hasSingleChild = branchNode.Children.Count == 1;

        if (isArrayType)
        {
            var jsonArray = new JsonArray();

            if (ShouldCreateArrayOfBranchObjects(hasOnlyBranchChildren, childrenShareSameSemanticId, hasSingleChild))
            {
                foreach (var childBranch in branchNode.Children.Cast<SemanticBranchNode>())
                {
                    jsonArray.Add(ConvertTreeNodeToJson(childBranch));
                }
                return jsonArray;
            }

            if (ShouldCreateArrayOfLeafObjects(hasOnlyLeafChildren, childrenShareSameSemanticId, hasSingleChild))
            {
                foreach (var leafChild in branchNode.Children.Cast<SemanticLeafNode>())
                {
                    jsonArray.Add(CreateJsonObjectFromLeaf(leafChild));
                }
                return jsonArray;
            }

            var singleJsonObject = CreateJsonObjectFromChildren(branchNode.Children);
            jsonArray.Add(singleJsonObject);
            return jsonArray;
        }

        return CreateJsonObjectFromChildren(branchNode.Children);
    }

    private static bool ShouldCreateArrayOfBranchObjects(bool hasOnlyBranches, bool sharesSameId, bool isSingle) => hasOnlyBranches && sharesSameId && !isSingle;

    private static bool ShouldCreateArrayOfLeafObjects(bool hasOnlyLeaves, bool sharesSameId, bool isSingle) => hasOnlyLeaves && sharesSameId && !isSingle;

    private static JsonObject CreateJsonObjectFromLeaf(SemanticLeafNode leafNode)
    {
        return new JsonObject
        {
            [leafNode.SemanticId] = ConvertLeafToJsonValue(leafNode)
        };
    }

    private static JsonObject CreateJsonObjectFromChildren(IEnumerable<SemanticTreeNode> children)
    {
        var jsonObject = new JsonObject();

        var groupedBySemanticId = children.GroupBy(child => child.SemanticId);

        foreach (var group in groupedBySemanticId)
        {
            var convertedNodes = group.Select(ConvertTreeNodeToJson).ToList();
            var nodeCount = convertedNodes.Count;
            var allNodesAreArrays = convertedNodes.All(node => node is JsonArray);
            var hasSingleNode = nodeCount == 1;

            if (hasSingleNode)
            {
                jsonObject[group.Key] = convertedNodes[0];
            }
            else if (allNodesAreArrays)
            {
                jsonObject[group.Key] = MergeJsonArrays(convertedNodes);
            }
            else
            {
                jsonObject[group.Key] = WrapNodesInArray(convertedNodes);
            }
        }

        return jsonObject;
    }

    private static JsonArray MergeJsonArrays(IEnumerable<JsonNode> arrayNodes)
    {
        var mergedArray = new JsonArray();

        foreach (var arrayNode in arrayNodes.Cast<JsonArray>())
        {
            foreach (var element in arrayNode)
            {
                mergedArray.Add(element?.DeepClone());
            }
        }

        return mergedArray;
    }

    private static JsonArray WrapNodesInArray(IEnumerable<JsonNode> nodes)
    {
        var wrapperArray = new JsonArray();

        foreach (var node in nodes)
        {
            wrapperArray.Add(node?.DeepClone());
        }

        return wrapperArray;
    }
}

