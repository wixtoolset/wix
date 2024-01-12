// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    /// <summary>
    /// Managed bootstrapper application entry point.
    /// </summary>
    public static class ManagedBootstrapperApplication
    {
        /// <summary>
        /// Run the managed bootstrapper application.
        /// </summary>
        /// <param name="application">Bootstrapper applciation to run.</param>
        public static void Run(IBootstrapperApplication application)
        {
            MbaNative.BootstrapperApplicationRun(application);
        }
    }
}
