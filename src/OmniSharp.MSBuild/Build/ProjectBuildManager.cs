using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using OmniSharp.MSBuild.Constants;
using OmniSharp.Utilities;
using MSB = Microsoft.Build;

namespace OmniSharp.MSBuild.Build
{
    internal class ProjectBuildManager
    {
        private readonly ImmutableDictionary<string, string> _globalProperties;
        private MSB.Evaluation.ProjectCollection _projectCollection;

        public bool IsStarted { get; private set; }

        public ProjectBuildManager(ImmutableDictionary<string, string> globalProperties)
        {
            _globalProperties = globalProperties;
        }

        private MSB.Evaluation.ProjectCollection CreateProjectCollection()
            => new MSB.Evaluation.ProjectCollection(_globalProperties);

        public void Start()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException($"{nameof(ProjectBuildManager)} is already started.");
            }

            _projectCollection = CreateProjectCollection();

            IsStarted = true;
        }

        public void Stop()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException($"{nameof(ProjectBuildManager)} is not started.");
            }

            _projectCollection.UnloadAllProjects();
            _projectCollection = null;

            IsStarted = false;
        }

        private static readonly XmlReaderSettings s_xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        public async Task<MSB.Evaluation.Project> LoadProjectAsync(
            string filePath,
            IDictionary<string, string> globalProperties = null,
            CancellationToken cancellationToken = default)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var projectCollection = _projectCollection ?? CreateProjectCollection();
            var project = projectCollection.GetLoadedProject(filePath, globalProperties);

            if (project == null)
            {
                using (var stream = await FileUtilities.ReadFileAsync(filePath, cancellationToken))
                using (var xmlReader = XmlReader.Create(stream, s_xmlReaderSettings))
                {
                    var xml = MSB.Construction.ProjectRootElement.Create(xmlReader, projectCollection);

                    // When constructing a project from an XmlReader, MSBuild cannot determine the project file path.
                    // Setting the path explicitly is necessary so that reserved properties, like $(MSBuildProjectDirectory) will work.
                    xml.FullPath = filePath;

                    project = new MSB.Evaluation.Project(xml, globalProperties, toolsVersion: null, projectCollection: projectCollection);
                }
            }

            return project;
        }

        public Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project,
            CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var targetsToBuild = new[] { TargetNames.Compile, TargetNames.CoreCompile };

            return BuildProjectAsync(project, targetsToBuild, cancellationToken);
        }

        private async Task<MSB.Execution.ProjectInstance> BuildProjectAsync(
            MSB.Evaluation.Project project,
            string[] targetsToBuild,
            CancellationToken cancellationToken)
        {
            // Create a project instance to be executed by the build engine.
            var projectInstance = project.CreateProjectInstance();

            // Verify targets
            foreach (var target in targetsToBuild)
            {
                if (!projectInstance.Targets.ContainsKey(target))
                {
                    throw new InvalidOperationException($"Project does not support target: {target}");
                }
            }

            var buildRequestData = new MSB.Execution.BuildRequestData(projectInstance, targetsToBuild);
            var result = await BuildAsync(buildRequestData, cancellationToken);

            if (result.OverallResult == MSB.Execution.BuildResultCode.Failure)
            {
                if (result.Exception != null)
                {
                    throw result.Exception;
                }
            }

            return projectInstance;
        }

        // This lock is static because we're using the default build manager, and there's only one per process.
        private static readonly SemaphoreSlim s_buildManagerLock = new SemaphoreSlim(initialCount: 1);

        private async Task<MSB.Execution.BuildResult> BuildAsync(
            MSB.Execution.BuildRequestData requestData,
            CancellationToken cancellationToken)
        {
            using (await s_buildManagerLock.DisposableWaitAsync(cancellationToken))
            {
                return await BuildAsync(MSB.Execution.BuildManager.DefaultBuildManager, requestData, cancellationToken);
            }
        }

        private Task<MSB.Execution.BuildResult> BuildAsync(
            MSB.Execution.BuildManager buildManager,
            MSB.Execution.BuildRequestData requestData,
            CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<MSB.Execution.BuildResult>();

            // Enable build cancellation
            var registration = default(CancellationTokenRegistration);
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    // Note that we only expect that a single submission is being built,
                    // even though we're calling CancelAllSubmissions(). If we ever support
                    // parallel builds, this will need updating.

                    taskCompletionSource.TrySetCanceled();
                    buildManager.CancelAllSubmissions();
                    registration.Dispose();
                });
            }

            // execute asynchroous build...
            try
            {
                buildManager.PendBuildRequest(requestData).ExecuteAsync(submission =>
                {
                    // when finished
                    try
                    {
                        var result = submission.BuildResult;
                        registration.Dispose();
                        taskCompletionSource.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                }, context: null);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }

            return taskCompletionSource.Task;
        }
    }
}
