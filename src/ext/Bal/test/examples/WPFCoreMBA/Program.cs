// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.WPFCoreMBA
{
    using WixToolset.BootstrapperApplicationApi;
    // using WixToolset.BootstrapperApplications.Managed;

    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new WPFCoreBA();

            ManagedBootstrapperApplication.Run(app);

            return 0;
        }
    }
}
