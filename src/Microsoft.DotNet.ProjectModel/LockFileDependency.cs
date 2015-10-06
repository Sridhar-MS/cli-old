using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFileDependency
    {
        public string Name { get; }
        public NuGetVersion Version { get; }

        public LockFileDependency(string name, NuGetVersion version)
        {
            Name = name;
            Version = version;
        }
    }
}