// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    internal class CachedExtension
    {
        public CachedExtension(string id, string version, bool damaged)
        {
            this.Id = id;
            this.Version = version;
            this.Damaged = damaged;
        }

        public string Id { get; }

        public string Version { get; }

        public bool Damaged { get; }
    }
}
