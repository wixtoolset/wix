// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// This is no longer used.
    /// </summary>
    [Obsolete("Bootstrapper applications now run out of proc and do not use a BootstrapperApplicationFactory. Remove your BootstrapperApplicationFactory class. See https://wixtoolset.org/docs/fiveforfour/ for more details.")]
    public abstract class BaseBootstrapperApplicationFactory : IBootstrapperApplicationFactory
    {
        /// <summary>
        /// This is no longer used.
        /// </summary>
        /// <param name="pArgs">This is no longer used.</param>
        /// <param name="pResults">This is no longer used.</param>
        public void Create(IntPtr pArgs, IntPtr pResults)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is no longer used.
        /// </summary>
        /// <param name="engine">This is no longer used.</param>
        /// <param name="bootstrapperCommand">This is no longer used.</param>
        /// <returns>This is no longer used.</returns>
        protected abstract IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand);

        /// <summary>
        /// This is no longer used.
        /// </summary>
        /// <param name="pArgs">This is no longer used.</param>
        /// <param name="engine">This is no longer used.</param>
        /// <param name="bootstrapperCommand">This is no longer used.</param>
        public static void InitializeFromCreateArgs(IntPtr pArgs, out IEngine engine, out IBootstrapperCommand bootstrapperCommand)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is no longer used.
        /// </summary>
        /// <param name="pResults">This is no longer used.</param>
        /// <param name="ba">This is no longer used.</param>
        public static void StoreBAInCreateResults(IntPtr pResults, IBootstrapperApplication ba)
        {
            throw new NotImplementedException();
        }
    }
}
