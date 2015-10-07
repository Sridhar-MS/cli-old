using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;

namespace Microsoft.DotNet.ProjectModel
{
    public class LibraryDescription
    {
        /// <summary>
        /// Gets the specific <see cref="NuGetFramework"/> used for this package. May be null, and may be
        /// different from the target framework of the app if the library only supports other compatible libraries.
        /// </summary>
        public NuGetFramework TargetFramework { get; }

        /// <summary>
        /// Gets the direct dependencies of this library. The items in this list should all be available from the
        /// <see cref="ProjectContext"/> that provided this library
        /// </summary>
        public IReadOnlyCollection<PackageDependency> Dependencies { get; }

        /// <summary>
        /// Gets the MSIL metadata assets this library provides for compilation.
        /// </summary>
        public IReadOnlyCollection<LibraryAsset> CompilationAssets { get; }

        /// <summary>
        /// Gets the managed assemblies this library provides for runtime.
        /// </summary>
        public IReadOnlyCollection<LibraryAsset> RuntimeAssets { get; }

        /// <summary>
        /// Gets the native libraries this library provides for runtime.
        /// </summary>
        public IReadOnlyCollection<LibraryAsset> NativeAssets { get; }

        /// <summary>
        /// Gets the source files this library provides for compilation.
        /// </summary>
        public IReadOnlyCollection<LibraryAsset> SourceAssets { get; }

        /// <summary>
        /// Gets the names of framework assemblies required by this library.
        /// </summary>
        public IReadOnlyCollection<string> FrameworkReferences { get; }

        public LibraryDescription(
            NuGetFramework targetFramework,
            IEnumerable<PackageDependency> dependencies,
            IEnumerable<LibraryAsset> compilationAssets,
            IEnumerable<LibraryAsset> runtimeAssets,
            IEnumerable<LibraryAsset> nativeAssets,
            IEnumerable<LibraryAsset> sourceAssets,
            IEnumerable<string> frameworkReferences)
        {
            TargetFramework = targetFramework;
            Dependencies = dependencies.ToList().AsReadOnly();
            CompilationAssets = compilationAssets.ToList().AsReadOnly();
            RuntimeAssets = runtimeAssets.ToList().AsReadOnly();
            NativeAssets = nativeAssets.ToList().AsReadOnly();
            FrameworkReferences = frameworkReferences.ToList().AsReadOnly();
        }
    }
}