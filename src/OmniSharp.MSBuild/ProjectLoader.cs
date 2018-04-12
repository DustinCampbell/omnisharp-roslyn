using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Extensions.Logging;
using OmniSharp.MSBuild.Logging;
using OmniSharp.MSBuild.ProjectFile;
using OmniSharp.Options;

using MSB = Microsoft.Build;

namespace OmniSharp.MSBuild
{
    internal class ProjectLoader
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _globalProperties;
        private readonly MSBuildOptions _options;
        private readonly SdksPathResolver _sdksPathResolver;

        public ProjectLoader(MSBuildOptions options, string solutionDirectory, ImmutableDictionary<string, string> propertyOverrides, ILoggerFactory loggerFactory, SdksPathResolver sdksPathResolver)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = loggerFactory.CreateLogger<ProjectLoader>();
            _sdksPathResolver = sdksPathResolver ?? throw new ArgumentNullException(nameof(sdksPathResolver));
            _globalProperties = CreateGlobalProperties(solutionDirectory, propertyOverrides);
        }

        private Dictionary<string, string> CreateGlobalProperties(string solutionDirectory, ImmutableDictionary<string, string> propertyOverrides)
        {
            var result = new Dictionary<string, string>
            {
                { PropertyNames.DesignTimeBuild, bool.TrueString },
                { PropertyNames.BuildingInsideVisualStudio, bool.TrueString },
                { PropertyNames.BuildProjectReferences, bool.FalseString },
                { PropertyNames.BuildingProject, bool.FalseString },
                { PropertyNames.SolutionDir, solutionDirectory + Path.DirectorySeparatorChar },

                // Setting this property will cause any XAML markup compiler tasks to run in the
                // current AppDomain, rather than creating a new one. This is important because
                // our AppDomain.AssemblyResolve handler for MSBuild will not be connected to
                // the XAML markup compiler's AppDomain, causing the task not to be able to find
                // MSBuild.
                { PropertyNames.AlwaysCompileMarkupFilesInSeparateDomain, bool.FalseString },

                // This properties allow the design-time build to handle the Compile target without actually invoking the compiler.
                // See https://github.com/dotnet/roslyn/pull/4604 for details.
                { PropertyNames.ProvideCommandLineArgs, bool.TrueString },
                { PropertyNames.SkipCompilerExecution, bool.TrueString }
            };

            AddPropertyOverride(PropertyNames.MSBuildExtensionsPath, _options.MSBuildExtensionsPath);
            AddPropertyOverride(PropertyNames.TargetFrameworkRootPath, _options.TargetFrameworkRootPath);
            AddPropertyOverride(PropertyNames.RoslynTargetsPath, _options.RoslynTargetsPath);
            AddPropertyOverride(PropertyNames.CscToolPath, _options.CscToolPath);
            AddPropertyOverride(PropertyNames.CscToolExe, _options.CscToolExe);
            AddPropertyOverride(PropertyNames.VisualStudioVersion, _options.VisualStudioVersion);
            AddPropertyOverride(PropertyNames.Configuration, _options.Configuration);
            AddPropertyOverride(PropertyNames.Platform, _options.Platform);

            if (propertyOverrides.TryGetValue(PropertyNames.BypassFrameworkInstallChecks, out var value))
            {
                result.Add(PropertyNames.BypassFrameworkInstallChecks, value);
            }

            return result;

            void AddPropertyOverride(string propertyName, string userOverrideValue)
            {
                var overrideValue = propertyOverrides.GetValueOrDefault(propertyName);

                if (!string.IsNullOrEmpty(userOverrideValue))
                {
                    // If the user set the option, we should use that.
                    result.Add(propertyName, userOverrideValue);
                    _logger.LogDebug($"'{propertyName}' set to '{userOverrideValue}' (user override)");
                }
                else if (!string.IsNullOrEmpty(overrideValue))
                {
                    // If we have a custom environment value, we should use that.
                    result.Add(propertyName, overrideValue);
                    _logger.LogDebug($"'{propertyName}' set to '{overrideValue}'");
                }
            }
        }

        public (MSB.Execution.ProjectInstance projectInstance, ImmutableArray<MSBuildDiagnostic> diagnostics) BuildProject(string filePath)
        {
            using (_sdksPathResolver.SetSdksPathEnvironmentVariable(filePath))
            {
                var evaluatedProject = EvaluateProjectFileCore(filePath);

                SetTargetFrameworkIfNeeded(evaluatedProject);

                var projectInstance = evaluatedProject.CreateProjectInstance();
                var msbuildLogger = new MSBuildLogger(_logger);
                var buildResult = projectInstance.Build(
                    targets: new string[] { TargetNames.Compile, TargetNames.CoreCompile },
                    loggers: new[] { msbuildLogger });

                var diagnostics = msbuildLogger.GetDiagnostics();

                return buildResult
                    ? (projectInstance, diagnostics)
                    : (null, diagnostics);
            }
        }

        public MSB.Evaluation.Project EvaluateProjectFile(string filePath)
        {
            using (_sdksPathResolver.SetSdksPathEnvironmentVariable(filePath))
            {
                return EvaluateProjectFileCore(filePath);
            }
        }

        private MSB.Evaluation.Project EvaluateProjectFileCore(string filePath)
        {
            // Evaluate the MSBuild project
            var projectCollection = new MSB.Evaluation.ProjectCollection(_globalProperties);

            var toolsVersion = _options.ToolsVersion;
            if (string.IsNullOrEmpty(toolsVersion) || Version.TryParse(toolsVersion, out _))
            {
                toolsVersion = projectCollection.DefaultToolsVersion;
            }

            toolsVersion = GetLegalToolsetVersion(toolsVersion, projectCollection.Toolsets);

            return projectCollection.LoadProject(filePath, toolsVersion);
        }

        private static void SetTargetFrameworkIfNeeded(MSB.Evaluation.Project evaluatedProject)
        {
            var targetFramework = evaluatedProject.GetPropertyValue(PropertyNames.TargetFramework);
            var targetFrameworks = PropertyConverter.SplitList(evaluatedProject.GetPropertyValue(PropertyNames.TargetFrameworks), ';');

            // If the project supports multiple target frameworks and specific framework isn't
            // selected, we must pick one before execution. Otherwise, the ResolveReferences
            // target might not be available to us.
            if (string.IsNullOrWhiteSpace(targetFramework) && targetFrameworks.Length > 0)
            {
                // For now, we'll just pick the first target framework. Eventually, we'll need to
                // do better and potentially allow OmniSharp hosts to select a target framework.
                targetFramework = targetFrameworks[0];
                evaluatedProject.SetProperty(PropertyNames.TargetFramework, targetFramework);
            }
            else if (!string.IsNullOrWhiteSpace(targetFramework) && targetFrameworks.Length == 0)
            {
                targetFrameworks = ImmutableArray.Create(targetFramework);
            }
        }

        private static string GetLegalToolsetVersion(string toolsVersion, ICollection<MSB.Evaluation.Toolset> toolsets)
        {
            // It's entirely possible the the toolset specified does not exist. In that case, we'll try to use
            // the highest version available.
            var version = new Version(toolsVersion);

            bool exists = false;
            Version highestVersion = null;

            var legalToolsets = new SortedList<Version, MSB.Evaluation.Toolset>(toolsets.Count);
            foreach (var toolset in toolsets)
            {
                // Only consider this toolset if it has a legal version, we haven't seen it, and its path exists.
                if (Version.TryParse(toolset.ToolsVersion, out var toolsetVersion) &&
                    !legalToolsets.ContainsKey(toolsetVersion) &&
                    Directory.Exists(toolset.ToolsPath))
                {
                    legalToolsets.Add(toolsetVersion, toolset);

                    if (highestVersion == null ||
                        toolsetVersion > highestVersion)
                    {
                        highestVersion = toolsetVersion;
                    }

                    if (toolsetVersion == version)
                    {
                        exists = true;
                    }
                }
            }

            if (highestVersion == null)
            {
                throw new InvalidOperationException("No legal MSBuild toolsets available.");
            }

            if (!exists)
            {
                toolsVersion = legalToolsets[highestVersion].ToolsPath;
            }

            return toolsVersion;
        }
    }
}
