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
            
            // Run the command
            _app.RunAndAssert(["release", "-c"], "");

            // Check branch creation
            var newBranch = repo.Branches["release/v1.0.0"];
            Assert.NotNull(newBranch);
            Assert.Equal(initialHash, newBranch.Tip.Sha);

            // Check commit
            var commitMessage = repo.Head.Tip.Message;
            Assert.Contains("Chrono: Set version", commitMessage);
        }

        [Fact]
        public void CreateTagCommand_ShouldCreateTag()
        {
            using var repo = new Repository(_app.TempDirectory);
            var initialHash = repo.Head.Tip.Sha;
            
            // Run the command  
            _app.RunAndAssert(["tag"], "Tag v1.0.0 created");

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

            // Run the command
            _app.RunAndAssert(["branch", "release"], "Creating new branch release/v1.0.0");

            // Check branch creation
            var newBranch = repo.Branches["release/v1.0.0"];
            Assert.NotNull(newBranch);
            Assert.Equal(initialHash, newBranch.Tip.Sha);
        }
    }
}
