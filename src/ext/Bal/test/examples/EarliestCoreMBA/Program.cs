// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.EarliestCoreMBA
{
    using WixToolset.BootstrapperApplicationApi;

    internal class Program
    {
        private static int Main()
        {
            var application = new EarliestCoreBA();

            ManagedBootstrapperApplication.Run(application);

            return 0;
        }
    }
}
