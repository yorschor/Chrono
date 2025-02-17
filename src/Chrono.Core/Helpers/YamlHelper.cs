using YamlDotNet.Serialization;

namespace Chrono.Core.Helpers;

public static class YamlHelper
{
    public static string MergeYamlContent(string baseYaml, string overrideYaml)
    {
        var deserializer = new DeserializerBuilder().Build();
        var serializer = new SerializerBuilder().Build();

        // Deserialize the base and override YAML strings into dynamic objects
        var baseYamlObject = deserializer.Deserialize(new StringReader(baseYaml));
        var overrideYamlObject = deserializer.Deserialize(new StringReader(overrideYaml));

        var mergedYamlObject = MergeYamlObjects(baseYamlObject, overrideYamlObject);
        var writer = new StringWriter();
        serializer.Serialize(writer, mergedYamlObject);
        return writer.ToString();
    }

    private static object MergeYamlObjects(object baseObj, object overrideObj)
    {
        switch (baseObj)
        {
            case IDictionary<object, object> baseDict when overrideObj is IDictionary<object, object> overrideDict:
            {
                foreach (var key in overrideDict.Keys)
                {
                    var baseValue = baseDict.ContainsKey(key) ? baseDict[key] : null;
                    baseDict[key] = MergeYamlObjects(baseValue, overrideDict[key]);
                }

                return baseDict;
            }
            case IList<object> when overrideObj is IList<object> overrideList:
                return overrideList;
            default:
                return overrideObj ?? baseObj;
        }
    }
}