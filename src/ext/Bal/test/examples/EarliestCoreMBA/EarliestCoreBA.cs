// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.EarliestCoreMBA
{
    using WixToolset.Mba.Core;

    public class EarliestCoreBA : BootstrapperApplication
    {
        protected override void Run()
        {
            this.engine.Quit(0);
        }

        protected override void OnStartup(StartupEventArgs args)
        {
            base.OnStartup(args);

            this.engine.Log(LogLevel.Standard, nameof(EarliestCoreBA));
        }

        protected override void OnShutdown(ShutdownEventArgs args)
        {
            base.OnShutdown(args);

            var message = "Shutdown," + args.Action.ToString() + "," + args.HResult.ToString();
            this.engine.Log(LogLevel.Standard, message);
        }
    }
}
