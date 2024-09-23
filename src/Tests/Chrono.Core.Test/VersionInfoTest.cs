using Chrono.Core.Helpers;
using Huxy;
using LibGit2Sharp;

namespace Chrono.Core.Test
{
    public class VersionInfoTest
    {
        private const string TestYamlContent = """

                                               version: '1.0.0'
                                               default:
                                                 versionSchema: '{major}.{minor}.{patch}'
                                                 newBranchSchema: '{branch}schema'
                                                 newTagSchema: 'specificTagSchema[-]{branch}'
                                                 precision: 'high'
                                                 prereleaseTag: 'beta'
                                                 release:
                                                   match:
                                                     - 'release'
                                                   versionSchema: '{major}.{minor}.{patch}'
                                                   newBranchSchema: 'releaseSchema[-]{branch}'
                                                   newTagSchema: 'tagSchema'
                                                   precision: 'high'
                                                   prereleaseTag: 'beta'
                                               branches:
                                                 main:
                                                   match:
                                                     - 'main'
                                                   versionSchema: '{major}.{minor}.{patch}'
                                                   newBranchSchema: 'specificBranchSchema[-]{branch}'
                                                   newTagSchema: 'tagSchema'
                                                   precision: 'high'
                                                   prereleaseTag: 'beta'

                                               """;

        private const string InvalidYamlContent = @"
version: 'invalid_version'
";

        private static VersionInfo CreateVersionInfoInstance(string yamlContent, bool allowDirtyRepo = true)
        {
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, yamlContent);

            return new VersionInfo(tempFilePath, allowDirtyRepo);
        }

        [Fact]
        public void TestConstructor_ValidYaml_CreatesInstance()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);

            Assert.NotNull(versionInfo);
            Assert.Equal(1, versionInfo.Major);
            Assert.Equal(0, versionInfo.Minor);
            Assert.Equal(0, versionInfo.Patch);
            Assert.Equal(-1, versionInfo.Build); // -1 because it's not specified in the YAML
        }

        [Fact]
        public void TestConstructor_InvalidYaml_ThrowsException()
        {
            Assert.Throws<NullReferenceException>(() => CreateVersionInfoInstance(InvalidYamlContent));
        }

        [Fact]
        public void TestParseVersion_ValidSchema_ReturnsParsedVersion()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.GetVersion();

            Assert.True(result.Success);
            Assert.Equal("1.0.0", result.Data);
        }

        [Fact]
        public void TestGetNumericVersion_ValidVersion_ReturnsNumericVersion()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.GetNumericVersion();

            Assert.True(result.Success);
            Assert.Equal("1.0.0", result.Data);
        }

        [Fact]
        public void TestSetVersion_ValidVersion_UpdatesVersion()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.SetVersion("2.1.0");

            Assert.True(result.Success);
            Assert.Equal(2, versionInfo.Major);
            Assert.Equal(1, versionInfo.Minor);
            Assert.Equal(0, versionInfo.Patch);
            Assert.Equal(-1, versionInfo.Build); // -1 because it's not specified in the new version
        }

        [Fact]
        public void TestSetVersion_InvalidVersion_ReturnsError()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.SetVersion("invalid_version");

            Assert.False(result.Success);
            if (result is IErrorResult)
            {
                Assert.Equal("invalid_version is not a valid version!", result.Message);
            }
        }

        [Theory]
        [InlineData(VersionComponent.Major, 2, 0, 0, -1)]
        [InlineData(VersionComponent.Minor, 1, 1, 0, -1)]
        [InlineData(VersionComponent.Patch, 1, 0, 1, -1)]
        [InlineData(VersionComponent.Build, 1, 0, 0, 0)] // Build is -1 initially, it should increment to 0
        public void TestBumpVersion_ValidComponent_BumpsVersion(VersionComponent component, int expectedMajor, int expectedMinor, int expectedPatch,
            int expectedBuild)
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.BumpVersion(component);

            Assert.True(result.Success);
            Assert.Equal(expectedMajor, versionInfo.Major);
            Assert.Equal(expectedMinor, versionInfo.Minor);
            Assert.Equal(expectedPatch, versionInfo.Patch);
            Assert.Equal(expectedBuild, versionInfo.Build);
        }

        [Fact]
        public void TestGetConfigForCurrentBranch_ValidBranch_ReturnsConfig()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.GetConfigForCurrentBranch();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("{major}.{minor}.{patch}", result.Data.VersionSchema);
        }

        [Fact]
        public void GetNewBranchName_ValidBranch_ReturnsNewBranchName()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            versionInfo.BranchName = "aBranchWithAName";
            var result = versionInfo.GetNewBranchName();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("aBranchWithANameschema", result.Data);
        }

        [Fact]
        public void GetNewBranchNameByKey_ValidKey_ReturnsNewBranchName()
        {
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            // Set to some branch
            versionInfo.BranchName = "relaese/Test";
            var result = versionInfo.GetNewBranchNameFromKey("main");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            //Schema from "main" branch is used with current branch name being "relaese/Test"
            Assert.Equal("specificBranchSchema-relaese/Test", result.Data);
        }

        [Fact]
        public void GetNewTagName_ValidTag_ReturnsTagName()
        {
            //default branch config is used
            var versionInfo = CreateVersionInfoInstance(TestYamlContent);
            var result = versionInfo.GetNewTagName();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Assert.Equal($"specificTagSchema-{versionInfo.BranchName}", result.Data);
        }
    }
}