using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Impl;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectContext
    {
        private static readonly IDictionary<string, string> DefaultSourceFileMetadata = new Dictionary<string, string>
        {
            { "language", "csharp" }
        };

        public List<LibraryDescription> _libraries;

        public IReadOnlyList<LibraryDescription> Libraries
        {
            get { return _libraries.AsReadOnly(); }
        }

        private ProjectContext(IEnumerable<LibraryDescription> libraries)
        {
            _libraries = libraries.ToList();
        }

        // TODO: See if we can abstract the file system stuff away
        public static Task<ProjectContext> CreateAsync(string projectFilePath, NuGetFramework targetFramework)
        {
            return CreateAsync(projectFilePath, targetFramework, targetRuntime: null);
        }

        public static async Task<ProjectContext> CreateAsync(string projectDirectory, NuGetFramework targetFramework, string targetRuntime)
        {
            // Read the lock file
            LockFile lockFile;
            var lockFilePath = Path.Combine(projectDirectory, LockFile.FileName);
            using (var stream = await FileSystemUtility.OpenFileStreamAsync(lockFilePath))
            {
                lockFile = new LockFileFormat().Read(stream);
            }
            var lookup = new LockFileLookup(lockFile);

            // Select the appropriate target
            var target = SelectTarget(lockFile, targetFramework, targetRuntime);
            if (target == null)
            {
                // TODO: Report diagnostics?
                throw new InvalidOperationException("Unable to locate an appropriate target in the lock file");
            }

            // Load library descriptions
            var libraries = target.Libraries.Select(t => CreateLibraryDescription(t, lookup)); 

            // Create the context from the lock file
            return new ProjectContext(libraries);
        }

        private static LibraryDescription CreateLibraryDescription(LockFileTargetLibrary library, LockFileLookup lookup)
        {
            // Collect source paths
            var sourcePaths = new List<string>();
            var package = lookup.GetPackage(library.Name, library.Version);
            if(package != null)
            {
                sourcePaths.AddRange(package.Files.Where(f => f.StartsWith("shared" + Path.DirectorySeparatorChar)));
            }
            else
            {
                // TODO(anurse): Shared sources from projects!
            }

            // Calculate metadata about shared sources
            var sourceAssets = sourcePaths.Select(s => new LibraryAsset(s, DefaultSourceFileMetadata));

            // Discover any source assets
            return new LibraryDescription(
                library.TargetFramework,
                library.Dependencies,
                library.CompileTimeAssemblies.Select(CreateAsset),
                library.RuntimeAssemblies.Select(CreateAsset),
                library.NativeLibraries.Select(CreateAsset),
                sourceAssets,
                library.FrameworkAssemblies);
        }

        private static LibraryAsset CreateAsset(LockFileItem item)
        {
            return new LibraryAsset(item.Path, item.Properties);
        }

        private static LockFileTarget SelectTarget(LockFile lockFile, NuGetFramework targetFramework, string targetRuntime)
        {
            foreach (var scanTarget in lockFile.Targets)
            {
                if (scanTarget.TargetFramework == targetFramework &&
                    string.Equals(scanTarget.RuntimeIdentifier, targetRuntime, StringComparison.Ordinal))
                {
                    return scanTarget;
                }
            }

            return null;
        }
    }
}
