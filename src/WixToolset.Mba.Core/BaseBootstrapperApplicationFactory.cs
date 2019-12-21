// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;

    public abstract class BaseBootstrapperApplicationFactory : IBootstrapperApplicationFactory
    {
        public void Create(IntPtr pArgs, IntPtr pResults)
        {
            InitializeFromCreateArgs(pArgs, out var engine, out var bootstrapperCommand);

            var ba = this.Create(engine, bootstrapperCommand);
            StoreBAInCreateResults(pResults, ba);
        }

        protected abstract IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand);

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

        public static void StoreBAInCreateResults(IntPtr pResults, IBootstrapperApplication ba)
        {
            BalUtil.StoreBAInCreateResults(pResults, ba);
        }
    }
}
