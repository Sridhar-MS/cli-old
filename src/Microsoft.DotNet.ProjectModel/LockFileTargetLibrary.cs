using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFileTargetLibrary
    {
        public LockFileLibrary Library { get; }
        public NuGetFramework Framework { get; }
        public IReadOnlyCollection<LockFileDependency> Dependencies { get; }
        public IReadOnlyCollection<LockFileAssetGroup> AssetGroups { get; }

        public LockFileTargetLibrary(LockFileLibrary library, NuGetFramework framework, IEnumerable<LockFileDependency> dependencies, IEnumerable<LockFileAssetGroup> assetGroups)
        {
            Library = library;
            Framework = framework;
            Dependencies = dependencies.ToList().AsReadOnly();
            AssetGroups = assetGroups.ToList().AsReadOnly();
        }
    }
}