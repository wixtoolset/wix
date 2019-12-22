// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.FullFramework2MBA
{
    using WixToolset.Mba.Core;

    public class FullFramework2BA : BootstrapperApplication
    {
        public FullFramework2BA(IEngine engine)
            : base(engine)
        {

        }

        protected override void Run()
        {
        }

        protected override void OnShutdown(ShutdownEventArgs args)
        {
            base.OnShutdown(args);

            var message = "Shutdown," + args.Action.ToString() + "," + args.HResult.ToString();
            this.engine.Log(LogLevel.Standard, message);
        }
    }
}
