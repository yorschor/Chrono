using Chrono.Core.Helpers;

namespace Chrono.Core.Test;

public class DirectoryHelperFileNamesTest : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("chrono-file-names").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }

    private string Touch(params string[] relativePath)
    {
        var path = Path.Combine([_root, .. relativePath]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "version: 1.0.0");
        return path;
    }

    [Fact]
    public void Find_CustomName_FindsOnlyThatName()
    {
        var component = Touch("component.yml");
        Touch("other.yml");

        var result = DirectoryHelper.Find(_root, _root, ["component.yml"]);

        Assert.True(result.Success, result.Message);
        Assert.Equal(component, result.Data);
    }

    [Fact]
    public void Find_CustomName_IsCaseInsensitive()
    {
        var file = Touch("Component.YML");

        var result = DirectoryHelper.Find(_root, _root, ["component.yml"]);

        Assert.True(result.Success, result.Message);
        Assert.Equal(file, result.Data);
    }

    [Theory]
    [InlineData("component.yml", "component.yaml")]
    [InlineData("component.yaml", "component.yml")]
    [InlineData("Component.YML", "COMPONENT.YAML")]
    public void Find_CustomName_AlsoMatchesTheOtherYamlExtension(string requested, string onDisk)
    {
        var file = Touch(onDisk);

        var result = DirectoryHelper.Find(_root, _root, [requested]);

        Assert.True(result.Success, result.Message);
        Assert.Equal(file, result.Data);
    }

    [Fact]
    public void Find_NameAsWritten_BeatsItsAliasInTheSameDirectory()
    {
        Touch("component.yaml");
        var asWritten = Touch("component.yml");

        Assert.Equal(asWritten, DirectoryHelper.Find(_root, _root, ["component.yml"]).Data);
    }

    [Fact]
    public void Find_NonYamlExtension_IsNotAliased()
    {
        Touch("component.json");
        Touch("component.yml");

        var result = DirectoryHelper.Find(_root, _root, ["component.json"]);

        Assert.True(result.Success, result.Message);
        Assert.EndsWith("component.json", result.Data);
        Assert.False(DirectoryHelper.Find(_root, _root, ["component.txt"]).Success);
    }

    [Fact]
    public void Find_DefaultNameInList_StillAcceptsYaml()
    {
        var file = Touch("VERSION.YAML");

        var result = DirectoryHelper.Find(_root, _root, ["version.yml"]);

        Assert.True(result.Success, result.Message);
        Assert.Equal(file, result.Data);
    }

    [Fact]
    public void Find_SameDirectory_FirstNameInListWins()
    {
        var component = Touch("component.yml");
        var version = Touch("version.yml");

        Assert.Equal(component, DirectoryHelper.Find(_root, _root, ["component.yml", "version.yml"]).Data);
        Assert.Equal(version, DirectoryHelper.Find(_root, _root, ["version.yml", "component.yml"]).Data);
    }

    [Fact]
    public void Find_NearerDirectoryWinsOverListOrder()
    {
        Touch("version.yml");
        var component = Touch("svc", "component.yml");
        var start = Path.Combine(_root, "svc");

        var result = DirectoryHelper.Find(start, _root, ["version.yml", "component.yml"]);

        Assert.True(result.Success, result.Message);
        Assert.Equal(component, result.Data);
    }

    [Fact]
    public void Find_FallsBackToRootFileWhenNearNameIsMissing()
    {
        var version = Touch("version.yml");
        Directory.CreateDirectory(Path.Combine(_root, "svc", "sub"));
        var start = Path.Combine(_root, "svc", "sub");

        var result = DirectoryHelper.Find(start, _root, ["component.yml", "version.yml"]);

        Assert.True(result.Success, result.Message);
        Assert.Equal(version, result.Data);
    }

    [Fact]
    public void Find_NothingFound_MessageNamesTheRequestedFiles()
    {
        var result = DirectoryHelper.Find(_root, _root, ["component.yml", "other.yml"]);

        Assert.False(result.Success);
        Assert.Contains("component.yml", result.Message);
        Assert.Contains("other.yml", result.Message);
    }

    [Fact]
    public void FindUpwards_NearerDirectoryWins_ThenListOrderInSameDirectory()
    {
        Touch("version.yml");
        var component = Touch("svc", "component.yml");
        Touch("svc", "version.yml");
        var start = Path.Combine(_root, "svc", "sub");
        Directory.CreateDirectory(start);

        Assert.Equal(component, DirectoryHelper.FindUpwards(start, ["component.yml", "version.yml"]).Data);
    }

    [Fact]
    public void FindUpwards_FallsBackToParentDirectory()
    {
        var version = Touch("version.yml");
        var start = Path.Combine(_root, "svc");
        Directory.CreateDirectory(start);

        Assert.Equal(version, DirectoryHelper.FindUpwards(start, ["component.yml", "version.yml"]).Data);
    }

    [Theory]
    [InlineData(null, new[] { "version.yml" })]
    [InlineData("", new[] { "version.yml" })]
    [InlineData("   ", new[] { "version.yml" })]
    [InlineData("component.yml", new[] { "component.yml" })]
    [InlineData("component.yml, version.yml", new[] { "component.yml", "version.yml" })]
    [InlineData("a.yml;b.yml , c.yml", new[] { "a.yml", "b.yml", "c.yml" })]
    [InlineData(",,", new string[0])]
    public void ParseFileNames_SplitsAndTrims(string? input, string[] expected)
    {
        Assert.Equal(expected, DirectoryHelper.ParseFileNames(input!));
    }

    [Theory]
    [InlineData("component.yml")]
    [InlineData("version.yaml")]
    public void ValidateFileNames_AcceptsPlainNames(string name)
    {
        Assert.True(DirectoryHelper.ValidateFileNames([name]).Success);
    }

    [Theory]
    [InlineData("sub/component.yml")]
    [InlineData("..\\component.yml")]
    [InlineData("..")]
    [InlineData(" ")]
    public void ValidateFileNames_RejectsPathsAndEmpty(string name)
    {
        Assert.False(DirectoryHelper.ValidateFileNames([name]).Success);
    }

    [Fact]
    public void ValidateFileNames_RejectsEmptyList()
    {
        Assert.False(DirectoryHelper.ValidateFileNames([]).Success);
    }
}
