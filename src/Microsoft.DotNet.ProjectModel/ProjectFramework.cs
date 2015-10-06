using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectFramework
    {
        public NuGetFramework TargetFramework { get; }
        public IReadOnlyCollection<ProjectDependency> Dependencies { get; }

        public ProjectFramework(NuGetFramework targetFramework, IEnumerable<ProjectDependency> dependencies)
        {
            TargetFramework = targetFramework;
            Dependencies = dependencies.ToList().AsReadOnly();
        }
    }
}