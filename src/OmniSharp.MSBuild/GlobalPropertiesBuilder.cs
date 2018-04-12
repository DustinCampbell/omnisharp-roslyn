using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Extensions.Logging;
using OmniSharp.MSBuild.Constants;
using OmniSharp.MSBuild.Discovery;
using OmniSharp.Options;

namespace OmniSharp.MSBuild
{
    internal class GlobalPropertiesBuilder
    {
        private static readonly ImmutableDictionary<string, string> s_defaultGlobalProperties = new Dictionary<string, string>()
        {
            { PropertyNames.DesignTimeBuild, bool.TrueString },
            { PropertyNames.BuildingInsideVisualStudio, bool.TrueString },
            { PropertyNames.BuildProjectReferences, bool.FalseString },
            { PropertyNames.BuildingProject, bool.FalseString },
            
            // Setting this property will cause any XAML markup compiler tasks to run in the
            // current AppDomain, rather than creating a new one. This is important because
            // our AppDomain.AssemblyResolve handler for MSBuild will not be connected to
            // the XAML markup compiler's AppDomain, causing the task not to be able to find
            // MSBuild.
            { PropertyNames.AlwaysCompileMarkupFilesInSeparateDomain, bool.FalseString },

            // Retrieve the compiler command-line arguments but don't actually run the compiler
            { PropertyNames.ProvideCommandLineArgs, bool.TrueString },
            { PropertyNames.SkipCompilerExecution, bool.TrueString },

            { PropertyNames.ContinueOnError, PropertyValues.ErrorAndContinue }
        }.ToImmutableDictionary();

        private readonly Dictionary<string, string> _additionalProperties = new Dictionary<string, string>();
        private readonly ILogger _logger;

        public GlobalPropertiesBuilder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GlobalPropertiesBuilder>();
        }

        public void AddSolutionDirProperty(string solutionDirectory)
        {
            _additionalProperties.Add(PropertyNames.SolutionDir, solutionDirectory + Path.DirectorySeparatorChar);
        }

        public void AddPropertyOverrides(MSBuildOptions options, IMSBuildLocator msbuildLocator)
        {
            var propertyOverrides = msbuildLocator.RegisteredInstance.PropertyOverrides;

            AddPropertyOverride(PropertyNames.MSBuildExtensionsPath, options.MSBuildExtensionsPath);
            AddPropertyOverride(PropertyNames.TargetFrameworkRootPath, options.TargetFrameworkRootPath);
            AddPropertyOverride(PropertyNames.RoslynTargetsPath, options.RoslynTargetsPath);
            AddPropertyOverride(PropertyNames.CscToolPath, options.CscToolPath);
            AddPropertyOverride(PropertyNames.CscToolExe, options.CscToolExe);
            AddPropertyOverride(PropertyNames.VisualStudioVersion, options.VisualStudioVersion);
            AddPropertyOverride(PropertyNames.Configuration, options.Configuration);
            AddPropertyOverride(PropertyNames.Platform, options.Platform);

            if (propertyOverrides.TryGetValue(PropertyNames.BypassFrameworkInstallChecks, out var value))
            {
                _additionalProperties.Add(PropertyNames.BypassFrameworkInstallChecks, value);
            }

            void AddPropertyOverride(string propertyName, string userOverrideValue)
            {
                var overrideValue = propertyOverrides.GetValueOrDefault(propertyName);

                if (!string.IsNullOrEmpty(userOverrideValue))
                {
                    // If the user set the option, we should use that.
                    _additionalProperties.Add(propertyName, userOverrideValue);
                    _logger.LogDebug($"'{propertyName}' set to '{userOverrideValue}' (user override)");
                }
                else if (!string.IsNullOrEmpty(overrideValue))
                {
                    // If we have a custom environment value, we should use that.
                    _additionalProperties.Add(propertyName, overrideValue);
                    _logger.LogDebug($"'{propertyName}' set to '{overrideValue}'");
                }
            }
        }

        public ImmutableDictionary<string, string> ToGlobalProperties()
            => s_defaultGlobalProperties.AddRange(_additionalProperties);
    }
}
