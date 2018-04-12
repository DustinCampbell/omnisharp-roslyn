using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using OmniSharp.Utilities;
using MSB = Microsoft.Build;

namespace OmniSharp.MSBuild.Build
{
    internal class ProjectBuildManager
    {
        private static readonly XmlReaderSettings s_xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

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

        public async Task<MSB.Evaluation.Project> LoadProjectAsync(string filePath, IDictionary<string, string> globalProperties = null, CancellationToken cancellationToken = default)
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
    }
}
