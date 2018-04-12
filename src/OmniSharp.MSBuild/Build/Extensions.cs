using System.Collections.Generic;
using System.Collections.Immutable;
using OmniSharp.Utilities;
using MSB = Microsoft.Build;

namespace OmniSharp.MSBuild.Build
{
    internal static class Extensions
    {
        public static MSB.Evaluation.Project GetLoadedProject(
            this MSB.Evaluation.ProjectCollection projectCollection,
            string filePath,
            IDictionary<string, string> globalProperties)
        {
            var loadedProjects = projectCollection.GetLoadedProjects(filePath);
            if (loadedProjects == null || loadedProjects.Count == 0)
            {
                return null;
            }

            // Walk through all of the loaded projects and find the one that
            // has the expected set of global properties.

            // Note: The logic below may not be correct if the global properties
            // overrides any of the properties in the ProjectCollection's global properties.

            globalProperties = globalProperties ?? ImmutableDictionary<string, string>.Empty;
            var totalGlobalProperties = projectCollection.GlobalProperties.Count + globalProperties.Count;

            foreach (var loadedProject in loadedProjects)
            {
                // If this project has a different number of global properties, it's
                // definitely not the one we're looking for.
                if (loadedProject.GlobalProperties.Count != totalGlobalProperties)
                {
                    continue;
                }

                // Since the project belongs to this collection, we can assume that it has the
                // default properties of the gollection. So, we just need to check the extra
                // global properties.

                var found = true;
                foreach (var (key, value) in globalProperties)
                {
                    // MSBuild escapes the values of a project's global properties, so we must too.
                    var escapedValue = MSB.Evaluation.ProjectCollection.Escape(value);

                    if (!loadedProject.GlobalProperties.TryGetValue(key, out var actualValue) ||
                        !string.Equals(actualValue, escapedValue, System.StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    return loadedProject;
                }
            }

            return null;
        }
    }
}
