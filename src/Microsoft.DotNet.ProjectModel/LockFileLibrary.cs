using System.Collections.Generic;
using System.Linq;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFileLibrary
    {
        public string Name { get; }
        public string Type { get; }
        public NuGetVersion Version { get; }
        public bool Serviceable { get; }
        public string Sha512 { get; }
        public IReadOnlyCollection<string> Files { get; }

        public LockFileLibrary(string name, string type, NuGetVersion version, bool serviceable, string sha512, IEnumerable<string> files)
        {
            Name = name;
            Type = type;
            Version = version;
            Serviceable = serviceable;
            Sha512 = sha512;
            Files = files.ToList().AsReadOnly();
        }
    }
}