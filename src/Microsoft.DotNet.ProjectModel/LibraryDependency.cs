using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel
{
    public class LibraryDependency
    {
        public string Name { get; }
        public VersionRange VersionRange { get; }
        public string Target { get; }

        public string SourceFilePath { get; }
        public int SourceLine { get; }
        public int SourceColumn { get; }

        public LibraryDependency(string name, VersionRange versionRange, string target, string sourceFilePath, int sourceLine, int sourceColumn)
        {
            Name = name;
            VersionRange = versionRange;
            Target = target;
            SourceFilePath = sourceFilePath;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }
    }
}