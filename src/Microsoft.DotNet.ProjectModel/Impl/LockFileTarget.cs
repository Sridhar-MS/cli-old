﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Impl
{
    internal class LockFileTarget
    {
        public NuGetFramework TargetFramework { get; set; }

        public string RuntimeIdentifier { get; set; }

        public IList<LockFileTargetLibrary> Libraries { get; set; } = new List<LockFileTargetLibrary>();
    }
}
