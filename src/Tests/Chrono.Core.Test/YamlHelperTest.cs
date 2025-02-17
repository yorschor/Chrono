using Chrono.Core.Helpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Chrono.Core.Test;

public class YamlHelperTest
{
    [Fact]
    public void MergeYamlContent_ValidYaml_ReturnsMergedContent()
    {
        const string baseYaml = """
                                version: "1.0.0"
                                default:
                                  versionSchema: "{major}.{minor}.{patch}"
                                  precision: "minor"  
                                  release:
                                    match:
                                      - "^release/.*"
                                """;

        const string overrideYaml = """
                                    version: "3.2.4"
                                    default:
                                        versionSchema: "{major}.{minor}.{patch}[-]{branchname}[-]{commitShortHash}"
                                    """;

        const string expectedMergedYaml = """
                                          version: "3.2.4"
                                          default:
                                            versionSchema: "{major}.{minor}.{patch}[-]{branchname}[-]{commitShortHash}"
                                            precision: "minor"  
                                            release:
                                              match:
                                                - "^release/.*"
                                          """;

        var mergedYaml = YamlHelper.MergeYamlContent(baseYaml, overrideYaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var mergedObject = deserializer.Deserialize(new StringReader(mergedYaml));
        var expectedObject = deserializer.Deserialize(new StringReader(expectedMergedYaml));

        Assert.True(AreEqual(mergedObject, expectedObject), "The merged YAML content does not match the expected content.");
    }


    private bool AreEqual(object? obj1, object? obj2)
    {
        if (obj1 == null || obj2 == null)
        {
            return obj1 == obj2;
        }

        if (obj1.GetType() != obj2.GetType())
        {
            return false;
        }

        switch (obj1)
        {
            case IDictionary<object, object> dict1 when obj2 is IDictionary<object, object> dict2:
            {
                if (dict1.Count != dict2.Count)
                {
                    return false;
                }

                foreach (var key in dict1.Keys)
                {
                    if (!dict2.ContainsKey(key))
                    {
                        return false;
                    }

                    if (!AreEqual(dict1[key], dict2[key]))
                    {
                        return false;
                    }
                }

                return true;
            }
            case IList<object> list1 when obj2 is IList<object> list2:
            {
                if (list1.Count != list2.Count)
                {
                    return false;
                }

                for (int i = 0; i < list1.Count; i++)
                {
                    if (!AreEqual(list1[i], list2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            default:
                return obj1.Equals(obj2);
        }
    }
}