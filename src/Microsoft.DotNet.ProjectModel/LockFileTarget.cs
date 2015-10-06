using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFileTarget
    {
        public NuGetFramework TargetFramework { get; }
        public string RuntimeIdentifier { get; }
        public IReadOnlyCollection<LockFileTargetLibrary> Libraries { get; }

        public LockFileTarget(NuGetFramework targetFramework, string runtimeIdentifier, IEnumerable<LockFileTargetLibrary> libraries)
        {
            TargetFramework = targetFramework;
            RuntimeIdentifier = runtimeIdentifier;
            Libraries = libraries.ToList().AsReadOnly();
        }
    }
}