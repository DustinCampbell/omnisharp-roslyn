using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OmniSharp.MSBuild.Build;
using OmniSharp.MSBuild.Discovery;
using OmniSharp.Options;
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

        private ProjectBuildManager CreateProjectBuildManager(OmniSharpTestHost host, ITestProject testProject = null)
        {
            var msbuildLocator = host.GetExport<IMSBuildLocator>();
            var options = new MSBuildOptions();

            var builder = new GlobalPropertiesBuilder(LoggerFactory);

            if (testProject != null)
            {
                builder.AddSolutionDirProperty(testProject.Directory);
            }

            builder.AddPropertyOverrides(options, msbuildLocator);

            return new ProjectBuildManager(
                builder.ToGlobalProperties());
        }

        [Fact]
        public void IsStartedEqualsFalseAfterCreation()
        {
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host);

                Assert.False(buildManager.IsStarted);
            }
        }

        [Fact]
        public void IsStartedEqualsTrueAfterStart()
        {
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host);
                buildManager.Start();

                Assert.True(buildManager.IsStarted);
            }
        }

        [Fact]
        public void IsStartedEqualsFalseAfterStartAndStop()
        {
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host);
                buildManager.Start();
                buildManager.Stop();

                Assert.False(buildManager.IsStarted);
            }
        }

        [Fact]
        public void StopThrowsAfterCreation()
        {
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host);

                Assert.Throws<InvalidOperationException>(() => buildManager.Stop());
            }
        }

        [Fact]
        public void StartThrowsIfAlreadyStarted()
        {
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host);
                buildManager.Start();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => buildManager.Start());
                }
                finally
                {
                    buildManager.Stop();
                }
            }
        }

        [Fact]
        public void StopThrowsIfAlreadyStopped()
        {
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host);
                buildManager.Start();
                buildManager.Stop();

                Assert.Throws<InvalidOperationException>(() => buildManager.Stop());
            }
        }

        [Fact]
        public async Task LoadProjectWithoutStart()
        {
            using (var testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectAndSolution"))
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host, testProject);
                var projectFilePath = Path.Combine(testProject.Directory, "ProjectAndSolution.csproj");
                var project = await buildManager.LoadProjectAsync(projectFilePath);

                Assert.NotNull(project);
            }
        }

        [Fact]
        public async Task LoadingSameProjectTwiceWithoutStart()
        {
            using (var testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectAndSolution"))
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host, testProject);
                var projectFilePath = Path.Combine(testProject.Directory, "ProjectAndSolution.csproj");

                var project1 = await buildManager.LoadProjectAsync(projectFilePath);
                var project2 = await buildManager.LoadProjectAsync(projectFilePath);

                Assert.NotNull(project1);
                Assert.NotNull(project2);

                // If the build manager is not started, the projects should be loaded from different
                // collections, resulting in different instances.
                Assert.NotSame(project1, project2);
            }
        }

        [Fact]
        public async Task LoadingSameProjectTwiceWithStart()
        {
            using (var testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectAndSolution"))
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host, testProject);
                buildManager.Start();

                try
                {
                    var projectFilePath = Path.Combine(testProject.Directory, "ProjectAndSolution.csproj");

                    var project1 = await buildManager.LoadProjectAsync(projectFilePath);
                    var project2 = await buildManager.LoadProjectAsync(projectFilePath);

                    Assert.NotNull(project1);
                    Assert.NotNull(project2);

                    // Because the build manager was started, the projects should be loaded from the same
                    // project collection. So, the second load should just return the same instance.
                    Assert.Same(project1, project2);
                }
                finally
                {
                    buildManager.Stop();
                }
            }
        }

        [Fact]
        public async Task LoadingSameProjectTwiceButDifferentPropertiesWithStart()
        {
            using (var testProject = await TestAssets.Instance.GetTestProjectAsync("ProjectAndSolution"))
            using (var host = CreateOmniSharpHost())
            {
                var buildManager = CreateProjectBuildManager(host, testProject);
                buildManager.Start();

                try
                {
                    var projectFilePath = Path.Combine(testProject.Directory, "ProjectAndSolution.csproj");

                    var project1 = await buildManager.LoadProjectAsync(projectFilePath);
                    var project2 = await buildManager.LoadProjectAsync(projectFilePath,
                        new Dictionary<string, string> { { "MyProp", "true" } });

                    Assert.NotNull(project1);
                    Assert.NotNull(project2);

                    // The build manager was started, so the projects are loaded from the same
                    // project collection. However, because they were loaded with different
                    // properties, they are not the same instance.
                    Assert.NotSame(project1, project2);
                }
                finally
                {
                    buildManager.Stop();
                }
            }
        }
    }
}
