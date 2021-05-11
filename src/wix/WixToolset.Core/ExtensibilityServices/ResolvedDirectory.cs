// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using WixToolset.Extensibility.Data;

    internal class ResolvedDirectory : IResolvedDirectory
    {
        public string DirectoryParent { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }
    }
}
