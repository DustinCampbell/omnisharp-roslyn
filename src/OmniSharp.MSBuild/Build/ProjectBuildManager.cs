using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using OmniSharp.MSBuild.Constants;
using MSB = Microsoft.Build;

namespace OmniSharp.MSBuild.Build
{
    internal class ProjectBuildManager
    {
        private static readonly ImmutableDictionary<string, string> s_defaultGlobalProperties = new Dictionary<string, string>()
        {
            { PropertyNames.DesignTimeBuild, bool.TrueString },
            { PropertyNames.BuildingInsideVisualStudio, bool.TrueString },
            { PropertyNames.BuildProjectReferences, bool.FalseString },
            { PropertyNames.BuildingProject, bool.FalseString },

            // Retrieve the compiler command-line arguments but don't actually run the compiler
            { PropertyNames.ProvideCommandLineArgs, bool.TrueString },
            { PropertyNames.SkipCompilerExecution, bool.TrueString },

            { PropertyNames.ContinueOnError, PropertyValues.ErrorAndContinue }
        }.ToImmutableDictionary();

        private MSB.Evaluation.ProjectCollection _projectCollection;

        public bool IsStarted { get; private set; }

        public void Start()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException($"{nameof(ProjectBuildManager)} is already started.");
            }

            _projectCollection = new MSB.Evaluation.ProjectCollection(s_defaultGlobalProperties);

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
    }
}
