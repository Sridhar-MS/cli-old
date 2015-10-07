// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Impl
{
    internal class LockFileProjectLibrary
    {
        public string Name { get; set; }

        public NuGetVersion Version { get; set; }

        public string Path { get; set; }
    }
}