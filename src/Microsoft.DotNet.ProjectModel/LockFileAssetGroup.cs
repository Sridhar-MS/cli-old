using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel
{
    public class LockFileAssetGroup
    {
        public string Name { get; }
        public IReadOnlyCollection<LockFileAsset> Assets { get; }

        public LockFileAssetGroup(string name, IEnumerable<LockFileAsset> assets)
        {
            Name = name;
            Assets = assets.ToList().AsReadOnly();
        }
    }
}