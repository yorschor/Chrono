using Huxy;

namespace Chrono.Core.Helpers;

public struct Commit(string hash, string message)
{
    public string Hash { get; } = hash;
    public string Message { get; } = message;
}

public struct Tag(string name, string tagHash, string commitHash)
{
    public string Name { get; } = name;
    public string TagHash { get; } = tagHash;
    public string CommitHash { get; } = commitHash;
}

public class TinyRepo
{
    public string GitDirectory { get; private set; }

    private TinyRepo(string gitDirectory)
    {
        GitDirectory = gitDirectory;
    }

    public static Result<TinyRepo> Discover()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            return Result.Error<TinyRepo>("No git directory found!");
        }

        return Result.Ok(new TinyRepo(directory.FullName));
    }

    public string GetCurrentBranchPath()
    {
        var headFilePath = Path.Combine(GitDirectory, ".git", "HEAD");
        if (!File.Exists(headFilePath))
        {
            throw new FileNotFoundException("HEAD file not found");
        }

        var headContent = File.ReadAllText(headFilePath).Trim();
        if (headContent.StartsWith("ref:"))
        {
            var branchPath = headContent.Substring(5).Trim();
            return branchPath;
        }

        return "detached HEAD";
    }

    public string GetCurrentBranchName()
    {
        var branchPath = GetCurrentBranchPath();
        if (branchPath == "detached HEAD")
        {
            return "HEAD";
        }
        else
        {
            return branchPath.Replace("refs/heads/", "");
        }
    }

    public Commit GetCurrentCommit()
    {
        var commitHash = GetCurrentCommitHash();
        var commitMessage = GetCurrentCommitMessage();
        return new Commit(commitHash, commitMessage);
    }

    public string GetCurrentCommitHash()
    {
        var branchPath = GetCurrentBranchPath();
        if (branchPath == "detached HEAD")
        {
            var headFilePath = Path.Combine(GitDirectory, ".git", "HEAD");
            return File.ReadAllText(headFilePath).Trim();
        }

        var branchFilePath = Path.Combine(GitDirectory, ".git", branchPath);
        if (!File.Exists(branchFilePath))
        {
            throw new FileNotFoundException($"Branch file not found: {branchFilePath}");
        }

        return File.ReadAllText(branchFilePath).Trim();
    }

    public string GetCurrentCommitMessage()
    {
        var commitHash = GetCurrentCommitHash();
        var objectDir = Path.Combine(GitDirectory, ".git", "objects", commitHash.Substring(0, 2), commitHash.Substring(2));
        if (!File.Exists(objectDir))
        {
            throw new FileNotFoundException("Commit object not found");
        }

        var commitContent = File.ReadAllBytes(objectDir);
        var decompressedContent = Decompress(commitContent);
        var commitMessageIndex = decompressedContent.IndexOf("\n\n") + 2;
        return decompressedContent.Substring(commitMessageIndex).Trim();
    }

    public List<Tag> GetTagsPointingToCurrentCommit()
    {
        var currentHash = GetCurrentCommitHash();
        var tagsDir = Path.Combine(GitDirectory, ".git", "refs", "tags");
        var tags = new List<Tag>();

        foreach (var tagFile in Directory.GetFiles(tagsDir))
        {
            var tagHash = File.ReadAllText(tagFile).Trim();
            var commitHash = ResolveTagHash(tagHash);

            if (commitHash == currentHash)
            {
                var tagName = Path.GetFileName(tagFile);
                tags.Add(new Tag(tagName, tagHash, commitHash));
            }
        }

        return tags;
    }


    #region Helpers

    private string ResolveTagHash(string hash)
    {
        var objectDir = Path.Combine(GitDirectory, ".git", "objects", hash.Substring(0, 2), hash.Substring(2));
        if (!File.Exists(objectDir))
        {
            throw new FileNotFoundException($"Git object not found: {objectDir}");
        }

        var objectContent = File.ReadAllBytes(objectDir);
        var decompressedContent = Decompress(objectContent);

        // Check if the object is a tag object
        if (decompressedContent.StartsWith("tag"))
        {
            var commitHashStart = decompressedContent.IndexOf("object ", StringComparison.Ordinal) + 7;
            var commitHashEnd = decompressedContent.IndexOf("\n", commitHashStart, StringComparison.Ordinal);
            return decompressedContent.Substring(commitHashStart, commitHashEnd - commitHashStart).Trim();
        }

        // If not a tag, it must be a commit hash
        return hash;
    }



    private string Decompress(byte[] data)
    {
        using var compressedStream = new MemoryStream(data);
        using var decompressedStream = new MemoryStream();
        using var decompressionStream = new Ionic.Zlib.ZlibStream(compressedStream, Ionic.Zlib.CompressionMode.Decompress);
        decompressionStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0;
        using var reader = new StreamReader(decompressedStream);
        return reader.ReadToEnd();
    }

    #endregion
}