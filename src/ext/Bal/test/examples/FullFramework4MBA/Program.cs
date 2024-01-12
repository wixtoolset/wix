// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.FullFramework4MBA
{
    using WixToolset.Mba.Core;

    internal class Program
    {
        private static int Main()
        {
            var application = new FullFramework4BA();

            ManagedBootstrapperApplication.Run(application);

            return 0;
        }
    }
}
