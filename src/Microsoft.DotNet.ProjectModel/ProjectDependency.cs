using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectDependency
    {
        public string Name { get; }
        public VersionRange Version { get; }

        public ProjectDependency(string name, VersionRange version)
        {
            Name = name;
            Version = version;
        }
    }
}