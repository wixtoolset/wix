// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using WixToolset.Extensibility.Data;

    internal class ExtensionCacheLocation : IExtensionCacheLocation
    {
        public ExtensionCacheLocation(string path, ExtensionCacheLocationScope scope)
        {
            this.Path = path;
            this.Scope = scope;
        }

        public string Path { get; }

        public ExtensionCacheLocationScope Scope { get; }
    }
}
