// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System;
    using WixToolset.Extensibility;

    internal class BurnExtensionFactory : IExtensionFactory
    {
        public bool TryCreateExtension(Type extensionType, out object extension)
        {
            extension = null;

            if (extensionType == typeof(IBackendFactory))
            {
                extension = new BurnBackendFactory();
            }

            return extension != null;
        }
    }
}
