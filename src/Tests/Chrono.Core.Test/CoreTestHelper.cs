using System.Diagnostics;
using LibGit2Sharp;

namespace Chrono.Core.Test;

public class CoreTestHelper : IDisposable
{
    public readonly string TempDirectory;

    #region Setup

    public CoreTestHelper()
    {
        TempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(TempDirectory, "version.yml"), GetDefaultVersionFileContent());

        Repository.Init(TempDirectory);
        using (var repo = new Repository(TempDirectory))
        {
            Commands.Stage(repo, "version.yml");
            repo.Commit("Initial commit", new Signature("Tester", "tester@example.com", DateTime.Now),
                new Signature("Tester", "tester@example.com", DateTime.Now));
            if (repo.Head.FriendlyName == "master")
            {
                repo.Branches.Rename("master", "trunk");
            }
        }
    }

    public void Dispose()
    {
        DeleteTempDirectory(TempDirectory);
    }

    #endregion

    #region Internals

    private static string GetDefaultVersionFileContent() => """
                                                            version: 1.0.0
                                                            default:
                                                                dirtyRepo: dirty-repo
                                                                versionSchema: '{major}.{minor}.{patch}[-]{branch}[.]{commitShortHash}'
                                                                prereleaseTag: local
                                                                precision: Minor
                                                                newTagSchema: v{major}.{minor}
                                                                newBranchSchema: 'abranch/v{major}.{minor}'
                                                                release:
                                                                    match:
                                                                        - tag::^v.*
                                                                    newTagSchema: 'v{major}.{minor}.{patch}'
                                                                    versionSchema: '{major}.{minor}.{patch}'
                                                                    newBranchSchema: release/v{major}.{minor}
                                                            branches:
                                                                branchRelease:
                                                                    match:
                                                                        - ^release/.*
                                                                    versionSchema: '{major}.{minor}.{patch}-{prereleaseTag}-{commitShortHash}'
                                                                    newBranchSchema: 'release/v{major}.{minor}.{patch}'
                                                                    newTagSchema: v{major}.{minor}.{patch}
                                                                    precision: Patch
                                                                    prereleaseTag: rc
                                                            """;

    private static string CreateTempDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
            return "";
        }
    }

    private static void DeleteTempDirectory(string tempDirectory)
    {
        try
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
                Debug.WriteLine($"Temporary directory {tempDirectory} deleted successfully.");
            }
            else
            {
                Debug.WriteLine($"Directory {tempDirectory} does not exist.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred while deleting the directory: {ex.Message}");
        }
    }

    #endregion
}