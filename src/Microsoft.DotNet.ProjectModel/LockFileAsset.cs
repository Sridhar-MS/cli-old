using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFileAsset
    {
        public string Path { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public LockFileAsset(string path, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            Path = path;
            Metadata = new ReadOnlyDictionary<string, string>(metadata.ToDictionary(p => p.Key, p => p.Value));
        }
    }
}