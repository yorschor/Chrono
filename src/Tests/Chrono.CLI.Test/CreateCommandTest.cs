using LibGit2Sharp;

namespace Chrono.CLI.Test
{
    public class CommandTests
    {
        private readonly CliTestHelper _app = new();

        [Fact]
        public void CreateReleaseBranchCommand_WithCommit_ShouldCreateBranchAndCommit()
        {
            using var repo = new Repository(_app.TempDirectory);
            var initialHash = repo.Head.Tip.Sha;

            // Set up initial conditions
            File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "version.yml"), "version: 1.0.0");
            repo.Index.Add("version.yml");
            repo.Commit("Initial commit", new Signature("Test User", "test@example.com", DateTimeOffset.Now), new Signature("Test User", "test@example.com", DateTimeOffset.Now));

            // Run the command
            _app.RunAndAssert(["create", "release"], "Creating new branch");

            // Check branch creation
            var newBranch = repo.Branches["release/v1.0.0"];
            Assert.NotNull(newBranch);
            Assert.Equal(initialHash, newBranch.Tip.Sha);

            // Check commit
            var commitMessage = newBranch.Tip.Message;
            Assert.Contains("Chrono: Set version", commitMessage);
        }

        [Fact]
        public void CreateTagCommand_ShouldCreateTag()
        {
            using var repo = new Repository(_app.TempDirectory);
            var initialHash = repo.Head.Tip.Sha;

            // Set up initial conditions
            File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "version.yml"), "version: 1.0.0");
            repo.Index.Add("version.yml");
            repo.Commit("Initial commit", new Signature("Test User", "test@example.com", DateTimeOffset.Now), new Signature("Test User", "test@example.com", DateTimeOffset.Now));

            // Run the command
            _app.RunAndAssert(new[] { "create-tag" }, "Tag v1.0.0 created");

            // Check tag creation
            var tag = repo.Tags["v1.0.0"];
            Assert.NotNull(tag);
            Assert.Equal(initialHash, ((Commit)tag.Target).Sha);
        }

        [Fact]
        public void CreateBranchCommand_WithBranchKey_ShouldCreateBranch()
        {
            using var repo = new Repository(_app.TempDirectory);
            var initialHash = repo.Head.Tip.Sha;

            // Set up initial conditions
            File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "version.yml"), "version: 1.0.0");
            repo.Index.Add("version.yml");
            repo.Commit("Initial commit", new Signature("Test User", "test@example.com", DateTimeOffset.Now), new Signature("Test User", "test@example.com", DateTimeOffset.Now));

            // Run the command
            _app.RunAndAssert(["create", "branch", "feature/test"], "Creating new branch feature/test");

            // Check branch creation
            var newBranch = repo.Branches["feature/test"];
            Assert.NotNull(newBranch);
            Assert.Equal(initialHash, newBranch.Tip.Sha);
        }
    }
}
