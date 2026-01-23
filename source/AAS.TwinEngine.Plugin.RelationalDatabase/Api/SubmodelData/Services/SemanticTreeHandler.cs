using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;

public class SemanticTreeHandler(IJsonSchemaValidator jsonSchemaValidator) : ISemanticTreeHandler
{
    private static readonly Dictionary<DataType, Func<string, JsonValue>> TypeConverters = new()
    {
        [DataType.Boolean] = ParseAsBooleanOrString,
        [DataType.Integer] = ParseAsIntegerOrString,
        [DataType.Number] = ParseAsNumberOrString,
        [DataType.String] = value => JsonValue.Create(value)!
    };

    public JsonObject GetJson(SemanticTreeNode semanticTreeNodeWithValues, JsonSchema dataQuery)
    {
        ArgumentNullException.ThrowIfNull(semanticTreeNodeWithValues);
        ArgumentNullException.ThrowIfNull(dataQuery);

        try
        {
            var jsonNode = ConvertTreeNodeToJson(semanticTreeNodeWithValues);
            var wrappedJsonObject = WrapInJsonObject(semanticTreeNodeWithValues.SemanticId, jsonNode);
            var serializedJson = JsonSerializer.Serialize(wrappedJsonObject);

            jsonSchemaValidator.ValidateResponseContent(serializedJson, dataQuery);

            return wrappedJsonObject;
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            throw new InvalidOperationException($"Failed to convert semantic tree node '{semanticTreeNodeWithValues.SemanticId}' to JSON.", ex);
        }
    }

    private static JsonObject WrapInJsonObject(string semanticId, JsonNode jsonNode) => new() { [semanticId] = jsonNode };

    private static JsonNode ConvertTreeNodeToJson(SemanticTreeNode treeNode)
    {
        ArgumentNullException.ThrowIfNull(treeNode);

        return treeNode switch
        {
            SemanticLeafNode leafNode => ConvertLeafToJsonValue(leafNode),
            SemanticBranchNode branchNode => ConvertBranchToJsonStructure(branchNode),
            _ => throw new NotSupportedException($"Unsupported node type: {treeNode.GetType().Name}")
        };
    }

    private static JsonValue ConvertLeafToJsonValue(SemanticLeafNode leafNode)
    {
        ArgumentNullException.ThrowIfNull(leafNode);

        return TypeConverters.TryGetValue(leafNode.DataType, out var converter)
            ? converter(leafNode.Value)
            : JsonValue.Create(leafNode.Value)!;
    }

    private static JsonValue ParseAsBooleanOrString(string textValue) => bool.TryParse(textValue, out var boolValue)
                                                                             ? JsonValue.Create(boolValue)!
                                                                             : JsonValue.Create(textValue)!;

    private static JsonValue ParseAsIntegerOrString(string textValue) => int.TryParse(textValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)
                                                                             ? JsonValue.Create(intValue)!
                                                                             : JsonValue.Create(textValue)!;

    private static JsonValue ParseAsNumberOrString(string textValue) => double.TryParse(textValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
                                                                            ? JsonValue.Create(doubleValue)!
                                                                            : JsonValue.Create(textValue)!;

    private static JsonNode ConvertBranchToJsonStructure(SemanticBranchNode branchNode)
    {
        ArgumentNullException.ThrowIfNull(branchNode);

        var analysis = AnalyzeBranchNode(branchNode);

        return branchNode.DataType == DataType.Array
            ? ConvertToJsonArray(branchNode, analysis)
            : CreateJsonObjectFromChildren(branchNode.Children);
    }

    private static BranchAnalysis AnalyzeBranchNode(SemanticBranchNode branchNode)
    {
        var children = branchNode.Children.ToList();

        return new BranchAnalysis(
            HasOnlyBranchChildren: children.All(c => c is SemanticBranchNode),
            HasOnlyLeafChildren: children.All(c => c is SemanticLeafNode),
            ChildrenShareSameSemanticId: children.Select(c => c.SemanticId).Distinct().Count() == 1,
            HasSingleChild: children.Count == 1
        );
    }

    private static JsonArray ConvertToJsonArray(SemanticBranchNode branchNode, BranchAnalysis analysis)
    {
        var jsonArray = new JsonArray();

        if (ShouldCreateArrayOfBranchObjects(analysis))
        {
            branchNode.Children
                .Cast<SemanticBranchNode>()
                .Select(ConvertTreeNodeToJson)
                .ToList()
                .ForEach(jsonArray.Add);

            return jsonArray;
        }

        if (ShouldCreateArrayOfLeafObjects(analysis))
        {
            branchNode.Children
                .Cast<SemanticLeafNode>()
                .Select(CreateJsonObjectFromLeaf)
                .ToList()
                .ForEach(jsonArray.Add);

            return jsonArray;
        }

        jsonArray.Add(CreateJsonObjectFromChildren(branchNode.Children));
        return jsonArray;
    }

    private static bool ShouldCreateArrayOfBranchObjects(BranchAnalysis analysis) => analysis is { HasOnlyBranchChildren: true, ChildrenShareSameSemanticId: true, HasSingleChild: false };

    private static bool ShouldCreateArrayOfLeafObjects(BranchAnalysis analysis) => analysis is { HasOnlyLeafChildren: true, ChildrenShareSameSemanticId: true, HasSingleChild: false };

    private static JsonObject CreateJsonObjectFromLeaf(SemanticLeafNode leafNode) => new() { [leafNode.SemanticId] = ConvertLeafToJsonValue(leafNode) };

    private static JsonObject CreateJsonObjectFromChildren(IEnumerable<SemanticTreeNode> children)
    {
        var jsonObject = new JsonObject();
        var groupedChildren = children.GroupBy(c => c.SemanticId);

        foreach (var group in groupedChildren)
        {
            var convertedNodes = group.Select(ConvertTreeNodeToJson).ToList();
            jsonObject[group.Key] = DetermineJsonNodeStructure(convertedNodes);
        }

        return jsonObject;
    }

    private static JsonNode DetermineJsonNodeStructure(List<JsonNode> convertedNodes)
    {
        return convertedNodes switch
        {
            { Count: 1 } => convertedNodes[0],
            _ when convertedNodes.All(n => n is JsonArray) => MergeJsonArrays(convertedNodes),
            _ => WrapNodesInArray(convertedNodes)
        };
    }

    private static JsonArray MergeJsonArrays(IEnumerable<JsonNode> arrayNodes)
    {
        var mergedArray = new JsonArray();

        arrayNodes
            .Cast<JsonArray>()
            .SelectMany(arr => arr)
            .Select(element => element?.DeepClone())
            .ToList()
            .ForEach(mergedArray.Add);

        return mergedArray;
    }

    private static JsonArray WrapNodesInArray(IEnumerable<JsonNode> nodes)
    {
        var wrapperArray = new JsonArray();

        nodes
            .Select(n => n?.DeepClone())
            .ToList()
            .ForEach(wrapperArray.Add);

        return wrapperArray;
    }

    private record BranchAnalysis(bool HasOnlyBranchChildren, bool HasOnlyLeafChildren, bool ChildrenShareSameSemanticId, bool HasSingleChild);
}

