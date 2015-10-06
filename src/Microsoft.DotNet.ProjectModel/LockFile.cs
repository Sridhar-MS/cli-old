using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFile
    {
        public static readonly LockFile Invalid = new LockFile(int.MinValue);

        public int Version { get; }
        public IReadOnlyCollection<LockFileTarget> Targets { get; }
        public IReadOnlyCollection<LockFileLibrary> Libraries { get; }
        public IReadOnlyCollection<ProjectDependencyGroup> ProjectDependencyGroups { get; }

        private LockFile(int version): this(version, Enumerable.Empty<LockFileTarget>(), Enumerable.Empty<LockFileLibrary>(), Enumerable.Empty<ProjectDependencyGroup>()) { }

        public LockFile(int version, IEnumerable<LockFileTarget> targets, IEnumerable<LockFileLibrary> libraries, IEnumerable<ProjectDependencyGroup> projectDependencyGroups)
        {
            Version = version;
            Targets = targets.ToList().AsReadOnly();
            Libraries = libraries.ToList().AsReadOnly();
            ProjectDependencyGroups = projectDependencyGroups.ToList().AsReadOnly();
        }

        public IEnumerable<LockFileAsset> ResolveAssetGroups(NuGetFramework framework, string runtimeIdentifier, ISet<string> assetGroups)
        {
            return Targets
                .FirstOrDefault(t =>
                    t.TargetFramework.Equals(framework) &&
                    string.Equals(runtimeIdentifier, t.RuntimeIdentifier, StringComparison.Ordinal))
                ?.Libraries
                ?.SelectMany(l => l.AssetGroups.Where(g => assetGroups.Contains(g.Name)))
                ?.SelectMany(g => g.Assets);
        }
    }
}