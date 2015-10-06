using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectDependencyGroup
    {
        public NuGetFramework TargetFramework { get; }
        public IReadOnlyCollection<string> Dependencies { get; }

        public ProjectDependencyGroup(NuGetFramework targetFramework, IEnumerable<string> dependencies)
        {
            TargetFramework = targetFramework;
            Dependencies = dependencies.ToList().AsReadOnly();
        }
    }
}