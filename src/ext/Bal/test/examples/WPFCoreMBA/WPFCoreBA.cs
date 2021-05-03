// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.WPFCoreMBA
{
    using System.Windows.Threading;
    using WixToolset.Mba.Core;

    public class WPFCoreBA : BootstrapperApplication
    {
        public WPFCoreBA(IEngine engine)
            : base(engine)
        {
        }
        
        public Dispatcher BADispatcher { get; private set; }

        protected override void Run()
        {
            this.BADispatcher = Dispatcher.CurrentDispatcher;
            var window = new MainWindow();
            window.Closed += (s, e) => this.BADispatcher.InvokeShutdown();
            //window.Show();
            //Dispatcher.Run();
            //this.engine.Quit(0);
        }

        protected override void OnStartup(StartupEventArgs args)
        {
            base.OnStartup(args);

            this.engine.Log(LogLevel.Standard, nameof(WPFCoreBA));
        }

        protected override void OnShutdown(ShutdownEventArgs args)
        {
            base.OnShutdown(args);

            var message = "Shutdown," + args.Action.ToString() + "," + args.HResult.ToString();
            this.engine.Log(LogLevel.Standard, message);
        }
    }
}
