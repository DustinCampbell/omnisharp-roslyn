using OmniSharp.MSBuild.Build;
using System;
using TestUtility;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.MSBuild.Tests
{
    public class ProjectBuildManagerTests : AbstractTestFixture
    {
        public ProjectBuildManagerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void IsStartedEqualsFalseAfterCreation()
        {
            var buildManager = new ProjectBuildManager();

            Assert.False(buildManager.IsStarted);
        }

        [Fact]
        public void IsStartedEqualsTrueAfterStart()
        {
            var buildManager = new ProjectBuildManager();
            buildManager.Start();

            Assert.True(buildManager.IsStarted);
        }

        [Fact]
        public void IsStartedEqualsFalseAfterStartAndStop()
        {
            var buildManager = new ProjectBuildManager();
            buildManager.Start();
            buildManager.Stop();

            Assert.False(buildManager.IsStarted);
        }

        [Fact]
        public void StopThrowsAfterCreation()
        {
            var buildManager = new ProjectBuildManager();

            Assert.Throws<InvalidOperationException>(() => buildManager.Stop());
        }

        [Fact]
        public void StartThrowsIfAlreadyStarted()
        {
            var buildManager = new ProjectBuildManager();
            buildManager.Start();

            Assert.Throws<InvalidOperationException>(() => buildManager.Start());
        }

        [Fact]
        public void StopThrowsIfAlreadyStopped()
        {
            var buildManager = new ProjectBuildManager();
            buildManager.Start();
            buildManager.Stop();

            Assert.Throws<InvalidOperationException>(() => buildManager.Stop());
        }
    }
}
