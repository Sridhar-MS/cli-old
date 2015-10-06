using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.ProjectModel
{
    /// <summary>
    /// Represents a complete model of a project.json-based .NET Project
    /// </summary>
    public class Project
    {
        public string Name { get; }
        public string Path { get; }
        public IReadOnlyCollection<ProjectFramework> Frameworks { get; }
        // TODO: Configurations

        public LockFile LockFile { get; }
    }
}
