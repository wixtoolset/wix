// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Default implementation of <see cref="IBootstrapperApplicationFactory"/>.
    /// </summary>
    public abstract class BaseBootstrapperApplicationFactory : IBootstrapperApplicationFactory
    {
        /// <summary>
        /// Default implementation of <see cref="IBootstrapperApplicationFactory.Create(IntPtr, IntPtr)"/>
        /// </summary>
        /// <param name="pArgs"></param>
        /// <param name="pResults"></param>
        public void Create(IntPtr pArgs, IntPtr pResults)
        {
            InitializeFromCreateArgs(pArgs, out var engine, out var bootstrapperCommand);

            var ba = this.Create(engine, bootstrapperCommand);
            StoreBAInCreateResults(pResults, ba);
        }

        /// <summary>
        /// Called by <see cref="BaseBootstrapperApplicationFactory.Create(IntPtr, IntPtr)"/> to get the <see cref="IBootstrapperApplication"/>.
        /// </summary>
        /// <param name="engine">The bundle engine.</param>
        /// <param name="bootstrapperCommand">Command information passed from the engine for the BA to perform.</param>
        /// <returns>The <see cref="IBootstrapperApplication"/> for the bundle.</returns>
        protected abstract IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand);

        /// <summary>
        /// Initializes the native part of <see cref="WixToolset.Mba.Core"/>.
        /// Most users should inherit from <see cref="BaseBootstrapperApplicationFactory"/> instead of calling this method.
        /// </summary>
        /// <param name="pArgs">The args struct given by the engine when initially creating the BA.</param>
        /// <param name="engine">The bundle engine interface.</param>
        /// <param name="bootstrapperCommand">The context of the current run of the bundle.</param>
        public static void InitializeFromCreateArgs(IntPtr pArgs, out IEngine engine, out IBootstrapperCommand bootstrapperCommand)
        {
            Command pCommand = new Command
            {
                cbSize = Marshal.SizeOf(typeof(Command))
            };
            var pEngine = BalUtil.InitializeFromCreateArgs(pArgs, ref pCommand);
            engine = new Engine(pEngine);
            bootstrapperCommand = pCommand.GetBootstrapperCommand();
        }

        /// <summary>
        /// Registers the BA with the engine using the default mapping between the message based interface and the COM interface.
        /// Most users should inherit from <see cref="BaseBootstrapperApplicationFactory"/> instead of calling this method.
        /// </summary>
        /// <param name="pResults">The results struct given by the engine when initially creating the BA</param>
        /// <param name="ba">The <see cref="IBootstrapperApplication"/>.</param>
        public static void StoreBAInCreateResults(IntPtr pResults, IBootstrapperApplication ba)
        {
            BalUtil.StoreBAInCreateResults(pResults, ba);
        }
    }
}
